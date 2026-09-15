using System;
using System.Reflection;
using UnityEditor;

[CustomEditor(typeof(InteractMotion))]
public class InteractMotionEditor : Editor
{
    private SerializedProperty _trigger;
    private SerializedProperty _enterDelay;
    private SerializedProperty _enterTime;
    private SerializedProperty _exitTime;
    private SerializedProperty _ignoreCollisionsOnInteract;
    private SerializedProperty _controllerSource;
    private SerializedProperty _fixedController;
    private SerializedProperty _targetSource;
    private SerializedProperty _motionType;
    private SerializedProperty _footTarget;
    private SerializedProperty _exitType;
    private SerializedProperty _exitTarget;
    private SerializedProperty _onEnterMotionEnd;
    private SerializedProperty _onExitMotionStart;

    private int _fixedControllerValue;
    private int _fixedFootTargetValue;
    private int _moveToExitPositionValue;

    private void OnEnable()
    {
        _trigger = serializedObject.FindProperty("trigger");
        _enterDelay = serializedObject.FindProperty("enterDelay");
        _enterTime = serializedObject.FindProperty("enterTime");
        _exitTime = serializedObject.FindProperty("exitTime");
        _ignoreCollisionsOnInteract = serializedObject.FindProperty("ignoreCollisionsOnInteract");
        _controllerSource = serializedObject.FindProperty("controllerSource");
        _fixedController = serializedObject.FindProperty("fixedController");
        _targetSource = serializedObject.FindProperty("targetSource");
        _motionType = serializedObject.FindProperty("motionType");
        _footTarget = serializedObject.FindProperty("footTarget");
        _exitType = serializedObject.FindProperty("exitType");
        _exitTarget = serializedObject.FindProperty("exitTarget");
        _onEnterMotionEnd = serializedObject.FindProperty("onEnterMotionEnd");
        _onExitMotionStart = serializedObject.FindProperty("onExitMotionStart");

        _fixedControllerValue = GetEnumValue(typeof(InteractMotion), "ControllerSource", "FixedController");
        _fixedFootTargetValue = GetEnumValue(typeof(InteractMotion), "TargetSource", "FixedFootTarget");
        _moveToExitPositionValue = GetEnumValue(typeof(InteractMotion), "ExitType", "MoveToExitPosition");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_trigger);
        EditorGUILayout.PropertyField(_enterDelay);
        EditorGUILayout.PropertyField(_enterTime);
        EditorGUILayout.PropertyField(_exitTime);
        EditorGUILayout.PropertyField(_ignoreCollisionsOnInteract);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Who to move/rotate", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_controllerSource);
        if (_controllerSource.enumValueIndex == _fixedControllerValue)
        {
            EditorGUILayout.PropertyField(_fixedController);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enter Target Source", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_targetSource);
        EditorGUILayout.PropertyField(_motionType);
        if (_targetSource.enumValueIndex == _fixedFootTargetValue)
        {
            EditorGUILayout.PropertyField(_footTarget);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Exit Target Source", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_exitType);
        if (_exitType.enumValueIndex == _moveToExitPositionValue)
        {
            EditorGUILayout.PropertyField(_exitTarget);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_onEnterMotionEnd);
        EditorGUILayout.PropertyField(_onExitMotionStart);

        serializedObject.ApplyModifiedProperties();
    }

    private static int GetEnumValue(Type declaringType, string enumTypeName, string valueName)
    {
        var type = declaringType.GetNestedType(enumTypeName, BindingFlags.NonPublic);
        if (type == null)
        {
            return -1;
        }

        var value = Enum.Parse(type, valueName);
        return Convert.ToInt32(value);
    }
}