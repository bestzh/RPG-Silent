using System;

namespace RPGSilent.Domain
{
    // Only durable progress. Scene state, pending rewards, HP and cooldowns are excluded.
    [Serializable]
    public sealed class PlayerProgressData
    {
        public string profileId;
        public int gold;
        public int totalExp;
        public int stance;
        public string[] skillIds;
        public string[] weaponIds;
        public int activeWeaponSlot;
        public string classId;
        public int gender;
        public SkillProgressData[] learnedSkills;
        public WeaponUpgradeData[] weaponUpgrades;
    }

    [Serializable]
    public sealed class SkillProgressData { public string skillId; public int rank; }
    [Serializable]
    public sealed class WeaponUpgradeData { public string weaponId; public int level; }
}
