using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Owns a temporary selection. Progress changes only after the final confirmation.</summary>
public sealed class CharacterCreationView : MonoBehaviour
{
    static readonly Color Gold = new(.83f, .70f, .45f);
    static readonly Color Muted = new(.58f, .68f, .72f);
    static readonly Color Surface = new(.075f, .115f, .145f);
    readonly Dictionary<string, Image> classCards = new();
    readonly Dictionary<PlayerGender, Button> genderButtons = new();
    PlayerClassDefinition[] definitions;
    Action<string, PlayerGender> confirm;
    Action cancel;
    RectTransform page;
    TMP_FontAsset font;
    TMP_Text title, description, role, weapon, branches, selectedLabel, error;
    Button start;
    CharacterModelPreview preview;
    public PlayerClassDefinition SelectedClass { get; private set; }
    public PlayerGender SelectedGender { get; private set; } = PlayerGender.Male;
    public CharacterModelPreview Preview => preview;

    public void Initialize(IEnumerable<PlayerClassDefinition> classes, TMP_FontAsset uiFont,
        Action<string, PlayerGender> onConfirm, Action onCancel)
    {
        definitions = classes.ToArray(); font = uiFont; confirm = onConfirm; cancel = onCancel;
        page = ProgressionUIWidgets.CenterPanel(transform, "CreationPage", 1600, 900);
        page.GetComponent<Image>().color = new Color(.035f, .055f, .075f);
        Label(page, "Brand", "S I L E N T   /   角色创建", 60, 30, 900, 36, 22, Gold);
        Label(page, "Heading", "选择你的起点", 60, 76, 1000, 72, 52, Color.white);
        Label(page, "Subtitle", "每一种选择，都有自己的冒险方式。", 62, 155, 1100, 38, 24, Muted);
        Label(page, "Step", "01   /   职业与外观", 1260, 65, 280, 42, 22, Gold).alignment = TextAlignmentOptions.Right;
        Line(page, 60, 210, 1480, Gold * new Color(1, 1, 1, .4f));
        Label(page, "ClassCaption", "选择职业", 60, 235, 300, 38, 24, Muted);
        var baseClasses = definitions.Where(c => !c.IsAdvanced).OrderBy(c => c.Id == "swordsman" ? 0 : c.Id == "mage" ? 1 : 2).ToArray();
        for (int i = 0; i < baseClasses.Length; i++)
        {
            var choice = baseClasses[i];
            var button = Button(page, choice.Id, "", 60, 290 + i * 126, 300, 108, () => ChooseClass(choice.Id));
            classCards.Add(choice.Id, (Image)button.targetGraphic);
            Label(button.transform, "Number", "0" + (i + 1), 20, 16, 45, 30, 20, Gold);
            Label(button.transform, "Name", choice.DisplayName, 78, 12, 200, 42, 32, Color.white);
            Label(button.transform, "Role", RoleLabel(choice.Id), 78, 60, 200, 32, 21, Muted);
        }
        Label(page, "BeginnerNote", "从普攻开始冒险\n拜访导师，学习专属技能", 60, 700, 310, 78, 23, Muted);

        var portrait = ProgressionUIWidgets.Rect(page, "Portrait", 400, 235, 660, 510);
        portrait.gameObject.AddComponent<RawImage>().color = Color.white;
        preview = portrait.gameObject.AddComponent<CharacterModelPreview>();
        Label(portrait, "LiveCaption", "角色预览", 22, 16, 250, 36, 22, Gold);
        Label(portrait, "RotateHint", "拖动旋转  ·  滚轮缩放", 170, 468, 330, 30, 20, Muted).alignment = TextAlignmentOptions.Center;
        Button(portrait, "RotateLeft", "左转", 18, 452, 88, 42, () => preview.Rotate(30));
        Button(portrait, "RotateRight", "右转", 554, 452, 88, 42, () => preview.Rotate(-30));
        Line(page, 400, 235, 660, Gold * new Color(1, 1, 1, .65f));
        genderButtons[PlayerGender.Male] = Button(page, "Male", "男", 552, 760, 164, 48, () => ChooseGender(PlayerGender.Male));
        genderButtons[PlayerGender.Female] = Button(page, "Female", "女", 736, 760, 164, 48, () => ChooseGender(PlayerGender.Female));

        role = Label(page, "Role", "", 1110, 238, 410, 36, 23, Gold);
        title = Label(page, "ClassName", "", 1106, 284, 410, 74, 56, Color.white);
        description = Label(page, "Description", "", 1110, 378, 405, 105, 26, new Color(.83f, .88f, .88f));
        Line(page, 1110, 510, 430, new Color(.2f, .29f, .32f));
        Label(page, "WeaponCaption", "初始武器", 1110, 534, 400, 36, 23, Muted);
        weapon = Label(page, "Weapon", "", 1110, 577, 410, 40, 28, Color.white);
        Label(page, "BranchCaption", "10 级 · 解锁转职", 1110, 655, 410, 36, 23, Gold);
        branches = Label(page, "Branches", "", 1110, 701, 420, 90, 27, Color.white);

        Line(page, 60, 832, 1480, new Color(.2f, .29f, .32f));
        Button(page, "Back", "返回", 60, 850, 140, 44, () => cancel?.Invoke());
        selectedLabel = Label(page, "Selected", "", 240, 853, 650, 40, 24, Muted);
        start = Button(page, "StartAdventure", "踏入主城  →", 1110, 840, 430, 54, Submit);
        start.GetComponent<Image>().color = Gold;
        start.GetComponentInChildren<TMP_Text>().color = new Color(.06f, .09f, .10f);
        error = Label(page, "Error", "", 400, 807, 660, 28, 20, new Color(1, .65f, .5f));
        error.alignment = TextAlignmentOptions.Center;
        ChooseClass(baseClasses[0].Id);
        Fit();
    }
    TMP_Text Label(Transform parent, string name, string content, float x, float y, float w, float h, float size, Color color)
    {
        var text = ProgressionUIWidgets.Text(parent, font, name, content, x, y, w, h, size);
        text.color = color; return text;
    }
    Button Button(Transform parent, string name, string content, float x, float y, float w, float h, UnityEngine.Events.UnityAction callback)
    {
        var b = ProgressionUIWidgets.Button(parent, font, name, content, x, y, w, h, callback);
        b.GetComponent<Image>().color = Surface;
        b.GetComponentInChildren<TMP_Text>().fontSize = 24;
        var colors = b.colors; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f); colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.8f, .85f, .9f); b.colors = colors;
        return b;
    }
    void Line(Transform parent, float x, float y, float w, Color color)
    {
        var line = ProgressionUIWidgets.Rect(parent, "Rule", x, y, w, 1).gameObject.AddComponent<Image>();
        line.color = color; line.raycastTarget = false;
    }
    public void ChooseClass(string id)
    {
        var choice = definitions.FirstOrDefault(c => c.Id == id && !c.IsAdvanced);
        if (choice == null) return;
        SelectedClass = choice;
        title.text = choice.DisplayName; role.text = RoleLabel(choice.Id);
        description.text = choice.Description;
        weapon.text = choice.StartingWeapon != null ? choice.StartingWeapon.DisplayName : "徒手";
        branches.text = string.Join("  /  ", definitions.Where(c => c.BaseClass == choice).Select(c => c.DisplayName));
        foreach (var card in classCards) card.Value.color = card.Key == id ? new Color(.19f, .28f, .31f) : Surface;
        RefreshAppearance();
    }
    public void ChooseGender(PlayerGender gender)
    {
        if (gender != PlayerGender.Male && gender != PlayerGender.Female) return;
        SelectedGender = gender; RefreshAppearance();
    }
    void RefreshAppearance()
    {
        if (preview == null || SelectedClass == null) return;
        bool ready = SelectedClass.GetModel(SelectedGender) != null && SelectedClass.PreviewIdle != null;
        preview.Show(SelectedClass, SelectedGender);
        foreach (var pair in genderButtons)
        {
            pair.Value.GetComponent<Image>().color = pair.Key == SelectedGender ? Gold : Surface;
            pair.Value.GetComponentInChildren<TMP_Text>().color = pair.Key == SelectedGender ? new Color(.06f, .09f, .1f) : Color.white;
        }
        if (start != null) start.interactable = ready;
        if (error != null) error.text = ready ? "" : "角色资源暂不可用，请选择其他外观。";
        if (selectedLabel != null) selectedLabel.text = SelectedClass.DisplayName + "  /  " + (SelectedGender == PlayerGender.Male ? "男" : "女") + "     ·     初始等级 1";
    }
    public void Submit()
    {
        if (start == null || !start.interactable) return;
        start.interactable = false;
        confirm?.Invoke(SelectedClass.Id, SelectedGender);
    }
    public void ShowFailure(string message) { RefreshAppearance(); error.text = message; }
    void OnEnable() { if (SelectedClass != null) RefreshAppearance(); }
    void Update()
    {
        Fit();
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) cancel?.Invoke();
    }
    void Fit()
    {
        if (page == null) return;
        var r = ((RectTransform)transform).rect;
        page.localScale = Vector3.one * Mathf.Min(r.width / 1600, r.height / 900);
    }
    static string RoleLabel(string id) => id == "swordsman" ? "近战 · 连击" : id == "mage" ? "远程 · 法术" : "远程 · 灵巧";
}
