using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LadderConstructor))]
public class LadderConstructorEditor : Editor
{
    private LadderConstructor _target;
    private bool _pendingAction;

    private void OnEnable()
    {
        _target = (LadderConstructor)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("cellPrefab"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startZonePrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startZoneOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startZoneScale"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("endZonePrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("endZoneOffset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("endZoneScale"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cellHeight"));

        var cellCountProp = serializedObject.FindProperty("cellCount");
        EditorGUILayout.PropertyField(cellCountProp, new GUIContent("Кол-во ячеек"));
        if (cellCountProp.intValue < 0) cellCountProp.intValue = 0;

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_pendingAction))
            {
                if (GUILayout.Button("− Ячейка"))
                {
                    RequestAction(() =>
                    {
                        _target.cellCount = Mathf.Max(0, _target.cellCount - 1);
                        _target.Construct();
                    }, "Remove Ladder Cell");
                }

                if (GUILayout.Button("+ Ячейка"))
                {
                    RequestAction(() =>
                    {
                        _target.cellCount += 1;
                        _target.Construct();
                    }, "Add Ladder Cell");
                }
            }
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_pendingAction))
        {
            if (GUILayout.Button("Очистить (убрать все ячейки)"))
            {
                RequestAction(() => _target.ClearAll(), "Clear Ladder");
            }
        }

        EditorGUILayout.HelpBox(
            $"Высота: {_target.TotalHeight:0.##} м, ячеек: {_target.cellCount}",
            MessageType.Info);
    }

    private void RequestAction(System.Action action, string undoName)
    {
        if (_pendingAction) return;
        _pendingAction = true;

        EditorApplication.delayCall += () =>
        {
            _pendingAction = false;

            if (!_target) return;

            Undo.RecordObject(_target, undoName);
            action.Invoke();
            EditorUtility.SetDirty(_target);
            Repaint();
        };
    }

    private void OnSceneGUI()
    {
        if (_target.cellHeight <= 0f) return;

        var topY = _target.cellCount * _target.cellHeight;
        var handlePos = _target.transform.TransformPoint(new Vector3(0f, topY, 0f));

        EditorGUI.BeginChangeCheck();
        var handleSize = HandleUtility.GetHandleSize(handlePos) * 0.5f;
        var newHandlePos = Handles.Slider(
            handlePos,
            _target.transform.up,
            handleSize,
            Handles.ConeHandleCap,
            0f);

        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }
        
        var localY = _target.transform.InverseTransformPoint(newHandlePos).y;
        var newCount = Mathf.Max(0, Mathf.RoundToInt(localY / _target.cellHeight));

        if (newCount == _target.cellCount)
        {
            return;
        }
        
        Undo.RecordObject(_target, "Resize Ladder");
        _target.cellCount = newCount;
        _target.Construct();
        EditorUtility.SetDirty(_target);
    }
}