using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class VisionSystem : IInitializable, IDisposable
{
    [Inject] private readonly SceneCharacterService _sceneCharacterService;
    [Inject] private readonly VisionSettings _visionSettings;

    private readonly Dictionary<BaseCharacter, HashSet<BaseCharacter>> _visibleTargets = new ();

    private CancellationTokenSource _cts;

    public event Action<BaseCharacter, BaseCharacter> CharacterSpotted;
    public event Action<BaseCharacter, BaseCharacter> CharacterLost;

    public void Initialize()
    {
        _cts = new CancellationTokenSource();
        UpdateLoop(_cts.Token).Forget();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public IReadOnlyCollection<BaseCharacter> GetVisibleTargets(BaseCharacter observer)
    {
        return _visibleTargets.TryGetValue(observer, out var targets)
            ? targets
            : Array.Empty<BaseCharacter>();
    }

    public bool IsVisible(BaseCharacter observer, BaseCharacter target)
    {
        return _visibleTargets.TryGetValue(observer, out var targets) && targets.Contains(target);
    }

    private async UniTaskVoid UpdateLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            UpdateVisibility();

            var isCanceled = await UniTask
                .Delay(TimeSpan.FromSeconds(_visionSettings.UpdateInterval), cancellationToken: token)
                .SuppressCancellationThrow();

            if (isCanceled)
            {
                break;
            }
        }
    }

    private void UpdateVisibility()
    {
        var characters = _sceneCharacterService.GetAllCharacters();

        foreach (var observer in characters)
        {
            if (!observer || observer.CharacterType == CharacterType.Neutral)
            {
                continue;
            }

            if (observer.CharacterType != CharacterType.Enemy
                && observer.CharacterType != CharacterType.Player
                && observer.CharacterType != CharacterType.Ally)
            {
                continue;
            }

            if (!_visibleTargets.TryGetValue(observer, out var previouslyVisible))
            {
                previouslyVisible = new HashSet<BaseCharacter>();
                _visibleTargets[observer] = previouslyVisible;
            }

            var currentlyVisible = new HashSet<BaseCharacter>();

            foreach (var target in characters)
            {
                if (!target || target == observer)
                {
                    continue;
                }

                if (!IsHostile(observer.CharacterType, target.CharacterType))
                {
                    continue;
                }

                var wasVisible = previouslyVisible.Contains(target);

                if (!IsInVisionCone(observer, target, wasVisible))
                {
                   continue;
                }
                
                currentlyVisible.Add(target);

                if (!wasVisible)
                {
                    CharacterSpotted?.Invoke(observer, target);
                }
            }

            foreach (var lost in previouslyVisible)
            {
                if (lost && !currentlyVisible.Contains(lost))
                {
                    CharacterLost?.Invoke(observer, lost);
                }
            }

            previouslyVisible.Clear();
            foreach (var visible in currentlyVisible)
            {
                previouslyVisible.Add(visible);
            }
        }
    }

    private bool IsInVisionCone(BaseCharacter observer, BaseCharacter target, bool alreadyVisible)
    {
        var eyeOffset = Vector3.up * _visionSettings.EyeHeight;
        var origin = observer.Transform.position + eyeOffset;
        var targetPoint = target.Transform.position + eyeOffset;

        var toTarget = targetPoint - origin;
        var distance = toTarget.magnitude;

        if (distance > _visionSettings.VisionRadius)
        {
            return false;
        }

        if (distance > Mathf.Epsilon && !HasLineOfSight(origin, toTarget, distance))
        {
            return false;
        }

        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        if (alreadyVisible)
        {
            return true;
        }

        if (observer.CharacterType == CharacterType.Player)
        {
            return true;
        }

        var direction = toTarget / distance;
        var angle = Vector3.Angle(observer.Transform.forward, direction);

        return angle <= _visionSettings.ViewAngleDegrees * 0.5f;
    }

    private bool IsHostile(CharacterType observer, CharacterType target)
    {
        switch (observer)
        {
            case CharacterType.Enemy:
                return target == CharacterType.Player || target == CharacterType.Ally;
            case CharacterType.Player:
                return target == CharacterType.Enemy;
            case CharacterType.Ally:
                return target == CharacterType.Enemy;
            default:
                return false;
        }
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 toTarget, float distance)
    {
        var direction = toTarget / distance;

        return !Physics.Raycast(
            origin,
            direction,
            distance,
            _visionSettings.ObstacleMask,
            QueryTriggerInteraction.Ignore);
    }
}