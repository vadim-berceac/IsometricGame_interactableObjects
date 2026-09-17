using System;
using System.Collections.Generic;
using Zenject;

public class CombatGroupRegistry : IInitializable, IDisposable
{
    [Inject] private readonly AttackRangeService _attackRangeService;
    [Inject] private readonly CombatGroup.Factory _groupFactory;

    private readonly Dictionary<BaseCharacter, CombatGroup> _groups = new();

    public event Action<BaseCharacter, BaseCharacter> ActiveAttackerChanged;
    public event Action<BaseCharacter, BaseCharacter> AttackerJoined;
    public event Action<BaseCharacter, BaseCharacter> AttackerLeft;

    public void Initialize()
    {
        _attackRangeService.RangeEntered += OnRangeEntered;
        _attackRangeService.RangeExited += OnRangeExited;
    }

    public void Dispose()
    {
        _attackRangeService.RangeEntered -= OnRangeEntered;
        _attackRangeService.RangeExited -= OnRangeExited;

        foreach (var group in _groups.Values)
        {
            group.ActiveAttackerChanged -= OnGroupActiveAttackerChanged;
            group.Dispose();
        }

        _groups.Clear();
    }

    public CombatGroup GetGroup(BaseCharacter defender)
    {
        return _groups.TryGetValue(defender, out var group) ? group : null;
    }

    private void OnRangeEntered(BaseCharacter observer, BaseCharacter target, AttackRangeZone zone)
    {
        GetOrCreateGroup(target).Enqueue(observer);
    }

    private void OnRangeExited(BaseCharacter observer, BaseCharacter target, AttackRangeZone zone)
    {
        if (_attackRangeService.GetZone(observer) != AttackRangeZone.None)
        {
            return;
        }

        if (!_groups.TryGetValue(target, out var group))
        {
            return;
        }

        group.Dequeue(observer);

        if (group.IsEmpty)
        {
            RemoveGroup(target);
        }
    }

    private CombatGroup GetOrCreateGroup(BaseCharacter defender)
    {
        if (_groups.TryGetValue(defender, out var existing))
        {
            return existing;
        }

        var group = _groupFactory.Create(defender);
        group.ActiveAttackerChanged += OnGroupActiveAttackerChanged;
        group.AttackerJoined += OnGroupAttackerJoined; 
        group.AttackerLeft += OnGroupAttackerLeft;    
        _groups[defender] = group;

        return group;
    }

    private void RemoveGroup(BaseCharacter defender)
    {
        if (!_groups.TryGetValue(defender, out var group))
        {
            return;
        }

        group.ActiveAttackerChanged -= OnGroupActiveAttackerChanged;
        group.AttackerJoined -= OnGroupAttackerJoined; 
        group.AttackerLeft -= OnGroupAttackerLeft;    
        group.Dispose();
        _groups.Remove(defender);
    }

    private void OnGroupAttackerJoined(BaseCharacter defender, BaseCharacter attacker) =>
        AttackerJoined?.Invoke(defender, attacker);

    private void OnGroupAttackerLeft(BaseCharacter defender, BaseCharacter attacker) =>
        AttackerLeft?.Invoke(defender, attacker);

    private void OnGroupActiveAttackerChanged(BaseCharacter defender, BaseCharacter activeAttacker)
    {
        ActiveAttackerChanged?.Invoke(defender, activeAttacker);
    }
}