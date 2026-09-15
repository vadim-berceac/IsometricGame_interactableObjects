using UnityEngine;

public class InteractionVolume : InteractionActivator
{
    public enum VolumeShape
    {
        Box,
        Sphere
    }

    [Header("Shape")]
    [SerializeField] private VolumeShape shape = VolumeShape.Box;
    [SerializeField] private Vector3 center = Vector3.zero;
    [SerializeField] private Vector3 boxSize = Vector3.one;
    [SerializeField] private float sphereRadius = 0.5f;

    [Header("Detection")]
    [SerializeField] private LayerMask interactionLayers = ~0;

    [Header("Gizmo")]
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private bool drawGizmoOnlyWhenSelected = false;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.4f, 0.35f);

    private Collider _collider;
    private Rigidbody _rigidbody;

    public VolumeShape Shape => shape;

    public Vector3 Center
    {
        get => center;
        set => center = value;
    }

    public Vector3 BoxSize
    {
        get => boxSize;
        set => boxSize = value;
    }

    public float SphereRadius
    {
        get => sphereRadius;
        set => sphereRadius = value;
    }

    private void Awake()
    {
        SetupPhysicsComponents();
    }

    private void SetupPhysicsComponents()
    {
        if (shape == VolumeShape.Box)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = center;
            box.size = boxSize;
            _collider = box;
        }
        else
        {
            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.center = center;
            sphere.radius = sphereRadius;
            _collider = sphere;
        }

        _collider.isTrigger = true;

        _rigidbody = gameObject.AddComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _rigidbody.hideFlags = HideFlags.NotEditable;
        _collider.hideFlags = HideFlags.NotEditable;
    }

    private bool IsInLayerMask(Collider other)
    {
        return (interactionLayers.value & (1 << other.gameObject.layer)) != 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsInLayerMask(other))
            OnEnter?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsInLayerMask(other))
            OnExit?.Invoke(other);
    }

    private void OnDestroy()
    {
        OnExit?.Invoke(null);
    }

    private void OnDrawGizmos()
    {
        if (drawGizmo && !drawGizmoOnlyWhenSelected)
            DrawVolumeGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (drawGizmo && drawGizmoOnlyWhenSelected)
            DrawVolumeGizmo();
    }

    private void DrawVolumeGizmo()
    {
        if (shape == VolumeShape.Box)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(center), transform.rotation, transform.lossyScale);
            Gizmos.color = gizmoColor;
            Gizmos.DrawCube(Vector3.zero, boxSize);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(Vector3.zero, boxSize);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            var worldCenter = transform.TransformPoint(center);
            var maxScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
            var radius = sphereRadius * maxScale;

            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(worldCenter, radius);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireSphere(worldCenter, radius);
        }
    }
}