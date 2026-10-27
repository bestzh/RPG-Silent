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
    }
}
