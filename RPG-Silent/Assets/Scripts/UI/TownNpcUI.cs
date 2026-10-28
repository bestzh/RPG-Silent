using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI : UIBase
{
    [Inject] PlayerProgressService progress;
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
        W.Panel(transform, "Shade", new Color(0, 0, 0, .65f));
        panel = W.CenterPanel(transform, "Conversation", 1060, 740);
        title = W.Text(panel, Font, "Title", "", 32, 22, 880, 48, 32);
        details = W.Text(panel, Font, "Details", "", 32, 76, 980, 64);
        W.Button(panel, Font, "Close", "关闭", 928, 24, 100, 42, () => interaction?.Close());
        slotLabel = W.Text(panel, Font, "Slot", "", 32, 142, 200, 36);
        W.Button(panel, Font, "PreviousSlot", "上一槽", 236, 138, 106, 40, () => { selectedSlot = (selectedSlot + 9) % 10; Refresh(); });
        W.Button(panel, Font, "NextSlot", "下一槽", 352, 138, 106, 40, () => { selectedSlot = (selectedSlot + 1) % 10; Refresh(); });
        W.Button(panel, Font, "ClearSlot", "卸下该槽技能", 470, 138, 180, 40, ClearSlot);
        weaponTab = W.Button(panel, Font, "WeaponForge", "武器强化", 32, 138, 160, 40, () => SelectForge(false));
        armorTab = W.Button(panel, Font, "ArmorForge", "防具饰品强化", 204, 138, 200, 40, () => SelectForge(true));
        gemTab = W.Button(panel, Font, "GemForge", "宝石工坊", 416, 138, 168, 40, () =>
        { gemForge = true; socketTarget = null; rows.anchoredPosition = Vector2.zero; status.text = "先制作宝石，再选择具体装备镶嵌。替换和拆卸会返还旧宝石。"; Refresh(); });
        materialBalance = W.Text(panel, Font, "ForgeMaterials", "", 602, 142, 418, 36, 19);
        var viewport = W.Rect(panel, "Viewport", 32, 192, 996, 430);
        viewport.gameObject.AddComponent<Image>().color = W.Card; viewport.gameObject.AddComponent<RectMask2D>();
        rows = W.Rect(viewport, "Rows", 0, 0, 980, 430);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = rows;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
        status = W.Text(panel, Font, "Status", "新技能自动装入第一个空槽。转职后可继续学习基础职业技能。", 32, 646, 996, 72, 21);
        BuildQuestTabs();
    }
    public override void OnOpen(params object[] args)
    {
        progress.Changed -= Refresh;
        interaction = args.Length > 0 ? args[0] as PlayerNpcInteraction : null;
        advanceConfirmation = null; selectedSlot = 0; armorForge = gemForge = false; socketTarget = null;
        questPage = false;
        supplyPage = false;
        status.text = interaction != null && interaction.Conversation != null && interaction.Conversation.Role == TownNpcRole.Blacksmith
            ? "强化必定成功。武器需先装入武器槽；防具饰品可直接选择背包中的装备。"
            : "新技能自动装入第一个空槽。转职后可继续学习基础职业技能。";
        progress.Changed += Refresh; Refresh();
    }
    public override void OnClose() { progress.Changed -= Refresh; var previous = interaction; interaction = null; previous?.Close(); }
    void OnDestroy() { if (progress != null) progress.Changed -= Refresh; }
    bool Validate(TownNpcRole role)
    {
        if (interaction == null) return false;
        bool valid = interaction.ValidateConversation(role, out string message);
        if (!valid) status.text = message; return valid;
    }
    void Refresh()
    {
        if (panel == null || interaction == null || interaction.Conversation == null) return;
        foreach (Transform child in rows) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var npc = interaction.Conversation;
        bool mentor = npc.Role == TownNpcRole.Mentor;
        bool smith = npc.Role == TownNpcRole.Blacksmith;
        foreach (string name in new[] { "Slot", "PreviousSlot", "NextSlot", "ClearSlot" }) panel.Find(name).gameObject.SetActive(mentor && !questPage && !supplyPage);
        weaponTab.gameObject.SetActive(smith && !questPage && !supplyPage); armorTab.gameObject.SetActive(smith && !questPage && !supplyPage); gemTab.gameObject.SetActive(smith && !questPage && !supplyPage); materialBalance.gameObject.SetActive(smith && !questPage && !supplyPage);
        weaponTab.image.color = !armorForge && !gemForge ? W.Accent : W.Card; armorTab.image.color = armorForge && !gemForge ? W.Accent : W.Card;
        gemTab.image.color = gemForge ? W.Accent : W.Card;
        materialBalance.text = $"锻造碎片 {progress.ArmorUpgradeMaterials} · 晶核碎片 {progress.Inventory.Count(PlayerProgressService.GemCraftMaterialId)}";
        var viewport = rows.parent.GetComponent<RectTransform>();
        viewport.anchoredPosition = new Vector2(32, questPage || supplyPage ? -192 : -246);
        viewport.sizeDelta = new Vector2(996, questPage || supplyPage ? 430 : 376);
        title.text = npc.DisplayName;
        details.text = $"{progress.CurrentClass.DisplayName}  ·  Lv.{progress.Level}  ·  金币 {progress.Gold}\n" +
            (smith ? gemForge ? "每件防具饰品可镶嵌 1 颗宝石。镶嵌、替换、拆卸均为 50 金币；旧宝石完整返还。" : armorForge ? "防具基础属性每级 +10%，最高 +5；随机词条保持不变。强化仅作用于所选装备。" :
            "武器每级增加 10% 普攻伤害，最高 +5。只强化所选槽位中的这一件武器。" : "基础职业技能最高 3 级；转职后最高 5 级。滚动查看全部技能与转职分支。");
        slotLabel.text = $"装配目标：槽 {(selectedSlot + 1) % 10}";
        RefreshQuestTabs();
        float y = 10;
        if (questPage) y = AddQuestRows(y);
        else if (supplyPage) y = AddSupplyRows(y);
        else if (smith && gemForge) y = AddGemRows(y);
        else if (smith && armorForge) y = AddArmorRows(y);
        else if (smith)
        {
            foreach (int slot in Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Where(i => progress.EquippedWeapon(i) != null))
            {
                var w = progress.EquippedWeapon(slot); string key = progress.EquippedWeaponKey(slot);
                int rank = progress.GetInstanceLevel(key); int cost = progress.WeaponUpgradeCost(rank + 1);
                W.Text(rows, Font, w.name, $"槽 {slot + 1} · {w.DisplayName}  +{rank}\n普攻伤害加成 {rank * 10}% → {Mathf.Min(5, rank + 1) * 10}%", 16, y, 620, 82, 23);
                var button = W.Button(rows, Font, "UpgradeWeapon", rank < 5 ? $"强化 · {cost} 金币" : "已满级", 710, y + 12, 240, 48, () =>
                { if (Validate(TownNpcRole.Blacksmith)) { progress.TryUpgradeWeaponInstance(key, rank, out string message); status.text = message; Refresh(); } });
                button.interactable = rank < 5 && progress.Gold >= cost; y += 108;
            }
        }
        else if (npc.Profession != null && npc.Profession.Id == progress.CurrentClass.FamilyId)
        {
            foreach (var skill in progress.AvailableLessons.Concat(progress.LearnedSkills).Distinct())
            {
                var s = skill; int rank = progress.GetSkillRank(s); int next = rank + 1;
                int cap = progress.SkillRankCap(s); bool study = progress.CanStudy(s) && rank < cap;
                string requirement = study ? $"下一级：Lv.{s.LevelForRank(next)} / {s.CostForRank(next)} 金币" : "当前职业已达上限";
                W.Text(rows, Font, s.name, $"{s.SkillName}  {(rank == 0 ? "未学习" : $"{rank} 级")}\n{requirement}  ·  能量 {s.EnergyCost:0} / 冷却 {s.CooldownTime:0.#} 秒", 16, y, 580, 78, 21);
                var train = W.Button(rows, Font, "TrainSkill", rank == 0 ? "学习" : "升级", 610, y + 10, 150, 48, () =>
                { if (Validate(TownNpcRole.Mentor)) { progress.TryTrainSkill(s, rank, out string message); status.text = message; Refresh(); } });
                train.interactable = study && progress.Level >= s.LevelForRank(next) && progress.Gold >= s.CostForRank(next);
                var equip = W.Button(rows, Font, "EquipSkill", "装配", 780, y + 10, 170, 48, () =>
                { if (Validate(TownNpcRole.Mentor)) { progress.TryEquipLearnedSkill(s, selectedSlot, out string message); status.text = message; Refresh(); } });
                equip.interactable = rank > 0; y += 92;
            }
            foreach (var branch in progress.Classes.Where(c => c.BaseClass == progress.CurrentClass))
            {
                var c = branch;
                W.Text(rows, Font, c.Id, $"转职 · {c.DisplayName}\nLv.{c.RequiredLevel} / {c.AdvancementCost} 金币 · {c.Description}", 16, y, 670, 100, 21);
                var b = W.Button(rows, Font, "Advance", advanceConfirmation == c ? "确认转职" : "选择分支", 760, y + 12, 190, 50, () =>
                {
                    if (!Validate(TownNpcRole.Mentor)) return;
                    if (advanceConfirmation != c) { advanceConfirmation = c; status.text = $"确定转职为{c.DisplayName}？本角色无法改选另一分支。再次点击确认。"; Refresh(); return; }
                    progress.TryAdvanceClass(c, out string message); advanceConfirmation = null; status.text = message; Refresh();
                });
                b.interactable = progress.Level >= c.RequiredLevel && progress.Gold >= c.AdvancementCost; y += 118;
            }
        }
        else W.Text(rows, Font, "WrongMentor", $"这里是{npc.Profession?.DisplayName}导师。\n请寻找与你职业对应的导师。", 24, y, 920, 100, 26);
        rows.sizeDelta = new Vector2(980, Mathf.Max(viewport.sizeDelta.y, y + 10));
    }
    void SelectForge(bool armor)
    {
        armorForge = armor; gemForge = false; socketTarget = null; rows.anchoredPosition = Vector2.zero;
        status.text = armor ? "基础属性强化加成按总等级计算并向上取整；减伤总上限 50%。滚动可查看全部防具饰品。" :
            "先装备要强化的武器。两件同名武器分别保存强化等级。";
        Refresh();
    }
    float AddArmorRows(float y)
    {
        var inventory = progress.Inventory;
        var entries = inventory.bag.Where(e => progress.FindItem(e.itemId)?.Kind == ItemKind.Equipment)
            .OrderByDescending(e => inventory.equipped.Contains(e.Key)).ThenBy(e => progress.FindItem(e.itemId).Slot)
            .ThenByDescending(e => progress.FindItem(e.itemId).Rarity).ToArray();
        if (entries.Length == 0)
        {
            W.Text(rows, Font, "EmptyArmor", "背包里还没有防具或饰品。\n可从副本拾取，或先领取待领取页中的装备。", 24, y, 920, 100, 24);
            return y + 110;
        }
        foreach (var entry in entries)
        {
            string key = entry.Key; var item = progress.FindItem(key);
            int rank = entry.upgradeLevel, next = Mathf.Min(EquipmentStats.MaxArmorLevel, rank + 1);
            int gold = progress.ArmorUpgradeGoldCost(next), material = progress.ArmorUpgradeMaterialCost(next);
            var nextEntry = entry.Copy(); nextEntry.upgradeLevel = next;
            string values = string.Join("  ·  ", new[] { ItemAffixStat.Health, ItemAffixStat.Energy }.Select(s => ForgeStat(item, entry, nextEntry, s))) + "\n" +
                string.Join("  ·  ", new[] { ItemAffixStat.AttackPercent, ItemAffixStat.DefensePercent }.Select(s => ForgeStat(item, entry, nextEntry, s)));
            string worn = inventory.equipped.Contains(key) ? "已穿戴" : "未穿戴";
            var heading = W.Text(rows, Font, "ArmorName", $"{item.SlotLabel} · {item.DisplayName}  +{rank}" + (rank < EquipmentStats.MaxArmorLevel ? $" → +{next}" : " · 已满级") + $" · {worn}", 16, y, 674, 34, 22);
            heading.color = item.QualityColor;
            W.Text(rows, Font, "ArmorStats", values, 16, y + 38, 674, 68, 21);
            var button = W.Button(rows, Font, "UpgradeArmor:" + key, rank < EquipmentStats.MaxArmorLevel ?
                $"强化 · {gold} 金币\n锻造碎片 {progress.ArmorUpgradeMaterials} / {material}" : "已达到 +5", 710, y + 18, 240, 68, () =>
            {
                if (Validate(TownNpcRole.Blacksmith))
                { progress.TryUpgradeArmor(key, rank, out string message); status.text = message; Refresh(); }
            });
            button.GetComponentInChildren<TMP_Text>().fontSize = 20;
            button.interactable = rank < EquipmentStats.MaxArmorLevel && progress.Gold >= gold && progress.ArmorUpgradeMaterials >= material && progress.CanManageInventory;
            y += 128;
        }
        return y;
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
        var inventory = progress.Inventory; var entry = inventory.Find(socketTarget);
        if (entry == null) socketTarget = null;
        if (entry != null)
        {
            var item = progress.FindItem(entry.Key); string oldGem = entry.gemId, key = entry.Key;
            W.Text(rows, Font, "SocketTarget", $"{item.DisplayName} +{entry.upgradeLevel} · {item.SlotLabel}\n当前宝石：{progress.FindItem(oldGem)?.DisplayName ?? "空槽"}", 16, y, 640, 66, 22).color = item.QualityColor;
            W.Button(rows, Font, "SocketBack", "返回制作 / 选装备", 700, y + 4, 250, 46, () => { socketTarget = null; rows.anchoredPosition = Vector2.zero; Refresh(); }); y += 72;
            foreach (var gem in progress.Gems)
            {
                string id = gem.Id; int count = inventory.Count(id);
                W.Text(rows, Font, "GemDescription", $"{gem.DisplayName} · {GemStats(gem)}\n背包持有 {count} 颗", 16, y, 654, 64, 22);
                var b = W.Button(rows, Font, "SocketGem:" + id, oldGem == id ? "已镶嵌" : $"{(oldGem.Length == 0 ? "镶嵌" : "替换")} · 50 金币", 710, y + 6, 240, 48, () =>
                { if (Validate(TownNpcRole.Blacksmith)) { progress.TrySocketGem(key, id, oldGem, out string message); status.text = message; Refresh(); } });
                b.interactable = oldGem != id && count > 0 && progress.Gold >= PlayerProgressService.GemSocketGold && progress.CanManageInventory; y += 70;
            }
            var remove = W.Button(rows, Font, "RemoveGem", "拆卸宝石 · 50 金币", 16, y, 360, 48, () =>
            { if (Validate(TownNpcRole.Blacksmith)) { progress.TrySocketGem(key, "", oldGem, out string message); status.text = message; Refresh(); } });
            remove.interactable = oldGem.Length > 0 && progress.Gold >= PlayerProgressService.GemSocketGold && progress.CanManageInventory;
            return y + 58;
        }
        foreach (var gem in progress.Gems)
        {
            string id = gem.Id;
            W.Text(rows, Font, "GemRecipe", $"{gem.DisplayName} · {GemStats(gem)}\n背包 {inventory.Count(id)} 颗 · 制作需晶核碎片 ×3", 16, y, 650, 64, 22);
            var b = W.Button(rows, Font, "CraftGem:" + id, "制作 · 150 金币", 710, y + 6, 240, 48, () =>
            { if (Validate(TownNpcRole.Blacksmith)) { progress.TryCraftGem(id, out string message); status.text = message; Refresh(); } });
            b.interactable = inventory.Count(PlayerProgressService.GemCraftMaterialId) >= PlayerProgressService.GemCraftCrystals && progress.Gold >= PlayerProgressService.GemCraftGold && progress.CanManageInventory; y += 82;
        }
        W.Text(rows, Font, "SocketHeading", "选择要镶嵌的装备（向下滚动）", 16, y, 900, 40, 24); y += 50;
        var entries = inventory.bag.Where(x => progress.FindItem(x.itemId)?.Kind == ItemKind.Equipment)
            .OrderByDescending(x => inventory.equipped.Contains(x.Key)).ThenBy(x => progress.FindItem(x.itemId).Slot).ToArray();
        if (entries.Length == 0) { W.Text(rows, Font, "NoSocketEquipment", "背包里还没有防具或饰品。", 16, y, 900, 48, 22); y += 60; }
        foreach (var armor in entries)
        {
            string key = armor.Key; var item = progress.FindItem(key);
            W.Text(rows, Font, "SocketEquipment", $"{item.DisplayName} +{armor.upgradeLevel} · {item.SlotLabel}\n宝石：{progress.FindItem(armor.gemId)?.DisplayName ?? "空槽"}", 16, y, 660, 64, 22).color = item.QualityColor;
            W.Button(rows, Font, "ChooseSocket:" + key, "选择镶嵌 / 拆卸", 710, y + 6, 240, 48, () => { socketTarget = key; rows.anchoredPosition = Vector2.zero; Refresh(); }); y += 82;
        }
        return y;
    }
    public static string GemStats(ItemDefinition gem) => string.Join(" · ", System.Enum.GetValues(typeof(ItemAffixStat)).Cast<ItemAffixStat>()
        .Where(s => EquipmentStats.BaseValue(gem, s) > 0).Select(s => (s == ItemAffixStat.Health ? "生命" : s == ItemAffixStat.Energy ? "能量" : s == ItemAffixStat.AttackPercent ? "伤害" : "减伤") +
            $" +{EquipmentStats.BaseValue(gem, s)}" + (s >= ItemAffixStat.AttackPercent ? "%" : "")));
    void ClearSlot()
    {
        if (!Validate(TownNpcRole.Mentor)) return;
        progress.TryEquipLearnedSkill(null, selectedSlot, out string message); status.text = message; Refresh();
    }
}
