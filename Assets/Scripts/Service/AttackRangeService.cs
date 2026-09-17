using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public enum AttackRangeZone
{
    None = 0,
    Ranged,
    Melee
}

public class AttackRangeService : IInitializable, IDisposable
{
    [Inject] private readonly AttackRangeSettings _settings;
    [Inject] private readonly VisionSystem _visionSystem;

    private class TrackedPair
    {
        public BaseCharacter Target;
        public AttackRangeZone Zone;
        public CancellationTokenSource Cts;
    }

    private readonly Dictionary<BaseCharacter, TrackedPair> _tracked = new();

    public event Action<BaseCharacter, BaseCharacter, AttackRangeZone> RangeEntered;
    public event Action<BaseCharacter, BaseCharacter, AttackRangeZone> RangeExited;

    private AttackRangeService()
    {
        Debug.Log("[AttackRangeService]: AttackRangeService created");
    }

    public void Initialize()
    {
        _visionSystem.CharacterSpotted += OnCharacterSpotted;
        _visionSystem.CharacterLost += OnCharacterLost;
    }

    public void Dispose()
    {
        _visionSystem.CharacterSpotted -= OnCharacterSpotted;
        _visionSystem.CharacterLost -= OnCharacterLost;

        foreach (var pair in _tracked.Values)
        {
            pair.Cts?.Cancel();
            pair.Cts?.Dispose();
        }

        _tracked.Clear();
    }

    private void OnCharacterSpotted(BaseCharacter observer, BaseCharacter target)
    {
        Debug.Log($"[AttackRange] OnCharacterSpotted: {observer.name} -> {target.name}, alreadyTracked: {_tracked.ContainsKey(observer)}");

        if (_tracked.ContainsKey(observer))
        {
            return;
        }

        Track(observer, target);
    }

    private void OnCharacterLost(BaseCharacter observer, BaseCharacter target)
    {
        Untrack(observer);
    }

    public void Track(BaseCharacter observer, BaseCharacter target)
    {
        if (!observer || !target)
        {
            return;
        }

        if (_tracked.TryGetValue(observer, out var existing))
        {
            if (existing.Target == target)
            {
                return;
            }

            StopTracking(observer, existing);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(observer.GetCancellationTokenOnDestroy());
        var pair = new TrackedPair { Target = target, Zone = AttackRangeZone.None, Cts = cts };
        _tracked[observer] = pair;

        UpdateLoop(observer, pair, cts.Token).Forget();
    }

    public void Untrack(BaseCharacter observer)
    {
        if (_tracked.TryGetValue(observer, out var pair))
        {
            StopTracking(observer, pair);
            _tracked.Remove(observer);
        }
    }

    public AttackRangeZone GetZone(BaseCharacter observer)
    {
        return _tracked.TryGetValue(observer, out var pair) ? pair.Zone : AttackRangeZone.None;
    }

    private void StopTracking(BaseCharacter observer, TrackedPair pair)
    {
        pair.Cts?.Cancel();
        pair.Cts?.Dispose();

        if (pair.Zone != AttackRangeZone.None)
        {
            RangeExited?.Invoke(observer, pair.Target, pair.Zone);
        }
    }

    private async UniTaskVoid UpdateLoop(BaseCharacter observer, TrackedPair pair, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!observer || !pair.Target)
            {
                Untrack(observer);
                break;
            }

            var distance = Vector3.Distance(observer.Transform.position, pair.Target.Transform.position);
            var newZone = EvaluateZone(pair.Zone, distance);

            if (newZone != pair.Zone)
            {
                var oldZone = pair.Zone;
                pair.Zone = newZone;

                if (oldZone != AttackRangeZone.None)
                {
                    RangeExited?.Invoke(observer, pair.Target, oldZone);
                }

                if (newZone != AttackRangeZone.None)
                {
                    RangeEntered?.Invoke(observer, pair.Target, newZone);
                }
            }

            var isCanceled = await UniTask
                .Delay(TimeSpan.FromSeconds(_settings.UpdateInterval), cancellationToken: token)
                .SuppressCancellationThrow();

            if (isCanceled)
            {
                break;
            }
        }
    }

    private AttackRangeZone EvaluateZone(AttackRangeZone current, float distance)
    {
        switch (current)
        {
            case AttackRangeZone.Melee:
                if (distance <= _settings.MeleeExitDistance)
                {
                    return AttackRangeZone.Melee;
                }
                return distance <= _settings.RangedExitDistance
                    ? AttackRangeZone.Ranged
                    : AttackRangeZone.None;

            case AttackRangeZone.Ranged:
                if (distance <= _settings.MeleeEnterDistance)
                {
                    return AttackRangeZone.Melee;
                }
                return distance <= _settings.RangedExitDistance
                    ? AttackRangeZone.Ranged
                    : AttackRangeZone.None;

            default:
                if (distance <= _settings.MeleeEnterDistance)
                {
                    return AttackRangeZone.Melee;
                }
                return distance <= _settings.RangedEnterDistance
                    ? AttackRangeZone.Ranged
                    : AttackRangeZone.None;
        }
    }
}