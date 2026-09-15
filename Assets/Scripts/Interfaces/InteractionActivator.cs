using UnityEngine;
using UnityEngine.Events;

public interface IInteractionActivator
{
    UnityEvent<Collider> OnEnter { get; }
    UnityEvent<Collider> OnExit { get; }
}

public abstract class InteractionActivator : MonoBehaviour, IInteractionActivator
{
    public UnityEvent<Collider> onEnter;
    public UnityEvent<Collider> onExit;

    public UnityEvent<Collider> OnEnter => onEnter;
    public UnityEvent<Collider> OnExit => onExit;
}
