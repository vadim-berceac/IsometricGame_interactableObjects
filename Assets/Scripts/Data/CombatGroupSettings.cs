using UnityEngine;

[CreateAssetMenu(fileName = "CombatGroupSettings", menuName = "Scriptable Objects/CombatGroupSettings")]
public class CombatGroupSettings : ScriptableObject
{
    [field: SerializeField] public float PlaceholderAttackDuration { get; private set; } = 1.5f;
}
