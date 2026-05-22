using UnityEngine;

public enum AttackDeliveryType
{
    MeleeArc = 0,
    Raycast = 1,
    Projectile = 2,
    AoeAtPoint = 3,
    SelfAoe = 4,
    MeleeHitbox = 5,
    InstantAoe = 6,
    PersistentAoe = 7
}

[CreateAssetMenu(fileName = "AttackProfile", menuName = "Combat/Attack Profile")]
public class AttackProfile : ScriptableObject
{
    [Header("Basic")]
    public AttackDeliveryType DeliveryType = AttackDeliveryType.MeleeArc;
    public int Damage = 20;
    public LayerMask TargetLayers = ~0;

    [Header("Attack Timing")]
    [Range(0f, 1f)] public float StartTime = 0.25f;
    [Range(0f, 1f)] public float ReleaseTime = 0.35f;
    [Range(0f, 1f)] public float EndTime = 0.6f;

    [Header("Melee Arc")]
    public float Range = 1.5f;
    public float Radius = 0.7f;
    [Range(0f, 180f)] public float Angle = 120f;

    [Header("Melee Hitbox")]
    public string HitboxName;
    public string HitboxGroup;

    [Header("Raycast")]
    public float MaxDistance = 30f;
    [Range(0f, 45f)] public float SpreadAngle = 0f;
    public float CastRadius = 0f;

    [Header("Projectile")]
    public GameObject ProjectilePrefab;
    public string SpawnPointName;
    public float ProjectileSpeed = 20f;
    public float ProjectileLifeTime = 5f;

    [Header("AOE")]
    public float AoeRadius = 3f;
    public Vector3 AoeOffset;
    public float AoeDelay = 0f;
    public GameObject AreaPrefab;
    public float AreaDuration = 5f;
    public float TickInterval = 1f;

    [Header("Effects")]
    public GameObject HitEffectPrefab;
}
