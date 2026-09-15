using UnityEngine;

[System.Serializable]
public struct StaticPartSettings
{
    [field: SerializeField] public GameObject Prefab { get; set; }
    [field: SerializeField] public PropBoneSettings BoneSettings { get; set; }
}
