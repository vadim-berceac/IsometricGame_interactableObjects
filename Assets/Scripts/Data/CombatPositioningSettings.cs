using UnityEngine;

[CreateAssetMenu(fileName = "CombatPositioningSettings", menuName = "Scriptable Objects/CombatPositioningSettings")]
public class CombatPositioningSettings : ScriptableObject
{
    [field: SerializeField] public float MeleeStopDistance { get; private set; } = 1.5f; 
    [field: SerializeField] public float HoldingDistance { get; private set; }= 5f;     
    [field: SerializeField] public float RotationSpeedDegrees { get; private set; } = 360f;
    [field: SerializeField] public float OrbitSpeedDegrees { get; private set; } = 30f;
}
