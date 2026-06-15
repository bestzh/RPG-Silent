namespace RPGSilent.Domain
{
    /// <summary>
    /// 控制器设置服务接口：应用灵敏度/冲刺时间/反转Y轴，持久化。
    /// </summary>
    public interface IControllerSettingsService
    {
        /// <summary>当前正在生效的设置。</summary>
        ControllerSettings CurrentSettings { get; }

        /// <summary>立即应用指定设置。</summary>
        void Apply(ControllerSettings settings);

        /// <summary>将当前设置持久化到 PlayerPrefs。</summary>
        void Save();

        /// <summary>从 PlayerPrefs 加载设置并应用。</summary>
        void Load();

        /// <summary>恢复出厂默认设置、应用并保存。</summary>
        void Reset();
    }
}
