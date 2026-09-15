using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

public class LadderInteractor : MonoBehaviour
{
   [SerializeField] private InteractionActivator trigger;
   [SerializeField] private LadderStopper stopper;
   [SerializeField] private float walkSpeed;
   [SerializeField] private float runSpeed;
   [SerializeField, Range(-1, 1)] private int direction;
   [SerializeField] private float rotationSpeed = 5f;

   [Header("Enter Settings")]
   [SerializeField] private Vector3 positionOffset;
   [SerializeField] private float enterDuration = 0.2f;
   [SerializeField] private Ease enterEase = Ease.OutQuad;
   [SerializeField] private float stopperDisableBuffer = 0.1f;

   [Header("Exit Settings")]
   [SerializeField] private Transform exitPosition;
   [SerializeField] private float exitDuration = 0.2f;
   [SerializeField] private Ease exitEase = Ease.OutQuad;

   public UnityEvent<Character> onInteractEnter, onInteractExit;
   public LadderInteractor OtherEnd { get; private set; }
   public Character CurrentController { get; private set; }

   [Inject] private readonly PlayerInputHandler _input;

   private Collider _currentCollider;
   private bool _isPlaying;
   private int _currentDirection;
   private CancellationTokenSource _rotationCts;
   private Sequence _enterTween;
   private Sequence _exitTween;
   private Tween _stopperReenableTween;
   private bool _isExiting;

   private void OnEnable()
   {
      if (trigger)
      {
         trigger.onEnter.AddListener(OnEnter);
         trigger.onExit.AddListener(OnExit);
      }

      _input.OnInteract += OnInteractEnter;
   }

   private void OnDisable()
   {
      if (trigger)
      {
         trigger.onEnter.RemoveListener(OnEnter);
         trigger.onExit.RemoveListener(OnExit);
      }

      if (_input != null)
      {
         _input.OnInteract -= OnInteractEnter;
      }

      StopAllTweens();
      StopRotationLoop();
   }

   public void SetOtherEnd(LadderInteractor other)
   {
      OtherEnd = other;
   }

   private void OnEnter(Collider other)
   {
      if (!trigger)
         return;

      _currentCollider = other;
   }

   private void OnExit(Collider other)
   {
      if (!trigger)
         return;

      if (_currentCollider == other)
         _currentCollider = null;
   }

   private void OnInteractEnter()
   {
      if (_isPlaying || !_currentCollider)
         return;

      if (!_currentCollider.gameObject.TryGetComponent<Character>(out var targetController))
         return;

      CurrentController = targetController;

      if (CurrentController.IsOnLadder)
         return;

      onInteractEnter?.Invoke(CurrentController);

      _currentDirection = direction;
      CurrentController.SetOnLadder(true, _currentDirection);
      _isPlaying = true;

      var targetPosition = transform.TransformPoint(positionOffset);
      targetPosition.y = CurrentController.transform.position.y;

      PlayEnterTween(targetPosition);
   }

   private void PlayEnterTween(Vector3 targetPosition)
   {
      StopAllTweens();

      if (!CurrentController)
         return;

      var characterTransform = CurrentController.transform;
      var targetRotation = transform.rotation;

      SetStopperActive(false);

      _enterTween = DOTween.Sequence()
         .Join(characterTransform.DOMove(targetPosition, enterDuration).SetEase(enterEase))
         .Join(characterTransform.DORotateQuaternion(targetRotation, enterDuration).SetEase(enterEase))
         .SetTarget(characterTransform)
         .SetLink(gameObject)
         .OnComplete(StartRotationLoop);

      _stopperReenableTween = DOVirtual.DelayedCall(enterDuration + stopperDisableBuffer, () => SetStopperActive(true))
         .SetTarget(this)
         .SetLink(gameObject);
   }

   private void SetStopperActive(bool value)
   {
      if (stopper)
         stopper.gameObject.SetActive(value);
   }

   public void OnInteractExit()
   {
      var owner = this;
      var controller = CurrentController;

      if (!controller && OtherEnd)
      {
         controller = OtherEnd.CurrentController;
         owner = OtherEnd;
      }

      if (!controller || !controller.IsOnLadder || owner._isExiting)
         return;

      owner._isExiting = true;
      controller.SetOnLadder(false);
      onInteractExit?.Invoke(controller);

      owner.StopRotationLoop();
      owner.StopAllTweens();

      if (exitPosition)
         PlayExitTween(controller, () => FinishExit(owner, controller));
      else
         FinishExit(owner, controller);
   }

   private void PlayExitTween(Character controller, TweenCallback onComplete)
   {
      var characterTransform = controller.transform;

      _exitTween = DOTween.Sequence()
         .Join(characterTransform.DOMove(exitPosition.position, exitDuration).SetEase(exitEase))
         .Join(characterTransform.DORotateQuaternion(exitPosition.rotation, exitDuration).SetEase(exitEase))
         .SetTarget(characterTransform)
         .SetLink(controller.gameObject)
         .OnComplete(onComplete);
   }

   private static void FinishExit(LadderInteractor owner, Character controller)
   {
      owner.CurrentController = null;
      owner._isPlaying = false;
      owner._isExiting = false;
   }

   private void StartRotationLoop()
   {
      StopRotationLoop();
      _rotationCts = new CancellationTokenSource();
      RotationLoopAsync(_rotationCts.Token).Forget();
   }

   private void StopRotationLoop()
   {
      _rotationCts?.Cancel();
      _rotationCts?.Dispose();
      _rotationCts = null;
   }

   private void StopAllTweens()
   {
      _enterTween?.Kill();
      _enterTween = null;

      _exitTween?.Kill();
      _exitTween = null;

      if (_stopperReenableTween != null)
      {
         _stopperReenableTween.Kill();
         _stopperReenableTween = null;
         SetStopperActive(true);
      }
   }

   private async UniTask RotationLoopAsync(CancellationToken token)
   {
      while (!token.IsCancellationRequested)
      {
         if (_isPlaying && CurrentController)
         {
            CurrentController.transform.rotation = Quaternion.Slerp(
               CurrentController.transform.rotation,
               transform.rotation,
               rotationSpeed * Time.deltaTime);

            var motionY = CurrentController.GetMotionY();
            if (Mathf.Abs(motionY) > 0.01f)
            {
               _currentDirection = motionY > 0f ? 1 : -1;
               CurrentController.SetOnLadder(true, _currentDirection);

               var motionSpeed = Mathf.Clamp01(CurrentController.GetMotionSpeed());
               var verticalSpeed = Mathf.Lerp(walkSpeed, runSpeed, motionSpeed);
               var position = CurrentController.transform.position;
               position.y += motionY * verticalSpeed * Time.deltaTime;
               CurrentController.transform.position = position;
            }
         }

         await UniTask.Yield(PlayerLoopTiming.Update, token);
      }
   }
}