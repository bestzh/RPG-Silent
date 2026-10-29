using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using TMPro;

public class SettingsUI : UIBase
{
    [Inject]
    private IUIService _uiService;
    public Button Screen;
    public Button Sound;
    public Button Controller;
    public Button Game;
    public Button Back;
    private SettingsPage[] pages;
    private string _returnUiKey;
    private RectTransform _content, _shade;
    private TMP_Text saveHint;
    private const string SaveHint = "应用后保存；返回撤销未应用修改；恢复默认仅预览。";
    private class SettingsPage
    {
        public Button Button;
        public GameObject Select;
        public GameObject Page;
    }

    private void Awake()
    {
        InitPages();
        _content = UIView.Find(transform, "Image") as RectTransform;
        _shade = UIView.Panel(transform, "SettingsShade");
        _shade.SetAsFirstSibling();
        AddPageButtonListener(Screen);
        AddPageButtonListener(Sound);
        AddPageButtonListener(Controller);
        AddPageButtonListener(Game);
        Back?.onClick.AddListener(OnBackButtonClicked);
        var right = UIView.Find(transform, "Image/Right");
        if (right != null && Back != null)
        {
            saveHint = UIView.Text(right, "SettingsSaveHint", SaveHint);
            var rect = saveHint.rectTransform;
            saveHint.alignment = TextAlignmentOptions.Center;
        }
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        _returnUiKey = args.Length > 0 ? args[0] as string : null;
        ShowPage(Screen);
        ShowNotice(SaveHint);
    }

    public void HandleBack()
    {
        foreach (var dropdown in GetComponentsInChildren<TMP_Dropdown>())
            if (dropdown.IsExpanded)
            {
                dropdown.Hide();
                return;
            }

        OnBackButtonClicked();
    }

    public void ShowNotice(string text)
    {
        if (saveHint != null)
            saveHint.text = string.IsNullOrEmpty(text) ? SaveHint : text;
    }

    private void OnBackButtonClicked()
    {
        _uiService.CloseUI("UI/SettingsUI");
        if (!string.IsNullOrEmpty(_returnUiKey))
            _uiService.OpenUI(_returnUiKey);
    }

    private void InitPages()
    {
        Transform leftControl = UIView.Find(transform, "Image/Lift/Control") ?? UIView.Find(transform, "Image/Left/Control");
        Transform right = UIView.Find(transform, "Image/Right");
        Screen = ResolveButton(Screen, leftControl, "Screen");
        Sound = ResolveButton(Sound, leftControl, "Sound");
        Controller = ResolveButton(Controller, leftControl, "Controller");
        Game = ResolveButton(Game, leftControl, "Game");
        Back = ResolveButton(Back, leftControl, "Back");
        pages = new[]
        {
            CreatePage(Screen, right, "Screen"),
            CreatePage(Sound, right, "Sound"),
            CreatePage(Controller, right, "Controller"),
            CreatePage(Game, right, "Game")
        };
    }

    private Button ResolveButton(Button button, Transform parent, string buttonName)
    {
        if (button != null || parent == null)
            return button;
        Transform t = UIView.Find(parent, buttonName);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private SettingsPage CreatePage(Button button, Transform right, string pageName)
    {
        return new SettingsPage
        {
            Button = button,
            Select = button != null ? UIView.Find(button.transform, "select")?.gameObject : null,
            Page = right != null ? UIView.Find(right, pageName)?.gameObject : null
        };
    }

    private void AddPageButtonListener(Button button)
    {
        if (button == null)
            return;
        button.onClick.AddListener(() => ShowPage(button));
    }

    private void ShowPage(Button activeButton)
    {
        if (pages == null || pages.Length == 0)
            InitPages();
        foreach (SettingsPage page in pages)
        {
            bool isActive = page.Button == activeButton;
            page.Select?.SetActive(isActive);
            page.Page?.SetActive(isActive);
        }
    }
}
