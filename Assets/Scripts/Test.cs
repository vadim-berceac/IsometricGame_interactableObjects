using UnityEngine;
using Zenject;

public class Test : MonoBehaviour
{
    [Inject] private readonly AttackRangeService _attackRangeService;

    private void OnEnable()
    {
        _attackRangeService.RangeEntered += OnRangeEntered;
        _attackRangeService.RangeExited += OnRangeExited;
    }

    private void OnDisable()
    {
        _attackRangeService.RangeEntered -= OnRangeEntered;
        _attackRangeService.RangeExited -= OnRangeExited;
    }

    private void OnRangeEntered(BaseCharacter observer, BaseCharacter target, AttackRangeZone zone)
    {
        switch (zone)
        {
            case AttackRangeZone.Melee: OnMeleeAttack(observer); break;
            case AttackRangeZone.Ranged: OnRangedAttack(observer); break;
        }
    }

    private void OnRangeExited(BaseCharacter observer, BaseCharacter target, AttackRangeZone zone)
    {
        Debug.Log(observer.name + " exited " + zone + " range of " + target.name);
    }

    private void OnMeleeAttack(BaseCharacter observer) => Debug.Log(observer.name + " Melee attack triggered");
    private void OnRangedAttack(BaseCharacter observer) => Debug.Log(observer.name + " Ranged attack triggered");
}