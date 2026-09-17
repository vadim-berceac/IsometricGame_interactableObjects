using System;
using UnityEngine;

public class AIInputHandler : ICharacterInput
{
    public Action<Vector2> OnMove { get; set; }
    public Action<Vector2> OnLook { get; set; }
    public Action<bool> OnRun { get; set; }
    public Action OnInteract { get; set; }

    private Vector2 _currentMove;

    public void SetMove(Vector2 move)
    {
        if (_currentMove == move)
        {
            return;
        }

        _currentMove = move;
        OnMove?.Invoke(move);
    }
}