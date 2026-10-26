using RPGSilent.Application;
using RPGSilent.Domain;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 全局作用域容器，随初始场景中的 GameObject 持续存在（DontDestroyOnLoad）。
/// 
/// ===== Unity 场景配置步骤 =====
/// 1. 在初始场景（开始菜单场景）找到挂有 UIManager/InputManager/SceneLoaderManager 的 GameObject
/// 2. 给该 GameObject 添加此组件（Add Component → GameLifetimeScope）
/// 3. 在 Inspector 中，将 UIManager / InputManager / SceneLoaderManager
///    以及 ScreenSettingsService / SoundSettingsService / GameSettingsService 拖入对应字段
/// ================================
/// </summary>
[DefaultExecutionOrder(-6000)]
public class GameLifetimeScope : LifetimeScope
{
    public static GameLifetimeScope Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;
    private PlayerProgressService progress;

    private void OnApplicationPause(bool paused) { if (paused) progress?.Save(); }
    private void OnApplicationQuit() => progress?.Save();

    protected override void Awake()
    {
        // 必须早于容器构建和其他全局组件的 Awake，避免重复根先启动再被销毁。
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }

    protected override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    [SerializeField] private UIManager                   uiManager;
    [SerializeField] private InputManager                inputManager;
    [SerializeField] private SceneLoaderManager          sceneLoaderManager;
    [SerializeField] private ScreenSettingsService       screenSettingsService;
    [SerializeField] private SoundSettingsService        soundSettingsService;
    [SerializeField] private ControllerSettingsService   controllerSettingsService;
    [SerializeField] private GameSettingsService         gameSettingsService;
    [SerializeField] private PlayerInputActionsService   playerInputActionsService;

    [Header("玩家（同一局跨场景保留，新游戏重置）")]
    [SerializeField] private int   playerMaxHealth = 100;
    [SerializeField] private float playerMaxEnergy = 100f;

    protected override void Configure(IContainerBuilder builder)
    {
        // 玩家数据与用例：注册为全局单例，使玩家可跨场景持久保留（HP 等状态延续）
        builder.Register<PlayerStats>(
            _ => new PlayerStats(playerMaxHealth, playerMaxEnergy),
            Lifetime.Singleton
        ).AsSelf().As<IPlayerStatsReader>();

        // 技能槽 UI 桥接：玩家生成后由 PlayerSkillController 绑定
        builder.Register<PlayerSkillAccess>(Lifetime.Singleton);
        string saveDirectory = System.IO.Path.Combine(Application.persistentDataPath, "Saves");
#if UNITY_EDITOR
        // Isolated MCP test storage survives Play Mode restarts, never player builds.
        string validationDirectory = UnityEditor.SessionState.GetString("RPGSilent.Validation.SaveDirectory", string.Empty);
        if (!string.IsNullOrEmpty(validationDirectory)) saveDirectory = validationDirectory;
#endif
        builder.RegisterInstance(new ProgressSaveStore(saveDirectory));
        builder.Register<PlayerProgressService>(Lifetime.Singleton);
        builder.Register<PlayerSessionService>(Lifetime.Singleton);

        builder.Register<PlayerTakeDamageUseCase>(Lifetime.Singleton);
        builder.Register<PlayerAddRewardUseCase>(Lifetime.Singleton);
        builder.Register<PlayerHealUseCase>(Lifetime.Singleton);

        // 基础服务
        builder.RegisterComponent(uiManager).As<IUIService>();
        builder.RegisterComponent(inputManager).As<IInputService>();
        builder.RegisterComponent(sceneLoaderManager).As<ISceneLoader>();

        // 屏幕设置服务
        builder.RegisterComponent(screenSettingsService).As<IScreenSettingsService>();

        // 声音设置服务
        builder.RegisterComponent(soundSettingsService).As<ISoundSettingsService>();

        // 控制器设置服务
        builder.RegisterComponent(controllerSettingsService).As<IControllerSettingsService>();

        // 游戏设置服务
        builder.RegisterComponent(gameSettingsService).As<IGameSettingsService>();

        // 玩家输入动作服务（集中管理所有 InputAction，支持改键）
        builder.RegisterComponent(playerInputActionsService).As<IPlayerInputActions>();

        // 光标状态（游戏中隐藏，Ctrl 切换，UI 模式强制显示）
        builder.Register<CursorService>(Lifetime.Singleton).As<ICursorService>();

        // 容器构建完成后，直接把容器实例传给 UIManager
        // 比依赖 [Inject] 字段注入更可靠，避免 DontDestroyOnLoad 的时序问题
        builder.RegisterBuildCallback(container =>
        {
            progress = container.Resolve<PlayerProgressService>();
            uiManager.SetGlobalResolver(container);
        });
    }
}
