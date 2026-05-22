using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackExecutor : MonoBehaviour
{
    [SerializeField] private PlayerAttackLoadout attackLoadout;
    [SerializeField] private AttackHitboxController hitboxController;
    [SerializeField] private Transform defaultProjectileSpawnPoint;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private bool useDefaultMeleeFallback = true;

    private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
    private int currentComboIndex = 1;
    private AttackProfile defaultMeleeProfile;

    private void Awake()
    {
        if (attackLoadout == null)
        {
            attackLoadout = GetComponent<PlayerAttackLoadout>();
        }

        if (hitboxController == null)
        {
            hitboxController = GetComponentInChildren<AttackHitboxController>();
        }

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }
    }

    public void SetCurrentComboIndex(int comboIndex)
    {
        currentComboIndex = Mathf.Max(1, comboIndex);
        hitEnemies.Clear();
    }

    public AttackProfile GetCurrentAttackProfile()
    {
        return GetCurrentProfile();
    }

    public void AttackStart()
    {
        AttackProfile profile = GetCurrentProfile();
        if (profile == null)
        {
            WarnMissingProfile();
            return;
        }

        if (profile.DeliveryType == AttackDeliveryType.MeleeHitbox)
        {
            hitboxController?.EnableHitbox(profile, this);
        }
    }

    public void AttackRelease()
    {
        AttackProfile profile = GetCurrentProfile();
        if (profile != null && profile.DeliveryType == AttackDeliveryType.MeleeHitbox)
        {
            return;
        }

        ExecuteCurrentAttack();
    }

    public void AttackEnd()
    {
        hitboxController?.DisableAll();
    }

    public void ExecuteHit()
    {
        AttackRelease();
    }

    public void ExecuteShoot()
    {
        AttackRelease();
    }

    public void ExecuteAoe()
    {
        AttackRelease();
    }

    public void ExecuteCurrentAttack()
    {
        AttackProfile profile = GetCurrentProfile();
        if (profile == null)
        {
            WarnMissingProfile();
            return;
        }

        switch (profile.DeliveryType)
        {
            case AttackDeliveryType.MeleeArc:
                ExecuteMeleeArc(profile);
                break;
            case AttackDeliveryType.Raycast:
                ExecuteRaycast(profile);
                break;
            case AttackDeliveryType.Projectile:
                ExecuteProjectile(profile);
                break;
            case AttackDeliveryType.AoeAtPoint:
            case AttackDeliveryType.InstantAoe:
                ExecuteAoeAtPoint(profile, transform.position + transform.rotation * profile.AoeOffset);
                break;
            case AttackDeliveryType.SelfAoe:
                ExecuteAoeAtPoint(profile, transform.position + profile.AoeOffset);
                break;
            case AttackDeliveryType.PersistentAoe:
                ExecutePersistentAoe(profile, transform.position + transform.rotation * profile.AoeOffset);
                break;
            case AttackDeliveryType.MeleeHitbox:
                AttackStart();
                break;
        }
    }

    public void ApplyDamage(EnemyController enemy, AttackProfile profile, Vector3 hitPoint)
    {
        ApplyDamage(enemy, profile, hitPoint, allowRepeatedDamage: false);
    }

    public void ApplyDamage(EnemyController enemy, AttackProfile profile, Vector3 hitPoint, bool allowRepeatedDamage)
    {
        if (enemy == null || enemy.IsDead || profile == null)
        {
            return;
        }

        if (!allowRepeatedDamage && hitEnemies.Contains(enemy))
        {
            return;
        }

        if (!allowRepeatedDamage)
        {
            hitEnemies.Add(enemy);
        }

        enemy.TakeDamage(profile.Damage);

        if (profile.HitEffectPrefab != null)
        {
            Instantiate(profile.HitEffectPrefab, hitPoint, Quaternion.identity);
        }
    }

    private void ExecuteMeleeArc(AttackProfile profile)
    {
        Vector3 center = transform.position + transform.forward * profile.Range;
        Collider[] hits = Physics.OverlapSphere(center, profile.Radius, profile.TargetLayers);

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy == null)
            {
                continue;
            }

            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;

            if (toEnemy.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(transform.forward, toEnemy.normalized);
                if (angle > profile.Angle * 0.5f)
                {
                    continue;
                }
            }

            ApplyDamage(enemy, profile, hit.ClosestPoint(center));
        }
    }

    private void ExecuteRaycast(AttackProfile profile)
    {
        Transform spawnPoint = ResolveSpawnPoint(profile);
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
        Vector3 direction = GetAimDirection(origin);
        direction = ApplySpread(direction, profile.SpreadAngle);

        RaycastHit hit;
        bool hasHit = profile.CastRadius > 0f
            ? Physics.SphereCast(origin, profile.CastRadius, direction, out hit, profile.MaxDistance, profile.TargetLayers)
            : Physics.Raycast(origin, direction, out hit, profile.MaxDistance, profile.TargetLayers);

        if (hasHit)
        {
            EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();
            ApplyDamage(enemy, profile, hit.point);
        }
    }

    private void ExecuteProjectile(AttackProfile profile)
    {
        if (profile.ProjectilePrefab == null)
        {
            Debug.LogWarning($"AttackProfile {profile.name} 没有配置 ProjectilePrefab。", this);
            return;
        }

        Transform spawnPoint = ResolveSpawnPoint(profile);
        Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
        Vector3 direction = GetAimDirection(origin);
        direction = ApplySpread(direction, profile.SpreadAngle);

        GameObject projectileObject = Instantiate(profile.ProjectilePrefab, origin, Quaternion.LookRotation(direction));
        AttackProjectile projectile = projectileObject.GetComponent<AttackProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<AttackProjectile>();
        }

        projectile.Initialize(profile, this, direction);
    }

    private void ExecuteAoeAtPoint(AttackProfile profile, Vector3 center)
    {
        if (profile.AoeDelay > 0f)
        {
            StartCoroutine(ExecuteAoeAfterDelay(profile, center));
            return;
        }

        ApplyAoeDamage(profile, center);
    }

    private IEnumerator ExecuteAoeAfterDelay(AttackProfile profile, Vector3 center)
    {
        yield return new WaitForSeconds(profile.AoeDelay);
        ApplyAoeDamage(profile, center);
    }

    private void ExecutePersistentAoe(AttackProfile profile, Vector3 center)
    {
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
            areaCollider.radius = profile.AoeRadius;
            areaCollider.isTrigger = true;
        }

        AttackArea area = areaObject.GetComponent<AttackArea>();
        if (area == null)
        {
            area = areaObject.AddComponent<AttackArea>();
        }

        area.Initialize(profile, this);
    }

    private void ApplyAoeDamage(AttackProfile profile, Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, profile.AoeRadius, profile.TargetLayers);

        foreach (Collider hit in hits)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            ApplyDamage(enemy, profile, hit.ClosestPoint(center));
        }
    }

    private Transform ResolveSpawnPoint(AttackProfile profile)
    {
        if (!string.IsNullOrEmpty(profile.SpawnPointName))
        {
            Transform namedSpawnPoint = transform.Find(profile.SpawnPointName);
            if (namedSpawnPoint != null)
            {
                return namedSpawnPoint;
            }
        }

        return defaultProjectileSpawnPoint != null ? defaultProjectileSpawnPoint : transform;
    }

    private void WarnMissingProfile()
    {
        Debug.LogWarning($"当前姿态第 {currentComboIndex} 段攻击没有配置 AttackProfile。", this);
    }

    private Vector3 GetAimDirection(Vector3 origin)
    {
        if (aimCamera == null)
        {
            return transform.forward;
        }

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return (hit.point - origin).normalized;
        }

        return ray.direction.normalized;
    }

    private static Vector3 ApplySpread(Vector3 direction, float spreadAngle)
    {
        if (spreadAngle <= 0f)
        {
            return direction.normalized;
        }

        Quaternion spread = Quaternion.Euler(
            Random.Range(-spreadAngle, spreadAngle),
            Random.Range(-spreadAngle, spreadAngle),
            0f);

        return (spread * direction).normalized;
    }

    private AttackProfile GetCurrentProfile()
    {
        AttackProfile profile = attackLoadout != null ? attackLoadout.GetProfile(currentComboIndex) : null;
        if (profile != null || !useDefaultMeleeFallback)
        {
            return profile;
        }

        if (defaultMeleeProfile == null)
        {
            defaultMeleeProfile = ScriptableObject.CreateInstance<AttackProfile>();
            defaultMeleeProfile.name = "Runtime Default Melee Attack";
            defaultMeleeProfile.DeliveryType = AttackDeliveryType.MeleeArc;
            defaultMeleeProfile.Damage = 20;
            defaultMeleeProfile.Range = 1.5f;
            defaultMeleeProfile.Radius = 0.7f;
            defaultMeleeProfile.Angle = 120f;
            defaultMeleeProfile.TargetLayers = ~0;
        }

        return defaultMeleeProfile;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackLoadout == null)
        {
            attackLoadout = GetComponent<PlayerAttackLoadout>();
        }

        AttackProfile profile = Application.isPlaying
            ? GetCurrentProfile()
            : attackLoadout != null ? attackLoadout.GetProfile(currentComboIndex) : null;
        if (profile == null)
        {
            return;
        }

        Gizmos.color = Color.red;

        if (profile.DeliveryType == AttackDeliveryType.MeleeArc)
        {
            Vector3 center = transform.position + transform.forward * profile.Range;
            Vector3 leftEdge = Quaternion.Euler(0f, -profile.Angle * 0.5f, 0f) * transform.forward;
            Vector3 rightEdge = Quaternion.Euler(0f, profile.Angle * 0.5f, 0f) * transform.forward;

            Gizmos.DrawWireSphere(center, profile.Radius);
            Gizmos.DrawLine(transform.position, transform.position + leftEdge * profile.Range);
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
            Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.up;
            Vector3 direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;
            Gizmos.DrawLine(origin, origin + direction.normalized * profile.MaxDistance);
            if (profile.CastRadius > 0f)
            {
                Gizmos.DrawWireSphere(origin + direction.normalized * profile.MaxDistance, profile.CastRadius);
            }
        }
    }
}
