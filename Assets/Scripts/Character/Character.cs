using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class Character : BaseCharacter
{
    [Header("Player only Settings")]
    [SerializeField] private float rotationToCursorSpeed = 5f;
    [SerializeField] private float minRotationDistance = 1f;
    [SerializeField] private float closeRotationDistance = 2f;
    [SerializeField] private float maxCloseRotationAngle = 45f;
    [SerializeField] private int playerLayer = 14;

    [Inject] private readonly ICharacterInput _currentInput;
    [Inject] private readonly Cursor _cursor;
    [Inject] private readonly AnimationStates _animationStates;
    [Inject] private readonly AnimatorCache _animCache;
    [Inject] private readonly CameraSystem _cameraSystem;
    [Inject] private readonly CharacterTalkInteractor _characterTalkInteractor;
    [Inject] private readonly PlayableGraphHandle _graphHandle;
    [Inject] private readonly PropBones _propBones;
    [Inject] private readonly CharacterPhysics  _characterPhysics;
    [Inject] private readonly CombatPositioning _combatPositioning;

    private Quaternion _targetRotation;
    private CancellationTokenSource _rotationCts;
    private float _lastRotationDirection;
    
    public event Action<float> OnRotationDirection;
    public PropBones PropBones => _propBones;
    public bool IsInteracting { get; private set; }
    public bool IsOnLadder { get; private set; }
    public bool IsGrounded => _characterPhysics.IsGrounded;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        _characterPhysics.OnGroundedChanged +=  OnGroundedChanged;
        
        if (CharacterType == CharacterType.Player)
        {
            gameObject.AddComponent<AudioListener>();
            _cameraSystem.SetTarget(Transform);
            _targetRotation = Transform.rotation;
            _characterTalkInteractor.gameObject.SetActive(false);
            _cursor.OnCursorMoved += RotatePlayerToCursor;
            StartRotationLoop();

            gameObject.layer = playerLayer;
        
            OnRotationDirection += OnTurn;
            return;
        }

        if (CharacterType == CharacterType.Enemy)
        {
            _characterTalkInteractor.gameObject.SetActive(false);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        _characterPhysics.OnGroundedChanged -= OnGroundedChanged;
        
        if (CharacterType == CharacterType.Player)
        {
            _cameraSystem.SetTarget(null);
            _cursor.OnCursorMoved -= RotatePlayerToCursor;
            StopRotationLoop();
        
            OnRotationDirection -= OnTurn;
        }
    }

    private void FixedUpdate()
    {
        if (CharacterType == CharacterType.Enemy || (CharacterType == CharacterType.Ally))
        {
            _combatPositioning.Tick(Time.fixedDeltaTime);
        }
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

    private void OnGroundedChanged(bool value)
    {
        _animCache.SetOnAir(!value);
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
            if (IsGrounded && !IsInteracting && !IsOnLadder) 
            {
                var previousRotation = Transform.rotation;
                Transform.rotation = Quaternion.Slerp(Transform.rotation, _targetRotation, rotationToCursorSpeed * Time.deltaTime);

                var direction = 0f;
                if (Quaternion.Angle(previousRotation, Transform.rotation) > 0.01f)
                {
                    var angle = Vector3.SignedAngle(previousRotation * Vector3.forward,Transform.rotation * Vector3.forward, Vector3.up);
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
        if (!IsGrounded || IsInteracting || IsOnLadder)
        {
            return;
        }

        var direction = cursorPosition - Transform.position;
        direction.y = 0f;

        var sqrMagnitude = direction.sqrMagnitude;
        if (sqrMagnitude < minRotationDistance * minRotationDistance)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (sqrMagnitude < closeRotationDistance * closeRotationDistance)
        {
            var angle = Quaternion.Angle(Transform.rotation, targetRotation);
            if (angle > maxCloseRotationAngle)
            {
                return;
            }
        }

        _targetRotation = targetRotation;
    }
}