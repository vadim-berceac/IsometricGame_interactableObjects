using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class AnimationEventData
{
    [Tooltip("Время срабатывания события (0-1)")]
    public float Time;

    [Tooltip("Имя события (для идентификации)")]
    public string EventName;

    [Tooltip("Дополнительные данные события")]
    public string EventData;

    [Tooltip("Допустимое отклонение от целевого времени, при котором событие считается 'достигнутым'")]
    public float TriggerTolerance = 0.01f;

    [Tooltip("На сколько время должно отступить назад, чтобы событие могло сработать снова")]
    public float ResetHysteresis = 0.05f;

    [Tooltip("Событие Unity, которое будет вызвано")]
    public UnityEvent OnEventTriggered = new UnityEvent();

    [NonSerialized] public bool IsArmed = true;
}

[System.Serializable]
public class ClipEventMapping
{
    [Tooltip("Целевой анимационный клип")]
    public AnimationClip Clip;

    [Tooltip("Кривая анимации для отслеживания прогресса")]
    public AnimationCurve ProgressCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Tooltip("События, которые должны сработать на этой кривой")]
    public AnimationEventData[] Events = Array.Empty<AnimationEventData>();
}

public class InteractAnimationEvents : MonoBehaviour
{
    [Tooltip("InteractAnimation, из клипов которого берутся события")]
    [SerializeField] private InteractAnimation trigger;

    [Tooltip("Настройки событий для каждого клипа")]
    [SerializeField] private ClipEventMapping[] mappings = Array.Empty<ClipEventMapping>();

    [Tooltip("Интервал проверки прогресса анимации (0 = каждый кадр)")]
    [SerializeField] private float pollIntervalSeconds = 0f;

    [Tooltip("Общее событие для ВСЕХ анимационных клипов. Вызывается при срабатывании любого события — без параметров.")]
    public UnityEvent OnAnyEvent = new UnityEvent();

    private Dictionary<AnimationClip, ClipEventMapping> _clipLookup;
    private Dictionary<AnimationClip, List<AnimationEventData>> _activeEvents;
    private CancellationTokenSource _cts;
    private Character _currentController;
    private AnimationClip _currentClip;
    private float _currentClipLength;
    private float _clipStartTime;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (trigger == null)
        {
            Debug.LogWarning($"InteractAnimationEvents на {gameObject.name}: не задан InteractAnimation trigger", this);
        }

        if (mappings == null)
        {
            mappings = Array.Empty<ClipEventMapping>();
            EditorUtility.SetDirty(this);
            return;
        }

        foreach (var mapping in mappings)
        {
            if (mapping == null) continue;

            if (mapping.Clip == null)
            {
                Debug.LogWarning($"InteractAnimationEvents на {gameObject.name}: обнаружен ClipEventMapping с незаданным клипом", this);
            }

            if (mapping.ProgressCurve == null)
            {
                Debug.LogWarning($"InteractAnimationEvents на {gameObject.name}: обнаружен ClipEventMapping с незаданной кривой для клипа {mapping.Clip?.name}", this);
            }

            if (mapping.Events == null) continue;

            foreach (var evt in mapping.Events)
            {
                if (evt == null) continue;

                if (evt.TriggerTolerance > evt.ResetHysteresis)
                {
                    Debug.LogWarning($"InteractAnimationEvents на {gameObject.name}: TriggerTolerance ({evt.TriggerTolerance}) больше чем ResetHysteresis ({evt.ResetHysteresis}) для события {evt.EventName}. Это может привести к некорректной работе защиты от спама.", this);
                }
            }
        }
    }
#endif

    private void Awake()
    {
        if (mappings == null)
        {
            mappings = Array.Empty<ClipEventMapping>();
        }

        _clipLookup = mappings
            .Where(m => m != null && m.Clip != null)
            .ToDictionary(m => m.Clip, m => m);

        _activeEvents = new Dictionary<AnimationClip, List<AnimationEventData>>();
    }

    private void OnEnable()
    {
        if (trigger == null) return;

        trigger.onInteractEnter.AddListener(OnInteractEnter);
        trigger.onInteractExit.AddListener(OnInteractExit);
        trigger.onClipStarted.AddListener(OnClipStarted);
    }

    private void OnDisable()
    {
        Unsubscribe();
        Cleanup();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        Cleanup();
    }

    private void Unsubscribe()
    {
        if (trigger == null) return;

        trigger.onInteractEnter.RemoveListener(OnInteractEnter);
        trigger.onInteractExit.RemoveListener(OnInteractExit);
        trigger.onClipStarted.RemoveListener(OnClipStarted);
    }

    private void Cleanup()
    {
        Cancel();
        _currentController = null;
        _currentClip = null;
        _activeEvents?.Clear();
        _clipLookup?.Clear();
    }

    private void OnInteractEnter(Character controller)
    {
        _currentController = controller;
    }

    private void OnInteractExit(Character controller)
    {
        Cancel();
        _currentController = null;
        _currentClip = null;
        _activeEvents?.Clear();
    }

    private void OnClipStarted(AnimationClip clip, float blendLength)
    {
        if (!_currentController || !clip)
        {
            return;
        }

        if (_currentClip == clip)
        {
            return;
        }

        _currentClip = clip;
        _currentClipLength = clip.length;
        _clipStartTime = Time.time;

        if (_clipLookup.TryGetValue(clip, out var mapping))
        {
            _activeEvents[clip] = new List<AnimationEventData>(mapping.Events);
            ResetEventStates(clip);
        }

        Cancel();
        _cts = new CancellationTokenSource();
        WatchClipProgress(_cts.Token).Forget();
    }

    private void ResetEventStates(AnimationClip clip)
    {
        if (_activeEvents.TryGetValue(clip, out var events))
        {
            foreach (var evt in events)
            {
                if (evt != null)
                {
                    evt.IsArmed = true;
                }
            }
        }
    }

    private async UniTaskVoid WatchClipProgress(CancellationToken token)
    {
        if (!_currentClip || !_currentController)
        {
            return;
        }

        while (!token.IsCancellationRequested && _currentClip && _currentController)
        {
            var elapsed = Time.time - _clipStartTime;
            float normalizedTime;

            if (_currentClip.isLooping)
            {
                normalizedTime = (elapsed / _currentClipLength) % 1f;
            }
            else
            {
                normalizedTime = Mathf.Clamp01(elapsed / _currentClipLength);
            }

            if (_clipLookup.TryGetValue(_currentClip, out var mapping))
            {
                EvaluateClipProgress(mapping, normalizedTime);
            }

            if (pollIntervalSeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), cancellationToken: token);
            }
            else
            {
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }

    private void EvaluateClipProgress(ClipEventMapping mapping, float normalizedTime)
    {
        if (mapping.Events == null || mapping.Events.Length == 0)
        {
            return;
        }

        if (_currentClipLength <= 0f)
        {
            return;
        }

        float curvedTime;
        if (mapping.ProgressCurve == null || IsConstantCurve(mapping.ProgressCurve))
        {
            curvedTime = normalizedTime;
        }
        else
        {
            curvedTime = mapping.ProgressCurve.Evaluate(normalizedTime);
        }

        foreach (var evt in mapping.Events)
        {
            if (evt == null)
            {
                continue;
            }

            var eventTime = evt.Time;
            var distToEvent = Mathf.Abs(curvedTime - eventTime);

            if (distToEvent <= evt.TriggerTolerance)
            {
                if (evt.IsArmed)
                {
                    evt.IsArmed = false;

                    evt.OnEventTriggered?.Invoke();
                    OnAnyEvent?.Invoke();
                    OnEventTriggered?.Invoke(evt.EventName, evt.EventData);
                }
            }
            else if (distToEvent >= evt.ResetHysteresis)
            {
                evt.IsArmed = true;
            }
        }
    }

    private static bool IsConstantCurve(AnimationCurve curve)
    {
        if (curve == null || curve.keys == null || curve.keys.Length == 0)
        {
            return true;
        }

        var firstValue = curve.keys[0].value;
        for (int i = 1; i < curve.keys.Length; i++)
        {
            if (Mathf.Abs(curve.keys[i].value - firstValue) > 0.0001f)
            {
                return false;
            }
        }

        return true;
    }

    private void Cancel()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public event Action<string, string> OnEventTriggered;

    
    public void AddEventForClip(AnimationClip clip, AnimationEventData eventData)
    {
        if (clip == null || eventData == null) return;

        if (mappings == null)
        {
            mappings = Array.Empty<ClipEventMapping>();
        }

        var existingMapping = Array.Find(mappings, m => m != null && m.Clip == clip);

        if (existingMapping != null)
        {
            var eventsList = existingMapping.Events != null
                ? new List<AnimationEventData>(existingMapping.Events)
                : new List<AnimationEventData>();

            eventsList.Add(eventData);

            var newMapping = new ClipEventMapping
            {
                Clip = existingMapping.Clip,
                ProgressCurve = existingMapping.ProgressCurve,
                Events = eventsList.ToArray()
            };

            for (int i = 0; i < mappings.Length; i++)
            {
                if (mappings[i] != null && mappings[i].Clip == clip)
                {
                    mappings[i] = newMapping;
                    break;
                }
            }
        }
        else
        {
            var newMapping = new ClipEventMapping
            {
                Clip = clip,
                ProgressCurve = AnimationCurve.Linear(0, 0, 1, 1),
                Events = new[] { eventData }
            };

            var newMappings = new List<ClipEventMapping>();
            if (mappings != null)
            {
                newMappings.AddRange(mappings);
            }
            newMappings.Add(newMapping);
            mappings = newMappings.ToArray();
        }

        _clipLookup = mappings
            .Where(m => m != null && m.Clip != null)
            .ToDictionary(m => m.Clip, m => m);
    }
    
    public void RemoveAllEventsForClip(AnimationClip clip)
    {
        if (clip == null) return;

        if (mappings == null)
        {
            mappings = Array.Empty<ClipEventMapping>();
            return;
        }

        var newMappings = new List<ClipEventMapping>();
        foreach (var mapping in mappings)
        {
            if (mapping != null && mapping.Clip != clip)
            {
                newMappings.Add(mapping);
            }
        }

        mappings = newMappings.ToArray();

        _clipLookup = mappings
            .Where(m => m != null && m.Clip != null)
            .ToDictionary(m => m.Clip, m => m);

        _activeEvents.Remove(clip);
    }
}