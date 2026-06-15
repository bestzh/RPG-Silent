namespace RPGSilent.Domain
{
    /// <summary>
    /// 声音设置服务接口：应用音量、持久化。
    /// </summary>
    public interface ISoundSettingsService
    {
        /// <summary>当前正在生效的设置。</summary>
        SoundSettings CurrentSettings { get; }

        /// <summary>立即应用指定设置到 AudioMixer。</summary>
        void Apply(SoundSettings settings);

        /// <summary>将当前设置持久化到 PlayerPrefs。</summary>
        void Save();

        /// <summary>从 PlayerPrefs 加载设置并应用。</summary>
        void Load();

        /// <summary>恢复出厂默认设置、应用并保存。</summary>
        void Reset();
    }
}
