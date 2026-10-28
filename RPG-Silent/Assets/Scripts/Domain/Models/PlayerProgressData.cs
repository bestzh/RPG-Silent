using System;

namespace RPGSilent.Domain
{
    // Durable progress includes the shared consumable deadline; scene state, HP and skill cooldowns remain transient.
    [Serializable]
    public sealed class PlayerProgressData
    {
        public string profileId;
        public int gold;
        public int totalExp;
        public int stance;
        public string[] skillIds;
        public string[] weaponIds;
        public string[] weaponInstanceIds;
        public int equipmentVersion;
        public int activeWeaponSlot;
        public string classId;
        public int gender;
        public SkillProgressData[] learnedSkills;
        public WeaponUpgradeData[] weaponUpgrades;
        public InventoryData inventory;
        public QuestJournalData quests = new();
        public ConsumableLoadoutData consumables = new();
    }

    [Serializable]
    public sealed class SkillProgressData { public string skillId; public int rank; }
    [Serializable]
    public sealed class WeaponUpgradeData { public string weaponId; public int level; }
}
