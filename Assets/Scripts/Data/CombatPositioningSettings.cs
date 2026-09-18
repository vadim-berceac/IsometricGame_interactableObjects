using UnityEngine;

[CreateAssetMenu(fileName = "CombatPositioningSettings", menuName = "Scriptable Objects/CombatPositioningSettings")]
public class CombatPositioningSettings : ScriptableObject
{
    [field: SerializeField] public float MeleeStopDistance { get; private set; } = 1.5f; 
    [field: SerializeField] public float HoldingDistance { get; private set; }= 5f;     
    [field: SerializeField] public float RotationSpeedDegrees { get; private set; } = 360f;
    [field: SerializeField] public float OrbitSpeedDegrees { get; private set; } = 30f;
    [field: SerializeField] public float ObstacleCheckRadius { get; private set; } = 0.3f;  
    [field: SerializeField] public float ObstacleCheckDistance { get; private set; } = 0.5f; 
    [field: SerializeField] public float ObstacleCheckHeight { get; private set; } = 1f;  
    [field: SerializeField] public LayerMask ObstacleMask { get; private set; } 
}
