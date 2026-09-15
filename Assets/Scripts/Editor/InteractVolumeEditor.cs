using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(InteractionVolume))]
[CanEditMultipleObjects]
public class InteractionVolumeEditor : Editor
{
    private static readonly BoxBoundsHandle BoxHandle = new ();
    private static readonly SphereBoundsHandle SphereHandle = new ();

    private const EditMode.SceneViewEditMode EditModeVolume = (EditMode.SceneViewEditMode)90001;

    private SerializedProperty _shape;
    private SerializedProperty _center;
    private SerializedProperty _boxSize;
    private SerializedProperty _sphereRadius;
    private SerializedProperty _interactionLayers;
    private SerializedProperty _onEnter;
    private SerializedProperty _onExit;
    private SerializedProperty _drawGizmo;
    private SerializedProperty _drawGizmoOnlyWhenSelected;
    private SerializedProperty _gizmoColor;

    private static readonly GUIContent EditVolumeLabel = new GUIContent(
        "Edit Volume",
        "Тянуть границы объёма прямо в Scene View (как NavMesh Modifier Volume).");

    private void OnEnable()
    {
        _shape = serializedObject.FindProperty("shape");
        _center = serializedObject.FindProperty("center");
        _boxSize = serializedObject.FindProperty("boxSize");
        _sphereRadius = serializedObject.FindProperty("sphereRadius");
        _interactionLayers = serializedObject.FindProperty("interactionLayers");
        _onEnter = serializedObject.FindProperty("onEnter");
        _onExit = serializedObject.FindProperty("onExit");
        _drawGizmo = serializedObject.FindProperty("drawGizmo");
        _drawGizmoOnlyWhenSelected = serializedObject.FindProperty("drawGizmoOnlyWhenSelected");
        _gizmoColor = serializedObject.FindProperty("gizmoColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_shape);
        EditorGUILayout.PropertyField(_center);

        if ((InteractionVolume.VolumeShape)_shape.enumValueIndex == InteractionVolume.VolumeShape.Box)
            EditorGUILayout.PropertyField(_boxSize);
        else
            EditorGUILayout.PropertyField(_sphereRadius);

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            EditMode.DoInspectorToolbar(new[] { EditModeVolume }, new[] { EditVolumeLabel }, GetVolumeWorldBounds, this);
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_interactionLayers);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_drawGizmo);
        if (_drawGizmo.boolValue)
        {
            EditorGUILayout.PropertyField(_drawGizmoOnlyWhenSelected);
            EditorGUILayout.PropertyField(_gizmoColor);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_onEnter);
        EditorGUILayout.PropertyField(_onExit);

        serializedObject.ApplyModifiedProperties();
    }

    private Bounds GetVolumeWorldBounds()
    {
        var volume = (InteractionVolume)target;
        var t = volume.transform;

        if (volume.Shape == InteractionVolume.VolumeShape.Box)
        {
            var bounds = new Bounds(volume.Center, volume.BoxSize);
            var worldCenter = t.TransformPoint(bounds.center);
            var worldSize = Vector3.Scale(bounds.size, t.lossyScale);
            return new Bounds(worldCenter, worldSize);
        }
        else
        {
            var worldCenter = t.TransformPoint(volume.Center);
            var maxScale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z));
            var radius = volume.SphereRadius * maxScale;
            return new Bounds(worldCenter, radius * 2f * Vector3.one);
        }
    }

    private void OnSceneGUI()
    {
        if (!EditMode.IsOwner(this) || EditMode.editMode != EditModeVolume)
            return;

        var volume = (InteractionVolume)target;
        var t = volume.transform;

        using (new Handles.DrawingScope(t.localToWorldMatrix))
        {
            EditorGUI.BeginChangeCheck();

            if (volume.Shape == InteractionVolume.VolumeShape.Box)
            {
                BoxHandle.center = volume.Center;
                BoxHandle.size = volume.BoxSize;

                BoxHandle.SetColor(new Color(0f, 1f, 0.4f, 1f));

                BoxHandle.DrawHandle();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volume, "Resize Interaction Volume");
                    volume.Center = BoxHandle.center;
                    volume.BoxSize = Vector3.Max(BoxHandle.size, Vector3.zero);
                    EditorUtility.SetDirty(volume);
                }
            }
            else
            {
                SphereHandle.center = volume.Center;
                SphereHandle.radius = volume.SphereRadius;
                SphereHandle.SetColor(new Color(0f, 1f, 0.4f, 1f));

                SphereHandle.DrawHandle();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volume, "Resize Interaction Volume");
                    volume.Center = SphereHandle.center;
                    volume.SphereRadius = Mathf.Max(SphereHandle.radius, 0f);
                    EditorUtility.SetDirty(volume);
                }
            }
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    private static void DrawEditModeOutline(InteractionVolume volume, GizmoType gizmoType)
    {
    }
}