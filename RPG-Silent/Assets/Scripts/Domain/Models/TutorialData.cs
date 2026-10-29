using System;
using System.IO;

namespace RPGSilent.Domain
{
    // Append new steps rather than reordering persisted bit positions.
    public enum TutorialStep
    {
        Move, AcceptQuest, EquipWeapon, EnterDungeon, BasicAttack, Dodge, Guard,
        ManualPickup, ReturnTown, TurnInQuest, InventorySort, LearnSkill, Enhance, WarehouseDeposit
    }

    [Serializable]
    public sealed class TutorialData
    {
        public const int StepCount = 14;
        public const int AllSteps = (1 << StepCount) - 1;
        public bool enabled;
        public int completed;
        public bool IsComplete => completed == AllSteps;
        public bool Has(TutorialStep step) => ValidStep(step) && (completed & (1 << (int)step)) != 0;
        public int CompletedCount
        {
            get { int count = 0; for (int i = 0; i < StepCount; i++) if (Has((TutorialStep)i)) count++; return count; }
        }
        public TutorialStep? NextStep
        {
            get { for (int i = 0; i < StepCount; i++) if (!Has((TutorialStep)i)) return (TutorialStep)i; return null; }
        }
        public bool Record(TutorialStep step)
        {
            if (!enabled || !ValidStep(step) || Has(step)) return false;
            completed |= 1 << (int)step; return true;
        }
        public TutorialData Copy() => new() { enabled = enabled, completed = completed };
        static bool ValidStep(TutorialStep step) => (int)step >= 0 && (int)step < StepCount;
        public static void Validate(TutorialData data)
        {
            if (data == null || data.completed < 0 || (data.completed & ~AllSteps) != 0)
                throw new InvalidDataException("新手引导记录无效，原存档已保留。");
        }
    }
}
