using System;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 玩家数据只读接口，供 UI 层和 Controller 层订阅与读取，不暴露修改方法。
    /// </summary>
    public interface IPlayerStatsReader
    {
        int  MaxHealth     { get; }
        int  CurrentHealth { get; }
        int  Gold          { get; }
        int  Exp           { get; }
        bool IsDead        { get; }

        event Action<int, int> OnHealthChanged;  // (current, max)
        event Action<int>      OnGoldChanged;
        event Action<int>      OnExpChanged;

        /// <summary>重新触发所有事件，用于订阅方初始化时同步当前值。</summary>
        void Refresh();
    }
}
