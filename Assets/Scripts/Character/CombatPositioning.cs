using System;
using UnityEngine;
using Zenject;

public class CombatPositioning : IInitializable, IDisposable
{
    [Inject] private readonly CombatGroupRegistry _registry;
    [Inject] private readonly Character _self;
    [Inject] private readonly ICharacterInput _characterInput;
    [Inject] private readonly CombatPositioningSettings _settings;
    [Inject] private readonly Transform _transform;

    private BaseCharacter _defender;
    private bool _isActiveAttacker;
    private float _orbitAngle;
    private float _orbitDirection;

    public void Initialize()
    {
        _registry.AttackerJoined += OnAttackerJoined;
        _registry.AttackerLeft += OnAttackerLeft;
        _registry.ActiveAttackerChanged += OnActiveAttackerChanged;
    }

    public void Dispose()
    {
        _registry.AttackerJoined -= OnAttackerJoined;
        _registry.AttackerLeft -= OnAttackerLeft;
        _registry.ActiveAttackerChanged -= OnActiveAttackerChanged;
    }

    private void OnAttackerJoined(BaseCharacter defender, BaseCharacter attacker)
    {
        if (attacker != _self)
        {
            return;
        }

        _defender = defender;
        _isActiveAttacker = _registry.GetGroup(defender)?.ActiveAttacker == _self;

        // Стартовый угол берём из текущего положения относительно дефендера,
        // чтобы не было "прыжка" в момент присоединения к группе.
        var initialOffset = _transform.position - defender.Transform.position;
        initialOffset.y = 0f;
        _orbitAngle = initialOffset.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(initialOffset.z, initialOffset.x) * Mathf.Rad2Deg
            : UnityEngine.Random.Range(0f, 360f);

        // Случайное направление обхода, чтобы разные атакующие не двигались синхронно.
        _orbitDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
    }

    private void OnAttackerLeft(BaseCharacter defender, BaseCharacter attacker)
    {
        if (attacker != _self)
        {
            return;
        }

        _defender = null;
        _isActiveAttacker = false;
        _characterInput.SetMove(Vector2.zero);
    }

    private void OnActiveAttackerChanged(BaseCharacter defender, BaseCharacter activeAttacker)
    {
        if (defender != _defender)
        {
            return;
        }

        _isActiveAttacker = activeAttacker == _self;
    }

    public void Tick(float deltaTime)
    {
        if (!_defender)
        {
            return;
        }

        // Обходим дефендера по кругу, пока не наш ход атаковать.
        if (!_isActiveAttacker)
        {
            _orbitAngle += _settings.OrbitSpeedDegrees * _orbitDirection * deltaTime;
        }

        var currentForward = _transform.forward;
        var targetPoint = GetTargetPoint();

        var toTarget = targetPoint - _transform.position;
        toTarget.y = 0f;

        FaceTowards(_defender.Transform.position, deltaTime);

        var localMove = ToLocalMove(currentForward, toTarget);
        _characterInput.SetMove(localMove);
    }

    private Vector3 GetTargetPoint()
    {
        var distance = _isActiveAttacker ? _settings.MeleeStopDistance : _settings.HoldingDistance;
        var radians = _orbitAngle * Mathf.Deg2Rad;
        var offset = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * distance;

        return _defender.Transform.position + offset;
    }

    private void FaceTowards(Vector3 worldPoint, float deltaTime)
    {
        var direction = worldPoint - _transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(direction);
        _transform.rotation = Quaternion.Slerp(
            _transform.rotation,
            targetRotation,
            _settings.RotationSpeedDegrees * deltaTime / 180f);
    }

    private Vector2 ToLocalMove(Vector3 forward, Vector3 worldDirection)
    {
        if (worldDirection.sqrMagnitude < 0.01f)
        {
            return Vector2.zero;
        }

        var local = Quaternion.Inverse(Quaternion.LookRotation(forward)) * worldDirection.normalized;
        return new Vector2(local.x, local.z);
    }
}