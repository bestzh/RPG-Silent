/// <summary>
/// 进入副本时的运行时上下文：在切换到副本场景前记录玩家所选的副本与难度，
/// 供副本场景内的逻辑（怪物强度、掉落等）读取。场景为 Single 加载，无法用参数传递，
/// 因此用静态上下文承载这一次性选择。
/// </summary>
public static class DungeonLaunchContext
{
    public static bool HasValue { get; private set; }
    public static int DungeonId { get; private set; }
    public static DungeonDifficulty Difficulty { get; private set; }

    public static void Set(int dungeonId, DungeonDifficulty difficulty)
    {
        DungeonId  = dungeonId;
        Difficulty = difficulty;
        HasValue   = true;
    }

    public static void Clear()
    {
        DungeonId  = 0;
        Difficulty = default;
        HasValue   = false;
    }
}
