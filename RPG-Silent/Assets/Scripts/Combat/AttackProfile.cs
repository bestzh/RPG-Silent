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
    [Header("基础")]
    public AttackDeliveryType DeliveryType = AttackDeliveryType.MeleeArc;
    public int Damage = 20;
    public LayerMask TargetLayers = ~0;

    [Header("近战扇形")]
    public float Range = 1.5f;
    public float Radius = 0.7f;
    [Range(0f, 180f)] public float Angle = 120f;

    [Header("近战 Hitbox")]
    public string HitboxName;
    public string HitboxGroup;

    [Header("射线")]
    public float MaxDistance = 30f;
    [Range(0f, 45f)] public float SpreadAngle = 0f;
    public float CastRadius = 0f;

    [Header("投射物")]
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

    [Header("特效")]
    public GameObject HitEffectPrefab;
}
