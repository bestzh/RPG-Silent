using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public partial class PlayerInputActionsService
{
    public string LastRebindMessage { get; private set; } = string.Empty;
    bool attackStartedOnUi;
    PointerEventData uiPointer;
    EventSystem pointerSystem;
    readonly List<RaycastResult> uiPointerHits = new();

    string FindBindingConflict(InputAction action, int bindingIndex, InputControl control)
    {
        if (control == null) return "无效输入";
        foreach (var candidate in _map.actions)
        {
            for (int i = 0; i < candidate.bindings.Count; i++)
            {
                if (candidate == action && i == bindingIndex || candidate.bindings[i].isComposite) continue;
                if (action == Sprint && candidate == Roll) continue;
                string path = candidate.bindings[i].effectivePath;
                if (!string.IsNullOrEmpty(path) && InputControlPath.Matches(path, control)) return BindingLabel(candidate.name, i);
            }
        }
        foreach (var candidate in _uiMap.actions)
        {
            for (int i = 0; i < candidate.bindings.Count; i++)
            {
                if (action == Interact && candidate == lootPickup && i == 0) continue;
                string path = candidate.bindings[i].effectivePath;
                if (!string.IsNullOrEmpty(path) && InputControlPath.Matches(path, control)) return BindingLabel(candidate.name, i);
            }
        }
        return null;
    }

    static string BindingLabel(string name, int index) => name switch
    {
        "Move" => index == 1 ? "前进" : index == 2 ? "后退" : index == 3 ? "左移" : "右移",
        "Sprint" or "Roll" => "冲刺／闪避", "Walk" => "慢走", "Jump" => "跳跃", "Attack" => "攻击",
        "LockTarget" => "锁定目标", "Guard" => "格挡", "NextTarget" => "下一个目标", "PreviousTarget" => "上一个目标",
        "StanceToggle" => "切换武器", "CursorToggle" => "显示光标", "Interact" => "交互",
        "Pause" => "返回／暂停", "Inventory" => "背包", "Journal" => "手札", "NearbyLoot" => "附近掉落",
        "LootPickup" => "掉落确认", "NextLoot" or "PreviousLoot" => "掉落列表切换",
        _ when name.StartsWith("SkillSlot") => "技能快捷栏",
        _ when name.StartsWith("ConsumableSlot") => "药品快捷栏",
        _ => name
    };

    bool IsPointerActionOverUi(InputAction.CallbackContext context)
    {
        if (context.control?.device is not Mouse mouse || EventSystem.current == null) return false;
        if (uiPointer == null || pointerSystem != EventSystem.current)
        { pointerSystem = EventSystem.current; uiPointer = new PointerEventData(pointerSystem); }
        uiPointer.position = mouse.position.ReadValue();
        uiPointerHits.Clear();
        EventSystem.current.RaycastAll(uiPointer, uiPointerHits);
        return uiPointerHits.Count > 0;
    }
}
