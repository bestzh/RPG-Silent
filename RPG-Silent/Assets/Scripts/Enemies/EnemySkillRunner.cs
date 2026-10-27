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
        Current = skill;
        elapsed = 0f;
        HasReleased = false;
        readyAt[skill] = Time.time + skill.Cooldown;
        Vector3 aim = target.position + Vector3.up;
        Vector3? ground = skill.AimAtTargetGround ? target.position : null;
        effectPosition = ground ?? owner.position;
        context = executor.CreateAttack(skill.Attack, multiplier, aim, ground);
        windup = SpawnEffect(skill.WindupEffect, owner.position + Vector3.up, owner.rotation, skill.Duration);
        if (skill.TelegraphMaterial != null)
        {
            telegraph = new GameObject("Attack warning: " + skill.DisplayName);
            telegraph.transform.position = effectPosition + Vector3.up * 0.08f;
            var line = telegraph.AddComponent<LineRenderer>();
            warningLine = line;
            line.sharedMaterial = skill.TelegraphMaterial;
            line.startColor = line.endColor = skill.TelegraphColor;
            line.startWidth = line.endWidth = 0.09f;
            line.useWorldSpace = true;
            line.loop = true;
            bool circle = skill.Attack.DeliveryType == AttackDeliveryType.SelfAoe || skill.AimAtTargetGround;
            line.positionCount = circle ? 49 : 50;
            float radius = circle ? skill.Attack.AoeRadius : skill.Attack.Radius;
            float angle = circle ? 360f : skill.Attack.Angle;
            telegraph.transform.rotation = owner.rotation;
            Vector3 center = circle ? effectPosition : owner.position + owner.forward * skill.Attack.Range;
            for (int i = 0; i < 49; i++)
            {
                float radians = (-angle * 0.5f + angle * i / 48f) * Mathf.Deg2Rad;
                Vector3 point = center + owner.rotation * new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * radius;
                line.SetPosition(i, OnGround(point));
            }
            if (!circle) line.SetPosition(49, OnGround(center));
        }
    }

    public bool Tick(float deltaTime)
    {
        if (Current == null) return true;
        elapsed += deltaTime;
        if (warningLine != null)
        {
            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, Current.Duration * Current.ReleaseTime));
            Color color = Color.Lerp(Current.TelegraphColor, new Color(1f, 0.9f, 0.25f, 1f), progress);
            warningLine.startColor = warningLine.endColor = color;
            warningLine.startWidth = warningLine.endWidth = Mathf.Lerp(0.09f, 0.15f, progress);
        }
        if (!HasReleased && elapsed >= Current.Duration * Current.ReleaseTime)
        {
            HasReleased = true;
            ClearWarning();
            var releasedSkill = Current;
            var releasedContext = context;
            executor.ReleaseAttack(context);
            // 命中回调可能导致玩家死亡/副本结算，进而禁用敌人并 Cancel 当前技能。
            if (Current != releasedSkill || context != releasedContext) return true;
            SpawnEffect(Current.ReleaseEffect, effectPosition, owner.rotation, 4f);
        }
        if (elapsed < Current.Duration) return false;
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

    private void ClearWarning()
    {
        if (telegraph != null) Object.Destroy(telegraph);
        if (windup != null) Object.Destroy(windup);
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
