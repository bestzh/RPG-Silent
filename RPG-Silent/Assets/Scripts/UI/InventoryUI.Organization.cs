using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class InventoryUI
{
    TMP_InputField searchField;
    readonly Button[] sortButtons = new Button[4];
    InventorySort sortOrder;
    bool sortDescending = true;
    enum BulkAction
    {
        None,
        Sell,
        Discard
    }

    BulkAction bulkAction;
    bool BulkSelecting => bulkAction != BulkAction.None;

    readonly Dictionary<string, int> bulkQuantities = new();
    Button bulkSell, bulkDiscard, bulkReview, bulkCommit;
    RectTransform bulkShade;
    TMP_Text bulkTitle, bulkHelp, bulkSummary, bulkPageLabel;
    readonly TMP_Text[] bulkNames = new TMP_Text[8], bulkAmounts = new TMP_Text[8];
    readonly Button[] bulkLess = new Button[8], bulkMore = new Button[8], bulkAll = new Button[8];
    string bulkExpectedQuote;
    int bulkPage;
    string[] BulkKeys => bulkQuantities.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

    InventorySelectionData[] BulkSelection => BulkKeys.Select(k => new InventorySelectionData { key = k, count = bulkQuantities[k] }).ToArray();

    void BuildOrganization()
    {
        string[] names =
        {
            "品质",
            "等级",
            "类型",
            "获得"
        };
        for (int i = 0; i < sortButtons.Length; i++)
        {
            int index = i;
            sortButtons[i] = UIView.Button(panel, "Sort" + i, names[i], () =>
            {
                if ((int)sortOrder == index)
                    sortDescending = !sortDescending;
                else
                {
                    sortOrder = (InventorySort)index;
                    sortDescending = true;
                }

                progress.RecordTutorialAction(TutorialStep.InventorySort);
                page = 0;
                confirmation = null;
                Refresh();
            });
        }

        var rect = UIView.Rect(panel, "Search");
        rect.gameObject.GetComponent<Image>().color = W.Card;
        searchField = rect.gameObject.GetComponent<TMP_InputField>();
        var viewport = UIView.Rect(rect, "Viewport");
        var text = UIView.Text(viewport, "SearchValue", "");
        var placeholder = UIView.Text(viewport, "Placeholder", "搜索物品名称");
        placeholder.color = Color.gray;
        searchField.textViewport = viewport;
        searchField.textComponent = (TextMeshProUGUI)text;
        searchField.placeholder = placeholder;
        searchField.characterLimit = 64;
        searchField.richText = false;
        searchField.onValueChanged.AddListener(_ =>
        {
            page = 0;
            confirmation = null;
            Refresh();
        });
        UIView.Button(panel, "ClearSearch", "清", () => searchField.text = "");
        bulkSell = UIView.Button(panel, "BulkSell", "批量出售", () => BeginBulk(BulkAction.Sell));
        bulkDiscard = UIView.Button(panel, "BulkDiscard", "批量丢弃", () => BeginBulk(BulkAction.Discard));
        bulkReview = UIView.Button(panel, "ReviewBulk", "核对清单", OpenBulkReview);
        bulkShade = UIView.Panel(panel, "BulkShade");
        var window = UIView.Panel(bulkShade, "BulkWindow");
        bulkTitle = UIView.Text(window, "BulkTitle", "");
        bulkHelp = UIView.Text(window, "BulkHelp", "");
        for (int i = 0; i < bulkNames.Length; i++)
        {
            int row = i;
            float y = 154 + i * 44;
            bulkNames[i] = UIView.Text(window, "BulkName" + i, "");
            bulkLess[i] = UIView.Button(window, "BulkLess" + i, "−", () => ChangeBulkQuantity(row, -1));
            bulkAmounts[i] = UIView.Text(window, "BulkAmount" + i, "");
            bulkAmounts[i].alignment = TextAlignmentOptions.Center;
            bulkMore[i] = UIView.Button(window, "BulkMore" + i, "+", () => ChangeBulkQuantity(row, 1));
            bulkAll[i] = UIView.Button(window, "BulkAll" + i, "全部", () => ChangeBulkQuantity(row, int.MaxValue));
        }

        UIView.Button(window, "BulkPrevious", "上一页", () =>
        {
            bulkPage = Mathf.Max(0, bulkPage - 1);
            RefreshBulkReview();
        });
        bulkPageLabel = UIView.Text(window, "BulkPage", "");
        UIView.Button(window, "BulkNext", "下一页", () =>
        {
            bulkPage = Mathf.Min((bulkQuantities.Count - 1) / 8, bulkPage + 1);
            RefreshBulkReview();
        });
        bulkSummary = UIView.Text(window, "BulkSummary", "");
        bulkCommit = UIView.Button(window, "ConfirmBulk", "确认", CommitBulk);
        UIView.Button(window, "CancelBulk", "取消 · 返回选择", CloseBulkReview);
        ResetOrganization();
    }

    void ResetOrganization()
    {
        EndBulkSelection();
        sortOrder = InventorySort.Quality;
        sortDescending = true;
        if (searchField != null)
            searchField.SetTextWithoutNotify("");
    }

    void EndBulkSelection()
    {
        bulkAction = BulkAction.None;
        bulkQuantities.Clear();
        CloseBulkReview();
    }

    public bool HandleBack()
    {
        if (equipmentEffectShade != null && equipmentEffectShade.gameObject.activeSelf)
        {
            CloseEquipmentEffectDetails();
            return true;
        }

        var selectedObject = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        var focused = selectedObject != null ? selectedObject.GetComponentInParent<TMP_InputField>() : null;
        if (focused != null && focused.isFocused)
        {
            focused.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            return true;
        }

        if (bulkShade.gameObject.activeSelf)
        {
            CloseBulkReview();
            return true;
        }

        if (discardShade.gameObject.activeSelf)
        {
            CloseDiscard();
            return true;
        }

        if (presetShade.gameObject.activeSelf)
        {
            presetShade.gameObject.SetActive(false);
            return true;
        }

        if (BulkSelecting)
        {
            EndBulkSelection();
            Show("已取消批量选择，物品未变化。");
            return true;
        }

        if (batchMode)
        {
            ToggleBatch();
            return true;
        }

        if (draft != null || TriedItem != null)
        {
            CancelTryOn();
            return true;
        }

        return false;
    }

    void BeginBulk(BulkAction action)
    {
        if (bulkAction == action)
        {
            EndBulkSelection();
            Show("已退出批量选择，物品未变化。");
            return;
        }

        EndBulkSelection();
        CloseDiscard();
        bulkAction = action;
        batchMode = false;
        batchKeys.Clear();
        batchQuote = null;
        draft = null;
        selected = confirmation = tryOnItemId = null;
        if (filter == 4)
            filter = 0;
        status.text = action == BulkAction.Sell ? "多选要出售的物品，核对清单后统一保存。默认整堆，确认窗可调整数量。" : "多选要丢弃的物品，核对后一起放在脚边。默认整堆，确认窗可调整数量；离开场景消失。";
        Refresh();
    }

    bool SelectBulkItem(string key)
    {
        selected = key;
        confirmation = tryOnItemId = null;
        if (bulkQuantities.Remove(key))
            status.text = "已从批量清单移除该物品。";
        else if (!progress.CanBatchSelect(key, bulkAction == BulkAction.Discard, out string reason))
            status.text = reason;
        else
        {
            bulkQuantities[key] = progress.FindInstance(key).count;
            status.text = $"已选择 {bulkQuantities.Count} 项，核对清单后才会处理；筛选后仍保留选择。";
        }

        Refresh();
        return true;
    }

    void RefreshOrganization()
    {
        if (searchField == null)
            return;
        string[] names =
        {
            "品质",
            "等级",
            "类型",
            "获得"
        };
        for (int i = 0; i < sortButtons.Length; i++)
        {
            sortButtons[i].image.color = (int)sortOrder == i ? W.Accent : W.Card;
            sortButtons[i].GetComponentInChildren<TMP_Text>().text = names[i] + ((int)sortOrder == i ? sortDescending ? " ↓" : " ↑" : "");
        }

        bulkSell.interactable = !batchMode && draft == null && progress.CanManageInventory && !player.CombatAllowed;
        bulkDiscard.interactable = !batchMode && draft == null && progress.CanManageInventory;
        bulkSell.GetComponentInChildren<TMP_Text>().text = bulkAction == BulkAction.Sell ? "退出出售" : "批量出售";
        bulkDiscard.GetComponentInChildren<TMP_Text>().text = bulkAction == BulkAction.Discard ? "退出丢弃" : "批量丢弃";
        bulkReview.gameObject.SetActive(BulkSelecting);
        bulkReview.GetComponentInChildren<TMP_Text>().text = $"核对清单 {bulkQuantities.Count} 项";
        bulkReview.interactable = bulkQuantities.Count > 0 && progress.CanManageInventory && progress.InventoryBatchQuote(BulkSelection, bulkAction == BulkAction.Discard) != null;
        if (BulkSelecting)
        {
            sell.interactable = dismantle.interactable = wear.interactable = secondary.interactable = remove.interactable = false;
            if (quickButtons != null)
                foreach (var button in quickButtons)
                    button.interactable = false;
        }

        if (bulkShade.gameObject.activeSelf && bulkExpectedQuote != progress.InventoryBatchQuote(BulkSelection, bulkAction == BulkAction.Discard))
        {
            CloseBulkReview();
            status.text = "物品状态或数量已变化，已取消旧确认；请重新选择。";
        }
    }

    void OpenBulkReview()
    {
        var quote = progress.InventoryBatchQuote(BulkSelection, bulkAction == BulkAction.Discard);
        if (!BulkSelecting || quote == null)
        {
            Show("请先选择可处理的背包物品。");
            return;
        }

        bulkExpectedQuote = quote;
        bulkPage = 0;
        confirmation = null;
        bulkShade.gameObject.SetActive(true);
        bulkShade.SetAsLastSibling();
        RefreshBulkReview();
    }

    void CloseBulkReview()
    {
        bulkExpectedQuote = null;
        if (bulkShade != null)
            bulkShade.gameObject.SetActive(false);
    }

    void ChangeBulkQuantity(int row, int delta)
    {
        var keys = BulkKeys;
        int index = bulkPage * 8 + row;
        if (index >= keys.Length)
            return;
        string key = keys[index];
        var entry = progress.FindInstance(key);
        if (entry == null || bulkExpectedQuote != progress.InventoryBatchQuote(BulkSelection, bulkAction == BulkAction.Discard))
        {
            CloseBulkReview();
            Show("物品已变化，请重新核对清单。");
            return;
        }

        bulkQuantities[key] = delta == int.MaxValue ? entry.count : Mathf.Clamp(bulkQuantities[key] + delta, 1, entry.count);
        bulkExpectedQuote = progress.InventoryBatchQuote(BulkSelection, bulkAction == BulkAction.Discard);
        RefreshBulkReview();
    }

    void RefreshBulkReview()
    {
        var keys = BulkKeys;
        bool dropping = bulkAction == BulkAction.Discard;
        bulkTitle.text = dropping ? "核 对 批 量 丢 弃" : "核 对 批 量 出 售";
        bulkHelp.text = dropping ? "以下物品会一起放到角色脚边，可手动拾回；离开场景或退出游戏后消失。\n已装备、两武器槽与锁定物品受保护；确认后一次保存，不获得金币。" : "已装备、两武器槽、锁定或镶嵌宝石的物品受保护。\n按清单一次保存；48 小时内可向铁匠原价回购，最多最近 24 项。";
        for (int i = 0; i < bulkNames.Length; i++)
        {
            int index = bulkPage * 8 + i;
            bool used = index < keys.Length;
            bulkNames[i].gameObject.SetActive(used);
            bulkAmounts[i].gameObject.SetActive(used);
            bulkLess[i].gameObject.SetActive(used);
            bulkMore[i].gameObject.SetActive(used);
            bulkAll[i].gameObject.SetActive(used);
            if (!used)
                continue;
            var entry = progress.FindInstance(keys[index]);
            var item = progress.FindItem(keys[index]);
            int amount = bulkQuantities[keys[index]];
            bulkNames[i].text = item.DisplayName + (entry.upgradeLevel > 0 ? $" +{entry.upgradeLevel}" : "") + $" · {item.RarityLabel}" + (entry.gemId.Length > 0 ? " · 含宝石" : "") + (dropping ? "" : $" · {item.SellPrice} 金币/件");
            bulkNames[i].color = item.QualityColor;
            bulkAmounts[i].text = "×" + amount;
            bulkLess[i].interactable = amount > 1;
            bulkMore[i].interactable = amount < entry.count;
            bulkAll[i].interactable = amount != entry.count;
            bulkAll[i].GetComponentInChildren<TMP_Text>().text = $"全部 ×{entry.count}";
        }

        bulkPageLabel.text = $"第 {bulkPage + 1} / {Mathf.Max(1, (keys.Length + 7) / 8)} 页";
        bulkSummary.text = $"共 {keys.Length} 项 · {bulkQuantities.Values.Sum()} 件\n" + (dropping ? "金币与经验不变" : $"获得 {progress.InventorySaleTotal(BulkSelection)} 金币");
        bulkCommit.GetComponentInChildren<TMP_Text>().text = dropping ? "确认全部丢弃" : "确认出售清单";
        bulkCommit.interactable = progress.CanManageInventory && bulkExpectedQuote != null && bulkExpectedQuote == progress.InventoryBatchQuote(BulkSelection, dropping) && (dropping || !player.CombatAllowed);
    }

    void CommitBulk()
    {
        var choices = BulkSelection;
        string quote = bulkExpectedQuote, message;
        bool dropping = bulkAction == BulkAction.Discard;
        CloseBulkReview();
        bool success = dropping ? progress.TryDiscardItems(choices, quote, out message) : progress.TrySellItems(choices, quote, out message);
        if (success)
        {
            bulkQuantities.Clear();
            selected = tryOnItemId = confirmation = null;
        }

        Show(message);
    }
}
