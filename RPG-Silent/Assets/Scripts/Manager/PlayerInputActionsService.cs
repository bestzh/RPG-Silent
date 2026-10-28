using System;
using System.Collections.Generic;
using System.Globalization;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

/// <summary>
/// 玩家输入动作服务：集中管理所有 InputAction，支持运行时改键与 PlayerPrefs 持久化。
/// 由 GameLifetimeScope 注册为全局单例（DontDestroyOnLoad）。
///
/// 绑定索引约定（Move 2DVector 复合体）：
///   Move[0] = 复合体本身（不可改键）
///   Move[1] = Up    → 前进 (W)
///   Move[2] = Down  → 后退 (S)
///   Move[3] = Left  → 左移 (A)
///   Move[4] = Right → 右移 (D)
///   Sprint[0] / Roll[0] / Jump[0] / Walk[0] / Attack[0] / StanceToggle[0]
/// </summary>
public class PlayerInputActionsService : MonoBehaviour, IPlayerInputActions
{
    private const string SaveKey = "KeyBindingsJson";

    [Inject] private IControllerSettingsService _controllerSettings;

    // ── 事件 ────────────────────────────────────────────────────────────────────
    public event Action SprintStarted;
    public event Action SprintEnded;
    public event Action RollTriggered;
    public event Action JumpTriggered;
    public event Action WalkStarted;
    public event Action WalkEnded;
    public event Action AttackTriggered;
    public event Action StanceToggleTriggered;
    public event Action PauseTriggered;
    public event Action InteractTriggered;
    public event Action InventoryTriggered;
    public event Action JournalTriggered;
    public event Action<int> ConsumableTriggered;
    public InputAction[] ConsumableSlots { get; private set; }
    public string GetConsumableSlotDisplay(int slotIndex) => ConsumableSlots != null && slotIndex >= 0 && slotIndex < ConsumableSlots.Length ? GetDisplayString(ConsumableSlots[slotIndex], 0) : "";
    private InputAction journalAction;
    private InputAction inventoryAction;
    public event Action CursorToggleTriggered;
    public event Action<int> SkillSlotTriggered;

    // ── 原始 Action ──────────────────────────────────────────────────────────────
    public InputAction Move         { get; private set; }
    public InputAction Sprint       { get; private set; }
    public InputAction Roll         { get; private set; }
    public InputAction Jump         { get; private set; }
    public InputAction Walk         { get; private set; }
    public InputAction Attack       { get; private set; }
    public InputAction Look         { get; private set; }
    private InputAction CursorToggle { get; set; }
    public InputAction StanceToggle { get; private set; }
    public InputAction Pause        { get; private set; }
    public InputAction Interact     { get; private set; }
    public InputAction[] SkillSlots { get; private set; }

    // 与项目旧 Mouse X/Y 轴的 sensitivity=0.1 对齐，保留现有灵敏度设置量级。
    private const float MouseDeltaScale = 0.1f;
    public Vector2 MoveInput => GameplayInputEnabled ? Move?.ReadValue<Vector2>() ?? Vector2.zero : Vector2.zero;
    public Vector2 LookInput => GameplayInputEnabled ? (Look?.ReadValue<Vector2>() ?? Vector2.zero) * MouseDeltaScale : Vector2.zero;
    public bool GameplayInputEnabled => _gameplayInputRequested && _gameplayInputBlockers.Count == 0;

    // ── 内部状态 ─────────────────────────────────────────────────────────────────
    private InputActionMap _map;
    private InputActionMap _uiMap;
    private InputActionRebindingExtensions.RebindingOperation _currentRebind;
    private float _sprintHoldTime = 0.5f;
    private bool _gameplayInputRequested = true;
    private readonly HashSet<object> _gameplayInputBlockers = new();

    // ── 生命周期 ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildMap(_sprintHoldTime);
        BuildUiMap();
        Load();
        ApplyGameplayInputState();
        _uiMap.Enable();
    }

    private void Start()
    {
        // 所有 Awake 完成后，从 ControllerSettings 读取真实持定时间
        if (_controllerSettings != null)
        {
            float savedTime = _controllerSettings.CurrentSettings.SprintHoldTime;
            if (!Mathf.Approximately(savedTime, _sprintHoldTime))
                ApplySprintHoldTime(savedTime);
        }
    }

    private void OnEnable()
    {
        if (_controllerSettings != null)
            _controllerSettings.OnSettingsApplied += OnControllerSettingsChanged;
    }

    private void OnDisable()
    {
        if (_controllerSettings != null)
            _controllerSettings.OnSettingsApplied -= OnControllerSettingsChanged;
    }

    private void OnDestroy()
    {
        _currentRebind?.Cancel();
        _currentRebind?.Dispose();
        UnsubscribeHandlers();
        _map?.Disable();
        _map?.Dispose();
        _uiMap?.Disable();
        _uiMap?.Dispose();
    }

    // ── IPlayerInputActions 实现 ─────────────────────────────────────────────────

    public void ApplySprintHoldTime(float duration)
    {
        if (Mathf.Approximately(duration, _sprintHoldTime)) return;

        CancelCurrentRebind();
        string savedJson = SaveBindingsToJson();

        _map.Disable();
        UnsubscribeGameplayHandlers();
        _map.Dispose();

        _sprintHoldTime = duration;
        BuildMap(duration);
        LoadBindingsFromJson(savedJson);

        ApplyGameplayInputState();
    }

    public void StartRebind(InputAction action, int bindingIndex,
        Action<string> onComplete, Action onCancel = null)
    {
        _currentRebind?.Cancel();
        _currentRebind?.Dispose();

        action.Disable();

        _currentRebind = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Mouse>/scroll")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op =>
            {
                op.Dispose();
                _currentRebind = null;

                // Sprint 改键后自动将同一路径镜像到 Roll
                if (action == Sprint)
                    MirrorSprintPathToRoll();

                if (GameplayInputEnabled) action.Enable();
                string display = GetDisplayString(action, bindingIndex);
                onComplete?.Invoke(display);
            })
            .OnCancel(op =>
            {
                op.Dispose();
                _currentRebind = null;
                if (GameplayInputEnabled) action.Enable();
                onCancel?.Invoke();
            })
            .Start();
    }

    public void CancelCurrentRebind()
    {
        _currentRebind?.Cancel();
        _currentRebind?.Dispose();
        _currentRebind = null;
    }

    public string GetDisplayString(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return "—";

        string path = action.bindings[bindingIndex].effectivePath;
        if (string.IsNullOrEmpty(path)) return "—";

        return InputControlPath.ToHumanReadableString(
            path, InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    public void Save()
    {
        PlayerPrefs.SetString(SaveKey, SaveBindingsToJson());
        PlayerPrefs.Save();
        Debug.Log("[PlayerInputActions] 按键绑定已保存。");
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        LoadBindingsFromJson(json);
    }

    public void ResetBindings()
    {
        foreach (var a in _map.actions)
            a.RemoveAllBindingOverrides();

        // Roll 的路径始终镜像 Sprint
        MirrorSprintPathToRoll();

        Debug.Log("[PlayerInputActions] 已恢复默认按键绑定。");
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        _gameplayInputRequested = enabled;
        ApplyGameplayInputState();
    }

    public void SetGameplayInputBlocked(object owner, bool blocked)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (blocked) _gameplayInputBlockers.Add(owner);
        else         _gameplayInputBlockers.Remove(owner);
        ApplyGameplayInputState();
    }

    private void ApplyGameplayInputState()
    {
        if (_map == null) return;

        if (GameplayInputEnabled) _map.Enable();
        else                      _map.Disable();
    }

    // ── 私有：构建 InputActionMap ─────────────────────────────────────────────────

    private void BuildMap(float holdDuration)
    {
        _map = new InputActionMap("Player");

        // 移动：2DVector 复合体（WASD）
        Move = _map.AddAction("Move", InputActionType.Value);
        Move.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/w")
            .With("Down",  "<Keyboard>/s")
            .With("Left",  "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        // 冲刺（Hold）/ 翻滚（Tap）共享同一按键
        string holdStr = holdDuration.ToString("F3", CultureInfo.InvariantCulture);
        Sprint = _map.AddAction("Sprint", InputActionType.Button);
        Sprint.AddBinding("<Keyboard>/leftShift")
              .WithInteraction($"hold(duration={holdStr})");

        Roll = _map.AddAction("Roll", InputActionType.Button);
        Roll.AddBinding("<Keyboard>/leftShift")
            .WithInteraction($"tap(duration={holdStr})");

        // 跳跃
        Jump = _map.AddAction("Jump", InputActionType.Button);
        Jump.AddBinding("<Keyboard>/space");

        // 步行
        Walk = _map.AddAction("Walk", InputActionType.Button);
        Walk.AddBinding("<Keyboard>/leftAlt");

        // 攻击
        Attack = _map.AddAction("Attack", InputActionType.Button);
        Attack.AddBinding("<Mouse>/leftButton");

        Look = _map.AddAction("Look", InputActionType.Value);
        Look.AddBinding("<Mouse>/delta");

        CursorToggle = _map.AddAction("CursorToggle", InputActionType.Button);
        CursorToggle.AddBinding("<Keyboard>/leftCtrl");
        CursorToggle.AddBinding("<Keyboard>/rightCtrl");

        // 姿态切换
        StanceToggle = _map.AddAction("StanceToggle", InputActionType.Button);
        StanceToggle.AddBinding("<Keyboard>/tab");
        Interact = _map.AddAction("Interact", InputActionType.Button);
        Interact.AddBinding("<Keyboard>/e");

        // 技能槽 0..9 → 键盘 1..9,0
        SkillSlots = new InputAction[10];
        string[] skillPaths =
        {
            "<Keyboard>/1", "<Keyboard>/2", "<Keyboard>/3", "<Keyboard>/4", "<Keyboard>/5",
            "<Keyboard>/6", "<Keyboard>/7", "<Keyboard>/8", "<Keyboard>/9", "<Keyboard>/0"
        };
        for (int i = 0; i < SkillSlots.Length; i++)
        {
            SkillSlots[i] = _map.AddAction($"SkillSlot{i}", InputActionType.Button);
            SkillSlots[i].AddBinding(skillPaths[i]);
        }

        ConsumableSlots = new InputAction[3];
        string[] consumablePaths = { "<Keyboard>/q", "<Keyboard>/r", "<Keyboard>/f" };
        for (int i = 0; i < ConsumableSlots.Length; i++)
        {
            int slot = i;
            ConsumableSlots[i] = _map.AddAction("ConsumableSlot" + i, InputActionType.Button, consumablePaths[i]);
            ConsumableSlots[i].performed += _ => { if (GameplayInputEnabled) ConsumableTriggered?.Invoke(slot); };
        }
        SubscribeHandlers();
    }

    private void BuildUiMap()
    {
        _uiMap = new InputActionMap("UI");

        // 暂停菜单：独立于游戏输入，暂停时仍可响应 ESC
        Pause = _uiMap.AddAction("Pause", InputActionType.Button);
        Pause.AddBinding("<Keyboard>/escape");
        Pause.performed += OnPausePerformed;
        inventoryAction = _uiMap.AddAction("Inventory", InputActionType.Button, "<Keyboard>/b");
        inventoryAction.performed += OnInventoryPerformed;
        journalAction = _uiMap.AddAction("Journal", InputActionType.Button, "<Keyboard>/j");
        journalAction.performed += OnJournalPerformed;
    }

    // ── 私有：内部事件桥接 ────────────────────────────────────────────────────────

    private void SubscribeHandlers()
    {
        Sprint.performed      += OnSprintPerformed;
        Sprint.canceled       += OnSprintCanceled;
        Roll.performed        += OnRollPerformed;
        Jump.performed        += OnJumpPerformed;
        Walk.performed        += OnWalkPerformed;
        Walk.canceled         += OnWalkCanceled;
        Attack.performed      += OnAttackPerformed;
        CursorToggle.performed += OnCursorTogglePerformed;
        StanceToggle.performed += OnStanceTogglePerformed;
        Interact.performed += OnInteractPerformed;

        if (SkillSlots != null)
        {
            for (int i = 0; i < SkillSlots.Length; i++)
            {
                int slot = i;
                SkillSlots[i].performed += _ =>
                {
                    if (GameplayInputEnabled) SkillSlotTriggered?.Invoke(slot);
                };
            }
        }
    }

    private void UnsubscribeGameplayHandlers()
    {
        if (Sprint       != null) { Sprint.performed       -= OnSprintPerformed;      Sprint.canceled -= OnSprintCanceled; }
        if (Roll         != null)   Roll.performed         -= OnRollPerformed;
        if (Jump         != null)   Jump.performed         -= OnJumpPerformed;
        if (Walk         != null) { Walk.performed         -= OnWalkPerformed;        Walk.canceled   -= OnWalkCanceled; }
        if (Attack       != null)   Attack.performed       -= OnAttackPerformed;
        if (CursorToggle != null)   CursorToggle.performed -= OnCursorTogglePerformed;
        if (StanceToggle != null)   StanceToggle.performed -= OnStanceTogglePerformed;
        if (Interact != null) Interact.performed -= OnInteractPerformed;

        // SkillSlots 在 RebuildMap 时整表 Dispose，旧回调随 Action 一起丢弃
    }

    private void UnsubscribeHandlers()
    {
        UnsubscribeGameplayHandlers();
        if (Pause != null) Pause.performed -= OnPausePerformed;
        if (inventoryAction != null) inventoryAction.performed -= OnInventoryPerformed;
        if (journalAction != null) journalAction.performed -= OnJournalPerformed;
    }

    private void RaiseGameplayAction(Action action)
    {
        if (GameplayInputEnabled) action?.Invoke();
    }

    private void OnSprintPerformed(InputAction.CallbackContext _)      => RaiseGameplayAction(SprintStarted);
    private void OnSprintCanceled(InputAction.CallbackContext _)       => SprintEnded?.Invoke();
    private void OnRollPerformed(InputAction.CallbackContext _)        => RaiseGameplayAction(RollTriggered);
    private void OnJumpPerformed(InputAction.CallbackContext _)        => RaiseGameplayAction(JumpTriggered);
    private void OnWalkPerformed(InputAction.CallbackContext _)        => RaiseGameplayAction(WalkStarted);
    private void OnWalkCanceled(InputAction.CallbackContext _)         => WalkEnded?.Invoke();
    private void OnAttackPerformed(InputAction.CallbackContext _)      => RaiseGameplayAction(AttackTriggered);
    private void OnStanceTogglePerformed(InputAction.CallbackContext _) => RaiseGameplayAction(StanceToggleTriggered);
    private void OnCursorTogglePerformed(InputAction.CallbackContext _) => RaiseGameplayAction(CursorToggleTriggered);
    private void OnPausePerformed(InputAction.CallbackContext _)       => PauseTriggered?.Invoke();
    private void OnInventoryPerformed(InputAction.CallbackContext _) => InventoryTriggered?.Invoke();
    private void OnJournalPerformed(InputAction.CallbackContext _) => JournalTriggered?.Invoke();
    private void OnInteractPerformed(InputAction.CallbackContext _) => RaiseGameplayAction(InteractTriggered);

    public string GetSkillSlotDisplay(int slotIndex)
    {
        if (SkillSlots == null || slotIndex < 0 || slotIndex >= SkillSlots.Length)
            return "—";
        return GetDisplayString(SkillSlots[slotIndex], 0);
    }

    // ── 私有：Sprint/Roll 路径镜像 ────────────────────────────────────────────────

    /// <summary>将 Sprint 的 overridePath 镜像到 Roll（两者始终用同一物理按键）。</summary>
    private void MirrorSprintPathToRoll()
    {
        if (Sprint == null || Roll == null) return;
        string effectivePath = Sprint.bindings[0].effectivePath;
        Roll.ApplyBindingOverride(0, new InputBinding { overridePath = effectivePath });
    }

    private void OnControllerSettingsChanged(ControllerSettings settings)
        => ApplySprintHoldTime(settings.SprintHoldTime);

    // ── 私有：绑定覆盖序列化（只保存 overridePath，不保存 interaction override）────

    [Serializable]
    private class BindingOverrideData
    {
        public string actionName;
        public int    bindingIndex;
        public string overridePath;
    }

    [Serializable]
    private class BindingOverrideContainer
    {
        public List<BindingOverrideData> overrides = new();
    }

    private string SaveBindingsToJson()
    {
        var container = new BindingOverrideContainer();
        foreach (var action in _map.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                string op = action.bindings[i].overridePath;
                if (!string.IsNullOrEmpty(op))
                    container.overrides.Add(new BindingOverrideData
                        { actionName = action.name, bindingIndex = i, overridePath = op });
            }
        }
        return JsonUtility.ToJson(container);
    }

    private void LoadBindingsFromJson(string json)
    {
        // 先清除所有当前 override，确保未保存的修改被完全丢弃
        foreach (var action in _map.actions)
            action.RemoveAllBindingOverrides();

        if (string.IsNullOrEmpty(json)) return;
        var container = JsonUtility.FromJson<BindingOverrideContainer>(json);
        if (container?.overrides == null) return;

        foreach (var data in container.overrides)
        {
            var action = _map.FindAction(data.actionName);
            action?.ApplyBindingOverride(data.bindingIndex,
                new InputBinding { overridePath = data.overridePath });
        }
    }
}
