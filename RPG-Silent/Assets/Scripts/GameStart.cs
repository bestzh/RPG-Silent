using RPGSilent.Domain;
using UnityEngine;
using VContainer;

/// <summary>
/// 菜单入口：使用唯一的全局容器注入本场景入口，再结束玩家会话并打开菜单。
/// </summary>
public class GameStart : MonoBehaviour
{
    [Inject] private IUIService _uiService;
    [Inject] private PlayerSessionService _playerSession;

    private void Start()
    {
        // 菜单每次重新加载都会创建本组件；依赖始终来自存活的全局根。
        var scope = GameLifetimeScope.Instance;
        if (scope == null || scope.Container == null)
        {
            Debug.LogError("[GameStart] IUIService 未注入！\n" +
                           "请检查：场景中是否已添加 GameLifetimeScope 组件，并将 UIManager 拖入其字段。\n" +
                           "参考文档：REFACTORING.md → 五、分阶段重构计划 → Phase 3");
            return;
        }

        scope.Container.Inject(this);
        _playerSession.EndSession();
        _uiService.OpenUI("UI/StartUI");
        _uiService.PreloadUI("UI/LoadingUI");
    }
}
