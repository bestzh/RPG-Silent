namespace RPGSilent.Domain
{
    /// <summary>
    /// 屏幕设置数据模型，纯 C# 类，无 Unity 依赖。
    /// </summary>
    public class ScreenSettings
    {
        public int   ResolutionIndex { get; set; }
        public bool  IsFullScreen    { get; set; }
        public int   QualityIndex    { get; set; }
        public float Brightness      { get; set; } = 1f;
    }
}
