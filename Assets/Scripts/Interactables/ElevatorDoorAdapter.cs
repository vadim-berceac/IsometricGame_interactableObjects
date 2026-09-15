using System.Linq;
using UnityEngine;

public class ElevatorDoorAdapter : MonoBehaviour
{
    [System.Serializable]
    private class DoorSettings
    {
        [field: SerializeField] public DoorController Controller { get; set; }
        [field: SerializeField] public int[] OpenOnPathIndexes { get; set; }

        public bool ShouldBeOpenAt(int pathIndex) =>
            OpenOnPathIndexes != null && OpenOnPathIndexes.Contains(pathIndex);
    }

    [SerializeField] private Elevator elevator;
    [SerializeField] private DoorSettings[] doorSettings;

    private void OnEnable()
    {
        if (!elevator || doorSettings == null || doorSettings.Length == 0)
        {
            return;
        }

        elevator.onArrived.AddListener(OnArrived);
        elevator.onDeparture.AddListener(OnDeparture);
    }

    private void OnDisable()
    {
        if (!elevator || doorSettings == null || doorSettings.Length == 0)
        {
            return;
        }

        elevator.onArrived.RemoveListener(OnArrived); 
        elevator.onDeparture.RemoveListener(OnDeparture); 
    }

    private void OnArrived(int currentPathIndex)
    {
        foreach (var settings in doorSettings)
        {
            if (settings.Controller == null)
            {
                continue;
            }

            if (settings.ShouldBeOpenAt(currentPathIndex))
            {
                settings.Controller.OpenLeafs();
            }
            else
            {
                settings.Controller.CloseLeafs();
            }
        }
    }

    private void OnDeparture(int currentPathIndex)
    {
        foreach (var settings in doorSettings)
        {
            settings.Controller?.CloseLeafs();
        }
    }
}