using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CharacterPhysics : IDisposable
{
    private const string ElevatorTag = "Elevator";
    private readonly Collider _col;
    private readonly Character _character;
    private readonly Transform _transform;
    private readonly CapsuleCollider _capsule;
    private readonly LayerMask _collisionMask;
    private readonly CancellationTokenSource _cts = new();
    private readonly Collider[] _overlapBuffer; 
    private readonly GravityCollisionSettings _settings;

    private readonly bool _isValid;

    private float _verticalVelocity;
    private Collider _groundCollider;
    private bool _isGrounded;
    private Elevator _currentElevator;

    private CharacterPhysics(Collider col, Character character,
        Transform transform, GravityCollisionSettings settings,
        LayerMask? collisionMask = null)
    {
        _col = col;
        _character = character;
        _transform = transform;
        _collisionMask = collisionMask ?? Physics.AllLayers;
        _settings = settings;

        _capsule = _col as CapsuleCollider;
        _isValid = _capsule != null;
        _overlapBuffer = new Collider[_settings.OverlapBufferSize];

        if (!_isValid)
        {
            Debug.LogError("CharacterPhysics ожидает CapsuleCollider на _col — физика отключена.");
            return;
        }

        UpdateLoop(_cts.Token).Forget();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async UniTaskVoid UpdateLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
            Tick(Time.fixedDeltaTime);
        }
    }

    private void Tick(float dt)
    {
        if (!_character.IsOnLadder)
        {
            CheckGround();
        }
        else
        {
            _isGrounded = false;
            _groundCollider = null;
        }

        ApplyGravity(dt);
        MoveVertical(_verticalVelocity * dt);
    }

    private void CheckGround()
    {
        GetCapsuleWorldPoints(out var p1, out var p2, out var radius);

        var castRadius = Mathf.Max(radius - _settings.SkinWidth, 0.01f);
        var castDistance = _settings.GroundCheckDistance + _settings.SkinWidth;

        var hasHit = Physics.CapsuleCast(p1, p2, castRadius, Vector3.down, out RaycastHit hit,
            castDistance, _collisionMask, QueryTriggerInteraction.Ignore);

        if (!hasHit)
        {
            _isGrounded = false;
            _groundCollider = null;
            ClearElevatorParent();
            return;
        }

        var slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        _isGrounded = slopeAngle <= _settings.MaxSlopeAngle;
        _groundCollider = hit.collider;

        if (!_isGrounded)
        {
            ClearElevatorParent();
            return;
        }

        if (_groundCollider.CompareTag(ElevatorTag))
        {
            if (_currentElevator == null || _currentElevator.transform != _groundCollider.transform.root)
            {
                var elevator = _groundCollider.GetComponentInParent<Elevator>();
                if (elevator != null)
                {
                    ClearElevatorParent();
                    _currentElevator = elevator;
                    _transform.SetParent(elevator.transform);
                }
            }
        }
        else
        {
            ClearElevatorParent();
        }

        if (_currentElevator)
        {
            return;
        }
        var distanceToGround = hit.distance - _settings.SkinWidth;
        if (Mathf.Abs(distanceToGround) > 0.001f)
        {
            _transform.position += Vector3.down * distanceToGround;
        }
    }
    
    private void ClearElevatorParent()
    {
        if (_currentElevator == null) return;

        _transform.SetParent(null);
        _currentElevator = null;
    }

    private void ApplyGravity(float dt)
    {
        if (_character.IsOnLadder)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = 0f;
            return;
        }

        if (_isGrounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = _settings.GroundStickSpeed;
        }
        else
        {
            _verticalVelocity += _settings.Gravity * dt;
        }
    }

    private void MoveVertical(float verticalDelta)
    {
        var delta = (!_isGrounded || _character.IsOnLadder)
            ? new Vector3(0f, verticalDelta, 0f)
            : Vector3.zero;

        if (delta != Vector3.zero)
            _transform.position += delta;

        if (!_character.IsOnLadder)
            ResolveOverlaps();
    }

    private void ResolveOverlaps()
    {
        for (var iteration = 0; iteration < _settings.MaxDepenetrationIterations; iteration++)
        {
            GetCapsuleWorldPoints(out var p1, out var p2, out var radius);
            var count = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, _overlapBuffer,
                _collisionMask, QueryTriggerInteraction.Ignore);

            var corrected = false;

            for (var i = 0; i < count; i++)
            {
                var other = _overlapBuffer[i];
                if (other == _col)
                    continue;

                if (_isGrounded && other == _groundCollider)
                    continue;

                var hasPenetration = Physics.ComputePenetration(
                    _col, _transform.position, _transform.rotation,
                    other, other.transform.position, other.transform.rotation,
                    out var direction, out var distance);

                if (hasPenetration && distance > 0.0001f)
                {
                    _transform.position += direction * (distance + _settings.SkinWidth);
                    corrected = true;
                }
            }

            if (!corrected)
                break;
        }
    }
    
    private void GetCapsuleWorldPoints(out Vector3 p1, out Vector3 p2, out float radius)
    {
        var center = _transform.TransformPoint(_capsule.center);
        var scaleY = _transform.lossyScale.y;
        var scaleXZ = Mathf.Max(_transform.lossyScale.x, _transform.lossyScale.z);

        var height = Mathf.Max(_capsule.height * scaleY - _capsule.radius * 2f * scaleXZ, 0f);
        radius = _capsule.radius * scaleXZ;

        var up = _transform.up;
        p1 = center + up * (height * 0.5f);
        p2 = center - up * (height * 0.5f);
    }
}