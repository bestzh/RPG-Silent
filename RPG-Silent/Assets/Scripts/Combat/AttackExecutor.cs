using System.Collections;
using RPGSilent.Combat;
using RPGSilent.Domain;
using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    [SerializeField] private PlayerAttackLoadout attackLoadout;
    [SerializeField] private AttackHitboxController hitboxController;
    [SerializeField] private Transform defaultProjectileSpawnPoint;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private bool useDefaultMeleeFallback = true;

    [Tooltip("命中特效实例的最大存活时间（秒），避免循环粒子在场景中累积。")]
    [SerializeField, Min(0.1f)] private float hitEffectLifetime = 5f;

    private AttackContext normalAttack;
    private int currentComboIndex = 1;
    private AttackProfile defaultMeleeProfile;
    private IDamageable selfDamageable;
    private bool combatSuspended;
    private bool enemyTargeting;
    private PlayerController playerOwner;
    private readonly RaycastHit[] aimHits = new RaycastHit[32];
    internal int CombatGeneration { get; private set; }

    // 场景边界/结算使旧弹体和延迟攻击失效，普通连击收招不影响它们。
    public void ResetCombat(bool suspended = false)
    {
        AttackEnd();
        hitboxController?.DisableAll();
        StopAllCoroutines();
        normalAttack = null;
        ++CombatGeneration;
        combatSuspended = suspended;
    }

    private void Awake()
    {
        playerOwner = GetComponent<PlayerController>();
        if (attackLoadout == null)   attackLoadout   = GetComponent<PlayerAttackLoadout>();
        if (hitboxController == null) hitboxController = GetComponentInChildren<AttackHitboxController>();
        if (aimCamera == null)        aimCamera        = Camera.main;
        selfDamageable = GetComponent<IDamageable>() ?? GetComponentInParent<IDamageable>();
    }

    public void SetCurrentComboIndex(int comboIndex)
    {
        AttackEnd();
        currentComboIndex = Mathf.Max(1, comboIndex);
        normalAttack = CreateScaledAttack(GetCurrentProfile(), attackLoadout != null ? attackLoadout.DamageMultiplier : 1f);
        if (normalAttack == null)
            Debug.LogWarning($"[AttackExecutor] 当前姿态第 {currentComboIndex} 段攻击未配置 AttackProfile。", this);
    }

    public AttackProfile GetCurrentAttackProfile() => normalAttack?.Profile ?? GetCurrentProfile();

    public AttackContext CreateAttack(AttackProfile profile) =>
        profile != null ? new AttackContext(profile, this) : null;

    public AttackContext CreateScaledAttack(AttackProfile profile, float multiplier) =>
        profile != null ? new AttackContext(profile, this, multiplier) : null;

    public AttackContext CreateAttack(AttackProfile profile, float damageMultiplier,
        Vector3 aimPoint, Vector3? areaPoint = null) =>
        profile != null ? new AttackContext(profile, this, damageMultiplier, aimPoint, areaPoint) : null;

    public void ConfigureEnemyTargeting() => enemyTargeting = true;
    public bool IsContextValid(AttackContext context) => Owns(context);

    public bool CanDamage(IDamageable target)
    {
        if (target == null || target is Object obj && obj == null || target.IsDead || IsSelfTarget(target)) return false;
        if (target is not Component component) return !enemyTargeting;
        CombatTeam other = CombatFaction.Resolve(component);
        CombatTeam own = enemyTargeting ? CombatTeam.Enemy : CombatFaction.Resolve(this);
        if (enemyTargeting) return other == CombatTeam.Player;
        return own == CombatTeam.Neutral || other == CombatTeam.Neutral || own != other;
    }

    /// <summary>按指定 Profile 立即结算（不依赖连招段数）。</summary>
    public void ExecuteProfile(AttackProfile profile)
    {
        if (profile == null)
        {
            Debug.LogWarning("[AttackExecutor] ExecuteProfile 收到空 AttackProfile。", this);
            return;
        }

        ReleaseAttack(CreateAttack(profile));
    }

    public void AttackStart()
    {
        StartAttack(normalAttack);
    }

    public void AttackRelease()
    {
        ReleaseAttack(normalAttack);
    }

    public void AttackEnd()    => EndAttack(normalAttack);
    public void ExecuteHit()   => AttackRelease();
    public void ExecuteShoot() => AttackRelease();
    public void ExecuteAoe()   => AttackRelease();

    public void ExecuteCurrentAttack() => AttackRelease();

    public void StartAttack(AttackContext context)
    {
        if (!Owns(context) || context.IsClosed || context.HasStarted) return;
        context.HasStarted = true;
        if (context.Profile.DeliveryType == AttackDeliveryType.MeleeHitbox)
            hitboxController?.EnableHitbox(context);
    }

    /// <summary>一个上下文只允许发射/结算一次，动画事件和兜底调用可以安全重入。</summary>
    public void ReleaseAttack(AttackContext context)
    {
        if (!Owns(context) || context.IsClosed || context.HasReleased) return;
        context.HasReleased = true;
        if (context.Profile.DeliveryType == AttackDeliveryType.MeleeHitbox)
            StartAttack(context);
        else
            DispatchProfile(context);
    }

    public void EndAttack(AttackContext context)
    {
        if (!Owns(context) || context.IsClosed) return;
        context.IsClosed = true;
        hitboxController?.DisableAttack(context);
        // 关闭释放入口和近战窗口，已发射的弹体/AOE 仍使用自己的命中记录。
    }

    private bool Owns(AttackContext context) =>
        !combatSuspended && (playerOwner == null || playerOwner.CombatAllowed
            && !(playerOwner.Weapons != null && playerOwner.Weapons.IsBusy))
        && context != null && context.Owner == this && context.Profile != null
        && context.Generation == CombatGeneration;

    private void DispatchProfile(AttackContext context)
    {
        AttackProfile profile = context.Profile;
        switch (profile.DeliveryType)
        {
            case AttackDeliveryType.MeleeArc:
                ExecuteMeleeArc(context);
                break;
            case AttackDeliveryType.Raycast:
                ExecuteRaycast(context);
                break;
            case AttackDeliveryType.Projectile:
                ExecuteProjectile(context);
                break;
            case AttackDeliveryType.AoeAtPoint:
            case AttackDeliveryType.InstantAoe:
                ExecuteAoeAtPoint(context, context.AreaPoint ?? transform.position + transform.rotation * profile.AoeOffset);
                break;
            case AttackDeliveryType.SelfAoe:
                ExecuteAoeAtPoint(context, transform.position + profile.AoeOffset);
                break;
            case AttackDeliveryType.PersistentAoe:
                ExecutePersistentAoe(context, context.AreaPoint ?? transform.position + transform.rotation * profile.AoeOffset);
                break;
            case AttackDeliveryType.MeleeHitbox:
                StartAttack(context);
                break;
        }
    }

    // ── 伤害应用（接受接口，不依赖具体 EnemyController）──────────────────────

    public bool ApplyDamage(IDamageable target, AttackContext context,
                            Vector3 hitPoint, bool allowRepeatedDamage = false)
    {
        if (!Owns(context) || target == null || target is Object obj && obj == null) return false;
        if (!CanDamage(target)) return false;
        if (!allowRepeatedDamage && !context.TryRegisterHit(target)) return false;

        AttackProfile profile = context.Profile;
        target.TakeDamage(context.Damage);

        if (profile.HitEffectPrefab != null)
        {
            GameObject hitEffect = Instantiate(profile.HitEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(hitEffect, Mathf.Max(0.1f, hitEffectLifetime));
        }
        return true;
    }

    /// <summary>忽略自身与同层级子物体，避免技能/AOE 误伤施法者。</summary>
    private bool IsSelfTarget(IDamageable target)
    {
        if (selfDamageable != null && ReferenceEquals(target, selfDamageable))
            return true;

        if (target is not Component targetComp) return false;

        Transform targetRoot = targetComp.transform.root;
        return targetRoot == transform.root;
    }

    // ── 各攻击执行实现 ──────────────────────────────────────────────────────

    private void ExecuteMeleeArc(AttackContext context)
    {
        AttackProfile profile = context.Profile;
        Vector3 center = transform.position + transform.forward * profile.Range;
        Collider[] hits = Physics.OverlapSphere(center, profile.Radius, profile.TargetLayers);

        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null) continue;

            if (target is not Component targetComp) continue;
            Vector3 toTarget = targetComp.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(transform.forward, toTarget.normalized);
                if (angle > profile.Angle * 0.5f) continue;
            }

            ApplyDamage(target, context, hit.ClosestPoint(center));
        }
    }

    private void ExecuteRaycast(AttackContext context)
    {
        AttackProfile profile = context.Profile;
        Transform spawnPoint = ResolveSpawnPoint(profile);
        Vector3 origin    = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
        Vector3 direction = context.AimPoint.HasValue ? (context.AimPoint.Value - origin).normalized : GetAimDirection(origin);
        direction = ApplySpread(direction, profile.SpreadAngle);

        RaycastHit hit;
        bool hasHit = profile.CastRadius > 0f
            ? Physics.SphereCast(origin, profile.CastRadius, direction, out hit, profile.MaxDistance, profile.TargetLayers)
            : Physics.Raycast(origin, direction, out hit, profile.MaxDistance, profile.TargetLayers);

        if (hasHit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            ApplyDamage(target, context, hit.point);
        }
    }

    private void ExecuteProjectile(AttackContext context)
    {
        AttackProfile profile = context.Profile;
        if (profile.ProjectilePrefab == null)
        {
            Debug.LogWarning($"[AttackExecutor] AttackProfile {profile.name} 未配置 ProjectilePrefab。", this);
            return;
        }

        Transform spawnPoint = ResolveSpawnPoint(profile);
        Vector3 origin    = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
        Vector3 direction = context.AimPoint.HasValue ? (context.AimPoint.Value - origin).normalized : GetAimDirection(origin);
        direction = ApplySpread(direction, profile.SpreadAngle);

        GameObject projectileObject = Instantiate(
            profile.ProjectilePrefab, origin, Quaternion.LookRotation(direction));

        AttackProjectile projectile = projectileObject.GetComponent<AttackProjectile>()
            ?? projectileObject.AddComponent<AttackProjectile>();

        projectile.Initialize(context, direction);
    }

    private void ExecuteAoeAtPoint(AttackContext context, Vector3 center)
    {
        if (context.Profile.AoeDelay > 0f) { StartCoroutine(ExecuteAoeAfterDelay(context, center)); return; }
        ApplyAoeDamage(context, center);
    }

    private IEnumerator ExecuteAoeAfterDelay(AttackContext context, Vector3 center)
    {
        yield return new WaitForSeconds(context.Profile.AoeDelay);
        ApplyAoeDamage(context, center);
    }

    private void ExecutePersistentAoe(AttackContext context, Vector3 center)
    {
        AttackProfile profile = context.Profile;
        GameObject areaObject;
        if (profile.AreaPrefab != null)
        {
            areaObject = Instantiate(profile.AreaPrefab, center, Quaternion.identity);
        }
        else
        {
            areaObject = new GameObject($"{profile.name}_Area");
            areaObject.transform.position = center;
            SphereCollider areaCollider = areaObject.AddComponent<SphereCollider>();
            areaCollider.radius    = profile.AoeRadius;
            areaCollider.isTrigger = true;
        }

        AttackArea area = areaObject.GetComponent<AttackArea>()
            ?? areaObject.AddComponent<AttackArea>();
        area.Initialize(context);
    }

    private void ApplyAoeDamage(AttackContext context, Vector3 center)
    {
        AttackProfile profile = context.Profile;
        Collider[] hits = Physics.OverlapSphere(center, profile.AoeRadius, profile.TargetLayers);
        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null) continue;
            ApplyDamage(target, context, hit.ClosestPoint(center));
        }
    }

    private Transform ResolveSpawnPoint(AttackProfile profile)
    {
        if (!string.IsNullOrEmpty(profile.SpawnPointName))
        {
            Transform named = transform.Find(profile.SpawnPointName);
            if (named != null) return named;
        }
        return defaultProjectileSpawnPoint != null ? defaultProjectileSpawnPoint : transform;
    }

    private Vector3 GetAimDirection(Vector3 origin)
    {
        if (enemyTargeting || aimCamera == null) return transform.forward;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int count = Physics.RaycastNonAlloc(ray, aimHits, 1000f, ~0, QueryTriggerInteraction.Ignore);
        float distance = float.PositiveInfinity;
        Vector3 aimPoint = ray.GetPoint(100f);
        for (int i = 0; i < count; i++)
        {
            if (aimHits[i].transform.IsChildOf(transform) || aimHits[i].distance >= distance) continue;
            distance = aimHits[i].distance;
            aimPoint = aimHits[i].point;
        }
        return (aimPoint - origin).normalized;
    }

    private static Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        if (spreadAngle <= 0f) return direction.normalized;

        Quaternion spread = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f);
        return (spread * direction).normalized;
    }

    private AttackProfile GetCurrentProfile()
    {
        AttackProfile profile = attackLoadout != null ? attackLoadout.GetProfile(currentComboIndex) : null;
        if (profile != null || !useDefaultMeleeFallback) return profile;

        if (defaultMeleeProfile == null)
        {
            defaultMeleeProfile = ScriptableObject.CreateInstance<AttackProfile>();
            defaultMeleeProfile.name         = "Runtime Default Melee Attack";
            defaultMeleeProfile.DeliveryType  = AttackDeliveryType.MeleeArc;
            defaultMeleeProfile.Damage        = 20;
            defaultMeleeProfile.Range         = 1.5f;
            defaultMeleeProfile.Radius        = 0.7f;
            defaultMeleeProfile.Angle         = 120f;
            defaultMeleeProfile.TargetLayers  = ~0;
        }
        return defaultMeleeProfile;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackLoadout == null) attackLoadout = GetComponent<PlayerAttackLoadout>();

        AttackProfile profile = Application.isPlaying
            ? GetCurrentProfile()
            : attackLoadout != null ? attackLoadout.GetProfile(currentComboIndex) : null;
        if (profile == null) return;

        Gizmos.color = Color.red;

        if (profile.DeliveryType == AttackDeliveryType.MeleeArc)
        {
            Vector3 center     = transform.position + transform.forward * profile.Range;
            Vector3 leftEdge   = Quaternion.Euler(0f, -profile.Angle * 0.5f, 0f) * transform.forward;
            Vector3 rightEdge  = Quaternion.Euler(0f,  profile.Angle * 0.5f, 0f) * transform.forward;
            Gizmos.DrawWireSphere(center, profile.Radius);
            Gizmos.DrawLine(transform.position, transform.position + leftEdge  * profile.Range);
            Gizmos.DrawLine(transform.position, transform.position + rightEdge * profile.Range);
            Gizmos.DrawLine(transform.position, center);
        }
        else if (profile.DeliveryType == AttackDeliveryType.SelfAoe
              || profile.DeliveryType == AttackDeliveryType.AoeAtPoint
              || profile.DeliveryType == AttackDeliveryType.InstantAoe
              || profile.DeliveryType == AttackDeliveryType.PersistentAoe)
        {
            Vector3 center = profile.DeliveryType == AttackDeliveryType.SelfAoe
                ? transform.position + profile.AoeOffset
                : transform.position + transform.rotation * profile.AoeOffset;
            Gizmos.DrawWireSphere(center, profile.AoeRadius);
        }
        else if (profile.DeliveryType == AttackDeliveryType.Raycast)
        {
            Transform spawnPoint = ResolveSpawnPoint(profile);
            Vector3 origin    = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
            Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;
            Gizmos.DrawLine(origin, origin + direction.normalized * profile.MaxDistance);
            if (profile.CastRadius > 0f)
                Gizmos.DrawWireSphere(origin + direction.normalized * profile.MaxDistance, profile.CastRadius);
        }
    }
}
