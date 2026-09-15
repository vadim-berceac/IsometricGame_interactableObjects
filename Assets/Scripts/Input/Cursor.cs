using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class Cursor : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float maxRayDistance = 100f;

    [Inject] private readonly CameraSystem _camera;

    public Action<Vector3> OnCursorMoved;

    private CancellationTokenSource _cts;

    private void OnEnable()
    {
        UnityEngine.Cursor.visible = false;
        _cts = new CancellationTokenSource();
        FollowAsync(_cts.Token).Forget();
    }

    private void OnDisable()
    {
        UnityEngine.Cursor.visible = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTask FollowAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _camera.Camera.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out var hit, maxRayDistance, groundMask))
            {
                transform.position = hit.point;
                OnCursorMoved?.Invoke(hit.point);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }
    }
}