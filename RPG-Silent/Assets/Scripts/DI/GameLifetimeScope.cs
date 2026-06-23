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
public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private UIManager                   uiManager;
    [SerializeField] private InputManager                inputManager;
    [SerializeField] private SceneLoaderManager          sceneLoaderManager;
    [SerializeField] private ScreenSettingsService       screenSettingsService;
    [SerializeField] private SoundSettingsService        soundSettingsService;
    [SerializeField] private ControllerSettingsService   controllerSettingsService;
    [SerializeField] private GameSettingsService         gameSettingsService;
    [SerializeField] private PlayerInputActionsService   playerInputActionsService;

    protected override void Configure(IContainerBuilder builder)
    {
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

        // 让 GameStart 也能被注入
        builder.RegisterComponentInHierarchy<GameStart>();

        // 容器构建完成后，直接把容器实例传给 UIManager
        // 比依赖 [Inject] 字段注入更可靠，避免 DontDestroyOnLoad 的时序问题
        builder.RegisterBuildCallback(container =>
        {
            uiManager.SetGlobalResolver(container);
        });
    }
}
