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
        private readonly HashSet<IDamageable> classTargets = new();
        internal bool TryRegisterClassHit(IDamageable target) => classTargets.Add(target);

        public long Id { get; } = Interlocked.Increment(ref nextId);
        public AttackProfile Profile { get; }
        public AttackExecutor Owner { get; }
        public int Damage { get; private set; }
        public CombatElement Element { get; internal set; }
        public bool IsSkill { get; internal set; }
        public int Combo { get; internal set; }
        public bool IsHeavyAttack { get; internal set; }
        public bool IsEquipmentProc { get; internal set; }
        public string SourceWeaponKey { get; }
        internal Vector3? EnemyOrigin { get; private set; }
        internal Quaternion EnemyRotation { get; private set; }
        internal Vector3 EnemyLaunchOrigin { get; private set; }
        internal void LockEnemyGeometry(Vector3 origin, Quaternion rotation, Vector3 launchOrigin)
        { EnemyOrigin = origin; EnemyRotation = rotation; EnemyLaunchOrigin = launchOrigin; }
        internal bool EquipmentProcUsed { get; set; }
        internal void SetEquipmentProcDamage(int damage) { Damage = System.Math.Max(0, damage); IsEquipmentProc = true; }
        public Vector3? AimPoint { get; }
        public Vector3? AreaPoint { get; }
        public float StaggerMultiplier { get; }
        public float ProjectileSpeedMultiplier { get; }
        public float EnergyOnHit { get; }
        internal bool EnergyRestored { get; set; }
        internal int Generation { get; }
        public bool HasStarted { get; internal set; }
        public bool HasReleased { get; internal set; }
        public bool IsClosed { get; internal set; }

        internal AttackContext(AttackProfile profile, AttackExecutor owner, float damageMultiplier = 1f,
            Vector3? aimPoint = null, Vector3? areaPoint = null, float staggerMultiplier = 1f,
            float projectileSpeedMultiplier = 1f, float energyOnHit = 0f)
        {
            Profile = profile;
            Owner = owner;
            SourceWeaponKey = owner.EquipmentWeaponKey;
            Damage = Mathf.Max(0, Mathf.RoundToInt(profile.Damage * Mathf.Max(0f, damageMultiplier)));
            AimPoint = aimPoint;
            AreaPoint = areaPoint;
            StaggerMultiplier = Mathf.Max(0f, staggerMultiplier);
            ProjectileSpeedMultiplier = Mathf.Max(.1f, projectileSpeedMultiplier);
            EnergyOnHit = Mathf.Max(0f, energyOnHit);
            Generation = owner.CombatGeneration;
        }

        internal bool TryRegisterHit(IDamageable target) => hitTargets.Add(target);
    }
}
