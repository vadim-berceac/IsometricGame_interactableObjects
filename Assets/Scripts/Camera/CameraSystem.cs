using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class CameraSystem : MonoBehaviour
{
   [SerializeField] private Camera cam;
   [SerializeField] private AudioSource audioSource;
   [SerializeField] private float followSpeed;
   [SerializeField] private float damping;
   [SerializeField] private float zoomSpeed;
   [SerializeField] private float zoomMin;
   [SerializeField] private float zoomMax;
   [SerializeField] private Vector3 offset;
   [SerializeField] private PlayerLoopTiming updateTiming  = PlayerLoopTiming.Update;

   public AudioSource AudioSource => audioSource;
   
   private Transform _currentTarget;
   private Transform _transform;
   private Vector3 _velocity;
   private float _currentZoom;
   private CancellationTokenSource _cts;

   [Inject] private readonly PlayerInputHandler _playerInputHandler;

   public Camera Camera => cam;

   private void Awake()
   {
      _transform = transform;
      _currentZoom = cam.orthographic ? cam.orthographicSize : cam.fieldOfView;
   }

   private void OnEnable()
   {
      _playerInputHandler.OnScrollWheel += Zoom;
      _cts = new CancellationTokenSource();
      FollowTargetAsync(_cts.Token).Forget();
   }

   private void OnDisable()
   {
      _playerInputHandler.OnScrollWheel -= Zoom;
      _cts?.Cancel();
      _cts?.Dispose();
      _cts = null;
   }

   public void SetTarget(Transform tr)
   {
      _currentTarget = tr;
   }

   private void Zoom(float scrollDelta)
   {
      _currentZoom -= scrollDelta * zoomSpeed;
      _currentZoom = Mathf.Clamp(_currentZoom, zoomMin, zoomMax);

      if (cam.orthographic)
      {
         cam.orthographicSize = _currentZoom;
      }
      else
      {
         cam.fieldOfView = _currentZoom;
      }
   }

   private async UniTask FollowTargetAsync(CancellationToken token)
   {
      while (!token.IsCancellationRequested)
      {
         if (_currentTarget)
         {
            var targetPosition = _currentTarget.position + offset;
            _transform.position = Vector3.SmoothDamp(
               _transform.position,
               targetPosition,
               ref _velocity,
               damping,
               followSpeed);
         }

         await UniTask.Yield(updateTiming, token);
      }
   }
}