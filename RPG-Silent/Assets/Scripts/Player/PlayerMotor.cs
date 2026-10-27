using System;
using UnityEngine;

[Serializable]
public class PlayerMovementSettings
{
    [Min(1f)] public float Acceleration = 35f;
    [Min(1f)] public float Braking = 50f;
    [Min(1f)] public float AirAcceleration = 18f;
    [Min(0f)] public float AirBraking = 6f;
    [Range(0.02f, 0.3f)] public float GroundProbeDistance = 0.12f;
    [Range(0f, 80f)] public float MaxSlopeAngle = 55f;
    [Range(0f, 0.3f)] public float CoyoteTime = 0.1f;
    public LayerMask GroundLayers = Physics.DefaultRaycastLayers;
}

/// <summary>
/// Owns physical movement only. The player state machine supplies world-space
/// velocity intent; this class does not read devices, animate, or resolve damage.
/// </summary>
public sealed class PlayerMotor
{
    private readonly Rigidbody body;
    private readonly CapsuleCollider capsule;
    private readonly PlayerMovementSettings settings;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private float lastGroundedTime = float.NegativeInfinity;
    private float ignoreGroundUntil = float.NegativeInfinity;

    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; } = Vector3.up;

    public PlayerMotor(Rigidbody body, CapsuleCollider capsule, PlayerMovementSettings settings)
    {
        this.body = body;
        this.capsule = capsule;
        this.settings = settings;
    }

    public bool CanCoyoteJump(float now) => now >= ignoreGroundUntil
        && now - lastGroundedTime <= settings.CoyoteTime;

    public void RefreshGrounded(float now)
    {
        IsGrounded = false;
        GroundNormal = Vector3.up;
        if (now < ignoreGroundUntil || capsule == null || !capsule.enabled) return;

        Bounds bounds = capsule.bounds;
        float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.9f;
        const float lift = 0.08f;
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + lift, bounds.center.z);
        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
            lift + settings.GroundProbeDistance, settings.GroundLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        float minNormalY = Mathf.Cos(settings.MaxSlopeAngle * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.collider == null || hit.rigidbody == body
                || hit.collider.transform.IsChildOf(body.transform)
                || hit.normal.y < minNormalY || hit.distance >= nearest) continue;
            nearest = hit.distance;
            IsGrounded = true;
            GroundNormal = hit.normal;
        }

        if (IsGrounded) lastGroundedTime = now;
    }

    public void Move(Vector3 desiredVelocity, float deltaTime, bool airborne = false)
    {
        if (body.isKinematic) return;
        desiredVelocity.y = 0f;
        Vector3 velocity = body.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        bool braking = desiredVelocity.sqrMagnitude < 0.0001f;
        float rate = airborne
            ? (braking ? settings.AirBraking : settings.AirAcceleration)
            : (braking ? settings.Braking : settings.Acceleration);
        horizontal = Vector3.MoveTowards(horizontal, desiredVelocity, rate * deltaTime);
        velocity.x = horizontal.x;
        velocity.z = horizontal.z;
        // Follow walkable slopes without treating animation displacement as physics.
        if (!airborne && IsGrounded && !braking)
        {
            Vector3 slopeVelocity = Vector3.ProjectOnPlane(horizontal, GroundNormal);
            velocity = slopeVelocity.normalized * horizontal.magnitude;
        }
        body.linearVelocity = velocity;
    }

    public void SetHorizontalVelocity(Vector3 velocity)
    {
        if (!body.isKinematic)
            body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
    }

    public void Jump(float impulse, float now)
    {
        if (body.isKinematic) return;
        Vector3 velocity = body.linearVelocity;
        velocity.y = 0f;
        body.linearVelocity = velocity;
        body.AddForce(Vector3.up * impulse, ForceMode.Impulse);
        IsGrounded = false;
        lastGroundedTime = float.NegativeInfinity;
        ignoreGroundUntil = now + 0.1f;
    }

    public void ResetGrounding()
    {
        IsGrounded = false;
        lastGroundedTime = ignoreGroundUntil = float.NegativeInfinity;
    }
}
