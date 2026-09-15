using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ElevatorPath))]
public class ElevatorPathEditor : Editor
{
    private ElevatorPath _path;
    private int _selectedPoint = -1;

    private void OnEnable()
    {
        _path = (ElevatorPath)target;
        _path.SyncSegments();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Gizmo / Display", EditorStyles.boldLabel);
        DrawPropertyField("gizmoPaneSize");
        DrawPropertyField("pathColor");
        DrawPropertyField("pointColor");
        DrawPropertyField("startPointColor");
        DrawPropertyField("endPointColor");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Stop Points", EditorStyles.boldLabel);

        var stopPointsProp = serializedObject.FindProperty("stopPoints");
        var segmentsProp = serializedObject.FindProperty("segments");

        for (var i = 0; i < stopPointsProp.arraySize; i++)
        {
            var pointProp = stopPointsProp.GetArrayElementAtIndex(i);
            var labelProp = pointProp.FindPropertyRelative("Label");
            var heightProp = pointProp.FindPropertyRelative("Height");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var isSelected = _selectedPoint == i;
            var prevColor = GUI.backgroundColor;
            if (isSelected) GUI.backgroundColor = Color.cyan;

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = prevColor;

            EditorGUILayout.LabelField($"#{i}", GUILayout.Width(25));
            EditorGUILayout.PropertyField(labelProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.PropertyField(heightProp, GUIContent.none);

            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                _selectedPoint = isSelected ? -1 : i;
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                Undo.RecordObject(_path, "Remove Elevator Point");
                _path.RemovePointAt(i);
                serializedObject.Update();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            if (i < segmentsProp.arraySize)
            {
                var segmentProp = segmentsProp.GetArrayElementAtIndex(i);
                var speedProp = segmentProp.FindPropertyRelative("Speed");
                var curveProp = segmentProp.FindPropertyRelative("SpeedCurve");

                EditorGUILayout.LabelField($"↓ Segment to the point #{i + 1}", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(speedProp, new GUIContent("Speed"));
                EditorGUILayout.PropertyField(curveProp, new GUIContent("Speed Curve"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Point"))
        {
            Undo.RecordObject(_path, "Add Elevator Point");
            var newHeight = _path.stopPoints.Count > 0
                ? _path.stopPoints[_path.stopPoints.Count - 1].Height + 3f
                : 0f;
            _path.AddPoint(newHeight);
        }
        // if (GUILayout.Button("Сортировать по высоте"))
        // {
        //     Undo.RecordObject(_path, "Sort Elevator Points");
        //     _path.SortPointsByHeight();
        // }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPropertyField(string propName)
    {
        var prop = serializedObject.FindProperty(propName);
        if (prop != null) EditorGUILayout.PropertyField(prop);
    }

   
    private void OnSceneGUI()
    {
        _path = (ElevatorPath)target;
        if (_path.stopPoints == null || _path.stopPoints.Count == 0) return;

        _path.SyncSegments();

        for (var i = 0; i < _path.stopPoints.Count - 1; i++)
        {
            var a = _path.GetWorldPoint(i);
            var b = _path.GetWorldPoint(i + 1);

            var speed = i < _path.segments.Count ? _path.segments[i].Speed : 1f;

            Handles.color = _path.pathColor;
            Handles.DrawDottedLine(a, b, 4f);

            var mid = (a + b) * 0.5f;
            Handles.Label(mid + Vector3.right * 0.3f,
                $"Скорость: {speed:0.##}",
                EditorStyles.boldLabel);
        }

        for (var i = 0; i < _path.stopPoints.Count; i++)
        {
            DrawPointGizmo(i);
        }
    }

    private void DrawPointGizmo(int index)
    {
        var point = _path.stopPoints[index];
        var worldPos = _path.GetWorldPoint(index);

        var col = _path.pointColor;
        if (index == 0) col = _path.startPointColor;
        else if (index == _path.stopPoints.Count - 1) col = _path.endPointColor;
        if (index == _selectedPoint) col = Color.cyan;

        var size = _path.gizmoPaneSize;
        var right = _path.transform.right * (size.x * 0.5f);
        var forward = _path.transform.forward * (size.y * 0.5f);

        Vector3[] verts = 
        {
            worldPos - right - forward,
            worldPos + right - forward,
            worldPos + right + forward,
            worldPos - right + forward,
        };

        Handles.color = new Color(col.r, col.g, col.b, 0.25f);
        Handles.DrawSolidRectangleWithOutline(verts, new Color(col.r, col.g, col.b, 0.2f), col);

        Handles.color = col;
        Handles.Label(worldPos + Vector3.up * 0.3f + Vector3.right * (size.x * 0.5f + 0.1f),
            $"{point.Label} (#{index}, h={point.Height:0.##})");

        var pickSize = Mathf.Max(size.x, size.y) * 0.5f;
        var controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (Handles.Button(worldPos, Quaternion.LookRotation(Vector3.up), pickSize, pickSize, Handles.RectangleHandleCap))
        {
            _selectedPoint = index;
            Repaint();
        }

        EditorGUI.BeginChangeCheck();
        var newPos = Handles.Slider(
            worldPos,
            Vector3.up,
            HandleUtility.GetHandleSize(worldPos) * 0.5f,
            Handles.ArrowHandleCap,
            0.1f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_path, "Move Elevator Point");
            var deltaY = newPos.y - worldPos.y;
            point.Height += deltaY;
            EditorUtility.SetDirty(_path);
        }
    }
}