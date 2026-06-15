namespace RPGSilent.Domain
{
    /// <summary>
    /// 控制器设置数据模型，纯 C# 类，无 Unity 依赖。
    /// </summary>
    public class ControllerSettings
    {
        public float MouseSensitivity { get; set; } = 3f;
        public float SprintHoldTime   { get; set; } = 0.5f;
        public bool  InvertY          { get; set; } = false;
    }
}
