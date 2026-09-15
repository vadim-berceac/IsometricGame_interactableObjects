using System;
using TMPro;
using UnityEngine;
using Zenject;
using System.Threading;
using Cysharp.Threading.Tasks;

public class DialogueCanvasController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    [Inject] private readonly AnimationStates _animationStates;

    private CancellationTokenSource _deactivationCts;
    private CancellationTokenSource _temporaryDisableCts;
    private bool _isTemporarilyDisabled;

    public void ActivateCanvasWithText(string text)
    {
        if (_isTemporarilyDisabled) return;

        CancelDeactivation();

        gameObject.SetActive(true);
        animator.SetBool(_animationStates.HashActivePara, true);
        textMeshProUGUI.text = text;
    }

    public void DeactivateCanvasWithDelay(float delay)
    {
        CancelDeactivation();

        _deactivationCts = new CancellationTokenSource();
        DeactivateWithDelayAsync(delay, _deactivationCts.Token).Forget();
    }

    public void TemporaryDisable(float time)
    {
        CancelTemporaryDisable();

        _isTemporarilyDisabled = true;
        _temporaryDisableCts = new CancellationTokenSource();
        TemporaryDisableAsync(time, _temporaryDisableCts.Token).Forget();
    }

    private async UniTask TemporaryDisableAsync(float time, CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(time),
                ignoreTimeScale: false,
                cancellationToken: cancellationToken
            );

            if (this == null) return;

            _isTemporarilyDisabled = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async UniTask DeactivateWithDelayAsync(float delay, CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(delay),
                ignoreTimeScale: false,
                cancellationToken: cancellationToken
            );

            if (this == null || !gameObject) return;

            animator.SetBool(_animationStates.HashActivePara, false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelDeactivation()
    {
        if (_deactivationCts != null)
        {
            _deactivationCts.Cancel();
            _deactivationCts.Dispose();
            _deactivationCts = null;
        }
    }

    private void CancelTemporaryDisable()
    {
        if (_temporaryDisableCts != null)
        {
            _temporaryDisableCts.Cancel();
            _temporaryDisableCts.Dispose();
            _temporaryDisableCts = null;
        }
    }

    private void OnDestroy()
    {
        CancelDeactivation();
        CancelTemporaryDisable();
    }
}