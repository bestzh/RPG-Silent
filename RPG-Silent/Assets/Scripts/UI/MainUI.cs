using System.Collections.Generic;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public partial class MainUI : UIBase
{
    // 通过 VContainer 注入，不再 FindWithTag
    [Inject] private IPlayerStatsReader   _stats;
    [Inject] private IGameSettingsService _gameSettings;
    [Inject] private PlayerSkillAccess    _skillAccess;
    [Inject] private PlayerProgressService _progress;
    [Inject] private PlayerSessionService _session;
    public WeaponEquipmentUI weaponEquipment;

    public Image           avatarImage;
    public Slider          hpBar;
    public Slider          mpBar;
    public TextMeshProUGUI  goldText;
    public TextMeshProUGUI  moneyText;
    public RawImage        miniMap;
    public TMP_Text        levelText;
    public Slider          expBar;
    public TMP_Text        expProgressText;
    public TMP_Text        saveStatusText;
    public TMP_Text npcPromptText;
    public TMP_Text classNameText;
    private PlayerNpcInteraction npcInteraction;
    private Button inventoryButton;
    private PlayerLootInteraction lootInteraction;

    private void Update()
    {
        RefreshCombatHud();
        RefreshAdventureHud();
        if (npcPromptText != null) npcPromptText.text = !string.IsNullOrEmpty(lootInteraction?.Prompt) ? lootInteraction.Prompt : !string.IsNullOrEmpty(combatPlayer?.Exploration?.Prompt) ? combatPlayer.Exploration.Prompt : npcInteraction != null ? npcInteraction.Prompt : string.Empty;
        if (classNameText != null) classNameText.text = _progress?.CurrentClass?.DisplayName ?? string.Empty;
    }

    [Header("Skill Bar")]
    [Tooltip("可选：用于 HUD 显隐。在 Inspector 拖 SkillBar 节点。")]
    public GameObject skillBarRoot;

    [Tooltip("按槽位顺序拖入 10 个 SkillSlotUI（对应按键 1~9,0）。改名也不影响。")]
    public List<SkillSlotUI> skillSlots = new List<SkillSlotUI>();

    private IPlayerSkillReader _skills;
    private IPlayerStatsReader _boundStats;
    private IGameSettingsService _boundGameSettings;
    private PlayerSkillAccess _boundSkillAccess;
    private PlayerProgressService _boundProgress;

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        // UIManager 会对已打开的 HUD 再次调用 OnOpen；先解绑，保证重复打开只有一份订阅。
        // 保留实际绑定的来源，以便容器重新注入后仍能解绑旧来源。
        UnbindSources();

        if (_stats == null)
        {
            Debug.LogWarning("[MainUI] IPlayerStatsReader 未注入，无法绑定数据。");
            return;
        }

        _boundStats = _stats;
        _boundGameSettings = _gameSettings;
        _boundSkillAccess = _skillAccess;
        _boundProgress = _progress;
        if (_boundProgress != null) _boundProgress.Changed += RefreshSaveStatus;

        _boundStats.OnHealthChanged += OnHealthChanged;
        _boundStats.OnGoldChanged   += OnGoldChanged;
        _boundStats.OnExpChanged    += OnExpChanged;
        _boundStats.OnEnergyChanged += OnEnergyChanged;

        if (_boundGameSettings != null)
            _boundGameSettings.OnSettingsApplied += OnGameSettingsApplied;

        if (_boundSkillAccess != null)
        {
            _boundSkillAccess.OnBound   += OnSkillReaderBound;
            _boundSkillAccess.OnUnbound += OnSkillReaderUnbound;
            if (_boundSkillAccess.Current != null)
                OnSkillReaderBound(_boundSkillAccess.Current);
            else
                Debug.LogWarning("[MainUI] PlayerSkillAccess.Current 为空，等待玩家技能组件绑定。");
        }
        else
        {
            Debug.LogWarning("[MainUI] PlayerSkillAccess 未注入，技能栏无法同步。");
        }

        if (skillSlots == null || skillSlots.Count == 0)
            Debug.LogWarning("[MainUI] skillSlots 列表为空，请在 Inspector 拖入 10 个 SkillSlotUI。");

        _stats.Refresh();
        if (inventoryButton == null)
        {
            inventoryButton = ProgressionUIWidgets.Button(transform.Find("TopLeftPanel"), classNameText.font, "InventoryButton", "背包 [B]", 0, 0, 156, 44,
                () => _session?.PlayerRig?.GetComponentInChildren<PlayerInventoryInteraction>(true)?.Toggle());
            var r = inventoryButton.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(1, 1); r.anchoredPosition = new Vector2(-24, -330);
        }
        npcInteraction = _session?.PlayerRig != null ? _session.PlayerRig.GetComponentInChildren<PlayerNpcInteraction>(true) : null;
        lootInteraction = _session?.PlayerRig != null ? _session.PlayerRig.GetComponentInChildren<PlayerLootInteraction>(true) : null;
        RefreshSaveStatus();
        BuildQuestHud();
        BuildCombatHud();
        BuildTargetHud();
        BuildAdventureHud();
        if (weaponEquipment != null)
            weaponEquipment.Bind(_session?.PlayerRig != null ? _session.PlayerRig.GetComponentInChildren<PlayerWeaponEquipment>(true) : null);
        ApplyGameSettings(_gameSettings?.CurrentSettings);

        Debug.Log("[MainUI] 已打开并绑定玩家数据。");
    }

    public override void OnClose()
    {
        base.OnClose();
        UnbindSources();
        Debug.Log("[MainUI] 已关闭。");
    }

    private void OnDestroy()
    {
        UnbindSources();
    }

    private void UnbindSources()
    {
        weaponEquipment?.Unbind();
        if (_boundStats != null)
        {
            _boundStats.OnHealthChanged -= OnHealthChanged;
            _boundStats.OnGoldChanged   -= OnGoldChanged;
            _boundStats.OnExpChanged    -= OnExpChanged;
            _boundStats.OnEnergyChanged -= OnEnergyChanged;
        }

        if (_boundGameSettings != null)
            _boundGameSettings.OnSettingsApplied -= OnGameSettingsApplied;

        UnbindSkills();

        if (_boundSkillAccess != null)
        {
            _boundSkillAccess.OnBound   -= OnSkillReaderBound;
            _boundSkillAccess.OnUnbound -= OnSkillReaderUnbound;
        }

        _boundStats = null;
        _boundGameSettings = null;
        _boundSkillAccess = null;
        if (_boundProgress != null) _boundProgress.Changed -= RefreshSaveStatus;
        _boundProgress = null;
    }

    private void OnSkillReaderBound(IPlayerSkillReader reader)
    {
        UnbindSkills();
        _skills = reader;
        if (_skills == null) return;

        _skills.OnSlotsChanged        += RefreshSkillSlots;
        _skills.OnSlotCooldownChanged += OnSlotCooldownChanged;
        _skills.Refresh();
    }

    private void OnSkillReaderUnbound()
    {
        UnbindSkills();
    }

    private void UnbindSkills()
    {
        if (_skills == null) return;
        _skills.OnSlotsChanged        -= RefreshSkillSlots;
        _skills.OnSlotCooldownChanged -= OnSlotCooldownChanged;
        _skills = null;
    }

    private void RefreshSkillSlots()
    {
        if (skillSlots == null || _skills == null) return;

        int count = Mathf.Min(skillSlots.Count, _skills.SlotCount);
        for (int i = 0; i < count; i++)
        {
            if (skillSlots[i] == null) continue;
            skillSlots[i].Bind(_skills.GetEquippedSkill(i), _skills.GetHotkeyDisplay(i));
            skillSlots[i].SetCooldownNormalized(_skills.GetCooldownNormalized(i));
        }
    }

    private void OnSlotCooldownChanged(int slotIndex)
    {
        if (skillSlots == null || _skills == null) return;
        if (slotIndex < 0 || slotIndex >= skillSlots.Count) return;
        if (skillSlots[slotIndex] == null) return;

        skillSlots[slotIndex].SetCooldownNormalized(_skills.GetCooldownNormalized(slotIndex));
    }

    private void OnHealthChanged(int current, int max)
    {
        if (hpBar != null)
            hpBar.value = max > 0 ? (float)current / max : 0f;
    }

    private void OnGoldChanged(int gold)
    {
        if (goldText != null) goldText.text = $"{gold}";
    }

    private void OnExpChanged(int exp)
    {
        if (moneyText != null) moneyText.text = $"{exp}";
        if (levelText != null) levelText.text = $"LV.{_stats.Level}";
        bool capped = _stats.ExpToNextLevel == 0;
        if (expBar != null) expBar.value = capped ? 1f : (float)_stats.ExpInLevel / _stats.ExpToNextLevel;
        if (expProgressText != null) expProgressText.text = capped ? "已满级" : $"{_stats.ExpInLevel} / {_stats.ExpToNextLevel}";
    }

    private void RefreshSaveStatus()
    {
        RefreshQuestHud();
        if (saveStatusText != null) saveStatusText.text = _progress != null && _progress.HasUnsavedChanges
            ? _progress.StatusMessage + "\n可在菜单中重试保存。" : string.Empty;
    }

    private void OnEnergyChanged(float current, float max)
    {
        if (mpBar != null)
            mpBar.value = max > 0f ? current / max : 0f;
    }

    private void OnGameSettingsApplied(GameSettings settings) => ApplyGameSettings(settings);

    private void ApplyGameSettings(GameSettings settings)
    {
        if (settings == null) return;

        bool showHud = settings.ShowHud;
        SetActiveIfNotNull(avatarImage?.gameObject, showHud);
        SetActiveIfNotNull(hpBar?.gameObject,       showHud);
        SetActiveIfNotNull(mpBar?.gameObject,       showHud);
        SetActiveIfNotNull(goldText?.gameObject,    showHud);
        SetActiveIfNotNull(moneyText?.gameObject,   showHud);
        SetActiveIfNotNull(miniMap?.gameObject,     showHud && settings.ShowMiniMap);
        SetActiveIfNotNull(skillBarRoot,            showHud);
        SetActiveIfNotNull(levelText?.gameObject, showHud);
        SetActiveIfNotNull(expBar?.gameObject, showHud);
        SetActiveIfNotNull(questTracker?.gameObject, showHud);
        SetActiveIfNotNull(consumableBar?.gameObject, showHud);
        SetActiveIfNotNull(statusBar?.gameObject, showHud);
    }

    private static void SetActiveIfNotNull(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}
