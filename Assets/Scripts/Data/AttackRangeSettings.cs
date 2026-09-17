using UnityEngine;

[CreateAssetMenu(fileName = "AttackRangeSettings", menuName = "Scriptable Objects/AttackRangeSettings")]
public class AttackRangeSettings : ScriptableObject
{
    [field: SerializeField] public float MeleeEnterDistance {get; private set;} = 2f;
    [field: SerializeField] public float MeleeExitDistance {get; private set;} = 2.5f;
    [field: SerializeField] public float RangedEnterDistance {get; private set;} = 8f;
    [field: SerializeField] public float RangedExitDistance {get; private set;} = 9f;
    [field: SerializeField] public float UpdateInterval {get; private set;} = 0.1f;
}
