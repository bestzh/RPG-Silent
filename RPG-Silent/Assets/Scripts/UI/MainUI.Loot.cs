using System;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    RectTransform lootShade, lootWindow;
    Button nearbyButton, lootPick;
    readonly Button[] lootRows = new Button[8], lootFilters = new Button[6];
    TMP_Text lootHeading, lootDetails, lootFooter, lootPages;
    WorldLootPickup displayedLoot;
    float nextLootRefresh;
    int lootPage;
    void BuildLootHud()
    {
        if (lootShade != null)
            return;
        var font = classNameText.font;
        nearbyButton = UIView.Button(UIView.Find(transform, "TopLeftPanel"), "NearbyLootButton", "附近掉落 [L]", () => lootInteraction?.ToggleList());
        var r = nearbyButton.GetComponent<RectTransform>();
        lootShade = UIView.Panel(transform, "NearbyLootShade");
        lootWindow = UIView.Panel(lootShade, "NearbyLootWindow");
        lootHeading = UIView.Text(lootWindow, "NearbyHeading", "附近掉落");
        for (int i = 0; i < lootFilters.Length; i++)
        {
            var rarity = (ItemRarity)i;
            lootFilters[i] = UIView.Button(lootWindow, "LootQuality" + i, new[] { "全部品质", "精良以上", "稀有以上", "史诗以上", "传说以上", "仅神话" }[i], () =>
            {
                lootInteraction?.SetMinimumRarity(rarity);
                lootPage = 0;
                nextLootRefresh = 0;
            });
        }

        for (int i = 0; i < lootRows.Length; i++)
            lootRows[i] = UIView.Button(lootWindow, "NearbyItem" + i, "", () =>
            {
            });
        UIView.Button(lootWindow, "LootPrevious", "上一页", () =>
        {
            lootPage = Mathf.Max(0, lootPage - 1);
            nextLootRefresh = 0;
        });
        lootPages = UIView.Text(lootWindow, "LootPages", "");
        UIView.Button(lootWindow, "LootNext", "下一页", () =>
        {
            if (lootInteraction != null)
                lootPage = Mathf.Min(Mathf.Max(0, (lootInteraction.Nearby.Count - 1) / 8), lootPage + 1);
            nextLootRefresh = 0;
        });
        lootDetails = UIView.Text(lootWindow, "NearbyDetails", "");
        lootFooter = UIView.Text(lootWindow, "NearbyFeedback", "");
        lootPick = UIView.Button(lootWindow, "PickupSelected", "拾取所选", () =>
        {
            if (lootInteraction != null)
                lootInteraction.TryCollectTarget(displayedLoot, out _);
            nextLootRefresh = 0;
        });
        UIView.Button(lootWindow, "CloseNearbyLoot", "关闭 · 返回游戏", () => lootInteraction?.CloseList());
        lootShade.gameObject.SetActive(false);
    }

    void RefreshLootHud()
    {
        if (lootShade == null || lootInteraction == null)
            return;
        bool open = lootInteraction.IsListOpen;
        if (lootShade.gameObject.activeSelf != open)
        {
            lootShade.gameObject.SetActive(open);
            if (open)
            {
                lootShade.SetAsLastSibling();
                lootPage = 0;
                nextLootRefresh = 0;
            }
        }

        nearbyButton.GetComponentInChildren<TMP_Text>().text = $"附近掉落 [{lootInteraction.ListKey}]";
        if (!open)
        {
            displayedLoot = null;
            return;
        }

        if (Time.unscaledTime < nextLootRefresh)
            return;
        nextLootRefresh = Time.unscaledTime + .1f;
        var nearby = lootInteraction.Nearby;
        displayedLoot = lootInteraction.Target;
        lootInteraction.PresentTarget(displayedLoot);
        int selectedIndex = -1;
        for (int i = 0; i < nearby.Count; i++)
            if (nearby[i] == displayedLoot)
                selectedIndex = i;
        // Keyboard cycling follows its selected row across pages.
        if (selectedIndex >= 0 && selectedIndex / 8 != lootPage && lastLootTarget != displayedLoot)
            lootPage = selectedIndex / 8;
        lastLootTarget = displayedLoot;
        lootPage = Mathf.Clamp(lootPage, 0, Mathf.Max(0, (nearby.Count - 1) / 8));
        lootHeading.text = $"附近掉落 · 显示 {nearby.Count} / 可拾取 {lootInteraction.ReachableCount} 项";
        for (int i = 0; i < lootRows.Length; i++)
        {
            int index = lootPage * 8 + i;
            bool used = index < nearby.Count;
            lootRows[i].gameObject.SetActive(used);
            if (!used)
                continue;
            var drop = nearby[index];
            var row = lootRows[i];
            row.onClick.RemoveAllListeners();
            row.onClick.AddListener(() =>
            {
                lootInteraction.SelectTarget(drop);
                nextLootRefresh = 0;
            });
            row.image.color = drop == displayedLoot ? W.Accent : W.Card;
            var label = row.GetComponentInChildren<TMP_Text>();
            label.color = drop.QualityColor;
            label.text = (drop == displayedLoot ? "选中 · " : "") + drop.Item.RarityLabel + " · " + drop.DisplayName + (drop.Payload.upgradeLevel > 0 ? $" +{drop.Payload.upgradeLevel}" : "") + $"  {Vector3.Distance(lootInteraction.transform.position, drop.transform.position):0.0}米";
        }

        for (int i = 0; i < lootFilters.Length; i++)
            lootFilters[i].image.color = (int)lootInteraction.MinimumRarity == i ? W.Accent : W.Card;
        lootPages.text = $"第 {lootPage + 1} / {Mathf.Max(1, (nearby.Count + 7) / 8)} 页";
        lootPick.interactable = displayedLoot != null && lootInteraction.CanReach(displayedLoot);
        lootPick.GetComponentInChildren<TMP_Text>().text = $"拾取所选 [{lootInteraction.PickupKey} / Enter]";
        lootFooter.text = string.IsNullOrEmpty(lootInteraction.Feedback) ? "↑/↓ 切换目标；品质筛选仅改变显示。\n金币、经验继续吸附；满包或保存失败时物品保留。" : lootInteraction.Feedback;
        lootDetails.text = DescribeGroundLoot(displayedLoot);
    }

    WorldLootPickup lastLootTarget;
    string DescribeGroundLoot(WorldLootPickup drop)
    {
        if (drop == null)
            return "没有符合筛选条件、距离和视线要求的物品。\n移动靠近或降低品质门槛后查看。\n\n筛选不会删除物品，也不会自动拾取装备。";
        var item = drop.Item;
        var payload = drop.Payload;
        string text = $"{item.DisplayName} · {item.RarityLabel}\n{item.SlotLabel} · Lv.{item.RequiredLevel} · 数量 {payload.count}\n{item.Description}\n";
        if (item.IsStackable)
            return text + $"\n背包持有 {_progress.Inventory.Count(item.Id)} 件\n拾取后仍可在背包中整理、使用或存储。";
        text += $"\n强化 +{payload.upgradeLevel} · 宝石 {_progress.FindItem(payload.gemId)?.DisplayName ?? "无"}\n";
        string equipped = item.Kind == ItemKind.Equipment ? _progress.Inventory.equipped[(int)item.Slot] : _progress.EquippedWeaponKey(_progress.CurrentLoadout.activeWeaponSlot);
        text += "与当前" + (item.Kind == ItemKind.Equipment ? item.SlotLabel : "使用武器") + "比较（单件含强化与宝石）\n";
        foreach (ItemAffixStat stat in Enum.GetValues(typeof(ItemAffixStat)))
        {
            var gem = _progress.FindItem(payload.gemId);
            int value = EquipmentStats.Total(item, payload, stat) + (gem != null && gem.IsGem ? EquipmentStats.BaseValue(gem, stat) : 0);
            int delta = value - _progress.ItemStat(equipped, stat);
            string suffix = stat >= ItemAffixStat.AttackPercent ? "%" : "";
            text += new[]
            {
                "生命",
                "能量",
                "伤害",
                "减伤"
            }[(int)stat] + $" +{value}{suffix}  ({delta:+0;-0;0}{suffix})\n";
        }

        if (item.Kind == ItemKind.Weapon)
            text += $"品质与强化伤害倍率 {item.Weapon.DamageMultiplier * (1 + payload.upgradeLevel * .1f):0.00}\n";
        if (payload.affixes.Length > 0)
            text += "\n随机词条已包含在以上数值中；拾取保留原实例。";
        return text;
    }
}
