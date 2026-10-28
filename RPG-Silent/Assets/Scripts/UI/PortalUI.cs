using System.Collections.Generic;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 传送门 UI：传入传送门 ID，左侧列出该传送门对应的副本按钮，
/// 点击后右侧显示背景图与介绍，底部选择难度，确认进入副本，退出关闭页面。
/// </summary>
public class PortalUI : UIBase
{
    private IUIService     _uiService;
    private IPortalService _portalService;

    [Inject]
    public void Construct(IUIService uiService, IPortalService portalService)
    {
        _uiService     = uiService;
        _portalService = portalService;
    }

    [Header("数据表（留空则自动从 Resources 加载 PortalDatabase）")]
    [SerializeField] private PortalDatabase portalDatabase;

    [Header("左侧副本列表")]
    [Tooltip("Scroll View/Viewport/Content")]
    [SerializeField] private Transform contentRoot;
    [Tooltip("Content 下作为模板的 Item，会被克隆，自身保持隐藏")]
    [SerializeField] private GameObject itemTemplate;

    [Header("右侧详情")]
    [Tooltip("BG，副本背景图")]
    [SerializeField] private RawImage bgImage;
    [Tooltip("BG/Description，副本介绍")]
    [SerializeField] private TMP_Text descriptionText;

    [Header("底部")]
    [Tooltip("Bottom/Dropdown，难度选择")]
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [Tooltip("Bottom/OK，确认进入")]
    [SerializeField] private Button okButton;
    [Tooltip("Bottom/Quite，退出关闭")]
    [SerializeField] private Button quitButton;

    [Header("进入副本流程")]
    [SerializeField] private string loadingUiKey   = "UI/LoadingUI";
    [SerializeField] private string dungeonHudUiKey = "UI/MainUI";

    [Header("选中高亮")]
    [Tooltip("Item 下用于高亮的子物体名；存在则切换其显隐，否则改 Item 自身 Image 颜色")]
    [SerializeField] private string highlightChildName = "Select";
    [SerializeField] private Color normalColor   = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.4f, 1f);


    private class DungeonItem
    {
        public GameObject            Root;
        public DungeonDatabase.Entry Dungeon;
        public GameObject            Highlight;
        public Image                 Background;
    }

    private readonly List<DungeonItem> _items = new List<DungeonItem>();
    private List<DungeonDatabase.DifficultyTier> _currentTiers = new List<DungeonDatabase.DifficultyTier>();
    private DungeonDatabase.Entry _selectedDungeon;
    private Button challengeButton;
    private DungeonChallenge challenge;

    protected override void OnInit()
    {
        AutoBind();
        if (bgImage != null && descriptionText != null)
        {
            var backdrop = ProgressionUIWidgets.Rect(bgImage.transform, "DescriptionCard", 0, 0, 0, 300);
            backdrop.anchorMin = new Vector2(0, 0); backdrop.anchorMax = new Vector2(1, 0);
            backdrop.pivot = new Vector2(.5f, 0); backdrop.sizeDelta = new Vector2(-80, 300);
            backdrop.anchoredPosition = new Vector2(0, 8);
            backdrop.gameObject.AddComponent<Image>().color = new Color(.04f, .07f, .1f, .9f);
            var description = descriptionText.rectTransform;
            description.anchorMin = new Vector2(0, 0); description.anchorMax = new Vector2(1, 0);
            description.pivot = new Vector2(.5f, 0); description.sizeDelta = new Vector2(-120, 260);
            description.anchoredPosition = new Vector2(0, 28);
            descriptionText.fontSize = 28; descriptionText.enableAutoSizing = false;
            descriptionText.alignment = TextAlignmentOptions.TopLeft;
            description.SetAsLastSibling();
        }
        if (difficultyDropdown != null && descriptionText != null)
        {
            challengeButton = ProgressionUIWidgets.Button(bgImage != null ? bgImage.transform : difficultyDropdown.transform.parent, descriptionText.font, "ChallengeMode", "", 0, 0, 340, 48, () =>
            { challenge = (DungeonChallenge)(((int)challenge + 1) % 3); OnDifficultyChanged(difficultyDropdown.value); });
            var rect = challengeButton.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one; rect.anchoredPosition = new Vector2(-32,-28);
        }

        if (portalDatabase == null)
            portalDatabase = Resources.Load<PortalDatabase>("PortalDatabase");

        if (itemTemplate != null)
            itemTemplate.SetActive(false);

        okButton?.onClick.AddListener(OnConfirmClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
        difficultyDropdown?.onValueChanged.AddListener(OnDifficultyChanged);
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        int portalId = (args != null && args.Length > 0 && args[0] is int id) ? id : 0;
        BuildDungeonList(portalId);
    }

    public override void OnClose()
    {
        base.OnClose();
        ClearItems();
        _selectedDungeon = null;
    }

    private void BuildDungeonList(int portalId)
    {
        ClearItems();
        _selectedDungeon = null;

        if (portalDatabase == null)
        {
            Debug.LogError("[PortalUI] 未找到 PortalDatabase，无法构建副本列表。");
            return;
        }

        if (contentRoot == null || itemTemplate == null)
        {
            Debug.LogError("[PortalUI] contentRoot 或 itemTemplate 未配置。");
            return;
        }

        IReadOnlyList<DungeonDatabase.Entry> dungeons = portalDatabase.GetDungeonsForPortal(portalId);

        foreach (DungeonDatabase.Entry dungeon in dungeons)
        {
            GameObject go = Instantiate(itemTemplate, contentRoot);
            go.SetActive(true);

            TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = dungeon.DisplayName;
            }

            var item = new DungeonItem
            {
                Root       = go,
                Dungeon    = dungeon,
                Highlight  = FindHighlight(go),
                Background = go.GetComponent<Image>()
            };
            _items.Add(item);

            Button button = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>(true);
            DungeonItem captured = item;
            button?.onClick.AddListener(() => SelectItem(captured));
        }

        if (_items.Count > 0)
            SelectItem(_items[0]);
        else
            ShowEmptyDetail();
    }

    private void SelectItem(DungeonItem item)
    {
        _selectedDungeon = item.Dungeon;
        challenge = DungeonChallenge.Exploration;
        UpdateHighlight(item);

        if (bgImage != null)
        {
            Sprite bg = item.Dungeon.BackgroundImage;
            bgImage.texture = bg != null ? bg.texture : null;
            bgImage.enabled = bg != null;
        }

        if (descriptionText != null)
            descriptionText.text = item.Dungeon.Description;

        PopulateDifficulties(item.Dungeon);
        OnDifficultyChanged(difficultyDropdown != null ? difficultyDropdown.value : 0);
        if (okButton != null) okButton.interactable = _currentTiers.Count > 0;
    }

    private void UpdateHighlight(DungeonItem selected)
    {
        foreach (DungeonItem item in _items)
        {
            bool isSelected = item == selected;

            if (item.Highlight != null)
                item.Highlight.SetActive(isSelected);
            else if (item.Background != null)
                item.Background.color = isSelected ? selectedColor : normalColor;
        }
    }

    private GameObject FindHighlight(GameObject itemRoot)
    {
        if (string.IsNullOrEmpty(highlightChildName)) return null;

        Transform highlight = itemRoot.transform.Find(highlightChildName);
        if (highlight == null) return null;

        highlight.gameObject.SetActive(false);
        return highlight.gameObject;
    }

    private void PopulateDifficulties(DungeonDatabase.Entry dungeon)
    {
        _currentTiers = new List<DungeonDatabase.DifficultyTier>(dungeon.GetEnabledDifficultyTiers());

        if (difficultyDropdown == null) return;

        var options = new List<string>();
        foreach (DungeonDatabase.DifficultyTier tier in _currentTiers)
            options.Add(tier.DifficultyLabel);

        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(options);
        difficultyDropdown.value = 0;
        difficultyDropdown.RefreshShownValue();
        difficultyDropdown.interactable = _currentTiers.Count > 0;
    }

    private void ShowEmptyDetail()
    {
        if (bgImage != null) bgImage.enabled = false;
        if (descriptionText != null) descriptionText.text = "暂无可用副本";

        _currentTiers.Clear();
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.interactable = false;
        }

        if (okButton != null) okButton.interactable = false;
    }

    private void OnDifficultyChanged(int index)
    {
        if (_selectedDungeon == null || descriptionText == null || index < 0 || index >= _currentTiers.Count) return;
        var tier = _currentTiers[index];
        if (challengeButton != null)
        { challengeButton.gameObject.SetActive(!_selectedDungeon.IsTraining); challengeButton.GetComponentInChildren<TMP_Text>().text = "模式：" + DungeonChallengeRules.Label(challenge) + "  >"; }
        descriptionText.text = _selectedDungeon.IsTraining ? _selectedDungeon.Description + "\n\n训练不发放金币、经验或物品；使用已学技能。训练区可战斗，主城仍然禁战。" :
            $"{_selectedDungeon.Description}\n\n{DungeonChallengeRules.Label(challenge)} · 敌人生命 ×{tier.EnemyHealthMultiplier:0.##} / 攻击 ×{tier.EnemyDamageMultiplier:0.##}\n" +
            (challenge == DungeonChallenge.Timed ? $"目标：{DungeonChallengeRules.Duration(_selectedDungeon.ChallengeTimeLimit)} 内通关，额外奖励随难度提升。超时仍可继续探索。\n" :
             challenge == DungeonChallenge.BossTrial ? "只挑战强化首领：额外生命 ×2、伤害 ×1.25，击败后获得试炼奖励。\n" : "宝箱、机关和隐藏支路可探索，精英事件可选。\n") +
            "金币 / 经验自动吸附，装备与材料靠近后按交互键拾取。";
    }

    private void OnConfirmClicked()
    {
        if (_selectedDungeon == null || _currentTiers.Count == 0)
        {
            Debug.LogWarning("[PortalUI] 未选择有效副本或难度。");
            return;
        }

        if (string.IsNullOrEmpty(_selectedDungeon.SceneKey))
        {
            Debug.LogError($"[PortalUI] 副本「{_selectedDungeon.DisplayName}」未配置 SceneKey。");
            return;
        }

        int index = difficultyDropdown != null
            ? Mathf.Clamp(difficultyDropdown.value, 0, _currentTiers.Count - 1)
            : 0;
        DungeonDatabase.DifficultyTier tier = _currentTiers[index];

        bool previousHasValue = DungeonLaunchContext.HasValue;
        int previousDungeonId = DungeonLaunchContext.DungeonId;
        DungeonDifficulty previousDifficulty = DungeonLaunchContext.Difficulty;
        DungeonChallenge previousChallenge = DungeonLaunchContext.Challenge;
        System.Action restoreSelection = () =>
        {
            if (previousHasValue) DungeonLaunchContext.Set(previousDungeonId, previousDifficulty);
            else DungeonLaunchContext.Clear();
            DungeonLaunchContext.SetChallenge(previousChallenge);
        };
        DungeonLaunchContext.Set(_selectedDungeon.Id, tier.Difficulty);
        DungeonLaunchContext.SetChallenge(_selectedDungeon.IsTraining ? DungeonChallenge.Exploration : challenge);
        Debug.Log($"[PortalUI] 进入副本「{_selectedDungeon.DisplayName}」难度：{tier.DifficultyLabel}");

        string sceneKey = _selectedDungeon.SceneKey;
        ClosePortalUI();
        _uiService.OpenUI(loadingUiKey, sceneKey, dungeonHudUiKey, restoreSelection);
    }

    private void OnQuitClicked()
    {
        ClosePortalUI();
    }

    private void ClosePortalUI()
    {
        // 优先经服务关闭，以恢复玩家输入与光标；服务缺失时退回直接关闭
        if (_portalService != null)
            _portalService.ClosePortal();
        else
            _uiService.CloseUI(UIName);
    }

    private void ClearItems()
    {
        foreach (DungeonItem item in _items)
        {
            if (item?.Root != null) Destroy(item.Root);
        }

        _items.Clear();
    }

    private void AutoBind()
    {
        if (contentRoot == null)
            contentRoot = transform.Find("Scroll View/Viewport/Content");

        if (itemTemplate == null && contentRoot != null)
        {
            Transform item = contentRoot.Find("Item");
            if (item != null) itemTemplate = item.gameObject;
        }

        if (bgImage == null)
        {
            Transform bg = transform.Find("BG");
            if (bg != null) bgImage = bg.GetComponent<RawImage>();
        }

        if (descriptionText == null)
        {
            Transform desc = transform.Find("BG/Description");
            if (desc != null) descriptionText = desc.GetComponent<TMP_Text>();
        }

        if (difficultyDropdown == null)
        {
            Transform dropdown = transform.Find("Bottom/Dropdown");
            if (dropdown != null) difficultyDropdown = dropdown.GetComponent<TMP_Dropdown>();
        }

        if (okButton == null)
        {
            Transform ok = transform.Find("Bottom/OK");
            if (ok != null) okButton = ok.GetComponent<Button>();
        }

        if (quitButton == null)
        {
            Transform quit = transform.Find("Bottom/Quite");
            if (quit != null) quitButton = quit.GetComponent<Button>();
        }
    }
}
