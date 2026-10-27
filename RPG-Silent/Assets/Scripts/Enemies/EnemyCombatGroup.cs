using System.Collections.Generic;
using UnityEngine;

/// <summary>由 DungeonEncounter 持有，每波独立；不搜索全场景，也不影响未接入的小怪或 Boss。</summary>
public sealed class EnemyCombatGroup
{
    private sealed class Member
    {
        public EnemyController Enemy;
        public int Slot = -1;
        public Vector3 Goal, TargetPosition;
        public float NextPositionCheck, NextAlert;
    }
    private const int SlotCount = 12;
    private readonly List<Member> members = new();
    private readonly EnemyGroupSettings settings;
    private bool stopped;
    public EnemyAttackBudget Budget { get; }
    public int MemberCount => members.Count;
    public float PositionTolerance => Mathf.Max(0.2f, settings.PositionTolerance);

    public EnemyCombatGroup(EnemyGroupSettings settings)
    {
        this.settings = settings;
        Budget = new EnemyAttackBudget(settings);
    }

    public void Register(EnemyController enemy)
    {
        if (stopped || enemy == null || enemy.IsBoss || !enemy.isActiveAndEnabled || enemy.IsDead || Find(enemy) != null) return;
        members.Add(new Member { Enemy = enemy });
    }

    public void Remove(EnemyController enemy)
    {
        Budget.Release(enemy, Time.time);
        for (int i = members.Count - 1; i >= 0; i--)
            if (members[i].Enemy == enemy) members.RemoveAt(i);
    }

    public bool TryAttack(EnemyController enemy) => !stopped && Find(enemy) != null &&
        Budget.TryAcquire(enemy, enemy.Definition != null && enemy.Definition.Behavior == EnemyBehaviorStyle.Ranged, Time.time);
    public void ReleaseAttack(EnemyController enemy) => Budget.Release(enemy, Time.time);

    public void AlertAllies(EnemyController source)
    {
        if (stopped || !source.HasAggro || source.Target == null) return;
        var sender = Find(source);
        if (sender == null || Time.time < sender.NextAlert) return;
        // 每个成员独立限频，避免固定的首个决策者阻止队友继续传递警戒。
        sender.NextAlert = Time.time + 0.5f;
        float radiusSquared = settings.AllyAlertRadius * settings.AllyAlertRadius;
        foreach (var member in members)
            if (member.Enemy != null && (member.Enemy.transform.position - source.transform.position).sqrMagnitude <= radiusSquared)
                member.Enemy.AcceptSharedAggro(source.Target);
    }

    public bool TryGetPosition(EnemyController enemy, EnemyMotor motor, EnemyPerception perception, float radius, bool ranged, out Vector3 goal)
    {
        goal = default;
        if (stopped || !motor.CanNavigate || !perception.IsAlive) return false;
        var member = Find(enemy);
        if (member == null) return false;
        int engaged = 0;
        foreach (var other in members) if (Engaged(other)) engaged++;
        // 即使只有一名射手在战斗，未参战/被禁用的队友碰撞体仍可能挡住射线。
        if (!ranged && engaged < 2) return false;
        Vector3 target = perception.Target.position;
        if (Time.time < member.NextPositionCheck && (target - member.TargetPosition).sqrMagnitude < 1f)
        { goal = member.Goal; return member.Slot >= 0; }
        member.NextPositionCheck = Time.time + Mathf.Max(0.2f, settings.PositionRefreshInterval);
        member.TargetPosition = target;
        float bestScore = float.NegativeInfinity;
        int bestSlot = -1;
        // 保留可用的旧站位；新站位在可达范围内优先与同角色队友拉开角度。
        for (int attempt = -1; attempt < SlotCount; attempt++)
        {
            int slot = attempt < 0 ? member.Slot : attempt;
            if (slot < 0) continue;
            float separation = 180f;
            foreach (var other in members)
            {
                if (other == member || other.Slot < 0 || !Engaged(other) || IsRanged(other.Enemy) != ranged) continue;
                separation = Mathf.Min(separation, Mathf.Abs(Mathf.DeltaAngle(slot * 30f, other.Slot * 30f)));
            }
            if (separation < 60f) continue;
            Vector3 candidate = target + Quaternion.Euler(0f, slot * 30f, 0f) * Vector3.forward * radius;
            if (!motor.TryReachablePoint(candidate, out var point) ||
                Vector3.Distance(point, motor.Home) > enemy.LeashRadius ||
                !perception.HasLineOfSightFrom(point + Vector3.up, ranged)) continue;
            Vector3 radial = point - target; radial.y = 0f;
            if (Mathf.Abs(radial.magnitude - radius) > 0.8f ||
                Vector3.Angle(candidate - target, radial) > 20f) continue;
            float score = separation * 0.06f - Vector3.Distance(enemy.transform.position, point);
            if (attempt < 0 || score > bestScore)
            { bestScore = score; bestSlot = slot; goal = point; }
            if (attempt < 0) break;
        }
        member.Slot = bestSlot;
        if (bestSlot < 0) return false;
        member.Goal = goal;
        return true;
    }

    private static bool IsRanged(EnemyController enemy) => enemy.Definition != null && enemy.Definition.Behavior == EnemyBehaviorStyle.Ranged;
    private static bool Engaged(Member member) => member.Enemy != null && member.Enemy.isActiveAndEnabled &&
        !member.Enemy.IsDead && member.Enemy.HasAggro && !member.Enemy.ReturningHome;
    private Member Find(EnemyController enemy)
    {
        foreach (var member in members) if (member.Enemy == enemy) return member;
        return null;
    }

    public void Stop()
    {
        stopped = true;
        Budget.Clear(); members.Clear();
    }
}
