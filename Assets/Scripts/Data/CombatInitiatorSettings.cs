using UnityEngine;

[CreateAssetMenu(fileName = "CombatInitiatorSettings", menuName = "Scriptable Objects/CombatInitiatorSettings")]
public class CombatInitiatorSettings : ScriptableObject
{
    [field: SerializeField] public float AttackInterval { get; private set; } = 1.2f;
    [field: SerializeField] public float DefaultClinchChance { get; private set; } = 0.1f;
}
