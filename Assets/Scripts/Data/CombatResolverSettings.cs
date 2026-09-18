using UnityEngine;

[CreateAssetMenu(fileName = "CombatResolverSettings", menuName = "Scriptable Objects/CombatResolverSettings")]
public class CombatResolverSettings : ScriptableObject
{
    [field: SerializeField] public float BaseParryChance { get; set; } = 0.05f;
    [field: SerializeField] public float BaseBlockChance { get; set; } = 0.15f;
    [field: SerializeField] public float MasteryToChance { get; set; } = 0.02f;
    [field: SerializeField] public float StatToChance { get; set; } = 0.01f;  
}
