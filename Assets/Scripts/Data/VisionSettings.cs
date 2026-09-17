using UnityEngine;

[CreateAssetMenu(fileName = "VisionSettings", menuName = "Scriptable Objects/VisionSettings")]
public class VisionSettings : ScriptableObject
{
    [field: SerializeField] public float VisionRadius { get; private set; } = 10f;
    [field: SerializeField, Range(0f, 360f)] public float ViewAngleDegrees { get; private set; } = 90f;
    [field: SerializeField] public float UpdateInterval { get; private set; } = 0.2f;
    [field: SerializeField] public float EyeHeight { get; private set; } = 1.6f;
    [field: SerializeField] public LayerMask ObstacleMask { get; private set; }
}
