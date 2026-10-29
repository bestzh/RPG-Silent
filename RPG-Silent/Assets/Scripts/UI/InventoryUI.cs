using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed partial class InventoryUI : UIBase
{
    [Inject]
    PlayerProgressService progress;
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
    readonly Button[] tabs = new Button[7];
    Button wear, secondary, remove, sell, dismantle, collect, cancelTryOn;
    ItemStackData[] visible = System.Array.Empty<ItemStackData>();
    string selected, confirmation, tryOnItemId;
    int filter, page;
    protected override void OnInit()
    {
        UIView.Panel(transform, "Shade");
        panel = UIView.Panel(transform, "Inventory");
        var heading = UIView.Text(panel, "Heading", "行 囊 与 装 备");
        heading.color = new Color(.87f, .74f, .49f);
        capacity = UIView.Text(panel, "Capacity", "");
        UIView.Button(panel, "Close", "关闭 [B]", () => interaction?.Close());
        UIView.Text(panel, "EquipmentTitle", "当前穿戴");
        for (int i = 0; i < 5; i++)
        {
            int slot = i;
            var button = UIView.Button(panel, "Equipment" + i, "", () => Select(progress.Inventory.equipped[slot]));
            button.image.color = W.Card;
            equipmentLabels[i] = button.GetComponentInChildren<TMP_Text>();
        }

        summary = UIView.Text(panel, "Summary", "");
        previewTitle = UIView.Text(panel, "PreviewTitle", "角色外观");
        var portrait = UIView.Rect(panel, "CharacterPreview");
        preview = portrait.gameObject.GetComponent<CharacterModelPreview>();
        UIView.Button(panel, "RotateLeft", "左转", () => preview.Rotate(-30));
        UIView.Button(panel, "ResetView", "重置视角", preview.ResetView);
        UIView.Button(panel, "RotateRight", "右转", () => preview.Rotate(30));
        cancelTryOn = UIView.Button(panel, "CancelTryOn", "取消试穿 · 恢复当前外观", CancelTryOn);
        cancelTryOn.image.color = W.Card;
        var hint = UIView.Text(panel, "PreviewHelp", "按住鼠标左键拖动旋转\n滚轮缩放");
        hint.alignment = TextAlignmentOptions.Center;
        previewCaption = UIView.Text(panel, "PreviewCaption", "");
        previewCaption.alignment = TextAlignmentOptions.Center;
        string[] names =
        {
            "全部",
            "防具",
            "武器",
            "材料",
            "待领取",
            "药品",
            "宝石"
        };
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;
            tabs[i] = UIView.Button(panel, "Tab" + i, names[i], () => SelectCategory(index));
        }

        for (int i = 0; i < cells.Length; i++)
        {
            int index = i;
            cells[i] = UIView.Button(panel, "Item" + i, "", () =>
            {
                int position = page * 24 + index;
                if (position < visible.Length)
                    Select(visible[position].Key);
            });
            labels[i] = cells[i].GetComponentInChildren<TMP_Text>();
            var iconRect = UIView.Rect(cells[i].transform, "Icon");
            icons[i] = iconRect.gameObject.GetComponent<Image>();
            icons[i].preserveAspect = true;
            icons[i].raycastTarget = false;
        }

        UIView.Button(panel, "Previous", "上一页", () => ChangePage(Mathf.Max(0, page - 1)));
        pageLabel = UIView.Text(panel, "Page", "");
        UIView.Button(panel, "Next", "下一页", () => ChangePage(Mathf.Min(Mathf.Max(0, (visible.Length - 1) / 24), page + 1)));
        collect = UIView.Button(panel, "Collect", "领取物品", () =>
        {
            progress.TryCollectOverflow(out string message);
            Show(message);
        });
        UIView.Text(panel, "DetailsHeading", "物品详情");
        title = UIView.Text(panel, "ItemTitle", "");
        description = UIView.Text(panel, "Description", "");
        affixText = UIView.Text(panel, "Affixes", "");
        affixText.color = new Color(.55f, .85f, .72f);
        gemText = UIView.Text(panel, "Gem", "");
        comparison = UIView.Text(panel, "Comparison", "");
        wear = UIView.Button(panel, "Wear", "穿戴", Wear);
        secondary = UIView.Button(panel, "Secondary", "装入武器槽 2", () => EquipWeapon(1));
        remove = UIView.Button(panel, "Remove", "卸下", Remove);
        sell = UIView.Button(panel, "Sell", "出售 1 件", () => ConfirmAction(false));
        dismantle = UIView.Button(panel, "Dismantle", "分解 1 件", () => ConfirmAction(true));
        setText = UIView.Text(panel, "SetBonus", "");
        status = UIView.Text(panel, "Status", "点击背包中的装备试穿，确认后才会正式装备。");
        BuildManagement();
        BuildDiscard();
        BuildOrganization();
        BuildEquipmentEffectDetails();
        BindV5Inventory();
    }

    public override void OnOpen(params object[] args)
    {
        CloseEquipmentEffectDetails();
        interaction = args.Length > 0 ? args[0] as PlayerInventoryInteraction : null;
        if (interaction == null || !interaction.IsOpen)
        {
            gameObject.SetActive(false);
            return;
        }

        UnbindPlayer();
        player = interaction.GetComponent<PlayerController>();
        if (player != null)
            player.Weapons.PresentationChanged += RefreshPreview;
        progress.Changed -= Refresh;
        progress.Changed += Refresh;
        filter = page = 0;
        selected = confirmation = tryOnItemId = null;
        ResetManagement();
        ResetOrganization();
        status.text = progress.HasUnsavedChanges ? progress.StatusMessage : "点击背包中的装备试穿，确认后才会正式装备。";
        Refresh();
    }

    public override void OnClose()
    {
        CloseEquipmentEffectDetails();
        progress.Changed -= Refresh;
        UnbindPlayer();
        tryOnItemId = null;
        ResetManagement();
        ResetOrganization();
        preview?.Hide();
        var previous = interaction;
        interaction = null;
        previous?.OnUiClosed();
    }

    void UnbindPlayer()
    {
        if (player != null)
            player.Weapons.PresentationChanged -= RefreshPreview;
        player = null;
    }

    void OnDisable()
    {
        CloseEquipmentEffectDetails();
        if (progress != null)
            progress.Changed -= Refresh;
        UnbindPlayer();
        tryOnItemId = null;
        ResetManagement();
        ResetOrganization();
    }

    void Update()
    {
        RefreshConsumableCooldown();
        UpdateEquipmentEffectDetails();
    }

    public void Select(string id)
    {
        id = filter == 4 ? id : progress.ResolveItemKey(id);
        string seenFailure = null;
        if (filter != 4 && progress.FindInstance(id)?.isNew == true && !progress.TryMarkItemSeen(id, out string seenMessage))
            seenFailure = seenMessage;
        if (SelectManagedItem(id))
        {
            if (seenFailure != null)
                status.text = seenFailure;
            return;
        }

        selected = id;
        confirmation = null;
        var item = progress.FindItem(id);
        tryOnItemId = CanTryOn(id, progress.Inventory) ? id : null;
        status.text = TriedItem != null ? $"正在试穿 {item.DisplayName}，尚未装备。" + (progress.Level < item.RequiredLevel ? $"达到 {item.RequiredLevel} 级后可确认装备。" : "确认后才应用属性，取消可恢复当前外观。") : "当前显示实际穿戴外观。选择装备可试穿并查看属性差异。";
        Refresh();
        if (seenFailure != null)
            status.text = seenFailure;
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
        if (batchMode || BulkSelecting && category == 4)
            return;
        filter = Mathf.Clamp(category, 0, 6);
        page = 0;
        selected = confirmation = tryOnItemId = null;
        status.text = draft != null ? "已保留整套试穿，确认整套后才会正式穿戴。" : "点击背包中的装备试穿，确认后才会正式装备。";
        Refresh();
    }

    void ChangePage(int next)
    {
        if (next != page)
        {
            selected = confirmation = tryOnItemId = null;
            status.text = "已恢复当前外观。";
        }

        page = next;
        Refresh();
    }

    bool CanTryOn(string key, InventoryData data)
    {
        var item = progress.FindItem(key);
        return item != null && filter != 4 && data.Find(key) != null && (item.Kind == ItemKind.Weapon || (item.Kind == ItemKind.Equipment && data.equipped[(int)item.Slot] != key));
    }

    void Refresh()
    {
        if (panel == null)
            return;
        var data = progress.Inventory;
        ReconcileManagement();
        // Reconcile against current committed data, including sales and changes from other UI.
        if (tryOnItemId != null && !CanTryOn(tryOnItemId, data))
        {
            tryOnItemId = null;
            status.text = "试穿已结束，当前显示实际装备。";
        }

        capacity.text = $"背包 {data.bag.Length}/{InventoryData.Capacity}  ·  金币 {progress.Gold}  ·  待领取 {data.overflow.Sum(x => x.count)}";
        visible = progress.QueryInventory((InventoryCategory)filter, searchField?.text, sortOrder, sortDescending);
        page = Mathf.Clamp(page, 0, Mathf.Max(0, (visible.Length - 1) / 24));
        for (int i = 0; i < cells.Length; i++)
        {
            int index = page * 24 + i;
            bool used = index < visible.Length;
            var item = used ? progress.FindItem(visible[index].itemId) : null;
            cells[i].interactable = used;
            icons[i].enabled = used && item.Icon != null;
            cells[i].image.color = used && (selected == visible[index].Key || batchKeys.Contains(visible[index].Key) || bulkQuantities.ContainsKey(visible[index].Key)) ? new Color(.25f, .36f, .40f) : W.Card;
            icons[i].sprite = item?.Icon;
            icons[i].color = Color.white;
            labels[i].text = used ? item.DisplayName + "\n" + (visible[index].legacyReserve ? $"旧装备 ×{visible[index].count}" : !visible[index].IsInstance ? $"×{visible[index].count}" : progress.IsLastEquippedItem(visible[index].Key) ? "已装备" + (visible[index].upgradeLevel > 0 ? $" +{visible[index].upgradeLevel}" : "") : visible[index].upgradeLevel > 0 ? $"强化 +{visible[index].upgradeLevel}" : progress.Level >= item.RequiredLevel ? $"Lv.{item.RequiredLevel} · 可装备" : $"需要 Lv.{item.RequiredLevel}") : "";
            labels[i].color = item != null ? item.QualityColor : Color.white;
            badges[i].text = !used ? "" : (visible[index].isNew ? "新 · " : "") + (visible[index].isLocked ? "已锁定" : batchKeys.Contains(visible[index].Key) || bulkQuantities.ContainsKey(visible[index].Key) ? "已选择" : item.RarityLabel);
        }

        for (int i = 0; i < tabs.Length; i++)
            tabs[i].image.color = filter == i ? W.Accent : W.Card;
        for (int i = 0; i < 5; i++)
        {
            var item = progress.EquippedItem((EquipmentSlot)i);
            int rank = progress.GetInstanceLevel(data.equipped[i]);
            equipmentLabels[i].text = ItemDefinition.SlotName((EquipmentSlot)i) + "\n" + (item?.DisplayName ?? "未穿戴") + (rank > 0 ? $" +{rank}" : "");
            equipmentLabels[i].color = item != null ? item.QualityColor : Color.gray;
        }

        summary.text = $"Lv.{progress.Level} {progress.CurrentClass.DisplayName}\n生命 +{progress.EquipmentHealth} · 能量 +{progress.EquipmentEnergy}\n伤害 +{progress.EquipmentAttackPercent}% · 减伤 {progress.EquipmentDefensePercent}%";
        pageLabel.text = $"第 {page + 1} / {Mathf.Max(1, (visible.Length + 23) / 24)} 页";
        collect.interactable = data.overflow.Length > 0;
        RefreshDetails(data);
        RefreshPreview();
        RefreshManagement();
        RefreshDiscard();
        RefreshOrganization();
        RefreshV5Inventory();
    }

    void RefreshPreview()
    {
        if (player == null || preview == null || !isActiveAndEnabled)
            return;
        var equipped = Enumerable.Range(0, 5).Select(i => progress.EquippedItem((EquipmentSlot)i)).ToArray();
        if (draft != null)
            equipped = draft.armorKeys.Select(progress.FindItem).ToArray();
        var trial = TriedItem;
        if (trial != null && trial.Kind == ItemKind.Equipment)
            equipped[(int)trial.Slot] = trial;
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
        cancelTryOn.GetComponentInChildren<TMP_Text>().text = draft != null ? "取消整套" : "取消试穿 · 恢复当前外观";
        previewCaption.text = draft != null ? $"{draft.armorKeys.Count(k => k.Length > 0)} 件防具 · 武器槽 {draft.activeWeaponSlot + 1} · {shownWeapon?.DisplayName ?? "徒手"}" : trial != null ? trial.DisplayName + (trial.Kind == ItemKind.Equipment && trial.Appearance == null ? " · 无独立外观" : "") : shownWeapon != null ? shownWeapon.DisplayName + (weapons.IsSheathed ? " · 已收起" : " · 战斗姿态") : "未持武器";
    }

    void RefreshDetails(InventoryData data)
    {
        var item = progress.FindItem(selected);
        bool owned = item != null && data.Count(selected) > 0 && filter != 4;
        var entry = progress.FindInstance(selected);
        title.text = (item?.DisplayName ?? "选择一件物品") + (entry != null && entry.upgradeLevel > 0 ? $" +{entry.upgradeLevel}" : "");
        title.color = item != null ? item.QualityColor : Color.white;
        description.text = item == null ? "点击背包装备可预览试穿效果。\n点击左侧穿戴栏，可查看已装备物品。" : $"{item.RarityLabel} · {item.SlotLabel} · Lv.{item.RequiredLevel}\n{item.Description}" + (entry == null ? "" : "\n获得：" + (entry.acquiredUtcTicks == 0 ? "旧物品，时间未知" : new System.DateTime(entry.acquiredUtcTicks, System.DateTimeKind.Utc).ToLocalTime().ToString("MM-dd HH:mm")));
        RefreshEquipmentEffectDetails(item);
        affixText.text = entry == null || item.IsStackable ? "" : entry.legacyReserve ? "旧装备待领取 · 保留原属性与强化\n腾出背包空间后逐件领取。" : (item.Kind == ItemKind.Weapon ? $"品质伤害 +{(item.Weapon.DamageMultiplier - 1) * 100:0}% · 强化 +{entry.upgradeLevel}\n" : $"强化 +{entry.upgradeLevel} · 基础属性 +{entry.upgradeLevel * 10}% · 随机词条\n") + (entry.affixes.Length == 0 ? "无额外词条" : string.Join("\n", entry.affixes.Select(a => AffixLabel(a.stat) + $" +{a.value}" + (a.stat >= ItemAffixStat.AttackPercent ? "%" : ""))));
        comparison.text = item != null && !item.IsStackable ? Compare(selected, item.Kind == ItemKind.Equipment ? data.equipped[(int)item.Slot] : progress.EquippedWeaponKey(player.Weapons.ActiveSlot)) : item != null ? $"出售价格：{item.SellPrice} 金币 / 件" : "";
        bool gear = item != null && item.Kind == ItemKind.Equipment, weapon = item != null && item.Kind == ItemKind.Weapon;
        var gem = progress.FindItem(entry?.gemId);
        gemText.text = gear ? gem != null ? $"宝石：{gem.DisplayName} · {TownNpcUI.GemStats(gem)}\n拆卸或替换请找主城铁匠。" : "宝石：空槽 · 可在主城铁匠处镶嵌" : "";
        var set = item?.Set;
        int pieces = progress.SetPieces(set);
        setText.text = set == null ? "点击装备先试穿，确认后正式装备。\n宝石可在主城铁匠处制作、镶嵌与拆卸。\n镶嵌宝石的装备需先拆宝石再出售/分解。" : $"{set.DisplayName} · 实际穿戴 {pieces}/5 件\n" + SetLine(2, pieces, $"生命 +{set.TwoPieceHealth}") + SetLine(4, pieces, $"能量 +{set.FourPieceEnergy}") + SetLine(5, pieces, $"伤害 +{set.FivePieceAttackPercent}%");
        bool trying = TriedItem != null, worn = gear && data.equipped[(int)item.Slot] == selected;
        bool canConfirm = owned && progress.Level >= item.RequiredLevel && progress.CanManageInventory;
        wear.gameObject.SetActive(gear || weapon);
        wear.interactable = !worn && (trying ? canConfirm : owned);
        wear.GetComponentInChildren<TMP_Text>().text = worn ? "已穿戴" : trying ? (weapon ? "确认装入武器槽 1" : "确认穿戴") : "试穿";
        secondary.gameObject.SetActive(weapon);
        secondary.interactable = trying && canConfirm;
        secondary.GetComponentInChildren<TMP_Text>().text = "确认装入武器槽 2";
        remove.gameObject.SetActive(gear);
        remove.interactable = owned && worn && progress.CanManageInventory;
        sell.interactable = owned && !progress.IsLastEquippedItem(selected) && string.IsNullOrEmpty(entry?.gemId) && entry?.isLocked != true;
        sell.GetComponentInChildren<TMP_Text>().text = confirmation == "sell:" + selected ? "确认出售" : $"出售 · {item?.SellPrice ?? 0} 金币";
        dismantle.interactable = owned && gear && progress.CanDismantle(selected);
        dismantle.GetComponentInChildren<TMP_Text>().text = confirmation == "dismantle:" + selected ? "确认分解" : "分解 1 件";
        RefreshConsumableDetails(item, owned);
    }

    static string AffixLabel(ItemAffixStat stat) => stat == ItemAffixStat.Health ? "生命" : stat == ItemAffixStat.Energy ? "能量" : stat == ItemAffixStat.AttackPercent ? "伤害" : "减伤";
    static string SetLine(int required, int pieces, string bonus) => $"<color=#{(pieces >= required ? "6EDDAB" : "9DAAB9")}>{required} 件：{bonus} · {(pieces >= required ? "已激活" : "未激活")}</color>\n";
    string Compare(string key, string current) => (draft != null ? "单件总属性 / 确认整套后全身变化\n" : progress.FindItem(key)?.Kind == ItemKind.Equipment ? "单件总属性 / 换装后全身变化（含套装）\n" : "总属性（含强化） / 与当前武器相比\n") + string.Concat(System.Enum.GetValues(typeof(ItemAffixStat)).Cast<ItemAffixStat>().Select(stat => Stat(AffixLabel(stat), progress.ItemStat(key, stat), progress.ItemStat(current, stat), progress.ItemUpgradeBonus(key, stat), progress.ItemGemBonus(key, stat), draft != null ? progress.LoadoutStat(draft, stat) - progress.LoadoutStat(progress.CurrentLoadout, stat) : progress.FindItem(key)?.Kind == ItemKind.Equipment ? progress.ArmorSwapDelta(key, stat) : (int? )null, stat >= ItemAffixStat.AttackPercent ? "%" : "")));
    static string Stat(string label, int value, int previous, int upgrade, int gem, int? loadoutDelta, string suffix = "")
    {
        int delta = loadoutDelta ?? value - previous;
        string color = delta > 0 ? "6EDDAB" : delta < 0 ? "F08C88" : "9DAAB9";
        return $"{label} +{value}{suffix}" + (upgrade > 0 ? $" [强化+{upgrade}{suffix}]" : "") + (gem > 0 ? $" [宝石+{gem}{suffix}]" : "") + $"  <color=#{color}>({delta:+0;-0;0}{suffix})</color>\n";
    }

    public void Wear()
    {
        if (draft != null)
        {
            if (progress.FindItem(selected)?.Kind == ItemKind.Weapon)
                PreviewWeaponSlot(0);
            else
                ConfirmOutfit();
            return;
        }

        var item = progress.FindItem(selected);
        if (item == null)
            return;
        if (TriedItem == null)
        {
            Select(selected);
            return;
        }

        if (item.Kind == ItemKind.Weapon)
        {
            EquipWeapon(0);
            return;
        }

        if (progress.TryWearItem(selected, out string message))
            tryOnItemId = null;
        Show(message);
    }

    void EquipWeapon(int slot)
    {
        if (draft != null)
        {
            PreviewWeaponSlot(slot);
            return;
        }

        var item = progress.FindItem(selected);
        if (item == null || item.Kind != ItemKind.Weapon || interaction == null || TriedItem != item || !progress.CanEquipWeapon(item.Weapon))
            return;
        if (!interaction.EquipWeaponInstance(slot, selected))
            status.text = string.IsNullOrEmpty(progress.StatusMessage) ? "当前动作尚未结束，请稍后再试。" : progress.StatusMessage;
    }

    void Remove()
    {
        var item = progress.FindItem(selected);
        if (item == null)
            return;
        if (draft != null)
        {
            draft.armorKeys[(int)item.Slot] = "";
            Show("已在预览中脱下该部位，确认整套后才生效。");
            return;
        }

        progress.TryRemoveEquipment(item.Slot, out string message);
        Show(message);
    }

    void ConfirmAction(bool isDismantle)
    {
        string key = (isDismantle ? "dismantle:" : "sell:") + selected;
        if (confirmation != key)
        {
            confirmation = key;
            status.text = isDismantle ? "再次点击确认分解 1 件物品。" : "再次点击确认出售 1 件；48 小时内可向铁匠原价回购，最多最近 24 项。";
            Refresh();
            return;
        }

        string message;
        if (isDismantle)
            progress.TryDismantleItem(selected, out message);
        else
            progress.TrySellItem(selected, out message);
        confirmation = null;
        Show(message);
    }

    void Show(string message)
    {
        status.text = message;
        Refresh();
    }
}
