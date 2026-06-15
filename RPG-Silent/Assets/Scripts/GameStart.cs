using RPGSilent.Domain;
using UnityEngine;
using VContainer;

/// <summary>
/// 游戏入口，由 VContainer 注入 IUIService，不再使用单例访问。
/// </summary>
public class GameStart : MonoBehaviour
{
    [Inject] private IUIService _uiService;

    private void Start()
    {
        if (_uiService == null)
        {
            Debug.LogError("[GameStart] IUIService 未注入！\n" +
                           "请检查：场景中是否已添加 GameLifetimeScope 组件，并将 UIManager 拖入其字段。\n" +
                           "参考文档：REFACTORING.md → 五、分阶段重构计划 → Phase 3");
            return;
        }

        _uiService.OpenUI("UI/StartUI");
        _uiService.PreloadUI("UI/LoadingUI");
    }
}
