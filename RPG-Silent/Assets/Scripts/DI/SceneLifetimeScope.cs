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

        // 从场景层级中找到 PlayerController 并注入依赖
        builder.RegisterComponentInHierarchy<PlayerController>();

        // 场景容器构建完毕后，把场景级容器传给 UIManager
        // 这样 UIManager 加载 MainUI 时能解析 IPlayerStatsReader 等场景级类型
        builder.RegisterBuildCallback(container =>
        {
            var uiService = container.Resolve<IUIService>() as UIManager;
            uiService?.SetSceneResolver(container);
        });
    }

    protected override void OnDestroy()
    {
        // 场景卸载时，清除 UIManager 中的场景级容器引用，恢复使用全局容器
        if (Container != null)
        {
            var uiService = Container.Resolve<IUIService>() as UIManager;
            uiService?.SetSceneResolver(null);
        }

        base.OnDestroy();
    }
}
