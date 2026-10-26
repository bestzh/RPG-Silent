using System;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 技能槽只读接口，供 UI 订阅冷却/装配变化，不暴露释放逻辑。
    /// </summary>
    public interface IPlayerSkillReader
    {
        int SlotCount { get; }

        SkillData GetEquippedSkill(int slotIndex);

        /// <summary>冷却进度：1 = 刚进入冷却（遮罩满），0 = 可用。</summary>
        float GetCooldownNormalized(int slotIndex);

        string GetHotkeyDisplay(int slotIndex);

        event Action OnSlotsChanged;
        event Action<int> OnSlotCooldownChanged;

        /// <summary>重新推送当前槽位与冷却状态，供 UI 打开时同步。</summary>
        void Refresh();
    }
}
