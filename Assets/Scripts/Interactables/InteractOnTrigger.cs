using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractOnTrigger : InteractionActivator
{
    private void OnTriggerEnter(Collider other) => OnEnter?.Invoke(other);
    private void OnTriggerExit(Collider other) => OnExit?.Invoke(other);
    private void OnDestroy() => OnExit?.Invoke(null);
}