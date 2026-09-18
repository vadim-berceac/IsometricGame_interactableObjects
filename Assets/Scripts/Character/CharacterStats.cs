using System;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
   [field: SerializeField] public Stats Stats { get; set; }
}

[Serializable]
public struct Stats
{
    [field: SerializeField] public int Mastery { get; set; }
    [field: SerializeField] public int Attack { get; set; }
    [field: SerializeField] public int Defence { get; set; }
}
