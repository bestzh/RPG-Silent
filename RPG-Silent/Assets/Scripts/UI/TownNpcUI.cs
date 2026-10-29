using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI : UIBase
{
    [Inject]
    PlayerProgressService progress;
    public TMP_FontAsset Font;
    PlayerNpcInteraction interaction;
    RectTransform panel, rows;
    TMP_Text title, details, status, slotLabel;
    PlayerClassDefinition advanceConfirmation;
    int selectedSlot;
    bool armorForge, gemForge;
    string socketTarget;
    Button weaponTab, armorTab, gemTab;
    TMP_Text materialBalance;
    protected override void OnInit()
    {
        UIView.Panel(transform, "Shade");
        panel = UIView.Panel(transform, "Conversation");
        title = UIView.Text(panel, "Title", "");
        details = UIView.Text(panel, "Details", "");
        UIView.Button(panel, "Close", "关闭", () => interaction?.Close());
        slotLabel = UIView.Text(panel, "Slot", "");
        UIView.Button(panel, "PreviousSlot", "上一槽", () =>
        {
            selectedSlot = (selectedSlot + 9) % 10;
            Refresh();
        });
        UIView.Button(panel, "NextSlot", "下一槽", () =>
        {
            selectedSlot = (selectedSlot + 1) % 10;
            Refresh();
        });
        UIView.Button(panel, "ClearSlot", "卸下该槽技能", ClearSlot);
        weaponTab = UIView.Button(panel, "WeaponForge", "武器强化", () => SelectForge(false));
        armorTab = UIView.Button(panel, "ArmorForge", "防具饰品强化", () => SelectForge(true));
        gemTab = UIView.Button(panel, "GemForge", "宝石工坊", () =>
        {
            gemForge = true;
            socketTarget = null;
            rows.anchoredPosition = Vector2.zero;
            status.text = "先制作宝石，再选择具体装备镶嵌。替换和拆卸会返还旧宝石。";
            Refresh();
        });
        materialBalance = UIView.Text(panel, "ForgeMaterials", "");
        var viewport = UIView.Rect(panel, "Viewport");
        viewport.gameObject.GetComponent<Image>().color = W.Card;
        rows = UIView.Rect(viewport, "Rows");
        var scroll = viewport.gameObject.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = rows;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35;
        status = UIView.Text(panel, "Status", "新技能自动装入第一个空槽。转职后可继续学习基础职业技能。");
        BuildQuestTabs();
        BuildWorldTabs();
        BuildWarehouse();
        BuildCommerce();
        BuildSpecialization();
        BindV5Presentation();
    }

    public override void OnOpen(params object[] args)
    {
        progress.Changed -= Refresh;
        interaction = args.Length > 0 ? args[0] as PlayerNpcInteraction : null;
        advanceConfirmation = null;
        selectedSlot = 0;
        armorForge = gemForge = false;
        socketTarget = null;
        questPage = interaction?.Conversation != null && interaction.Conversation.Role >= TownNpcRole.Steward;
        questCategory = 0;
        supplyPage = false;
        ResetWorldPages();
        advancementPage = false;
        selectedLesson = null;
        CloseWarehouse();
        CloseCommerce();
        shopBuybackPage = false;
        specializationPage = false;
        CloseSpecialization();
        status.text = interaction != null && interaction.Conversation != null && interaction.Conversation.Role == TownNpcRole.Blacksmith ? "强化必定成功。武器需先装入武器槽；防具饰品可直接选择背包中的装备。" : "新技能自动装入第一个空槽。转职后可继续学习基础职业技能。";
        if (questPage)
            status.text = interaction.Conversation.Greeting + "\n接取后完成目标，返回这里提交领取奖励。";
        progress.Changed += Refresh;
        Refresh();
    }

    public override void OnClose()
    {
        serviceConfirmation?.Hide();
        progress.Changed -= Refresh;
        CloseWarehouse();
        CloseCommerce();
        CloseSpecialization();
        var previous = interaction;
        interaction = null;
        previous?.Close();
    }

    void OnDestroy()
    {
        if (progress != null)
            progress.Changed -= Refresh;
    }

    bool Validate(TownNpcRole role)
    {
        if (interaction == null)
            return false;
        bool valid = interaction.ValidateConversation(role, out string message);
        if (!valid)
            status.text = message;
        return valid;
    }

    void Refresh()
    {
        if (panel == null || interaction == null || interaction.Conversation == null)
            return;
        dataRows.Begin();
        var npc = interaction.Conversation;
        if (questPage || supplyPage || ExtraNpcPage || specializationPage)
            advancementPage = false;
        RefreshWarehouseEntry();
        RefreshCommerce();
        bool mentor = npc.Role == TownNpcRole.Mentor, smith = npc.Role == TownNpcRole.Blacksmith;
        weaponTab.gameObject.SetActive(smith && !questPage && !supplyPage && !ExtraNpcPage);
        armorTab.gameObject.SetActive(weaponTab.gameObject.activeSelf);
        gemTab.gameObject.SetActive(weaponTab.gameObject.activeSelf);
        materialBalance.gameObject.SetActive(weaponTab.gameObject.activeSelf);
        weaponTab.image.color = !armorForge && !gemForge ? W.Accent : W.Card;
        armorTab.image.color = armorForge && !gemForge ? W.Accent : W.Card;
        gemTab.image.color = gemForge ? W.Accent : W.Card;
        materialBalance.text = $"锻造碎片 {progress.ArmorUpgradeMaterials} · 晶核碎片 {progress.Inventory.Count(PlayerProgressService.GemCraftMaterialId)}";
        title.text = storyPage ? "交谈与故事" : craftingPage ? reforgePage ? "词条重铸" : "装备配方锻造" : questPage ? "任务委托" : supplyPage ? shopBuybackPage ? "误售物品回购" : "战斗补给" : smith ? gemForge ? "宝石工坊" : armorForge ? "防具饰品强化" : "武器强化" : specializationPage ? "职业专精" : advancementPage ? "职业进阶" : "技能研习";
        details.text = npc.DisplayName + $" · {progress.CurrentClass.DisplayName} Lv.{progress.Level} · 金币 {progress.Gold}";
        RefreshQuestTabs();
        RefreshWorldTabs();
        RefreshSpecialization();
        RefreshV5Presentation(mentor, smith);
        if (storyPage)
            AddStoryRows(0);
        else if (craftingPage)
            AddCraftingRows(0);
        else if (questPage)
            AddQuestRows(0);
        else if (supplyPage)
            AddSupplyRows(0);
        else if (smith && gemForge)
            AddGemRows(0);
        else if (smith && armorForge)
            AddArmorRows(0);
        else if (smith)
        {
            foreach (int slot in Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Where(i => progress.EquippedWeapon(i) != null))
            {
                var weapon = progress.EquippedWeapon(slot);
                string key = progress.EquippedWeaponKey(slot);
                int rank = progress.GetInstanceLevel(key), cost = progress.WeaponUpgradeCost(rank + 1);
                var row = AddRow($"武器槽 {slot + 1} · {weapon.DisplayName} +{rank}", $"普攻强化加成 +{rank * 10}% → +{Mathf.Min(5, rank + 1) * 10}%\n费用 {cost} 金币");
                row.Action(0, rank < 5 ? "确认强化" : "已满级", () =>
                {
                    if (Validate(TownNpcRole.Blacksmith))
                    {
                        progress.TryUpgradeWeaponInstance(key, rank, out string message);
                        status.text = message;
                        Refresh();
                    }
                }, rank < 5 && progress.Gold >= cost);
            }
        }
        else if (npc.Profession != null && npc.Profession.Id == progress.CurrentClass.FamilyId)
        {
            if (specializationPage)
                AddSpecializationRows(0);
            else if (advancementPage)
                AddAdvancementRows();
            else
                AddMentorLessons();
        }
        else
        {
            lessonDetails.gameObject.SetActive(false);
            AddRow("请寻找本职业导师", $"这里是{npc.Profession?.DisplayName}导师。");
        }

        PresentServiceConfirmation();
    }

    void SelectForge(bool armor)
    {
        armorForge = armor;
        gemForge = false;
        socketTarget = null;
        rows.anchoredPosition = Vector2.zero;
        status.text = armor ? "基础属性强化加成按总等级计算并向上取整；减伤总上限 50%。滚动可查看全部防具饰品。" : "先装备要强化的武器。两件同名武器分别保存强化等级。";
        Refresh();
    }

    float AddArmorRows(float y)
    {
        var inventory = progress.Inventory;
        var entries = inventory.bag.Where(e => progress.FindItem(e.itemId)?.Kind == ItemKind.Equipment).OrderByDescending(e => inventory.equipped.Contains(e.Key)).ThenBy(e => progress.FindItem(e.itemId).Slot).ThenByDescending(e => progress.FindItem(e.itemId).Rarity).ToArray();
        if (entries.Length == 0)
        {
            AddRow("尚无防具或饰品", "可从副本拾取，或先领取待领取页中的装备。");
            return 0;
        }

        foreach (var entry in entries)
        {
            string key = entry.Key;
            var item = progress.FindItem(key);
            int rank = entry.upgradeLevel, next = Mathf.Min(EquipmentStats.MaxArmorLevel, rank + 1);
            int gold = progress.ArmorUpgradeGoldCost(next), material = progress.ArmorUpgradeMaterialCost(next);
            var nextEntry = entry.Copy();
            nextEntry.upgradeLevel = next;
            string values = string.Join("  ·  ", new[] { ItemAffixStat.Health, ItemAffixStat.Energy }.Select(s => ForgeStat(item, entry, nextEntry, s))) + "\n" + string.Join("  ·  ", new[] { ItemAffixStat.AttackPercent, ItemAffixStat.DefensePercent }.Select(s => ForgeStat(item, entry, nextEntry, s)));
            string worn = inventory.equipped.Contains(key) ? "已穿戴" : "未穿戴";
            var row = AddRow($"{item.SlotLabel} · {item.DisplayName} +{rank} · {worn}", values + $"\n金币 {gold} · 锻造碎片 {progress.ArmorUpgradeMaterials}/{material}", item.Icon);
            row.Title.color = item.QualityColor;
            row.Action(0, rank < EquipmentStats.MaxArmorLevel ? $"强化至 +{next}" : "已达到 +5", () =>
            {
                if (Validate(TownNpcRole.Blacksmith))
                {
                    progress.TryUpgradeArmor(key, rank, out string message);
                    status.text = message;
                    Refresh();
                }
            }, rank < EquipmentStats.MaxArmorLevel && progress.Gold >= gold && progress.ArmorUpgradeMaterials >= material && progress.CanManageInventory);
        }

        return 0;
    }

    string ForgeStat(ItemDefinition item, ItemStackData current, ItemStackData next, ItemAffixStat stat)
    {
        string label = stat == ItemAffixStat.Health ? "生命" : stat == ItemAffixStat.Energy ? "能量" : stat == ItemAffixStat.AttackPercent ? "伤害" : "减伤";
        string suffix = stat >= ItemAffixStat.AttackPercent ? "%" : "";
        int gem = progress.ItemGemBonus(current.Key, stat);
        return $"{label} {EquipmentStats.Total(item, current, stat) + gem}{suffix} → {EquipmentStats.Total(item, next, stat) + gem}{suffix}";
    }

    float AddGemRows(float y)
    {
        var inventory = progress.Inventory;
        var entry = inventory.Find(socketTarget);
        if (entry == null)
            socketTarget = null;
        if (entry != null)
        {
            var item = progress.FindItem(entry.Key);
            string oldGem = entry.gemId, key = entry.Key;
            var target = AddRow($"{item.DisplayName} +{entry.upgradeLevel} · {item.SlotLabel}", "当前宝石：" + (progress.FindItem(oldGem)?.DisplayName ?? "空槽"), item.Icon);
            target.Title.color = item.QualityColor;
            target.Action(0, "返回制作 / 选装备", () =>
            {
                socketTarget = null;
                rows.anchoredPosition = Vector2.zero;
                Refresh();
            });
            foreach (var gem in progress.Gems)
            {
                string id = gem.Id;
                int count = inventory.Count(id);
                var row = AddRow(gem.DisplayName, GemStats(gem) + $"\n背包持有 {count} 颗 · 费用 {PlayerProgressService.GemSocketGold} 金币", gem.Icon);
                row.Action(0, oldGem == id ? "已镶嵌" : oldGem.Length == 0 ? "镶嵌宝石" : "替换并返还旧宝石", () =>
                {
                    if (Validate(TownNpcRole.Blacksmith))
                    {
                        progress.TrySocketGem(key, id, oldGem, out string message);
                        status.text = message;
                        Refresh();
                    }
                }, oldGem != id && count > 0 && progress.Gold >= PlayerProgressService.GemSocketGold && progress.CanManageInventory);
            }

            AddRow("拆卸宝石", $"支付 {PlayerProgressService.GemSocketGold} 金币，旧宝石完整返还。").Action(0, "拆卸并返还宝石", () =>
            {
                if (Validate(TownNpcRole.Blacksmith))
                {
                    progress.TrySocketGem(key, "", oldGem, out string message);
                    status.text = message;
                    Refresh();
                }
            }, oldGem.Length > 0 && progress.Gold >= PlayerProgressService.GemSocketGold && progress.CanManageInventory);
            return 0;
        }

        foreach (var gem in progress.Gems)
        {
            string id = gem.Id;
            AddRow(gem.DisplayName, GemStats(gem) + $"\n持有 {inventory.Count(id)} 颗 · {PlayerProgressService.GemCraftGold} 金币 / 晶核碎片 ×{PlayerProgressService.GemCraftCrystals}", gem.Icon).Action(0, "制作一颗", () =>
            {
                if (Validate(TownNpcRole.Blacksmith))
                {
                    progress.TryCraftGem(id, out string message);
                    status.text = message;
                    Refresh();
                }
            }, inventory.Count(PlayerProgressService.GemCraftMaterialId) >= PlayerProgressService.GemCraftCrystals && progress.Gold >= PlayerProgressService.GemCraftGold && progress.CanManageInventory);
        }

        AddRow("选择要镶嵌的装备", "每件防具饰品可镶嵌一颗宝石；替换和拆卸均返还旧宝石。");
        var entries = inventory.bag.Where(x => progress.FindItem(x.itemId)?.Kind == ItemKind.Equipment).OrderByDescending(x => inventory.equipped.Contains(x.Key)).ThenBy(x => progress.FindItem(x.itemId).Slot).ToArray();
        if (entries.Length == 0)
            AddRow("暂无可镶嵌装备", "背包里还没有防具或饰品。");
        foreach (var armor in entries)
        {
            string key = armor.Key;
            var item = progress.FindItem(key);
            var row = AddRow($"{item.DisplayName} +{armor.upgradeLevel} · {item.SlotLabel}", "宝石：" + (progress.FindItem(armor.gemId)?.DisplayName ?? "空槽"), item.Icon);
            row.Title.color = item.QualityColor;
            row.Action(0, "选择镶嵌 / 拆卸", () =>
            {
                socketTarget = key;
                rows.anchoredPosition = Vector2.zero;
                Refresh();
            });
        }

        return 0;
    }

    public static string GemStats(ItemDefinition gem) => string.Join(" · ", System.Enum.GetValues(typeof(ItemAffixStat)).Cast<ItemAffixStat>().Where(s => EquipmentStats.BaseValue(gem, s) > 0).Select(s => (s == ItemAffixStat.Health ? "生命" : s == ItemAffixStat.Energy ? "能量" : s == ItemAffixStat.AttackPercent ? "伤害" : "减伤") + $" +{EquipmentStats.BaseValue(gem, s)}" + (s >= ItemAffixStat.AttackPercent ? "%" : "")));
    void ClearSlot()
    {
        if (!Validate(TownNpcRole.Mentor))
            return;
        progress.TryEquipLearnedSkill(null, selectedSlot, out string message);
        status.text = message;
        Refresh();
    }

    UIListView dataRows;
    UIListRowView AddRow(string heading, string body = "", Sprite icon = null) => dataRows.Add(heading, body, icon);
}
