using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed partial class InventoryUI : UIBase
{
    [Inject] PlayerProgressService progress;
    public TMP_FontAsset Font;
    PlayerInventoryInteraction interaction;
    PlayerController player;
    CharacterModelPreview preview;
    public CharacterModelPreview Preview => preview;
    public ItemDefinition TriedItem => progress?.FindItem(tryOnItemId);
    RectTransform panel;
    TMP_Text summary, capacity, title, description, comparison, affixText, gemText, setText, status, pageLabel, previewCaption, previewTitle;
    readonly Button[] cells = new Button[24];
    readonly Image[] icons = new Image[24];
    readonly TMP_Text[] labels = new TMP_Text[24];
    readonly TMP_Text[] equipmentLabels = new TMP_Text[5];
    readonly Button[] tabs = new Button[5];
    Button wear, secondary, remove, sell, dismantle, collect, cancelTryOn;
    ItemStackData[] visible = System.Array.Empty<ItemStackData>();
    string selected, confirmation, tryOnItemId;
    int filter, page;
    protected override void OnInit()
    {
        W.Panel(transform, "Shade", new Color(.015f, .025f, .04f, .94f));
        panel = W.CenterPanel(transform, "Inventory", 1680, 900);
        var heading = W.Text(panel, Font, "Heading", "行 囊 与 装 备", 30, 22, 600, 48, 34); heading.color = new Color(.87f, .74f, .49f);
        capacity = W.Text(panel, Font, "Capacity", "", 900, 28, 580, 42, 23);
        W.Button(panel, Font, "Close", "关闭 [B]", 1516, 24, 136, 44, () => interaction?.Close());
        W.Text(panel, Font, "EquipmentTitle", "当前穿戴", 28, 100, 166, 40, 24);
        for (int i = 0; i < 5; i++)
        {
            int slot = i;
            var button = W.Button(panel, Font, "Equipment" + i, "", 28, 150 + i * 88, 156, 72,
                () => Select(progress.Inventory.equipped[slot]));
            button.image.color = W.Card;
            equipmentLabels[i] = button.GetComponentInChildren<TMP_Text>(); equipmentLabels[i].fontSize = 20;
        }
        summary = W.Text(panel, Font, "Summary", "", 28, 610, 156, 190, 20);
        previewTitle = W.Text(panel, Font, "PreviewTitle", "角色外观", 200, 100, 340, 40, 24);
        var portrait = W.Rect(panel, "CharacterPreview", 200, 144, 340, 516);
        portrait.gameObject.AddComponent<RawImage>();
        preview = portrait.gameObject.AddComponent<CharacterModelPreview>();
        W.Button(panel, Font, "RotateLeft", "左转", 200, 672, 80, 36, () => preview.Rotate(-30));
        W.Button(panel, Font, "ResetView", "重置视角", 288, 672, 160, 36, preview.ResetView);
        W.Button(panel, Font, "RotateRight", "右转", 456, 672, 84, 36, () => preview.Rotate(30));
        cancelTryOn = W.Button(panel, Font, "CancelTryOn", "取消试穿 · 恢复当前外观", 200, 718, 340, 36, CancelTryOn);
        cancelTryOn.image.color = W.Card;
        var hint = W.Text(panel, Font, "PreviewHelp", "按住鼠标左键拖动旋转\n滚轮缩放", 200, 765, 340, 42, 16);
        hint.alignment = TextAlignmentOptions.Center;
        previewCaption = W.Text(panel, Font, "PreviewCaption", "", 200, 812, 340, 26, 17);
        previewCaption.alignment = TextAlignmentOptions.Center;
        string[] names = { "全部", "防具饰品", "武器", "材料药品", "待领取" };
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i] = W.Button(panel, Font, "Tab" + i, names[i], 566 + i * 122, 102, 114, 42,
                () => SelectCategory(index));
        }
        for (int i = 0; i < cells.Length; i++)
        {
            int index = i;
            cells[i] = W.Button(panel, Font, "Item" + i, "", 566 + i % 6 * 102, 158 + i / 6 * 134, 92, 124,
                () => { int position = page * 24 + index; if (position < visible.Length) Select(visible[position].Key); });
            labels[i] = cells[i].GetComponentInChildren<TMP_Text>(); labels[i].fontSize = 16;
            labels[i].rectTransform.anchoredPosition = new Vector2(4, -68); labels[i].rectTransform.sizeDelta = new Vector2(84, 52);
            var iconRect = W.Rect(cells[i].transform, "Icon", 23, 10, 46, 46);
            icons[i] = iconRect.gameObject.AddComponent<Image>(); icons[i].preserveAspect = true; icons[i].raycastTarget = false;
        }
        W.Button(panel, Font, "Previous", "上一页", 566, 718, 110, 40, () => ChangePage(Mathf.Max(0, page - 1)));
        pageLabel = W.Text(panel, Font, "Page", "", 686, 723, 104, 36, 20);
        W.Button(panel, Font, "Next", "下一页", 800, 718, 110, 40, () => ChangePage(Mathf.Min(Mathf.Max(0, (visible.Length - 1) / 24), page + 1)));
        collect = W.Button(panel, Font, "Collect", "领取物品", 990, 718, 180, 40, () => { progress.TryCollectOverflow(out string message); Show(message); });
        W.Text(panel, Font, "DetailsHeading", "物品详情", 1200, 100, 452, 40, 26);
        title = W.Text(panel, Font, "ItemTitle", "", 1200, 156, 452, 52, 28);
        description = W.Text(panel, Font, "Description", "", 1200, 216, 452, 68, 18);
        affixText = W.Text(panel, Font, "Affixes", "", 1200, 290, 452, 108, 17);
        affixText.color = new Color(.55f, .85f, .72f);
        gemText = W.Text(panel, Font, "Gem", "", 1200, 402, 452, 42, 17);
        comparison = W.Text(panel, Font, "Comparison", "", 1200, 450, 452, 130, 17);
        wear = W.Button(panel, Font, "Wear", "穿戴", 1200, 590, 220, 44, Wear);
        secondary = W.Button(panel, Font, "Secondary", "装入武器槽 2", 1432, 590, 220, 44, () => EquipWeapon(1));
        remove = W.Button(panel, Font, "Remove", "卸下", 1432, 590, 220, 44, Remove);
        sell = W.Button(panel, Font, "Sell", "出售 1 件", 1200, 650, 220, 44, () => ConfirmAction(false));
        dismantle = W.Button(panel, Font, "Dismantle", "分解 1 件", 1432, 650, 220, 44, () => ConfirmAction(true));
        setText = W.Text(panel, Font, "SetBonus", "", 1200, 720, 452, 112, 18);
        status = W.Text(panel, Font, "Status", "点击背包中的装备试穿，确认后才会正式装备。", 28, 840, 1624, 42, 21);
        BuildManagement();
    }
    public override void OnOpen(params object[] args)
    {
        interaction = args.Length > 0 ? args[0] as PlayerInventoryInteraction : null;
        if (interaction == null || !interaction.IsOpen) { gameObject.SetActive(false); return; }
        UnbindPlayer();
        player = interaction.GetComponent<PlayerController>();
        if (player != null) player.Weapons.PresentationChanged += RefreshPreview;
        progress.Changed -= Refresh; progress.Changed += Refresh;
        filter = page = 0; selected = confirmation = tryOnItemId = null;
        ResetManagement();
        status.text = progress.HasUnsavedChanges ? progress.StatusMessage : "点击背包中的装备试穿，确认后才会正式装备。"; Fit(); Refresh();
    }
    public override void OnClose()
    {
        progress.Changed -= Refresh; UnbindPlayer(); tryOnItemId = null; ResetManagement(); preview?.Hide();
        var previous = interaction; interaction = null; previous?.OnUiClosed();
    }
    void UnbindPlayer() { if (player != null) player.Weapons.PresentationChanged -= RefreshPreview; player = null; }
    void OnDisable() { if (progress != null) progress.Changed -= Refresh; UnbindPlayer(); tryOnItemId = null; ResetManagement(); }
    void Update() { Fit(); RefreshConsumableCooldown(); }
    void Fit()
    {
        if (panel == null) return;
        var size = ((RectTransform)transform).rect.size; panel.localScale = Vector3.one * Mathf.Min(size.x / 1720, size.y / 940);
    }
    public void Select(string id)
    {
        id = filter == 4 ? id : progress.ResolveItemKey(id);
        if (SelectManagedItem(id)) return;
        selected = id; confirmation = null;
        var item = progress.FindItem(id);
        tryOnItemId = CanTryOn(id, progress.Inventory) ? id : null;
        status.text = TriedItem != null ? $"正在试穿 {item.DisplayName}，尚未装备。" +
            (progress.Level < item.RequiredLevel ? $"达到 {item.RequiredLevel} 级后可确认装备。" : "确认后才应用属性，取消可恢复当前外观。") :
            "当前显示实际穿戴外观。选择装备可试穿并查看属性差异。";
        Refresh();
    }
    public void CancelTryOn()
    {
        draft = null;
        tryOnItemId = confirmation = null;
        status.text = "已取消试穿，恢复当前外观。";
        Refresh();
    }
    public void SelectCategory(int category)
    {
        if (batchMode) return;
        filter = Mathf.Clamp(category, 0, 4); page = 0; selected = confirmation = tryOnItemId = null;
        status.text = draft != null ? "已保留整套试穿，确认整套后才会正式穿戴。" : "点击背包中的装备试穿，确认后才会正式装备。"; Refresh();
    }
    void ChangePage(int next)
    {
        if (next != page) { selected = confirmation = tryOnItemId = null; status.text = "已恢复当前外观。"; }
        page = next; Refresh();
    }
    bool CanTryOn(string key, InventoryData data)
    {
        var item = progress.FindItem(key);
        return item != null && filter != 4 && data.Find(key) != null &&
            (item.Kind == ItemKind.Weapon || (item.Kind == ItemKind.Equipment && data.equipped[(int)item.Slot] != key));
    }
    void Refresh()
    {
        if (panel == null) return;
        var data = progress.Inventory;
        ReconcileManagement();
        // Reconcile against current committed data, including sales and changes from other UI.
        if (tryOnItemId != null && !CanTryOn(tryOnItemId, data))
        {
            tryOnItemId = null;
            status.text = "试穿已结束，当前显示实际装备。";
        }
        capacity.text = $"背包 {data.bag.Length}/{InventoryData.Capacity}  ·  金币 {progress.Gold}  ·  待领取 {data.overflow.Sum(x => x.count)}";
        visible = (filter == 4 ? data.overflow : data.bag).Where(x => filter == 0 || filter == 4 ||
            (filter == 1 && progress.FindItem(x.itemId).Kind == ItemKind.Equipment) ||
            (filter == 2 && progress.FindItem(x.itemId).Kind == ItemKind.Weapon) ||
            (filter == 3 && progress.FindItem(x.itemId).IsStackable))
            .OrderByDescending(x => progress.FindItem(x.itemId).Rarity).ThenBy(x => x.itemId).ToArray();
        page = Mathf.Clamp(page, 0, Mathf.Max(0, (visible.Length - 1) / 24));
        for (int i = 0; i < cells.Length; i++)
        {
            int index = page * 24 + i; bool used = index < visible.Length; var item = used ? progress.FindItem(visible[index].itemId) : null;
            cells[i].interactable = used; icons[i].enabled = used && item.Icon != null;
            cells[i].image.color = used && (selected == visible[index].Key || batchKeys.Contains(visible[index].Key)) ? new Color(.25f, .36f, .40f) : W.Card;
            icons[i].sprite = item?.Icon; icons[i].color = item != null ? item.QualityColor : Color.white;
            labels[i].text = used ? item.DisplayName + "\n" +
                (visible[index].legacyReserve ? $"旧装备 ×{visible[index].count}" : !visible[index].IsInstance ? $"×{visible[index].count}" :
                 progress.IsLastEquippedItem(visible[index].Key) ? "已装备" + (visible[index].upgradeLevel > 0 ? $" +{visible[index].upgradeLevel}" : "") : visible[index].upgradeLevel > 0 ? $"强化 +{visible[index].upgradeLevel}" : $"词条 {visible[index].affixes.Length}") : "";
            labels[i].color = item != null ? item.QualityColor : Color.white;
            badges[i].text = !used ? "" : visible[index].isLocked ? "已锁定" : batchKeys.Contains(visible[index].Key) ? "已选择" : "";
        }
        for (int i = 0; i < tabs.Length; i++) tabs[i].image.color = filter == i ? W.Accent : W.Card;
        for (int i = 0; i < 5; i++)
        {
            var item = progress.EquippedItem((EquipmentSlot)i);
            int rank = progress.GetInstanceLevel(data.equipped[i]);
            equipmentLabels[i].text = ItemDefinition.SlotName((EquipmentSlot)i) + "\n" + (item?.DisplayName ?? "未穿戴") + (rank > 0 ? $" +{rank}" : "");
            equipmentLabels[i].color = item != null ? item.QualityColor : Color.gray;
        }
        summary.text = $"Lv.{progress.Level} {progress.CurrentClass.DisplayName}\n生命 +{progress.EquipmentHealth}\n能量 +{progress.EquipmentEnergy}\n伤害 +{progress.EquipmentAttackPercent}%\n减伤 {progress.EquipmentDefensePercent}%";
        pageLabel.text = $"第 {page + 1} / {Mathf.Max(1, (visible.Length + 23) / 24)} 页";
        collect.interactable = data.overflow.Length > 0;
        RefreshDetails(data);
        RefreshPreview();
        RefreshManagement();
    }
    void RefreshPreview()
    {
        if (player == null || preview == null || !isActiveAndEnabled) return;
        var equipped = Enumerable.Range(0, 5).Select(i => progress.EquippedItem((EquipmentSlot)i)).ToArray();
        if (draft != null) equipped = draft.armorKeys.Select(progress.FindItem).ToArray();
        var trial = TriedItem;
        if (trial != null && trial.Kind == ItemKind.Equipment) equipped[(int)trial.Slot] = trial;
        var weapons = player.Weapons;
        var shownWeapon = draft != null ? progress.FindItem(draft.weaponKeys[draft.activeWeaponSlot])?.Weapon : trial?.Kind == ItemKind.Weapon ? trial.Weapon : weapons.ActiveWeapon;
        var controller = player.animator.runtimeAnimatorController;
        var idle = controller != null ? controller.animationClips.FirstOrDefault(c => c.name.EndsWith("Idle", System.StringComparison.OrdinalIgnoreCase)) : null;
        if ((trial?.Kind == ItemKind.Weapon || draft != null) && shownWeapon != null && !weapons.IsSheathed)
            idle = player.GetComponent<PlayerStanceController>().GetPreviewIdle(shownWeapon.Stance);
        preview.ShowLoadout(progress.CurrentClass, progress.CurrentGender, equipped.Where(i => i != null).ToArray(), shownWeapon, weapons.IsSheathed, idle ?? progress.CurrentClass.PreviewIdle);
        previewTitle.text = draft != null ? "整套试穿 · 未装备" : trial != null ? "试穿预览 · 未装备" : "角色外观";
        previewTitle.color = trial != null || draft != null ? new Color(.87f, .74f, .49f) : Color.white;
        cancelTryOn.gameObject.SetActive(trial != null || draft != null);
        cancelTryOn.GetComponent<RectTransform>().sizeDelta = new Vector2(draft != null ? 164 : 340, 36);
        cancelTryOn.GetComponentInChildren<TMP_Text>().text = draft != null ? "取消整套" : "取消试穿 · 恢复当前外观";
        cancelTryOn.GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = new Vector2(draft != null ? 148 : 324, 32);
        previewCaption.text = draft != null ? $"{draft.armorKeys.Count(k => k.Length > 0)} 件防具 · 武器槽 {draft.activeWeaponSlot + 1} · {shownWeapon?.DisplayName ?? "徒手"}" : trial != null ? trial.DisplayName + (trial.Kind == ItemKind.Equipment && trial.Appearance == null ? " · 无独立外观" : "") :
            shownWeapon != null ? shownWeapon.DisplayName + (weapons.IsSheathed ? " · 已收起" : " · 战斗姿态") : "未持武器";
    }
    void RefreshDetails(InventoryData data)
    {
        var item = progress.FindItem(selected); bool owned = item != null && data.Count(selected) > 0 && filter != 4;
        var entry = progress.FindInstance(selected);
        title.text = (item?.DisplayName ?? "选择一件物品") + (entry != null && entry.upgradeLevel > 0 ? $" +{entry.upgradeLevel}" : ""); title.color = item != null ? item.QualityColor : Color.white;
        description.text = item == null ? "点击背包装备可预览试穿效果。\n点击左侧穿戴栏，可查看已装备物品。" :
            $"{item.RarityLabel} · {item.SlotLabel} · Lv.{item.RequiredLevel}\n{item.Description}";
        affixText.text = entry == null || item.IsStackable ? "" : entry.legacyReserve ? "旧装备待领取 · 保留原属性与强化\n腾出背包空间后逐件领取。" :
            (item.Kind == ItemKind.Weapon ? $"品质伤害 +{(item.Weapon.DamageMultiplier - 1) * 100:0}% · 强化 +{entry.upgradeLevel}\n" : $"强化 +{entry.upgradeLevel} · 基础属性 +{entry.upgradeLevel * 10}% · 随机词条\n") +
            (entry.affixes.Length == 0 ? "无额外词条" : string.Join("\n", entry.affixes.Select(a => AffixLabel(a.stat) + $" +{a.value}" + (a.stat >= ItemAffixStat.AttackPercent ? "%" : ""))));
        comparison.text = item != null && !item.IsStackable ? Compare(selected,
            item.Kind == ItemKind.Equipment ? data.equipped[(int)item.Slot] : progress.EquippedWeaponKey(player.Weapons.ActiveSlot)) :
            item != null ? $"出售价格：{item.SellPrice} 金币 / 件" : "";
        bool gear = item != null && item.Kind == ItemKind.Equipment, weapon = item != null && item.Kind == ItemKind.Weapon;
        var gem = progress.FindItem(entry?.gemId);
        gemText.text = gear ? gem != null ? $"宝石：{gem.DisplayName} · {TownNpcUI.GemStats(gem)}\n拆卸或替换请找主城铁匠。" : "宝石：空槽 · 可在主城铁匠处镶嵌" : "";
        var set = item?.Set; int pieces = progress.SetPieces(set);
        setText.text = set == null ? "点击装备先试穿，确认后正式装备。\n宝石可在主城铁匠处制作、镶嵌与拆卸。\n镶嵌宝石的装备需先拆宝石再出售/分解。" :
            $"{set.DisplayName} · 实际穿戴 {pieces}/5 件\n" + SetLine(2, pieces, $"生命 +{set.TwoPieceHealth}") +
            SetLine(4, pieces, $"能量 +{set.FourPieceEnergy}") + SetLine(5, pieces, $"伤害 +{set.FivePieceAttackPercent}%");
        bool trying = TriedItem != null, worn = gear && data.equipped[(int)item.Slot] == selected;
        bool canConfirm = owned && progress.Level >= item.RequiredLevel && progress.CanManageInventory;
        wear.gameObject.SetActive(gear || weapon); wear.interactable = !worn && (trying ? canConfirm : owned);
        wear.GetComponentInChildren<TMP_Text>().text = worn ? "已穿戴" : trying ? (weapon ? "确认装入武器槽 1" : "确认穿戴") : "试穿";
        secondary.gameObject.SetActive(weapon); secondary.interactable = trying && canConfirm;
        secondary.GetComponentInChildren<TMP_Text>().text = "确认装入武器槽 2";
        remove.gameObject.SetActive(gear); remove.interactable = owned && worn && progress.CanManageInventory;
        sell.interactable = owned && !progress.IsLastEquippedItem(selected) && string.IsNullOrEmpty(entry?.gemId) && entry?.isLocked != true;
        sell.GetComponentInChildren<TMP_Text>().text = confirmation == "sell:" + selected ? "确认出售" : $"出售 · {item?.SellPrice ?? 0} 金币";
        dismantle.interactable = owned && gear && progress.CanDismantle(selected);
        dismantle.GetComponentInChildren<TMP_Text>().text = confirmation == "dismantle:" + selected ? "确认分解" : "分解 1 件";
        RefreshConsumableDetails(item, owned);
    }
    static string AffixLabel(ItemAffixStat stat) => stat == ItemAffixStat.Health ? "生命" : stat == ItemAffixStat.Energy ? "能量" : stat == ItemAffixStat.AttackPercent ? "伤害" : "减伤";
    static string SetLine(int required, int pieces, string bonus) => $"<color=#{(pieces >= required ? "6EDDAB" : "9DAAB9")}>{required} 件：{bonus} · {(pieces >= required ? "已激活" : "未激活")}</color>\n";
    string Compare(string key, string current) => (draft != null ? "单件总属性 / 确认整套后全身变化\n" : progress.FindItem(key)?.Kind == ItemKind.Equipment ? "单件总属性 / 换装后全身变化（含套装）\n" : "总属性（含强化） / 与当前武器相比\n") + string.Concat(
        System.Enum.GetValues(typeof(ItemAffixStat)).Cast<ItemAffixStat>().Select(stat => Stat(AffixLabel(stat), progress.ItemStat(key, stat), progress.ItemStat(current, stat),
            progress.ItemUpgradeBonus(key, stat), progress.ItemGemBonus(key, stat), draft != null ? progress.LoadoutStat(draft, stat) - progress.LoadoutStat(progress.CurrentLoadout, stat) : progress.FindItem(key)?.Kind == ItemKind.Equipment ? progress.ArmorSwapDelta(key, stat) : (int?)null,
            stat >= ItemAffixStat.AttackPercent ? "%" : "")));
    static string Stat(string label, int value, int previous, int upgrade, int gem, int? loadoutDelta, string suffix = "")
    {
        int delta = loadoutDelta ?? value - previous;
        string color = delta > 0 ? "6EDDAB" : delta < 0 ? "F08C88" : "9DAAB9";
        return $"{label} +{value}{suffix}" + (upgrade > 0 ? $" [强化+{upgrade}{suffix}]" : "") + (gem > 0 ? $" [宝石+{gem}{suffix}]" : "") + $"  <color=#{color}>({delta:+0;-0;0}{suffix})</color>\n";
    }
    public void Wear()
    {
        if (draft != null) { if (progress.FindItem(selected)?.Kind == ItemKind.Weapon) PreviewWeaponSlot(0); else ConfirmOutfit(); return; }
        var item = progress.FindItem(selected); if (item == null) return;
        if (TriedItem == null) { Select(selected); return; }
        if (item.Kind == ItemKind.Weapon) { EquipWeapon(0); return; }
        if (progress.TryWearItem(selected, out string message)) tryOnItemId = null;
        Show(message);
    }
    void EquipWeapon(int slot)
    {
        if (draft != null) { PreviewWeaponSlot(slot); return; }
        var item = progress.FindItem(selected);
        if (item == null || item.Kind != ItemKind.Weapon || interaction == null || TriedItem != item || !progress.CanEquipWeapon(item.Weapon)) return;
        if (!interaction.EquipWeaponInstance(slot, selected)) status.text = string.IsNullOrEmpty(progress.StatusMessage) ? "当前动作尚未结束，请稍后再试。" : progress.StatusMessage;
    }
    void Remove() { var item = progress.FindItem(selected); if (item == null) return; if (draft != null) { draft.armorKeys[(int)item.Slot] = ""; Show("已在预览中脱下该部位，确认整套后才生效。"); return; } progress.TryRemoveEquipment(item.Slot, out string message); Show(message); }
    void ConfirmAction(bool isDismantle)
    {
        string key = (isDismantle ? "dismantle:" : "sell:") + selected;
        if (confirmation != key) { confirmation = key; status.text = isDismantle ? "再次点击确认分解 1 件物品。" : "再次点击确认出售 1 件物品。"; Refresh(); return; }
        string message;
        if (isDismantle) progress.TryDismantleItem(selected, out message); else progress.TrySellItem(selected, out message);
        confirmation = null; Show(message);
    }
    void Show(string message) { status.text = message; Refresh(); }
}
