using System.Linq;
using UnityEngine;

public class PropBones : MonoBehaviour
{
   [SerializeField] private PropBone[] propBones;

   public PropBone GetPropBone(PropType propType)
   {
      if (propBones == null || propBones.Length == 0)
      {
         return null;
      }
      
      return propBones.FirstOrDefault(b => b.PropType == propType);
   }
}

[System.Serializable]
public class PropBone
{
   [field: SerializeField] public Transform Prop { get; set; }
   [field: SerializeField] public PropType PropType { get; set; }
}

[System.Serializable]
public class PropBoneSettings
{
   [field: SerializeField] public PropType PropType { get; set; }
   [field: SerializeField] public Vector3 Position { get; set; }
   [field: SerializeField] public Vector3 Rotation { get; set; }
   [field: SerializeField] public float Scale { get; set; }

   public void SetPropBone(Transform source, PropBones propBones)
   {
      var parent = propBones.GetPropBone(PropType).Prop;
      source.parent = parent;
      source.localPosition = Position;
      source.localRotation = Quaternion.Euler(Rotation);
      source.localScale = Vector3.one * Scale;
   }
}

public enum PropType
{
   LeftHand,
   RightHand,
   HipsLeft,
   HipsRight,
   BackCenter,
   BackLeft,
   BackRight,
}