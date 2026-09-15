using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

public class InteractAnimation : MonoBehaviour
{
   [SerializeField] private InteractionActivator trigger;
   [Tooltip("Выбор персонажа напрямую")]
   [SerializeField] private Character controller;
   [SerializeField] private AnimationClipSettings enterClip;
   [SerializeField] private AnimationClipSettings[] clips;
   [SerializeField] private AnimationClipSettings exitClip;
   [SerializeField] private bool canBeInterrupted;

   [Header("Clips Block Settings")]
   [Tooltip("Проигрывать клипы из clips в случайном порядке")]
   [SerializeField] private bool randomizeClipsOrder;
   [Tooltip("Зациклить блок clips целиком (даже если сами клипы не looped) — крутится до Interrupt")]
   [SerializeField] private bool loopClipsBlock;

   public UnityEvent<Character> onInteractEnter, onInteractExit;
   public UnityEvent<AnimationClip, float> onClipStarted;

   [Inject] private readonly PlayerInputHandler _input;
   private Collider _currentCollider;
   private Character _currentController;
   private bool _isPlaying;
   private bool _interruptRequested;
   
   public Character CurrentController => _currentController;

   private void OnEnable()
   {
      if (trigger)
      {
         trigger.onEnter.AddListener(OnEnter);
         trigger.onExit.AddListener(OnExit);
      }

      if (controller)
      {
         _currentController = controller;
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
         _input.OnInteract -= Interrupt;
      }

      if (_isPlaying && _currentController)
      {
         _currentController.SetInteracting(false);
         var exitBlend = exitClip.Clip ? exitClip.EnterBlendLength : 0.2f;
         _currentController.StopInteractClip(exitBlend);
      }

      _currentCollider = null;
      _currentController = null;
      _isPlaying = false;
      _interruptRequested = false;
   }

   private void OnEnter(Collider other)
   {
      if (!trigger)
      {
         return;
      }
      _currentCollider = other;
   }

   private void OnExit(Collider other)
   {
      if (!trigger)
      {
         return;
      }
      
      if (_currentCollider == other)
      {
         _currentCollider = null;
      }
   }

   private void OnInteractEnter()
   {
      if (_isPlaying)
      {
         return;
      }

      Character targetController = null;

      if (controller)
      {
         if (_currentCollider &&
             _currentCollider.gameObject.TryGetComponent(out Character other) &&
             other != controller)
         {
            targetController = controller;
         }
      }
      else if (_currentCollider)
      {
         if (!_currentCollider.gameObject.TryGetComponent(out targetController))
            return;
      }

      if (!targetController)
      {
         return;
      }

      _currentController = targetController;

      if (_currentController.IsInteracting)
      {
         return;
      }

      onInteractEnter?.Invoke(_currentController);

      if ((clips == null || clips.Length == 0) && !enterClip.Clip && !exitClip.Clip)
         return;

      _currentController.SetInteracting(true);
      _isPlaying = true;
      _interruptRequested = false;

      if (canBeInterrupted)
         _input.OnInteract += Interrupt;

      PlaySequence().Forget();
   }

   private void OnInteractExit()
   {
      if (!_currentController)
      {
         return;
      }
      
      if (!_currentController.IsInteracting)
      {
         return;
      }
      
      onInteractExit?.Invoke(_currentController);

      _currentController.SetInteracting(false);
      var exitBlend = exitClip.Clip ? exitClip.EnterBlendLength : 0.2f;
      _currentController.StopInteractClip(exitBlend);
      _currentController = null;

      if (canBeInterrupted)
      {
         _input.OnInteract -= Interrupt;
      }
   }

   private void Interrupt()
   {
      if (!_isPlaying)
      {
         return;
      }

      _interruptRequested = true;
   }

   private async UniTask PlaySequence()
   {
      var clipsBlock = BuildClipsBlock();
      var hasExitClip = exitClip.Clip;

      if (enterClip.Clip)
      {
         var enterNext = clipsBlock.Count > 0
            ? clipsBlock[0]
            : (hasExitClip ? exitClip : (AnimationClipSettings?)null);

         var enterOverlap = GetBlendOverlap(enterClip, enterNext);
         await PlayBlendedClip(enterClip, enterOverlap);
      }

      if (clipsBlock.Count > 0 && !_interruptRequested)
      {
         await PlayClipsBlock(clipsBlock, hasExitClip);
      }

      if (hasExitClip)
      {
         await PlayBlendedClip(exitClip, 0f);
      }

      _isPlaying = false;
      _interruptRequested = false;
      OnInteractExit();
   }

   private List<AnimationClipSettings> BuildClipsBlock()
   {
      var block = new List<AnimationClipSettings>();

      if (clips != null)
      {
         foreach (var clip in clips)
         {
            if (clip.Clip)
            {
               block.Add(clip);
            }
         }
      }

      return block;
   }

   private async UniTask PlayClipsBlock(List<AnimationClipSettings> clipsBlock, bool hasExitClip)
   {
      do
      {
         var order = randomizeClipsOrder ? ShuffleCopy(clipsBlock) : clipsBlock;

         for (var i = 0; i < order.Count; i++)
         {
            var current = order[i];
            AnimationClipSettings? next;

            if (i + 1 < order.Count)
            {
               next = order[i + 1];
            }
            else if (loopClipsBlock)
            {
               next = order.Count > 0 ? order[0] : null;
            }
            else
            {
               next = hasExitClip ? exitClip : null;
            }

            var exitOverlap = GetBlendOverlap(current, next);

            if (current.Clip.isLooping)
            {
               await PlayLoopedClip(current, exitOverlap);
            }
            else
            {
               await PlayBlendedClip(current, exitOverlap);
            }

            if (_interruptRequested)
            {
               return;
            }
         }
      }
      while (loopClipsBlock && !_interruptRequested);
   }

   private static List<AnimationClipSettings> ShuffleCopy(List<AnimationClipSettings> source)
   {
      var copy = new List<AnimationClipSettings>(source);

      for (var i = copy.Count - 1; i > 0; i--)
      {
         var j = Random.Range(0, i + 1);
         (copy[i], copy[j]) = (copy[j], copy[i]);
      }

      return copy;
   }

   private static float GetBlendOverlap(AnimationClipSettings current, AnimationClipSettings? next)
   {
      if (!next.HasValue)
      {
         return 0f;
      }

      var overlap = Mathf.Min(current.ExitBlendLength, next.Value.EnterBlendLength);

      return Mathf.Clamp(overlap, 0f, current.Clip.length);
   }

   private async UniTask PlayBlendedClip(AnimationClipSettings settings, float exitOverlap)
   {
      onClipStarted?.Invoke(settings.Clip, settings.EnterBlendLength);

      _currentController?.PlayInteractClip(settings.Clip, settings.EnterBlendLength, settings.Mask, settings.IsAdditive);

      var waitTime = settings.Clip.length - exitOverlap;

      if (waitTime > 0f)
      {
         await UniTask.WaitForSeconds(waitTime);
      }
   }

   private async UniTask PlayLoopedClip(AnimationClipSettings settings, float exitOverlap)
   {
      onClipStarted?.Invoke(settings.Clip, settings.EnterBlendLength);

      _currentController?.PlayInteractClip(settings.Clip, settings.EnterBlendLength, settings.Mask, settings.IsAdditive);

      var mainWait = Mathf.Max(settings.Clip.length - exitOverlap, 0f);

      do
      {
         if (mainWait > 0f)
         {
            await UniTask.WaitForSeconds(mainWait);
         }

         if (_interruptRequested)
         {
            break;
         }

         if (exitOverlap > 0f)
         {
            await UniTask.WaitForSeconds(exitOverlap);
         }
      }
      while (!_interruptRequested);
   }
}

[System.Serializable]
public struct AnimationClipSettings
{
   [field: SerializeField] public AnimationClip Clip { get; private set; }
   [field: SerializeField, Range(0, 1)] public float EnterBlendLength { get; private set; }
   [field: SerializeField, Range(0, 1)] public float ExitBlendLength { get; private set; }
   [field: SerializeField] public AvatarMask Mask { get; private set; }
   [field: SerializeField] public bool IsAdditive { get; private set; }
}