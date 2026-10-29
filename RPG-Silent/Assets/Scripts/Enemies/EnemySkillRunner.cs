using System.Collections.Generic;
using RPGSilent.Combat;
using UnityEngine;

/// <summary>技能生命周期独立于选招；每次动作拥有独立攻击上下文与瞄准快照。</summary>
public sealed class EnemySkillRunner
{
    private readonly Transform owner;
    private readonly AttackExecutor executor;
    private readonly Dictionary<EnemySkillData, float> readyAt = new();
    private AttackContext context;
    private GameObject telegraph;
    private GameObject windup;
    private LineRenderer warningLine;
    private readonly RaycastHit[] groundHits = new RaycastHit[24];
    private float elapsed;
    private Vector3 effectPosition;
    public EnemySkillData Current { get; private set; }
    public bool IsRunning => Current != null;
    public bool HasReleased { get; private set; }
    public Material WarningMaterial { get; set; }
    float ReleaseAt => Current == null ? 0 : Mathf.Max(.1f, Current.Duration) * Mathf.Clamp01(Current.ReleaseTime);
    float ImpactAt => ReleaseAt + (Current != null && EnemyAttackGeometry.IsCircle(Current.Attack.DeliveryType) ? Mathf.Max(0, Current.Attack.AoeDelay) : 0);
    public string WarningHint => Current == null || elapsed >= ImpactAt ? "" : Current.DisplayName + $" · {Mathf.Max(0, ImpactAt - elapsed):0.0}s\n" +
        (EnemyAttackGeometry.IsCircle(Current.Attack.DeliveryType) ? "[O] 范围 · 移出标线" : EnemyAttackGeometry.IsLine(Current.Attack.DeliveryType) ? "[>] 直线 · 躲避或格挡" : "[!] 扇形 · 可精准格挡");

    public EnemySkillRunner(Transform owner, AttackExecutor executor)
    {
        this.owner = owner;
        this.executor = executor;
    }

    public bool IsReady(EnemySkillData skill) => skill != null && skill.Attack != null &&
        (!readyAt.TryGetValue(skill, out float time) || Time.time >= time);

    public void Begin(EnemySkillData skill, Transform target, float multiplier)
    {
        Cancel();
        if (skill == null || skill.Attack == null || target == null || !target.gameObject.activeInHierarchy) return;
        Current = skill;
        elapsed = 0f;
        HasReleased = false;
        readyAt[skill] = Time.time + skill.Cooldown;
        Vector3 aim = target.position + Vector3.up;
        Vector3? ground = skill.AimAtTargetGround ? target.position : null;
        effectPosition = ground ?? owner.position;
        context = executor.CreateAttack(skill.Attack, multiplier, aim, ground);
        executor.LockEnemyGeometry(context);
        effectPosition = EnemyAttackGeometry.Center(skill.Attack, owner.position, owner.rotation, ground);
        windup = SpawnEffect(skill.WindupEffect, owner.position + Vector3.up, owner.rotation, skill.Duration);
        if (skill.TelegraphMaterial != null || WarningMaterial != null)
        {
            telegraph = new GameObject("Attack warning: " + skill.DisplayName);
            telegraph.transform.position = effectPosition + Vector3.up * 0.08f;
            var line = telegraph.AddComponent<LineRenderer>();
            warningLine = line;
            line.sharedMaterial = skill.TelegraphMaterial != null ? skill.TelegraphMaterial : WarningMaterial;
            line.startColor = line.endColor = skill.TelegraphColor;
            line.startWidth = line.endWidth = 0.09f;
            line.useWorldSpace = true;
            line.loop = true;
            bool circle = EnemyAttackGeometry.IsCircle(skill.Attack.DeliveryType), corridor = EnemyAttackGeometry.IsLine(skill.Attack.DeliveryType);
            line.positionCount = corridor ? 4 : circle ? 49 : 50;
            float radius = circle ? skill.Attack.AoeRadius : skill.Attack.Radius;
            float angle = circle ? 360f : skill.Attack.Angle;
            telegraph.transform.rotation = owner.rotation;
            Vector3 center = effectPosition;
            if (corridor)
            {
                Vector3 start = context.EnemyOrigin.HasValue ? context.EnemyLaunchOrigin : owner.position + Vector3.up;
                Vector3 direction = (aim - start).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
                float length = skill.Attack.DeliveryType == AttackDeliveryType.Projectile ? Mathf.Max(0, skill.Attack.ProjectileSpeed * skill.Attack.ProjectileLifeTime) : Mathf.Max(0, skill.Attack.MaxDistance);
                float width = skill.Attack.DeliveryType == AttackDeliveryType.Projectile ? EnemyAttackGeometry.ProjectileRadius(skill.Attack) : Mathf.Max(.03f, skill.Attack.CastRadius);
                line.SetPosition(0, OnGround(start - right * width)); line.SetPosition(1, OnGround(start + right * width));
                line.SetPosition(2, OnGround(start + direction * length + right * width)); line.SetPosition(3, OnGround(start + direction * length - right * width));
            }
            else for (int i = 0; i < 49; i++)
            {
                float radians = (-angle * 0.5f + angle * i / 48f) * Mathf.Deg2Rad;
                Vector3 point = center + owner.rotation * new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * radius;
                line.SetPosition(i, OnGround(point));
            }
            if (!circle && !corridor) line.SetPosition(49, OnGround(center));
        }
    }

    public bool Tick(float deltaTime)
    {
        if (Current == null) return true;
        elapsed += deltaTime;
        if (warningLine != null)
        {
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, ImpactAt));
            Color color = Color.Lerp(Current.TelegraphColor, new Color(1f, 0.9f, 0.25f, 1f), progress);
            warningLine.startColor = warningLine.endColor = color;
            warningLine.startWidth = warningLine.endWidth = Mathf.Lerp(0.09f, 0.15f, progress);
        }
        if (!HasReleased && elapsed >= ReleaseAt)
        {
            HasReleased = true;
            if (elapsed >= ImpactAt) ClearWarning();
            var releasedSkill = Current;
            var releasedContext = context;
            executor.ReleaseAttack(context);
            // 命中回调可能导致玩家死亡/副本结算，进而禁用敌人并 Cancel 当前技能。
            if (Current != releasedSkill || context != releasedContext) return true;
            SpawnEffect(Current.ReleaseEffect, effectPosition, owner.rotation, 4f);
        }
        if (HasReleased && elapsed >= ImpactAt) ClearWarning();
        if (elapsed < Mathf.Max(Current.Duration, ImpactAt)) return false;
        Cancel();
        return true;
    }

    public void Cancel()
    {
        if (context != null) executor.EndAttack(context);
        context = null;
        Current = null;
        ClearWarning();
    }
    public void ResetCooldowns() { Cancel(); readyAt.Clear(); }

    private void ClearWarning()
    {
        if (telegraph != null) { telegraph.SetActive(false); Object.Destroy(telegraph); }
        if (windup != null) { windup.SetActive(false); Object.Destroy(windup); }
        telegraph = windup = null;
        warningLine = null;
    }

    private Vector3 OnGround(Vector3 point)
    {
        int count = Physics.RaycastNonAlloc(point + Vector3.up * 2f, Vector3.down, groundHits, 8f, ~0, QueryTriggerInteraction.Ignore);
        float closest = float.PositiveInfinity;
        Vector3 ground = point;
        for (int i = 0; i < count; i++)
        {
            var hit = groundHits[i];
            if (hit.distance >= closest || hit.normal.y < 0.35f || hit.transform.IsChildOf(owner) ||
                hit.collider.GetComponentInParent<RPGSilent.Domain.IDamageable>() != null) continue;
            closest = hit.distance;
            ground = hit.point;
        }
        return ground + Vector3.up * 0.08f;
    }

    public static GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null) return null;
        var effect = Object.Instantiate(prefab, position, rotation);
        Object.Destroy(effect, Mathf.Max(0.1f, lifetime));
        return effect;
    }
}
