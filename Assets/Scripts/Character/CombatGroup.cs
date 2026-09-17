using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

public class CombatGroup : IDisposable
{
    public class Factory : PlaceholderFactory<BaseCharacter, CombatGroup> { }

    private readonly BaseCharacter _defender;
    private readonly CombatGroupSettings _settings;
    private readonly List<BaseCharacter> _queue = new();

    private CancellationTokenSource _cts;

    public BaseCharacter Defender => _defender;
    public BaseCharacter ActiveAttacker { get; private set; }
    public bool IsEmpty => _queue.Count == 0;

    public event Action<BaseCharacter, BaseCharacter> ActiveAttackerChanged;
    public event Action<BaseCharacter, BaseCharacter> AttackerJoined;
    public event Action<BaseCharacter, BaseCharacter> AttackerLeft;

    public CombatGroup(BaseCharacter defender, CombatGroupSettings settings)
    {
        _defender = defender;
        _settings = settings;
    }

    public void Enqueue(BaseCharacter attacker)
    {
        if (!attacker || _queue.Contains(attacker))
        {
            return;
        }

        _queue.Add(attacker);
        AttackerJoined?.Invoke(_defender, attacker);

        if (!ActiveAttacker)
        {
            PromoteNext();
        }
    }

    public void Dequeue(BaseCharacter attacker)
    {
        if (!_queue.Remove(attacker))
        {
            return;
        }

        AttackerLeft?.Invoke(_defender, attacker);

        if (ActiveAttacker == attacker)
        {
            StopActiveLoop();
            PromoteNext();
        }
    }

    private void PromoteNext()
    {
        ActiveAttacker = _queue.Count > 0 ? _queue[0] : null;
        ActiveAttackerChanged?.Invoke(_defender, ActiveAttacker);

        if (ActiveAttacker != null)
        {
            StartActiveLoop();
        }
    }

    private void StartActiveLoop()
    {
        _cts = new CancellationTokenSource();
        RotateAfterDelay(_cts.Token).Forget();
    }

    private void StopActiveLoop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async UniTaskVoid RotateAfterDelay(CancellationToken token)
    {
        var isCanceled = await UniTask
            .Delay(TimeSpan.FromSeconds(_settings.PlaceholderAttackDuration), cancellationToken: token)
            .SuppressCancellationThrow();

        if (isCanceled)
        {
            return;
        }

        // Активный "отработал" — в конец очереди, ход следующему.
        if (ActiveAttacker != null)
        {
            _queue.Remove(ActiveAttacker);
            _queue.Add(ActiveAttacker);
        }

        PromoteNext();
    }

    public void Dispose()
    {
        StopActiveLoop();
        _queue.Clear();
        ActiveAttacker = null;
    }
}