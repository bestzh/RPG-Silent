using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : UIBase
{
    public Button Screen;
    public Button Sound;
    public Button Controller;
    public Button Game;
    public Button Back;

    private SettingsPage[] pages;

    private class SettingsPage
    {
        public Button Button;
        public GameObject Select;
        public GameObject Page;
    }

    private void Awake()
    {
        InitPages();

        AddPageButtonListener(Screen);
        AddPageButtonListener(Sound);
        AddPageButtonListener(Controller);
        AddPageButtonListener(Game);

        Back?.onClick.AddListener(OnBackButtonClicked);
    }

    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);
        ShowPage(Screen);
    }

    private void OnBackButtonClicked()
    {
        UIManager.Instance.OpenUI("UI/StartUI");
        UIManager.Instance.CloseUI("UI/SettingsUI");
    }

    private void InitPages()
    {
        Transform leftControl = transform.Find("Image/Left/Control");
        Transform right = transform.Find("Image/Right");

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
        {
            return button;
        }

        Transform buttonTransform = parent.Find(buttonName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private SettingsPage CreatePage(Button button, Transform right, string pageName)
    {
        return new SettingsPage
        {
            Button = button,
            Select = button != null ? button.transform.Find("select")?.gameObject : null,
            Page = right != null ? right.Find(pageName)?.gameObject : null
        };
    }

    private void AddPageButtonListener(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.AddListener(() => ShowPage(button));
    }

    private void ShowPage(Button activeButton)
    {
        if (pages == null || pages.Length == 0)
        {
            InitPages();
        }

        foreach (SettingsPage page in pages)
        {
            bool isActive = page.Button == activeButton;
            page.Select?.SetActive(isActive);
            page.Page?.SetActive(isActive);
        }
    }
}
