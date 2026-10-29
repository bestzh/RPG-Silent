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
    private EnemyController enemyOwner;
    private GameAudioService audio;
    public void ConfigureAudio(GameAudioService service) => audio = service;
    internal string EquipmentWeaponKey => playerOwner?.Weapons?.GetInstanceId(playerOwner.Weapons.ActiveSlot) ?? "";
    private readonly RaycastHit[] aimHits = new RaycastHit[32];
    private readonly RaycastHit[] combatSightHits = new RaycastHit[64];
    internal void LockEnemyGeometry(AttackContext context)
    {
        if (enemyOwner == null || context == null || context.Owner != this) return;
        var spawn = ResolveSpawnPoint(context.Profile);
        context.LockEnemyGeometry(transform.position, transform.rotation, spawn != null ? spawn.position : transform.position + Vector3.up);
    }
    bool HasWorldSight(AttackContext context, Component target, Vector3? source)
    {
        Vector3 origin = EnemyAttackGeometry.IsCircle(context.Profile.DeliveryType)
            ? EnemyAttackGeometry.Center(context.Profile, context.EnemyOrigin.Value, context.EnemyRotation, context.AreaPoint) : context.EnemyOrigin.Value;
        origin = (source ?? origin) + Vector3.up;
        Vector3 delta = target.transform.position + Vector3.up - origin;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, combatSightHits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == combatSightHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = combatSightHits[i];
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(target.transform) || hit.collider.GetComponentInParent<IDamageable>() != null) continue;
            return false;
        }
        return true;
    }
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
        enemyOwner = GetComponent<EnemyController>();
        if (attackLoadout == null)   attackLoadout   = GetComponent<PlayerAttackLoadout>();
        if (hitboxController == null) hitboxController = GetComponentInChildren<AttackHitboxController>();
        if (aimCamera == null)        aimCamera        = Camera.main;
        selfDamageable = GetComponent<IDamageable>() ?? GetComponentInParent<IDamageable>();
    }

    public void SetCurrentComboIndex(int comboIndex) => SetNormalAttack(comboIndex, 1f, 1f, 1f, 0f);

    public void SetNormalAttack(int comboIndex, float damageMultiplier, float staggerMultiplier, float projectileSpeed, float energyOnHit)
    {
        AttackEnd();
        currentComboIndex = Mathf.Max(1, comboIndex);
        var profile = GetCurrentProfile();
        normalAttack = profile == null ? null : new AttackContext(profile, this,
            damageMultiplier * (attackLoadout != null ? attackLoadout.DamageMultiplier : 1f) * (playerOwner?.Defense?.ConsumeCounter() ?? 1f) * (playerOwner?.Progress?.SpecializationBasicDamage ?? 1f),
            staggerMultiplier: staggerMultiplier * (playerOwner?.Progress?.SpecializationBasicStagger ?? 1f), projectileSpeedMultiplier: projectileSpeed, energyOnHit: energyOnHit);
        if (normalAttack != null) normalAttack.Combo = currentComboIndex;
        if (normalAttack == null)
            Debug.LogWarning($"[AttackExecutor] 当前姿态第 {currentComboIndex} 段攻击未配置 AttackProfile。", this);
    }

    public AttackProfile GetCurrentAttackProfile() => normalAttack?.Profile ?? GetCurrentProfile();
    public void SetHeavyAttack(bool heavy) { if (normalAttack != null) normalAttack.IsHeavyAttack = heavy; }
    internal AttackContext CreateEquipmentProc(AttackContext source, int damage, float stagger)
    {
        var context = new AttackContext(source.Profile, this, 0f, staggerMultiplier: stagger);
        context.SetEquipmentProcDamage(damage); context.Element = source.Element; return context;
    }

    public AttackContext CreateAttack(AttackProfile profile) =>
        profile != null ? new AttackContext(profile, this) : null;

    public AttackContext CreateScaledAttack(AttackProfile profile, float multiplier) =>
        profile != null ? new AttackContext(profile, this, multiplier) : null;

    public AttackContext CreateAttack(AttackProfile profile, float damageMultiplier,
        Vector3 aimPoint, Vector3? areaPoint = null) =>
        profile != null ? new AttackContext(profile, this, damageMultiplier, aimPoint, areaPoint) : null;

    public void ConfigureEnemyTargeting() => enemyTargeting = true;
    public void ConfigureCompanionTargeting() => enemyTargeting = false;
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
        (playerOwner != null ? playerOwner.Audio : audio)?.Play(GameSound.Attack, transform.position);
        playerOwner?.CombatRecord.Release(context.IsSkill, context.Combo);
        if (context.Profile.DeliveryType == AttackDeliveryType.MeleeHitbox)
            StartAttack(context);
        else
            DispatchProfile(context);
    }

    public void EndAttack(AttackContext context)
    {
        // Cleanup must also work after the owner is disabled or a scene policy revokes combat.
        if (context == null || context.Owner != this || context.IsClosed) return;
        context.IsClosed = true;
        hitboxController?.DisableAttack(context);
        // 关闭释放入口和近战窗口，已发射的弹体/AOE 仍使用自己的命中记录。
    }

    private bool Owns(AttackContext context) =>
        !combatSuspended && (playerOwner == null || playerOwner.isActiveAndEnabled && playerOwner.CombatAllowed
            && !(playerOwner.Weapons != null && playerOwner.Weapons.IsBusy))
        && (enemyOwner == null || !enemyOwner.IsCompanion || enemyOwner.CompanionOwner != null && enemyOwner.CompanionOwner.CanReceiveCombatEffects)
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
                ExecuteAoeAtPoint(context, context.AreaPoint ?? (context.EnemyOrigin ?? transform.position) + (context.EnemyOrigin.HasValue ? context.EnemyRotation : transform.rotation) * profile.AoeOffset);
                break;
            case AttackDeliveryType.SelfAoe:
                ExecuteAoeAtPoint(context, (context.EnemyOrigin ?? transform.position) + profile.AoeOffset);
                break;
            case AttackDeliveryType.PersistentAoe:
                ExecutePersistentAoe(context, context.AreaPoint ?? (context.EnemyOrigin ?? transform.position) + (context.EnemyOrigin.HasValue ? context.EnemyRotation : transform.rotation) * profile.AoeOffset);
                break;
            case AttackDeliveryType.MeleeHitbox:
                StartAttack(context);
                break;
        }
    }

    // ── 伤害应用（接受接口，不依赖具体 EnemyController）──────────────────────

    public bool ApplyDamage(IDamageable target, AttackContext context,
                            Vector3 hitPoint, bool allowRepeatedDamage = false, Vector3? sourcePosition = null)
    {
        if (!Owns(context) || target == null || target is Object obj && obj == null) return false;
        if (!CanDamage(target)) return false;
        if (context.EnemyOrigin.HasValue && context.Profile.DeliveryType != AttackDeliveryType.Projectile && target is Component sightTarget &&
            !HasWorldSight(context, sightTarget, sourcePosition)) return false;
        if (!allowRepeatedDamage && !context.TryRegisterHit(target)) return false;

        AttackProfile profile = context.Profile;
        if (target is EnemyController enemy)
        {
            Vector3 origin = sourcePosition ?? transform.position;
            float stagger = context.StaggerMultiplier;
            int damage = !context.IsEquipmentProc && playerOwner?.ClassCombat != null ? playerOwner.ClassCombat.ModifyHit(enemy, context, origin, out stagger) : context.Damage;
            if (enemyOwner != null && enemyOwner.IsCompanion && enemyOwner.CompanionOwner?.EquipmentEffects != null)
                damage = enemyOwner.CompanionOwner.EquipmentEffects.CompanionDamage(damage, enemyOwner);
            int before = enemy.CurrentHealth;
            var record = playerOwner?.CombatRecord ?? enemyOwner?.CompanionOwner?.CombatRecord;
            record?.BeginHit();
            try
            {
                enemy.ReceiveHit(damage, origin, stagger);
                int actual = before - enemy.CurrentHealth;
                record?.Deal(actual);
                record?.CountHit(actual, context.IsSkill, context.IsHeavyAttack,
                    profile.DeliveryType == AttackDeliveryType.Projectile || profile.DeliveryType == AttackDeliveryType.Raycast,
                    enemyOwner != null && enemyOwner.IsCompanion, context.IsEquipmentProc);
            }
            finally { record?.EndHit(); }
            if (!context.IsEquipmentProc) playerOwner?.EquipmentEffects?.OnHit(enemy, context, before - enemy.CurrentHealth, hitPoint);
        }
        else if (target is PlayerController player)
        {
            if (!player.ReceiveAttack(context, sourcePosition ?? transform.position)) return true;
        }
        else target.TakeDamage(context.Damage);
        if (!context.EnergyRestored && context.EnergyOnHit > 0 && playerOwner != null && target is EnemyController)
        { context.EnergyRestored = true; playerOwner.CombatStats?.RecoverEnergy(context.EnergyOnHit); }
        if (!context.IsEquipmentProc && !target.IsDead && profile.StatusOnHit != null && target is Component receiver)
            receiver.GetComponent<CombatStatusController>()?.Apply(profile.StatusOnHit);

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
        Vector3 origin = context.EnemyOrigin ?? transform.position;
        Quaternion rotation = context.EnemyOrigin.HasValue ? context.EnemyRotation : transform.rotation;
        Vector3 center = origin + rotation * Vector3.forward * profile.Range;
        Collider[] hits = Physics.OverlapSphere(center, profile.Radius, profile.TargetLayers);

        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null) continue;

            if (target is not Component targetComp) continue;
            Vector3 toTarget = targetComp.transform.position - (context.EnemyOrigin.HasValue ? center : origin);
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(rotation * Vector3.forward, toTarget.normalized);
                if (angle > profile.Angle * 0.5f) continue;
            }

            ApplyDamage(target, context, hit.ClosestPoint(center));
        }
    }

    private void ExecuteRaycast(AttackContext context)
    {
        AttackProfile profile = context.Profile;
        Transform spawnPoint = ResolveSpawnPoint(profile);
        Vector3 origin    = context.EnemyOrigin.HasValue ? context.EnemyLaunchOrigin : spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
        Vector3 direction = context.AimPoint.HasValue ? (context.AimPoint.Value - origin).normalized : GetAimDirection(origin);
        direction = ApplySpread(direction, profile.SpreadAngle);

        RaycastHit hit;
        bool hasHit = profile.CastRadius > 0f
            ? Physics.SphereCast(origin, profile.CastRadius, direction, out hit, profile.MaxDistance, context.EnemyOrigin.HasValue ? ~0 : profile.TargetLayers.value)
            : Physics.Raycast(origin, direction, out hit, profile.MaxDistance, context.EnemyOrigin.HasValue ? ~0 : profile.TargetLayers.value);

        if (hasHit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if ((profile.TargetLayers.value & (1 << hit.collider.gameObject.layer)) != 0) ApplyDamage(target, context, hit.point);
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
        Vector3 origin    = context.EnemyOrigin.HasValue ? context.EnemyLaunchOrigin : spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
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
        if (context.Profile.AoeDelay > 0f) { StartCoroutine(ExecutePersistentAfterDelay(context, center)); return; }
        SpawnPersistentAoe(context, center);
    }
    private IEnumerator ExecutePersistentAfterDelay(AttackContext context, Vector3 center)
    {
        yield return new WaitForSeconds(context.Profile.AoeDelay);
        if (Owns(context)) SpawnPersistentAoe(context, center);
    }
    private void SpawnPersistentAoe(AttackContext context, Vector3 center)
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
        if (!enemyTargeting && playerOwner != null && playerOwner.TargetLock != null
            && playerOwner.TargetLock.IsLocked && playerOwner.TargetLock.HasLineOfSight)
            return (playerOwner.TargetLock.AimPoint - origin).normalized;
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
