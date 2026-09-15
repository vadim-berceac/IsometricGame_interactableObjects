#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractAnimationEvents))]
public class InteractAnimationEventsEditor : Editor
{
    private SerializedProperty _triggerProperty;
    private SerializedProperty _mappingsProperty;
    private SerializedProperty _pollIntervalProperty;
    private SerializedProperty _onAnyEventProperty;

    private void OnEnable()
    {
        _triggerProperty = serializedObject.FindProperty("trigger");
        _mappingsProperty = serializedObject.FindProperty("mappings");
        _pollIntervalProperty = serializedObject.FindProperty("pollIntervalSeconds");
        _onAnyEventProperty = serializedObject.FindProperty("OnAnyEvent");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_triggerProperty, new GUIContent("Trigger", "InteractAnimation компонент, который запускает анимации"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Настройки", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(_pollIntervalProperty, new GUIContent("Интервал проверки", "Интервал проверки прогресса анимации (0 = каждый кадр)"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Общее событие", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_onAnyEventProperty, new GUIContent("On Any Event", "Вызывается при срабатывании ЛЮБОГО события из любого клипа (без параметров). Чтобы получить имя/данные события - подпишитесь в коде на OnEventTriggered."));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Карта событий", EditorStyles.boldLabel);

        if (_mappingsProperty == null)
        {
            EditorGUILayout.HelpBox("Не удалось найти сериализуемое поле mappings.", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        if (_mappingsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Нет настроенных клипов. Добавьте клипы для отслеживания событий.", MessageType.Info);
        }

        for (int i = 0; i < _mappingsProperty.arraySize; i++)
        {
            var mappingProperty = _mappingsProperty.GetArrayElementAtIndex(i);
            var clipProperty = mappingProperty.FindPropertyRelative("Clip");
            var curveProperty = mappingProperty.FindPropertyRelative("ProgressCurve");
            var eventsProperty = mappingProperty.FindPropertyRelative("Events");

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(clipProperty, GUIContent.none, GUILayout.MinWidth(100));

            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                _mappingsProperty.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(curveProperty, new GUIContent("Кривая прогресса"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"События ({eventsProperty.arraySize})", EditorStyles.boldLabel);

            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                eventsProperty.arraySize++;
            }
            EditorGUILayout.EndHorizontal();

            for (int j = 0; j < eventsProperty.arraySize; j++)
            {
                var eventProperty = eventsProperty.GetArrayElementAtIndex(j);
                var timeProperty = eventProperty.FindPropertyRelative("Time");
                var nameProperty = eventProperty.FindPropertyRelative("EventName");
                var dataProperty = eventProperty.FindPropertyRelative("EventData");
                var toleranceProperty = eventProperty.FindPropertyRelative("TriggerTolerance");
                var hysteresisProperty = eventProperty.FindPropertyRelative("ResetHysteresis");
                var eventUnityEventProperty = eventProperty.FindPropertyRelative("OnEventTriggered");

                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Событие {j + 1}", EditorStyles.boldLabel);

                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    eventsProperty.DeleteArrayElementAtIndex(j);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Slider(timeProperty, 0f, 1f, "Время срабатывания");
                EditorGUILayout.PropertyField(nameProperty, new GUIContent("Имя события"));
                EditorGUILayout.PropertyField(dataProperty, new GUIContent("Данные события"));

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(toleranceProperty, new GUIContent("Допуск"));
                EditorGUILayout.PropertyField(hysteresisProperty, new GUIContent("Гистерезис"));
                EditorGUILayout.EndHorizontal();

                if (eventUnityEventProperty != null)
                {
                    EditorGUILayout.PropertyField(eventUnityEventProperty, new GUIContent("On Event Triggered", "Событие Unity без параметров. Вызывается только для ЭТОГО конкретного события."));
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Добавить клип"))
        {
            _mappingsProperty.arraySize++;
            var newMapping = _mappingsProperty.GetArrayElementAtIndex(_mappingsProperty.arraySize - 1);
            newMapping.FindPropertyRelative("Clip").objectReferenceValue = null;
            newMapping.FindPropertyRelative("ProgressCurve").animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);
            newMapping.FindPropertyRelative("Events").arraySize = 0;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif