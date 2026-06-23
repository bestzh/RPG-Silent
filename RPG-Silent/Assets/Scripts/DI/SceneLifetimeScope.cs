using RPGSilent.Application;
using RPGSilent.Domain;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 游戏场景级作用域容器，随场景加载而创建、卸载而销毁。
///
/// ===== Unity 场景配置步骤 =====
/// 1. 在游戏场景（Main 场景）中右键 Hierarchy → Create Empty，命名为 [SceneScope]
/// 2. 给该空对象添加此组件（Add Component → SceneLifetimeScope）
/// 3. 在 Inspector 的 "Parent Reference" 字段，下拉选择 GameLifetimeScope
/// 4. 根据需要调整 Player Max Health 字段
/// ================================
/// </summary>
public class SceneLifetimeScope : LifetimeScope
{
    private const string PauseUiKey = "UI/PauseUI";

    [SerializeField] private int playerMaxHealth = 100;

    protected override void Configure(IContainerBuilder builder)
    {
        // 玩家数据模型：同时以自身类型（供 UseCase 修改）和只读接口（供 UI 读取）注册
        builder.Register<PlayerStats>(
            _ => new PlayerStats(playerMaxHealth),
            Lifetime.Scoped
        ).AsSelf().As<IPlayerStatsReader>();

        // 用例层（构造函数依赖 PlayerStats，由容器自动注入）
        builder.Register<PlayerTakeDamageUseCase>(Lifetime.Scoped);
        builder.Register<PlayerAddRewardUseCase>(Lifetime.Scoped);
        builder.Register<PlayerHealUseCase>(Lifetime.Scoped);

        // 暂停菜单服务（ESC 唤出 PauseUI，不冻结游戏，仅游戏场景有效）
        builder.Register<GamePauseService>(Lifetime.Scoped).As<IGamePauseService>();

        // 从场景层级中找到 PlayerController / CameraControl 并注入依赖
        builder.RegisterComponentInHierarchy<PlayerController>();
        builder.RegisterComponentInHierarchy<CameraControl>();

        // 场景容器构建完毕后，把场景级容器传给 UIManager
        // 这样 UIManager 加载 MainUI 时能解析 IPlayerStatsReader 等场景级类型
        builder.RegisterBuildCallback(container =>
        {
            var uiService = container.Resolve<IUIService>() as UIManager;
            uiService?.SetSceneResolver(container);

            // 解析后订阅 ESC，并预加载 PauseUI，避免首次按键异步加载导致状态错乱
            container.Resolve<IGamePauseService>();
            container.Resolve<IUIService>().PreloadUI(PauseUiKey);
        });
    }

    protected override void OnDestroy()
    {
        // 场景卸载时，清除 UIManager 中的场景级容器引用，恢复使用全局容器
        if (Container != null)
        {
            var uiService = Container.Resolve<IUIService>();
            uiService?.CloseUI(PauseUiKey);
            uiService?.SetRaycastEnabled("UI/MainUI", true);
            (uiService as UIManager)?.SetSceneResolver(null);
        }

        base.OnDestroy();
    }
}
