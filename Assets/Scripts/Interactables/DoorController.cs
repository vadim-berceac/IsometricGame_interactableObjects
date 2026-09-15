using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class DoorController : MonoBehaviour
{
    private enum State
    {
        Open = 0,
        Locked = 1,
        Blocked = 2
    }

    private enum MoveMode
    {
        Rotate = 0,
        Slide = 1
    }

    [System.Serializable]
    private class Leaf
    {
        [field: SerializeField] public Transform LeafTransform { get; private set; }

        [Header("Rotate mode")]
        [field: SerializeField, Range(-180, 180)] public float OpenAngle { get; private set; }

        [Header("Slide mode")]
        [field: SerializeField] public Vector3 SlideOffset { get; private set; } 

        [HideInInspector] public bool isOpen;
        [HideInInspector] public Vector3 closedLocalPosition;
        [HideInInspector] public float closedLocalYAngle;
    }

    [SerializeField] private State state = State.Open;

    [Header("Movement")]
    [SerializeField] private MoveMode mode = MoveMode.Rotate;
    [SerializeField] private float openSpeed; 
    [SerializeField] private bool closeAutomatic;
    [SerializeField] private float closeDelay;

    [SerializeField] private Leaf[] leafs;
    [SerializeField] private GameObject[] interactors;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpenStart;
    [SerializeField] private UnityEvent onOpenEnd;
    [SerializeField] private UnityEvent onCloseStart;
    [SerializeField] private UnityEvent onCloseEnd;

    [Header("States")]
    [SerializeField] private SpriteRenderer openStateRenderer;
    [SerializeField] private SpriteRenderer lockedStateRenderer;
    [SerializeField] private SpriteRenderer blockedStateRenderer;

    private CancellationTokenSource _cts;
    private bool _isDoorOpen;

    private void Awake()
    {
        CacheClosedTransforms();
        SwitchState((int)state);
    }

    private void CacheClosedTransforms()
    {
        if (leafs == null) return;

        foreach (var leaf in leafs)
        {
            if (!leaf.LeafTransform) continue;

            leaf.closedLocalPosition = leaf.LeafTransform.localPosition;
            leaf.closedLocalYAngle = leaf.LeafTransform.localEulerAngles.y;
        }
    }

    public void OpenLeafs()
    {
        OpenLeafsAsync().Forget();
    }

    public void CloseLeafs()
    {
        CloseLeafsAsync().Forget();
    }

    public void SwitchState(int stateIndex)
    {
        state = (State) stateIndex;
        UpdateSprite();
        UpdateInteractors();
    }

    private void UpdateSprite()
    {
        if (openStateRenderer)
            openStateRenderer.gameObject.SetActive(state == State.Open);

        if (lockedStateRenderer)
            lockedStateRenderer.gameObject.SetActive(state == State.Locked);

        if (blockedStateRenderer)
            blockedStateRenderer.gameObject.SetActive(state == State.Blocked);
    }

    private void UpdateInteractors()
    {
        if (interactors == null || interactors.Length == 0)
        {
            return;
        }

        if (state == State.Open)
        {
            foreach (var interactor in interactors)
            {
                interactor.gameObject.SetActive(true);
            }
            return;
        }

        foreach (var interactor in interactors)
        {
            interactor.gameObject.SetActive(false);
        }
    }

    private async UniTask OpenLeafsAsync()
    {
        if (state != State.Open || _isDoorOpen || leafs == null || leafs.Length == 0)
        {
            return;
        }

        Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        var token = _cts.Token;

        onOpenStart?.Invoke();

        var tasks = new UniTask[leafs.Length];
        for (var i = 0; i < leafs.Length; i++)
        {
            var leaf = leafs[i];
            tasks[i] = leaf.isOpen ? UniTask.CompletedTask : MoveLeafAsync(leaf, opening: true, token);
        }

        await UniTask.WhenAll(tasks);

        foreach (var leaf in leafs)
        {
            leaf.isOpen = true;
        }
        _isDoorOpen = true;

        onOpenEnd?.Invoke();

        if (closeAutomatic && !token.IsCancellationRequested)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(closeDelay), cancellationToken: token);
            await CloseLeafsAsync();
        }
    }

    private async UniTask CloseLeafsAsync()
    {
        if (state != State.Open || !_isDoorOpen || leafs == null || leafs.Length == 0)
        {
            return;
        }

        Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        var token = _cts.Token;

        onCloseStart?.Invoke();

        var tasks = new UniTask[leafs.Length];
        for (var i = 0; i < leafs.Length; i++)
        {
            var leaf = leafs[i];
            tasks[i] = !leaf.isOpen ? UniTask.CompletedTask : MoveLeafAsync(leaf, opening: false, token);
        }

        await UniTask.WhenAll(tasks);

        foreach (var leaf in leafs)
        {
            leaf.isOpen = false;
        }
        _isDoorOpen = false;

        onCloseEnd?.Invoke();
    }

    private UniTask MoveLeafAsync(Leaf leaf, bool opening, CancellationToken token)
    {
        if (!leaf.LeafTransform)
        {
            return UniTask.CompletedTask;
        }

        return mode switch
        {
            MoveMode.Slide => SlideLeafAsync(leaf, opening, token),
            _ => RotateLeafAsync(leaf, opening ? leaf.OpenAngle : leaf.closedLocalYAngle, token)
        };
    }

    private async UniTask RotateLeafAsync(Leaf leaf, float targetAngle, CancellationToken token)
    {
        var transform = leaf.LeafTransform;
        var currentAngle = transform.localEulerAngles.y;
        var delta = Mathf.DeltaAngle(currentAngle, targetAngle);
        var duration = Mathf.Abs(delta) / Mathf.Max(openSpeed, 0.01f);

        var tween = transform.DOLocalRotate(
            new Vector3(transform.localEulerAngles.x, targetAngle, transform.localEulerAngles.z),
            duration
        ).SetEase(Ease.Linear);

        await AwaitTween(tween, token);
    }

    private async UniTask SlideLeafAsync(Leaf leaf, bool opening, CancellationToken token)
    {
        var transform = leaf.LeafTransform;
        var targetPosition = opening
            ? leaf.closedLocalPosition + leaf.SlideOffset
            : leaf.closedLocalPosition;

        var distance = Vector3.Distance(transform.localPosition, targetPosition);
        var duration = distance / Mathf.Max(openSpeed, 0.01f);

        var tween = transform.DOLocalMove(targetPosition, duration)
            .SetEase(Ease.Linear);

        await AwaitTween(tween, token);
    }

    private async UniTask AwaitTween(Tween tween, CancellationToken token)
    {
        var tcs = new UniTaskCompletionSource();

        tween.OnComplete(() => tcs.TrySetResult());
        tween.OnKill(() => tcs.TrySetResult());

        CancellationTokenRegistration registration = default;
        registration = token.Register(() =>
        {
            if (tween.IsActive())
                tween.Kill();

            registration.Dispose();
        });

        await tcs.Task;
    }

    private void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnDestroy()
    {
        Cancel();
    }
}