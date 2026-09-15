using System;
using UnityEngine;

public class AIInputHandler : ICharacterInput
{
    // Just a placeholder.
    public Action<Vector2> OnMove { get; set;}
    public Action<Vector2> OnLook { get; set;}
    public Action<bool> OnRun  { get; set;}
    public Action OnInteract { get; set;}
}
