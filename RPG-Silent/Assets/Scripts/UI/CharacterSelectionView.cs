using System;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

/// <summary>Read-only character browsing; only Continue changes the active save.</summary>
public sealed class CharacterSelectionView : MonoBehaviour
{
    const int PageSize = 6;
    PlayerProgressService progress;
    CharacterSaveInfo[] characters = Array.Empty<CharacterSaveInfo>();
    readonly Button[] cards = new Button[PageSize];
    RectTransform panel;
    CharacterModelPreview preview;
    TMP_Text heading, details, feedback, pageLabel;
    Button previous, next, resume;
    Action<string> onContinue;
    Action onBack;
    int page;
    public string SelectedKey { get; private set; }
    public CharacterModelPreview Preview => preview;
    public int CharacterCount => characters.Length;
    public int PageCount => Mathf.Max(1, (characters.Length + PageSize - 1) / PageSize);

    public void Initialize(PlayerProgressService service, TMP_FontAsset font, Action<string> continueCharacter, Action create, Action back)
    {
        progress = service;
        onContinue = continueCharacter;
        onBack = back;
        panel = UIView.Panel(transform, "CharactersPage");
        UIView.Text(panel, "Brand", "S I L E N T   /   角色存档");
        UIView.Text(panel, "Title", "继续你的冒险");
        UIView.Text(panel, "Hint", "每个角色独立成长；创建新角色会保留已有进度。");
        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            cards[i] = UIView.Button(panel, "Character:" + i, "", () => Select(characters[page * PageSize + index].Key));
            var text = cards[i].GetComponentInChildren<TMP_Text>();
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        previous = UIView.Button(panel, "PreviousPage", "上一页", () => ChangePage(-1));
        pageLabel = UIView.Text(panel, "Page", "");
        pageLabel.alignment = TextAlignmentOptions.Center;
        next = UIView.Button(panel, "NextPage", "下一页", () => ChangePage(1));
        var portrait = UIView.Rect(panel, "Portrait");
        preview = portrait.gameObject.GetComponent<CharacterModelPreview>();
        UIView.Text(portrait, "PreviewHint", "拖动旋转 · 滚轮缩放").alignment = TextAlignmentOptions.Center;
        heading = UIView.Text(panel, "SelectedCharacter", "");
        details = UIView.Text(panel, "CharacterDetails", "");
        feedback = UIView.Text(panel, "Feedback", "");
        UIView.Button(panel, "Back", "返回", () => onBack?.Invoke());
        UIView.Button(panel, "Refresh", "刷新列表", Refresh);
        UIView.Button(panel, "CreateCharacter", "创建新角色", () => create?.Invoke());
        resume = UIView.Button(panel, "ContinueCharacter", "进入主城  →", ContinueSelected);
        Refresh();
    }

    public void Refresh()
    {
        characters = progress.GetCharacters(out string message);
        feedback.text = message;
        if (!characters.Any(x => x.Key == SelectedKey))
            SelectedKey = characters.FirstOrDefault(x => x.Progress.CanContinue)?.Key ?? characters.FirstOrDefault()?.Key;
        int selected = Array.FindIndex(characters, x => x.Key == SelectedKey);
        page = selected < 0 ? 0 : selected / PageSize;
        RefreshCards();
        RefreshPreview();
    }

    public void Select(string key)
    {
        int index = Array.FindIndex(characters, x => x.Key == key);
        if (index < 0)
            return;
        SelectedKey = key;
        page = index / PageSize;
        feedback.text = "";
        RefreshCards();
        RefreshPreview();
    }

    public void ChangePage(int delta)
    {
        page = Mathf.Clamp(page + delta, 0, PageCount - 1);
        RefreshCards();
    }

    string Name(CharacterSaveInfo entry)
    {
        var data = entry.Progress.Data;
        var definition = data != null ? progress.Classes.FirstOrDefault(x => x.Id == data.classId) : null;
        return definition == null ? "无法读取的角色" : definition.DisplayName + " · " + (data.gender == (int)PlayerGender.Female ? "女" : data.gender == (int)PlayerGender.Male ? "男" : "原始外观");
    }

    void RefreshCards()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            int index = page * PageSize + i;
            cards[i].gameObject.SetActive(index < characters.Length);
            if (index >= characters.Length)
                continue;
            var character = characters[index];
            var data = character.Progress.Data;
            int level = 1;
            if (data != null)
                while (level < PlayerStats.LevelCap && data.totalExp >= PlayerStats.ExpAtLevel(level + 1))
                    level++;
            string identity = character.IsLegacy ? "原有单角色存档" : "角色 " + character.Key.Substring(0, 8);
            cards[i].GetComponentInChildren<TMP_Text>().text = Name(character) + (data != null ? " · Lv." + level : "") + "\n" + identity + " · " + (character.Progress.CanContinue ? character.ModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "暂不可继续 · 文件已保留");
            cards[i].image.color = SelectedKey == character.Key ? W.Accent : W.Card;
        }

        pageLabel.text = $"{page + 1} / {PageCount} 页 · {characters.Length} 个角色";
        previous.interactable = page > 0;
        next.interactable = page + 1 < PageCount;
    }

    void RefreshPreview()
    {
        var character = characters.FirstOrDefault(x => x.Key == SelectedKey);
        bool available = character != null && character.Progress.CanContinue;
        resume.interactable = available;
        if (!available)
        {
            preview.Hide();
            heading.text = character != null ? "此角色暂不可继续" : "还没有角色";
            details.text = character?.Progress.Message ?? "创建角色，选择职业与外观后开始冒险。";
            return;
        }

        var data = character.Progress.Data;
        var definition = progress.Classes.Single(x => x.Id == data.classId);
        var armor = data.inventory.equipped.Select(key => data.inventory.Find(key)).Where(x => x != null).Select(x => progress.FindItem(x.itemId)).ToArray();
        var weapon = progress.FindItem(data.weaponIds[data.activeWeaponSlot])?.Weapon;
        var rig = Resources.Load<GameObject>("RPG-Character");
        var stance = rig != null ? rig.GetComponentInChildren<PlayerStanceController>(true) : null;
        var idle = stance != null ? stance.GetPreviewIdle(weapon != null ? weapon.Stance : PlayerStance.Unarmed) : definition.PreviewIdle;
        preview.ShowLoadout(definition, (PlayerGender)data.gender, armor, weapon, false, idle ?? definition.PreviewIdle);
        heading.text = Name(character);
        details.text = $"金币 {data.gold} · 已学技能 {data.learnedSkills.Length} · 背包 {data.inventory.bag.Length}/{InventoryData.Capacity}\n" + (character.Progress.Status == ProgressLoadStatus.Recovered ? character.Progress.Message : "进入主城，继续该角色的装备、技能与任务进度。");
    }

    public void ContinueSelected()
    {
        if (resume.interactable)
        {
            resume.interactable = false;
            onContinue?.Invoke(SelectedKey);
        }
    }

    public void ShowFailure(string message)
    {
        Refresh();
        feedback.text = message;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            onBack?.Invoke();
    }
}
