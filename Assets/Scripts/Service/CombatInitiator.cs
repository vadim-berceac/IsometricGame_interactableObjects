using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

public class CombatInitiator : IInitializable, IDisposable
{
    [Inject] private readonly CombatGroupRegistry _registry;
    [Inject] private readonly ICombatResolver _resolver;
    [Inject] private readonly CombatInitiatorSettings _settings;

    private readonly Dictionary<BaseCharacter, CancellationTokenSource> _activeLoops = new();

    public event Action<BaseCharacter, BaseCharacter, InteractionOutcome> AttackResolved;

    public void Initialize()
    {
        _registry.ActiveAttackerChanged += OnActiveAttackerChanged;
    }

    public void Dispose()
    {
        _registry.ActiveAttackerChanged -= OnActiveAttackerChanged;

        foreach (var cts in _activeLoops.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _activeLoops.Clear();
    }

    private void OnActiveAttackerChanged(BaseCharacter defender, BaseCharacter activeAttacker)
    {
        StopLoopFor(defender);

        if (activeAttacker != null)
        {
            StartLoopFor(defender, activeAttacker);
        }
    }

    private void StartLoopFor(BaseCharacter defender, BaseCharacter attacker)
    {
        var cts = new CancellationTokenSource();
        _activeLoops[defender] = cts;

        AttackLoop(defender, attacker, cts.Token).Forget();
    }

    private void StopLoopFor(BaseCharacter defender)
    {
        if (_activeLoops.TryGetValue(defender, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _activeLoops.Remove(defender);
        }
    }

    private async UniTaskVoid AttackLoop(BaseCharacter defender, BaseCharacter attacker, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!attacker || !defender)
            {
                break;
            }

            var attackData = new AttackData { ClinchChance = _settings.DefaultClinchChance };
            var outcome = _resolver.Resolve(attacker.GetStats(), defender.GetStats(), attackData);

            AttackResolved?.Invoke(attacker, defender, outcome);

            var isCanceled = await UniTask
                .Delay(TimeSpan.FromSeconds(_settings.AttackInterval), cancellationToken: token)
                .SuppressCancellationThrow();

            if (isCanceled)
            {
                break;
            }
        }
    }
}