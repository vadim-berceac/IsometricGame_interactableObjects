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

    public bool IsInCombat => _defender != null;
    public BaseCharacter Defender => _defender;
    public bool IsActiveAttacker => _isActiveAttacker;
    public bool IsDefending => GetDefenderActiveAttacker() != null;

    public bool IsCombatControlled => IsInCombat || IsDefending;

    private BaseCharacter GetDefenderActiveAttacker()
    {
        if (!_self || _registry == null)
        {
            return null;
        }

        var myGroup = _registry.GetGroup(_self);
        return myGroup != null ? myGroup.ActiveAttacker : null;
    }

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
        SetPlayerCombatOverride(false);
    }

    private Transform SelfTransform => _self != null ? _self.transform : _transform;

    private void OnAttackerJoined(BaseCharacter defender, BaseCharacter attacker)
    {
        if (attacker != _self)
        {
            return;
        }

        _defender = defender;
        _isActiveAttacker = _registry.GetGroup(defender)?.ActiveAttacker == _self;

        var initialOffset = SelfTransform.position - defender.Transform.position;
        initialOffset.y = 0f;
        _orbitAngle = initialOffset.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(initialOffset.z, initialOffset.x) * Mathf.Rad2Deg
            : UnityEngine.Random.Range(0f, 360f);

        _orbitDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;

        SetPlayerCombatOverride(true);
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
        SetPlayerCombatOverride(false);
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
        var selfTransform = SelfTransform;
        if (!selfTransform || !_settings)
        {
            return;
        }

        if (_defender&& !_defender)
        {
            _defender = null;
            _isActiveAttacker = false;
            _characterInput.SetMove(Vector2.zero);
            SetPlayerCombatOverride(false);
        }

        var myActiveAttacker = GetDefenderActiveAttacker();
        var isPlayer = _self && _self.CharacterType == CharacterType.Player;

        if (_defender)
        {
            if (!_defender.Transform)
            {
                return;
            }

            if (!_isActiveAttacker)
            {
                _orbitAngle += _settings.OrbitSpeedDegrees * _orbitDirection * deltaTime;
            }

            var currentForward = selfTransform.forward;
            var targetPoint = GetTargetPoint();

            var toTarget = targetPoint - selfTransform.position;
            toTarget.y = 0f;

            if (isPlayer && myActiveAttacker && myActiveAttacker.Transform)
            {
                FaceTowards(myActiveAttacker.Transform.position, deltaTime);
            }
            else
            {
                FaceTowards(_defender.Transform.position, deltaTime);
            }

            var moveDirection = toTarget;

            if (IsBlocked(toTarget, out var slid))
            {
                if (!_isActiveAttacker)
                {
                    _orbitAngle += _settings.OrbitSpeedDegrees * _orbitDirection * deltaTime;
                }

                if (slid.sqrMagnitude < 0.0001f)
                {
                    _characterInput.SetMove(Vector2.zero);
                    return;
                }

                moveDirection = slid;
            }

            if (moveDirection != toTarget && IsBlocked(moveDirection, out _))
            {
                moveDirection = -moveDirection;
            }

            var localMove = ToLocalMove(currentForward, moveDirection);
            _characterInput.SetMove(localMove);
            return;
        }

        if (myActiveAttacker && myActiveAttacker.Transform)
        {
            FaceTowards(myActiveAttacker.Transform.position, deltaTime);
        }

        if (isPlayer && _characterInput is PlayerInputHandler playerInput)
        {
            var shouldLock = IsInCombat || myActiveAttacker;
            if (playerInput.IsCombatControlled != shouldLock)
            {
                playerInput.SetCombatControlled(shouldLock);
            }
        }
    }

    private bool IsBlocked(Vector3 worldDirection, out Vector3 slideDirection)
    {
        slideDirection = worldDirection;

        if (worldDirection.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        var st = SelfTransform;
        if (!st)
        {
            return false;
        }

        var distance = worldDirection.magnitude;
        var direction = worldDirection / distance;
        var origin = st.position + Vector3.up * _settings.ObstacleCheckHeight;

        if (!Physics.SphereCast(
            origin,
            _settings.ObstacleCheckRadius,
            direction,
            out var hit,
            distance + _settings.ObstacleCheckRadius,
            _settings.ObstacleMask,
            QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (!(hit.collider|| hit.collider.GetComponentInParent<BaseCharacter>()))
        {
            return false;
        }

        var normal = hit.normal;
        normal.y = 0f;

        if (normal.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        normal.Normalize();

        var projected = direction - normal * Vector3.Dot(direction, normal);
        if (projected.sqrMagnitude > 0.0001f)
        {
            slideDirection = projected;
            return true;
        }

        var tangent = Vector3.Cross(Vector3.up, normal);
        slideDirection = tangent * (Vector3.Dot(tangent, direction) >= 0f ? 1f : -1f);
        return true;
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
        var st = SelfTransform;
        if (!st || !_settings)
        {
            return;
        }

        var direction = worldPoint - st.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        var rotSpeed = _settings.RotationSpeedDegrees > 0f ? _settings.RotationSpeedDegrees : 360f;
        var targetRotation = Quaternion.LookRotation(direction);
        st.rotation = Quaternion.Slerp(
            st.rotation,
            targetRotation,
            rotSpeed * deltaTime / 180f);
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

    private void SetPlayerCombatOverride(bool value)
    {
        if (_self && _self.CharacterType == CharacterType.Player
            && _characterInput is PlayerInputHandler playerInput)
        {
            playerInput.SetCombatControlled(value);
        }
    }
}