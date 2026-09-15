using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class Elevator : MonoBehaviour
{
    [SerializeField] private ElevatorPath elevatorPath;

    [SerializeField] private PlayerLoopTiming updateTiming = PlayerLoopTiming.LastUpdate;

    public UnityEvent<int> onDeparture;
    public UnityEvent<int> onArrived;

    private int _currentIndex;
    private CancellationTokenSource _cts;

    public int CurrentIndex => _currentIndex;
    public int FloorCount => elevatorPath.stopPoints.Count;

    private void OnEnable()
    {
        if (elevatorPath != null)
            elevatorPath.SyncSegments();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }

    public void MoveToNext() => MoveToIndex(_currentIndex + 1);
    public void MoveToPrevious() => MoveToIndex(_currentIndex - 1);
    public void MoveToFirst() => MoveToIndex(0);
    public void MoveToLast() => MoveToIndex(elevatorPath.stopPoints.Count - 1);

    public void MoveToIndex(int targetIndex)
    {
        _ = MoveAsync(targetIndex);
    }

    private async UniTask MoveAsync(int targetIndex)
    {
        if (elevatorPath == null || elevatorPath.stopPoints.Count == 0)
            return;

        targetIndex = Mathf.Clamp(targetIndex, 0, elevatorPath.stopPoints.Count - 1);
        if (targetIndex == _currentIndex)
            return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        onDeparture?.Invoke(_currentIndex);

        var dir = targetIndex > _currentIndex ? 1 : -1;

        try
        {
            for (var i = _currentIndex; i != targetIndex; i += dir)
            {
                var segIndex = dir > 0 ? i : i - 1;
                var seg = elevatorPath.segments[segIndex];

                var startPos = transform.position;
                var endPos = elevatorPath.GetWorldPoint(i + dir);
                var distance = Mathf.Abs(endPos.y - startPos.y);
                var duration = distance / Mathf.Max(0.01f, seg.Speed);

                var elapsed = 0f;

                while (elapsed < duration)
                {
                    await UniTask.Yield(updateTiming, token);

                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    var easedT = seg.SpeedCurve.Evaluate(t);

                    var y = Mathf.Lerp(startPos.y, endPos.y, easedT);
                    transform.position = new Vector3(startPos.x, y, startPos.z);
                }
                transform.position = endPos;
                _currentIndex = i + dir;
            }

            onArrived?.Invoke(_currentIndex);
        }
        catch (OperationCanceledException) { }
    }
}