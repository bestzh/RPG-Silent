using System.Collections.Generic;
using System.Threading;
using RPGSilent.Domain;
using UnityEngine;

namespace RPGSilent.Combat
{
    /// <summary>一次攻击的结算状态；已发射的弹体和延迟效果持有它，不受下一招影响。</summary>
    public sealed class AttackContext
    {
        private static long nextId;
        private readonly HashSet<IDamageable> hitTargets = new();

        public long Id { get; } = Interlocked.Increment(ref nextId);
        public AttackProfile Profile { get; }
        public AttackExecutor Owner { get; }
        public int Damage { get; }
        public Vector3? AimPoint { get; }
        public Vector3? AreaPoint { get; }
        internal int Generation { get; }
        public bool HasStarted { get; internal set; }
        public bool HasReleased { get; internal set; }
        public bool IsClosed { get; internal set; }

        internal AttackContext(AttackProfile profile, AttackExecutor owner, float damageMultiplier = 1f,
            Vector3? aimPoint = null, Vector3? areaPoint = null)
        {
            Profile = profile;
            Owner = owner;
            Damage = Mathf.Max(0, Mathf.RoundToInt(profile.Damage * Mathf.Max(0f, damageMultiplier)));
            AimPoint = aimPoint;
            AreaPoint = areaPoint;
            Generation = owner.CombatGeneration;
        }

        internal bool TryRegisterHit(IDamageable target) => hitTargets.Add(target);
    }
}
