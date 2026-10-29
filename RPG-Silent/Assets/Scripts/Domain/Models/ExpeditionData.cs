using System;
using System.IO;

namespace RPGSilent.Domain
{
    public enum ExpeditionEventKind { Escort, Hold, Pursuit }
    public enum ExpeditionEventStatus { None, Available, Active, Completed, Failed, Abandoned }

    /// <summary>One character's latest expedition. Attempts reset the scene, never the reward receipt.</summary>
    [Serializable]
    public sealed class ExpeditionData
    {
        public int rulesVersion = 1;
        public string id = "";
        public int sequence, dungeonId, difficulty, seed, attempt;
        public ExpeditionEventStatus status;
        public bool rewardClaimed;
        public bool HasValue => id.Length > 0;
        public ExpeditionData Copy() => (ExpeditionData)MemberwiseClone();
        public bool SamePlan(ExpeditionData other) => other != null && id == other.id && sequence == other.sequence &&
            rulesVersion == other.rulesVersion && seed == other.seed && dungeonId == other.dungeonId && difficulty == other.difficulty;
        public static void Validate(ExpeditionData data)
        {
            if (data == null || data.rulesVersion != 1 || data.id == null || !Enum.IsDefined(typeof(ExpeditionEventStatus), data.status))
                throw new InvalidDataException("远征记录或规则版本无效。");
            if (data.id.Length == 0)
            {
                if (data.sequence != 0 || data.dungeonId != 0 || data.difficulty != 0 || data.seed != 0 || data.attempt != 0 || data.status != ExpeditionEventStatus.None || data.rewardClaimed)
                    throw new InvalidDataException("空远征含有残留记录。");
                return;
            }
            if (!Guid.TryParseExact(data.id, "N", out _) || data.sequence < 1 || (data.dungeonId != 1001 && data.dungeonId != 1002) ||
                data.difficulty < 1 || data.difficulty > 5 || data.seed < 1 || data.attempt < 1 || data.attempt > 1000000 ||
                data.status == ExpeditionEventStatus.None || data.status == ExpeditionEventStatus.Completed && !data.rewardClaimed)
                throw new InvalidDataException("远征种子、次数或奖励记录无效。");
        }
    }

    /// <summary>Versioned integer PRNG: independent of Unity Random, frame order and framework Random implementations.</summary>
    public sealed class ExpeditionPlan
    {
        public ExpeditionEventKind Event { get; }
        public int Formation { get; }
        public int TraitRotation { get; }
        public int OptionalNodes { get; }
        public int Reward { get; }
        public string EventLabel => Event == ExpeditionEventKind.Escort ? "护送信标" : Event == ExpeditionEventKind.Hold ? "守卫据点" : "追击信使";
        public string FormationLabel => Formation == 0 ? "原有阵型" : Formation == 1 ? "近远轮换" : "近战前锋";
        public string EventRules => Event == ExpeditionEventKind.Escort ? "120秒内护送信标抵达终点。保持6米内；敌人进入2.5米时暂停移动，清除后继续。" :
            Event == ExpeditionEventKind.Hold ? "90秒内在3米圈内累计守卫25秒，并清除两批守卫。离圈或敌人进圈时停止计时。" :
            "60秒内击败沿标线绕行三圈的精英信使；到达终点即逃脱。受击可打断移动，减速效果有效。";
        public string NodesLabel => (OptionalNodes & 1) != 0 ? ((OptionalNodes & 2) != 0 ? "补给箱、精英伏击" : "补给箱") : "精英伏击";
        public string TraitLabel => TraitRotation == 0 ? "护盾 / 治疗" : TraitRotation == 1 ? "狂暴 / 护盾" : "治疗 / 狂暴";
        public ExpeditionPlan(ExpeditionData data)
        {
            if (data == null || data.rulesVersion != 1 || data.seed < 1 || data.difficulty < 1 || data.difficulty > 5 || (data.dungeonId != 1001 && data.dungeonId != 1002))
                throw new ArgumentException("远征规则不可生成。", nameof(data));
            uint state = unchecked((uint)data.seed ^ (uint)data.dungeonId * 0x9e3779b9u ^ (uint)data.difficulty * 0x85ebca6bu);
            if (state == 0) state = 0xa341316cu;
            Event = (ExpeditionEventKind)(Next(ref state) % 3);
            Formation = (int)(Next(ref state) % 3);
            TraitRotation = (int)(Next(ref state) % 3);
            OptionalNodes = 1 + (int)(Next(ref state) % 3);
            Reward = 80 * data.difficulty;
        }
        static uint Next(ref uint state) { state ^= state << 13; state ^= state >> 17; state ^= state << 5; return state; }
        public string Preview(ExpeditionData data) => $"种子 {data.seed} · 规则 v{data.rulesVersion} · {EventLabel}\n{FormationLabel} · 精英 {TraitLabel}\n" +
            $"可选：{NodesLabel}；机关、封门箱及隐藏支路保留。\n{EventRules}\n" +
            (data.rewardClaimed ? "本组合事件奖励已领取；重试可体验事件，不再发放额外奖励。" : $"事件成功额外 {Reward} 金币 / {Reward} 经验，每个组合仅一次。") +
            "\n重试或读档沿用种子，波次、探索和事件进度重置；普通掉落每次按原规则结算。可放弃事件完成普通结算。";
    }
}
