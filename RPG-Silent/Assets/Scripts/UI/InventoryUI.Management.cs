using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class InventoryUI
{
    readonly TMP_Text[] badges = new TMP_Text[24];
    readonly HashSet<string> batchKeys = new();
    EquipmentLoadoutData draft;
    public EquipmentLoadoutData PreviewLoadout => draft?.Copy();
    bool batchMode;
    string batchQuote, presetConfirmation;
    Button lockItem, multiTryButton, batchButton, loadoutsButton, batchConfirm, outfitConfirm;
    RectTransform presetShade, presetRows;
    TMP_Text presetStatus;
    void BuildManagement()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            badges[i] = W.Text(cells[i].transform, Font, "Badge", "", 4, 2, 84, 19, 15);
            icons[i].rectTransform.anchoredPosition = new Vector2(27, -22); icons[i].rectTransform.sizeDelta = new Vector2(38, 38);
        }
        lockItem = W.Button(panel, Font, "LockItem", "锁定装备", 1492, 102, 160, 36, () =>
        {
            var entry = progress.FindInstance(selected); if (entry == null) return;
            progress.TrySetItemLocked(selected, !entry.isLocked, out string message); Show(message);
        });
        panel.Find("DetailsHeading").GetComponent<RectTransform>().sizeDelta = new Vector2(280, 40);
        multiTryButton = W.Button(panel, Font, "MultiTryOn", "多件试穿", 566, 778, 180, 40, BeginOutfitPreview);
        batchButton = W.Button(panel, Font, "BatchMode", "批量分解", 756, 778, 180, 40, ToggleBatch);
        loadoutsButton = W.Button(panel, Font, "Loadouts", "配装方案", 946, 778, 224, 40, () => { presetConfirmation = null; presetShade.gameObject.SetActive(true); RefreshPresets(); });
        batchConfirm = W.Button(panel, Font, "BatchConfirm", "分解所选", 990, 718, 180, 40, ConfirmBatch);
        outfitConfirm = W.Button(panel, Font, "ConfirmOutfit", "确认整套", 376, 718, 164, 36, ConfirmOutfit);
        presetShade = W.Panel(panel, "PresetShade", new Color(0, 0, 0, .92f));
        var window = W.CenterPanel(presetShade, "Window", 1060, 650);
        W.Text(window, Font, "Title", "配 装 方 案", 28, 20, 700, 48, 32);
        W.Button(window, Font, "Close", "关闭", 918, 24, 114, 42, () => presetShade.gameObject.SetActive(false));
        W.Text(window, Font, "Help", "保存实际穿戴的五件防具、两把武器和当前武器槽。先试穿可查看整套效果。\n涉及武器的整套调整请在主城完成；方案缺失装备时整套不更换。", 28, 76, 1004, 60, 20);
        presetRows = W.Rect(window, "Rows", 28, 150, 1004, 420);
        presetStatus = W.Text(window, Font, "Status", "覆盖或清空方案需再次点击确认，背包物品不会被删除。", 28, 590, 1004, 48, 19);
        ResetManagement();
    }
    void ResetManagement()
    {
        CloseDiscard();
        draft = null; batchMode = false; batchKeys.Clear(); batchQuote = presetConfirmation = null;
        if (presetShade != null) presetShade.gameObject.SetActive(false);
    }
    public void BeginOutfitPreview()
    {
        if (draft != null) { CancelTryOn(); return; }
        batchMode = false; batchKeys.Clear(); batchQuote = null; confirmation = tryOnItemId = null;
        draft = progress.CurrentLoadout; filter = page = 0;
        status.text = "已开始多件试穿。依次点击装备组合外观；确认整套后一次应用，取消不改存档。"; Refresh();
    }
    bool SelectManagedItem(string key)
    {
        if (!batchMode && draft == null) return false;
        selected = key; confirmation = tryOnItemId = null;
        if (batchMode)
        {
            batchQuote = null;
            if (!progress.CanDismantle(key)) status.text = "只能选择未穿戴、未锁定、没有宝石的防具饰品。";
            else { if (!batchKeys.Remove(key)) batchKeys.Add(key); status.text = $"已选择 {batchKeys.Count} 件，可获得锻造碎片 ×{progress.DismantleYield(batchKeys.ToArray())}。"; }
        }
        else
        {
            var item = progress.FindItem(key);
            if (filter != 4 && progress.Inventory.Find(key) != null)
            {
                if (item.Kind == ItemKind.Equipment) draft.armorKeys[(int)item.Slot] = key;
            }
            status.text = item?.Kind == ItemKind.Weapon ? "请选择“试穿到武器槽 1”或“试穿到武器槽 2”，确认整套后才装备。" :
                "整套试穿尚未装备；同一部位选择新装备会替换预览，取消可全部恢复。";
        }
        Refresh(); return true;
    }
    void ReconcileManagement()
    {
        batchKeys.RemoveWhere(k => !progress.CanDismantle(k));
        if (batchQuote != null && batchQuote != progress.DismantleQuote(batchKeys.ToArray())) batchQuote = null;
        if (draft != null && !progress.CheckLoadout(draft, false, out string message)) { draft = null; status.text = "试穿已取消：" + message; }
    }
    void RefreshManagement()
    {
        var entry = progress.FindInstance(selected);
        lockItem.interactable = entry != null && entry.IsInstance && !entry.legacyReserve && progress.Inventory.Find(selected) != null && progress.CanManageInventory;
        lockItem.GetComponentInChildren<TMP_Text>().text = entry?.isLocked == true ? "解除锁定" : "锁定装备";
        multiTryButton.GetComponentInChildren<TMP_Text>().text = draft == null ? "多件试穿" : "退出多件试穿";
        multiTryButton.interactable = !batchMode;
        batchButton.GetComponentInChildren<TMP_Text>().text = batchMode ? "退出批量分解" : "批量分解";
        loadoutsButton.interactable = !batchMode;
        foreach (var tab in tabs) tab.interactable = !batchMode;
        collect.gameObject.SetActive(!batchMode); batchConfirm.gameObject.SetActive(batchMode);
        batchConfirm.interactable = batchKeys.Count > 0 && progress.CanManageInventory && !player.CombatAllowed;
        batchConfirm.GetComponentInChildren<TMP_Text>().text = (batchQuote == null ? "分解所选 " : "确认分解 ") + batchKeys.Count + " 件";
        outfitConfirm.gameObject.SetActive(draft != null);
        outfitConfirm.interactable = draft != null && progress.CanManageInventory && progress.CheckLoadout(draft, true, out _);
        if (draft != null)
        {
            var item = progress.FindItem(selected); bool weapon = item?.Kind == ItemKind.Weapon;
            wear.interactable = weapon ? progress.Inventory.Find(selected) != null : outfitConfirm.interactable;
            wear.GetComponentInChildren<TMP_Text>().text = weapon ? "试穿到武器槽 1" : "确认整套";
            secondary.interactable = weapon && progress.Inventory.Find(selected) != null; secondary.GetComponentInChildren<TMP_Text>().text = "试穿到武器槽 2";
            remove.interactable = item?.Kind == ItemKind.Equipment && draft.armorKeys[(int)item.Slot].Length > 0;
            remove.GetComponentInChildren<TMP_Text>().text = "预览脱下此部位";
        }
        else remove.GetComponentInChildren<TMP_Text>().text = "卸下";
        if (batchMode || draft != null) { sell.interactable = false; dismantle.interactable = false; }
        if (batchMode) { wear.interactable = secondary.interactable = remove.interactable = false; }
        if (presetShade.gameObject.activeSelf) RefreshPresets();
    }
    void PreviewWeaponSlot(int slot)
    {
        if (draft == null || progress.Inventory.Find(selected) == null || progress.FindItem(selected)?.Kind != ItemKind.Weapon) return;
        draft.weaponKeys[slot] = selected; draft.activeWeaponSlot = slot; Refresh();
    }
    public void ConfirmOutfit()
    {
        if (draft == null) return;
        if (progress.TryApplyLoadout(draft, out string message)) draft = null;
        Show(message);
    }
    public void ToggleBatch()
    {
        batchMode = !batchMode; batchKeys.Clear(); batchQuote = null; draft = null; selected = confirmation = tryOnItemId = null; filter = 1; page = 0;
        status.text = batchMode ? "点击要分解的防具多选。已穿戴、锁定和镶嵌宝石的装备受保护；再次确认后统一分解。" : "已退出批量分解。"; Refresh();
    }
    void ConfirmBatch()
    {
        var keys = batchKeys.OrderBy(k => k).ToArray(); string quote = progress.DismantleQuote(keys);
        if (quote == null) { Show("请选择可分解装备。"); return; }
        if (batchQuote != quote) { batchQuote = quote; Show($"确认分解所选 {keys.Length} 件装备？获得锻造碎片 ×{progress.DismantleYield(keys)}；再次点击确认。"); return; }
        if (progress.TryDismantleItems(keys, batchQuote, out string message)) { batchKeys.Clear(); batchQuote = null; }
        Show(message);
    }
    void RefreshPresets()
    {
        foreach (Transform child in presetRows) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        for (int i = 0; i < EquipmentLoadoutData.MaxPresets; i++)
        {
            int slot = i; var saved = progress.SavedLoadout(i); string problem = "空方案";
            bool complete = saved != null && progress.CheckLoadout(saved, false, out problem);
            string stats = complete ? $"生命 +{progress.LoadoutStat(saved, ItemAffixStat.Health)} · 能量 +{progress.LoadoutStat(saved, ItemAffixStat.Energy)} · 伤害 +{progress.LoadoutStat(saved, ItemAffixStat.AttackPercent)}% · 减伤 {progress.LoadoutStat(saved, ItemAffixStat.DefensePercent)}%" : problem;
            W.Text(presetRows, Font, "Preset" + i, $"方案 {i + 1} · {(saved == null ? "未保存" : "已保存")}\n{stats}", 12, i * 140, 980, 58, 22);
            W.Button(presetRows, Font, "SavePreset:" + i, presetConfirmation == "save:" + i ? "确认覆盖方案" : "保存当前装备", 12, i * 140 + 68, 230, 44, () => SavePreset(slot));
            var previewButton = W.Button(presetRows, Font, "PreviewPreset:" + i, "试穿整套", 252, i * 140 + 68, 230, 44, () => PreviewPreset(slot)); previewButton.interactable = complete;
            var apply = W.Button(presetRows, Font, "ApplyPreset:" + i, "一键换装", 492, i * 140 + 68, 230, 44, () =>
            { if (progress.TryApplySavedLoadout(slot, out string message)) { draft = null; tryOnItemId = confirmation = null; } presetStatus.text = message; Refresh(); }); apply.interactable = complete && progress.CanManageInventory;
            var clear = W.Button(presetRows, Font, "DeletePreset:" + i, presetConfirmation == "delete:" + i ? "确认清空方案" : "清空方案", 732, i * 140 + 68, 250, 44, () => DeletePreset(slot)); clear.interactable = saved != null;
        }
    }
    void SavePreset(int slot)
    {
        if (progress.SavedLoadout(slot) != null && presetConfirmation != "save:" + slot)
        { presetConfirmation = "save:" + slot; presetStatus.text = $"再次点击，将用实际穿戴覆盖方案 {slot + 1}，不保存未确认的试穿。"; RefreshPresets(); return; }
        progress.TrySaveLoadout(slot, out string message); presetConfirmation = null; presetStatus.text = message; Refresh();
    }
    void DeletePreset(int slot)
    {
        if (presetConfirmation != "delete:" + slot) { presetConfirmation = "delete:" + slot; presetStatus.text = "再次点击清空该方案；不删除任何装备。"; RefreshPresets(); return; }
        progress.TryDeleteLoadout(slot, out string message); presetConfirmation = null; presetStatus.text = message; Refresh();
    }
    void PreviewPreset(int slot)
    {
        var saved = progress.SavedLoadout(slot); if (!progress.CheckLoadout(saved, false, out string message)) { presetStatus.text = message; return; }
        draft = saved; tryOnItemId = confirmation = null; selected = null; presetShade.gameObject.SetActive(false); filter = page = 0;
        Show($"正在试穿方案 {slot + 1}，尚未更换实际装备。");
    }
}
