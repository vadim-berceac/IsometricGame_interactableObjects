using UnityEngine;
using Zenject;

public class ElevatorPaneAdapter : MonoBehaviour
{
    [SerializeField] private Elevator elevator;
    [SerializeField] private float deactivateDelay;
    [SerializeField] private float temporaryDisableTime;
    
    [Inject] private readonly ElevatorPaneController _elevatorPaneController;
    private bool _enabled = true;

    public void Enable(bool value)
    {
        _enabled = value;

        if (!_enabled)
        {
            DeactivateElevatorPaneDelay();
        }
    }

    public void ActivateElevatorPane()
    {
        if(!_elevatorPaneController || !_enabled) return;
        _elevatorPaneController.ActivateCanvas(elevator.FloorCount, elevator.MoveToIndex);
    }

    public void DeactivateElevatorPaneDelay()
    {
        if(!_elevatorPaneController) return;
        _elevatorPaneController.DeactivateCanvasWithDelay(deactivateDelay);
    }

    public void TemporaryDisable()
    {
        if(!_elevatorPaneController) return;
        DeactivateElevatorPaneDelay();
        _elevatorPaneController.TemporaryDisable(temporaryDisableTime);
    }

    private void OnDisable()
    {
        DeactivateElevatorPaneDelay();
    }
}
