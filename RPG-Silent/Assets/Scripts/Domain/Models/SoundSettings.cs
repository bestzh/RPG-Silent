namespace RPGSilent.Domain
{
    /// <summary>
    /// 音频设置数据模型，纯 C# 类，无 Unity 依赖。
    /// 音量取值范围 0~1。
    /// </summary>
    public class SoundSettings
    {
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume  { get; set; } = 1f;
        public float SFXVolume    { get; set; } = 1f;
        public bool  IsMuted      { get; set; }
    }
}
