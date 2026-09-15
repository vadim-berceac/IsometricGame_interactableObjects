using UnityEngine;

[CreateAssetMenu(fileName = "GravityCollisionSettings", menuName = "Scriptable Objects/GravityCollisionSettings")]
public class GravityCollisionSettings : ScriptableObject
{
    [field: SerializeField] public float Gravity {get; private set;} = -20f;
    [field: SerializeField] public float GroundStickSpeed {get; private set;} = -2f;     
    [field: SerializeField] public float MaxSlopeAngle {get; private set;} = 50f;         
    [field: SerializeField] public float SkinWidth {get; private set;} = 0.03f;
    [field: SerializeField] public float GroundCheckDistance {get; private set;} = 0.15f;
    [field: SerializeField] public int MaxDepenetrationIterations {get; private set;} = 4;
    [field: SerializeField] public int OverlapBufferSize {get; private set;} = 8;
}
