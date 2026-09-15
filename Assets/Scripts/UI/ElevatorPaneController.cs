using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class ElevatorPaneController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject buttonsContainer;
    [SerializeField] private Button buttonPrefab;

    [Inject] private readonly AnimationStates _animationStates;

    private readonly List<Button> _spawnedButtons = new();

    private CancellationTokenSource _deactivationCts;
    private CancellationTokenSource _temporaryDisableCts;
    private CancellationTokenSource _creationCts;
    private bool _isTemporarilyDisabled;

    public void ActivateCanvas(int floorCount, Action<int> clickAction)
    {
        if (_isTemporarilyDisabled) return;

        CancelDeactivation();
        CancelCreation();

        _creationCts = new CancellationTokenSource();
        CreateButtonsAsync(floorCount, clickAction, _creationCts.Token).Forget();
    }

    public void DeactivateCanvasWithDelay(float delay)
    {
        CancelDeactivation();
        CancelCreation();

        buttonsContainer.SetActive(false);
        ClearButtons();

        _deactivationCts = new CancellationTokenSource();
        DeactivateWithDelayAsync(delay, _deactivationCts.Token).Forget();
    }

    public void TemporaryDisable(float time)
    {
        CancelTemporaryDisable();

        buttonsContainer.SetActive(false);
        _isTemporarilyDisabled = true;
        _temporaryDisableCts = new CancellationTokenSource();
        TemporaryDisableAsync(time, _temporaryDisableCts.Token).Forget();
    }

    private async UniTask CreateButtonsAsync(int floorCount, Action<int> clickAction, CancellationToken cancellationToken)
    {
        try
        {
            ClearButtons();

            for (var i = 0; i < floorCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var floorIndex = i;
                var button = Instantiate(buttonPrefab, buttonsContainer.transform);

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = (floorIndex + 1).ToString();

                if (clickAction != null)
                    button.onClick.AddListener(() => clickAction(floorIndex));

                _spawnedButtons.Add(button);

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }

            if (this == null) return;

            buttonsContainer.SetActive(true);
            animator.SetBool(_animationStates.HashActivePara, true);
        }
        catch (OperationCanceledException)
        {
        }
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

    private void ClearButtons()
    {
        foreach (var button in _spawnedButtons)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                Destroy(button.gameObject);
            }
        }

        _spawnedButtons.Clear();
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

    private void CancelCreation()
    {
        if (_creationCts != null)
        {
            _creationCts.Cancel();
            _creationCts.Dispose();
            _creationCts = null;
        }
    }

    private void OnDestroy()
    {
        CancelDeactivation();
        CancelTemporaryDisable();
        CancelCreation();
    }
}