using System.Collections.Generic;

namespace RPGSilent.Domain
{
    /// <summary>
    /// 屏幕设置服务接口：读取选项列表、应用设置、持久化。
    /// </summary>
    public interface IScreenSettingsService
    {
        /// <summary>当前正在生效的设置。</summary>
        ScreenSettings CurrentSettings { get; }

        /// <summary>所有可用分辨率字符串列表（如 "1920×1080"）。</summary>
        IReadOnlyList<string> ResolutionOptions { get; }

        /// <summary>画质选项列表（高 / 中 / 低）。</summary>
        IReadOnlyList<string> QualityOptions { get; }

        /// <summary>立即应用指定设置到 Unity（分辨率/全屏/画质/亮度）。</summary>
        void Apply(ScreenSettings settings);

        /// <summary>将当前设置持久化到 PlayerPrefs。</summary>
        void Save();

        /// <summary>从 PlayerPrefs 加载设置并应用。</summary>
        void Load();

        /// <summary>恢复出厂默认设置、应用并保存。</summary>
        void Reset();
    }
}
