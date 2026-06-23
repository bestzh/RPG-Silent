namespace RPGSilent.Domain
{
    /// <summary>游戏分页设置：难度、HUD、小地图、屏幕震动。</summary>
    public class GameSettings
    {
        /// <summary>难度索引：0=简单，1=普通，2=困难。</summary>
        public int DifficultyIndex;

        public bool ShowHud;
        public bool ShowMiniMap;

        /// <summary>屏幕震动强度，0~1。</summary>
        public float ScreenShakeIntensity;
    }
}
