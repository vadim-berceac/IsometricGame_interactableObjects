using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class Character : MonoBehaviour
{
    [field: SerializeField] public CharacterType CharacterType { get; private set; } = CharacterType.AI;
    
    [Header("Player only Settings")]
    [SerializeField] private float rotationToCursorSpeed = 5f;
    [SerializeField] private float minRotationDistance = 1f;
    [SerializeField] private float closeRotationDistance = 2f;
    [SerializeField] private float maxCloseRotationAngle = 45f;
    [SerializeField] private int playerLayer = 14;

    [Inject] private readonly ICharacterInput _currentInput;
    [Inject] private readonly Cursor _cursor;
    [Inject] private readonly Transform _transform;
    [Inject] private readonly AnimationStates _animationStates;
    [Inject] private readonly AnimatorCache _animCache;
    [Inject] private readonly CameraSystem _cameraSystem;
    [Inject] private readonly InteractableObject _interactableObject;
    [Inject] private readonly PlayableGraphHandle _graphHandle;
    [Inject] private readonly PropBones _propBones;

    private Quaternion _targetRotation;
    private CancellationTokenSource _rotationCts;
    private float _lastRotationDirection;
    
    public event Action<float> OnRotationDirection;
    public PropBones PropBones => _propBones;
    public bool IsInteracting { get; private set; }
    public bool IsOnLadder { get; private set; }

    private void OnEnable()
    {
        if (CharacterType != CharacterType.Player)
        {
            return;
        }

        gameObject.AddComponent<AudioListener>();
        _cameraSystem.SetTarget(_transform);
        _targetRotation = _transform.rotation;
        _interactableObject.gameObject.SetActive(false);
        _cursor.OnCursorMoved += RotatePlayerToCursor;
        StartRotationLoop();

        gameObject.layer = playerLayer;
        
        OnRotationDirection += OnTurn;
    }

    private void OnDisable()
    {
        if (CharacterType != CharacterType.Player)
        {
            return;
        }
        _cameraSystem.SetTarget(null);
        _cursor.OnCursorMoved -= RotatePlayerToCursor;
        StopRotationLoop();
        
        OnRotationDirection -= OnTurn;
    }
    
    private void LateUpdate()
    {
        if (_graphHandle.IsValid && (IsInteracting || _graphHandle.IsBlending))
        {
            _graphHandle.Evaluate(Time.deltaTime);
        }
    }
    
    public void SetInteracting(bool value)
    {
        IsInteracting = value;
        _animCache?.SetInteract(value);
    }

    public void SetOnLadder(bool value, float direction = 0)
    {
        IsOnLadder = value;
        _animCache?.SetOnLadder(value, direction);
    }

    public float GetMotionSpeed()
    {
        return _animCache.GetMotionSpeed();
    }

    public float GetMotionY()
    {
        return _animCache.GetMotionY();
    }
    
    public void PlayInteractClip(AnimationClip clip, float blendLength, AvatarMask mask = null, bool isAdditive = false)
    {
       if (!_graphHandle.IsValid || clip == null) return;
       _graphHandle.PlayClip(clip, blendLength, mask, isAdditive);
    }
       
    public void StopInteractClip(float blendLength = 0f)
    {
        if (!_graphHandle.IsValid) return;
    
        _graphHandle.Stop(blendLength);
    }

    private void OnTurn(float turn)
    {
        _animCache.OnTurn(turn);
    }

    private void StartRotationLoop()
    {
        _rotationCts = new CancellationTokenSource();
        RotationLoopAsync(_rotationCts.Token).Forget();
    }

    private void StopRotationLoop()
    {
        _rotationCts?.Cancel();
        _rotationCts?.Dispose();
        _rotationCts = null;
    }

    private async UniTaskVoid RotationLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!IsInteracting && !IsOnLadder) 
            {
                var previousRotation = _transform.rotation;
                _transform.rotation = Quaternion.Slerp(_transform.rotation, _targetRotation, rotationToCursorSpeed * Time.deltaTime);

                var direction = 0f;
                if (Quaternion.Angle(previousRotation, _transform.rotation) > 0.01f)
                {
                    var angle = Vector3.SignedAngle(previousRotation * Vector3.forward, _transform.rotation * Vector3.forward, Vector3.up);
                    direction = angle > 0f ? -1f : 1f;
                }

                if (Mathf.Abs(direction - _lastRotationDirection) > 0.01f)
                {
                    _lastRotationDirection = direction;
                    OnRotationDirection?.Invoke(direction);
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }

    private void RotatePlayerToCursor(Vector3 cursorPosition)
    {
        if (IsInteracting || IsOnLadder)
        {
            return;
        }

        var direction = cursorPosition - _transform.position;
        direction.y = 0f;

        var sqrMagnitude = direction.sqrMagnitude;
        if (sqrMagnitude < minRotationDistance * minRotationDistance)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (sqrMagnitude < closeRotationDistance * closeRotationDistance)
        {
            var angle = Quaternion.Angle(_transform.rotation, targetRotation);
            if (angle > maxCloseRotationAngle)
            {
                return;
            }
        }

        _targetRotation = targetRotation;
    }
}