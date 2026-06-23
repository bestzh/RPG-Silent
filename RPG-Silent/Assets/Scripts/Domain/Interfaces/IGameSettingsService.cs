using System;
using System.Collections.Generic;

namespace RPGSilent.Domain
{
    /// <summary>游戏设置服务接口：难度、HUD、小地图、屏幕震动。</summary>
    public interface IGameSettingsService
    {
        GameSettings CurrentSettings { get; }

        /// <summary>难度选项（简单 / 普通 / 困难）。</summary>
        IReadOnlyList<string> DifficultyOptions { get; }

        /// <summary>每次 Apply 后触发。</summary>
        event Action<GameSettings> OnSettingsApplied;

        /// <summary>将原始伤害按当前难度缩放后返回。</summary>
        int ScaleIncomingDamage(int rawDamage);

        void Apply(GameSettings settings);
        void Save();
        void Load();
        void Reset();
    }
}
