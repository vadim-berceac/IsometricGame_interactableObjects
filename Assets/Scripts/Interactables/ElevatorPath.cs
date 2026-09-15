using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ElevatorStopPoint
{
    public string Label = "Stop";
    public float Height;
    public ElevatorStopPoint() { }

    public ElevatorStopPoint(string label, float height)
    {
        Label = label;
        Height = height;
    }
}

[System.Serializable]
public class ElevatorSegment
{
    [Min(0f)]
    public float Speed = 1f;
    public AnimationCurve SpeedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}

[AddComponentMenu("Elevator/Elevator Path")]
[ExecuteAlways]
public class ElevatorPath : MonoBehaviour
{
    public List<ElevatorStopPoint> stopPoints = new ();

    public List<ElevatorSegment> segments = new ();

    public Vector2 gizmoPaneSize = new (1.2f, 1.2f);
    public Color pathColor = new (0.2f, 0.7f, 1f, 1f);
    public Color pointColor = new (1f, 0.85f, 0.1f, 1f);
    public Color startPointColor = new (0.2f, 1f, 0.3f, 1f);
    public Color endPointColor = new (1f, 0.25f, 0.25f, 1f);

    public void SyncSegments()
    {
        var needed = Mathf.Max(0, stopPoints.Count - 1);

        while (segments.Count < needed)
        {
            segments.Add(new ElevatorSegment());
        }

        while (segments.Count > needed)
        {
            segments.RemoveAt(segments.Count - 1);
        }
    }

    public Vector3 GetWorldPoint(int index)
    {
        if (index < 0 || index >= stopPoints.Count)
        {
            return transform.position;
        }

        var pos = transform.position;
        pos.y = transform.position.y + stopPoints[index].Height;
        return pos;
    }

    public void AddPoint(float height)
    {
        stopPoints.Add(new ElevatorStopPoint($"Stop {stopPoints.Count}", height));
        SyncSegments();
    }

    public void RemovePointAt(int index)
    {
        if (index < 0 || index >= stopPoints.Count) return;
        stopPoints.RemoveAt(index);
        SyncSegments();
    }

    private void OnDrawGizmosSelected()
    {
        if (stopPoints == null || stopPoints.Count == 0)
        {
            return;
        }

        Gizmos.color = pathColor;
        for (var i = 0; i < stopPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(GetWorldPoint(i), GetWorldPoint(i + 1));
        }
    }
}