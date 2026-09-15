using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AnimatorCache : IDisposable
{
    private readonly ICharacterInput _characterInput;
    private readonly Animator _animator;
    private readonly AnimationStates _animationStates;
    private CancellationTokenSource _motionCts;
    private UniTask _motionTask;
    private Vector2 _targetMotion;
    private Vector2 _currentMotion;
    private float _targetSpeed;
    private float _currentSpeed;

    public AnimatorCache(ICharacterInput characterInput, Animator animator,
        AnimationStates animationStates)
    {
        _characterInput = characterInput;
        _animator = animator;
        _animationStates = animationStates;

        _currentMotion = new Vector2(
            _animator.GetFloat(_animationStates.MotionXHash),
            _animator.GetFloat(_animationStates.MotionYHash));
        _targetMotion = _currentMotion;
        _currentSpeed = _animator.GetFloat(_animationStates.MotionSpeedHash);
        _targetSpeed = _currentSpeed;

        _characterInput.OnMove += OnMove;
        _characterInput.OnRun += OnRun;
    }

    public void Dispose()
    {
        _characterInput.OnMove -= OnMove;
        _characterInput.OnRun -= OnRun;
        StopMotionSmoothing();
    }

    public float GetMotionSpeed()
    {
        return _animator.GetFloat(_animationStates.MotionSpeedHash);
    }

    public float GetMotionY()
    {
        return _animator.GetFloat(_animationStates.MotionYHash);
    }
    
    public void SetInteract(bool value)
    { 
        _animator.SetBool(_animationStates.InteractHash, value);
    }

    public void SetOnLadder(bool value, float direction)
    {
        _animator.SetBool(_animationStates.Ladder, value);
        _animator.SetFloat(_animationStates.LadderDirectionHash, direction);
    }
    
    public void OnTurn(float turn)
    {
        _animator.SetFloat(_animationStates.TurnHash, turn);
    }

    private void OnMove(Vector2 input)
    {
        _targetMotion = input;
        StartMotionSmoothing();
    }

    private void OnRun(bool run)
    {
        _targetSpeed = run ? 1f : 0f;
        StartMotionSmoothing();
    }
    
    private void StartMotionSmoothing()
    {
        StopMotionSmoothing();
        _motionCts = new CancellationTokenSource();
        _motionTask = SmoothMotionAsync(_motionCts.Token);
    }

    private void StopMotionSmoothing()
    {
        _motionCts?.Cancel();
        if (_motionTask.Status == UniTaskStatus.Pending)
        {
            _motionTask.Forget();
        }
        _motionCts?.Dispose();
        _motionCts = null;
    }

    private async UniTask SmoothMotionAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            _currentMotion = Vector2.Lerp(_currentMotion, _targetMotion, 1f - Mathf.Exp(-12f * Time.deltaTime));
            _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, 1f - Mathf.Exp(-8f * Time.deltaTime));

            _animator.SetFloat(_animationStates.MotionXHash, _currentMotion.x);
            _animator.SetFloat(_animationStates.MotionYHash, _currentMotion.y);
            _animator.SetFloat(_animationStates.MotionSpeedHash, _currentSpeed);

            if (Vector2.Distance(_currentMotion, _targetMotion) < 0.001f
                && Mathf.Abs(_currentSpeed - _targetSpeed) < 0.001f)
            {
                _currentMotion = _targetMotion;
                _currentSpeed = _targetSpeed;
                _animator.SetFloat(_animationStates.MotionXHash, _currentMotion.x);
                _animator.SetFloat(_animationStates.MotionYHash, _currentMotion.y);
                _animator.SetFloat(_animationStates.MotionSpeedHash, _currentSpeed);
                break;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
}