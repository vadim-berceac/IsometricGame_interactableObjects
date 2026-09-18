using UnityEngine;
using Zenject;

public enum InteractionOutcome
{
    Hit,
    Blocked,
    Parried,
    Clinch
}

public struct AttackData
{
    public float ClinchChance;
}

public interface ICombatResolver
{
    InteractionOutcome Resolve(Stats attacker, Stats defender, AttackData attackData);
}

public class CombatResolver : ICombatResolver
{
    [Inject] private readonly CombatResolverSettings _settings;

    public InteractionOutcome Resolve(Stats attacker, Stats defender, AttackData attackData)
    {
        var parryChance = Mathf.Clamp01(
            _settings.BaseParryChance +
            (defender.Mastery - attacker.Mastery) * _settings.MasteryToChance);

        var blockChance = Mathf.Clamp01(
            _settings.BaseBlockChance +
            (defender.Defence - attacker.Attack) * _settings.StatToChance);

        var clinchChance = Mathf.Clamp01(attackData.ClinchChance);

        var total = parryChance + blockChance + clinchChance;
        if (total > 1f)
        {
            var scale = 1f / total;
            parryChance *= scale;
            blockChance *= scale;
            clinchChance *= scale;
        }

        var roll = Random.value;

        if (roll < parryChance)
        {
            return InteractionOutcome.Parried;
        }

        if (roll < parryChance + blockChance)
        {
            return InteractionOutcome.Blocked;
        }

        if (roll < parryChance + blockChance + clinchChance)
        {
            return InteractionOutcome.Clinch;
        }

        return InteractionOutcome.Hit;
    }
}