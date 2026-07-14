using RPGSilent.Domain;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 游戏场景级作用域容器，随场景加载而创建、卸载而销毁。
///
/// ===== Unity 场景配置步骤 =====
/// 1. 在游戏场景（Main / Dungeon01 等）中右键 Hierarchy → Create Empty，命名为 [SceneScope]
/// 2. 给该空对象添加此组件（Add Component → SceneLifetimeScope）
/// 3. 在 Inspector 的 "Parent Reference" 字段，下拉选择 GameLifetimeScope
/// 注：玩家最大生命值等玩家相关配置已移到 GameLifetimeScope。
/// ================================
/// </summary>
public class SceneLifetimeScope : LifetimeScope
{
    private const string PauseUiKey = "UI/PauseUI";

    protected override void Configure(IContainerBuilder builder)
    {
        // 注：玩家数据 / 用例 / 玩家与相机的注入已上移到 GameLifetimeScope（全局单例），
        // 以支持玩家跨场景持久保留。本场景作用域只保留“仅游戏场景内有效”的服务。

        // 暂停菜单服务（ESC 唤出 PauseUI，不冻结游戏，仅游戏场景有效）
        builder.Register<GamePauseService>(Lifetime.Scoped).As<IGamePauseService>();

        // 传送门服务（玩家进入传送门触发器时唤出 PortalUI）
        builder.Register<PortalService>(Lifetime.Scoped).As<IPortalService>();

        // 场景容器构建完毕后，把场景级容器传给 UIManager
        // 这样 UIManager 加载 MainUI 时能解析全局注册的 IPlayerStatsReader 等类型
        builder.RegisterBuildCallback(container =>
        {
            var uiService = container.Resolve<IUIService>() as UIManager;
            uiService?.SetSceneResolver(container);

            // 解析后订阅 ESC，并预加载 PauseUI，避免首次按键异步加载导致状态错乱
            container.Resolve<IGamePauseService>();
            container.Resolve<IUIService>().PreloadUI(PauseUiKey);

            // 实例化传送门服务，并给场景中所有传送门触发器注入依赖
            container.Resolve<IPortalService>();
            PortalTrigger[] triggers = FindObjectsByType<PortalTrigger>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (PortalTrigger trigger in triggers)
                container.InjectGameObject(trigger.gameObject);
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
