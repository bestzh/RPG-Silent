using System;
using System.Collections.Generic;

/// <summary>一波敌人的出手节奏。时间由调用方传入；只对仍持续申请的可攻击敌人排队。</summary>
public sealed class EnemyAttackBudget
{
    private sealed class Request
    {
        public object Owner;
        public bool Ranged;
        public float LastSeen;
    }

    private readonly EnemyGroupSettings settings;
    private readonly List<Request> waiting = new();
    private readonly Dictionary<object, bool> holders = new();
    private int burstCount;
    public int ActiveCount => holders.Count;
    public int ActiveMelee { get; private set; }
    public int ActiveRanged { get; private set; }
    public int WaitingCount => waiting.Count;
    public float NextAttackTime { get; private set; }
    public bool HasPermit(object owner) => owner != null && holders.ContainsKey(owner);

    public EnemyAttackBudget(EnemyGroupSettings settings) => this.settings = settings;

    public bool TryAcquire(object owner, bool ranged, float now)
    {
        if (owner == null) return false;
        if (holders.ContainsKey(owner)) return true;
        for (int i = waiting.Count - 1; i >= 0; i--)
            if (now - waiting[i].LastSeen > Math.Max(0.2f, settings.RequestLifetime)) waiting.RemoveAt(i);
        Request request = null;
        foreach (var item in waiting) if (ReferenceEquals(item.Owner, owner)) { request = item; break; }
        if (request == null) { request = new Request { Owner = owner, Ranged = ranged }; waiting.Add(request); }
        request.LastSeen = now;
        if (now < NextAttackTime || holders.Count >= Math.Max(1, settings.MaxAttackers) || !ChannelAvailable(ranged)) return false;
        // 不让近战队首占住远程名额；同一可用通道内仍按申请先后轮换。
        foreach (var item in waiting)
        {
            if (!ChannelAvailable(item.Ranged)) continue;
            if (!ReferenceEquals(item, request)) return false;
            break;
        }
        waiting.Remove(request);
        holders.Add(owner, ranged);
        if (ranged) ActiveRanged++; else ActiveMelee++;
        NextAttackTime = now + Math.Max(0f, settings.AttackStartInterval);
        if (++burstCount >= Math.Max(1, settings.AttacksPerBurst))
        {
            burstCount = 0;
            NextAttackTime = Math.Max(NextAttackTime, now + Math.Max(0f, settings.BurstPause));
        }
        return true;
    }

    private bool ChannelAvailable(bool ranged) => ranged ? ActiveRanged < Math.Max(1, settings.MaxRangedAttackers) :
        ActiveMelee < Math.Max(1, settings.MaxMeleeAttackers);

    public void Release(object owner, float now)
    {
        if (owner == null) return;
        for (int i = waiting.Count - 1; i >= 0; i--)
            if (ReferenceEquals(waiting[i].Owner, owner)) waiting.RemoveAt(i);
        if (!holders.TryGetValue(owner, out bool ranged)) return;
        holders.Remove(owner);
        if (ranged) ActiveRanged--; else ActiveMelee--;
        NextAttackTime = Math.Max(NextAttackTime, now + Math.Max(0f, settings.HandoffDelay));
    }

    public void Clear()
    {
        waiting.Clear(); holders.Clear();
        ActiveMelee = ActiveRanged = burstCount = 0;
        NextAttackTime = 0f;
    }
}
