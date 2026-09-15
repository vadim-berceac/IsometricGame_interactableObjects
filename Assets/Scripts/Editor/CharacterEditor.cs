using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Character))]
public class CharacterEditor : Editor
{
    private SerializedProperty _characterType;
    private SerializedProperty _rotationToCursorSpeed;
    private SerializedProperty _minRotationDistance;
    private SerializedProperty _closeRotationDistance;
    private SerializedProperty _maxCloseRotationAngle;
    private SerializedProperty _playerLayer;

    private void OnEnable()
    {
        _characterType = serializedObject.FindProperty("<CharacterType>k__BackingField");
        _rotationToCursorSpeed = serializedObject.FindProperty("rotationToCursorSpeed");
        _minRotationDistance = serializedObject.FindProperty("minRotationDistance");
        _closeRotationDistance = serializedObject.FindProperty("closeRotationDistance");
        _maxCloseRotationAngle = serializedObject.FindProperty("maxCloseRotationAngle");
        _playerLayer = serializedObject.FindProperty("playerLayer");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_characterType);

        var isPlayer = _characterType.enumValueIndex == (int)CharacterType.Player;

        if (isPlayer)
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_rotationToCursorSpeed);
            EditorGUILayout.PropertyField(_minRotationDistance);
            EditorGUILayout.PropertyField(_closeRotationDistance);
            EditorGUILayout.PropertyField(_maxCloseRotationAngle);
            EditorGUILayout.PropertyField(_playerLayer);
        }

        serializedObject.ApplyModifiedProperties();
    }
}