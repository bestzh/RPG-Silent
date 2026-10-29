using System;
using System.Collections.Generic;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    Button warehouseEntry, warehouseReview, warehouseConfirm;
    RectTransform warehouseShade, warehouseWindow, warehouseReviewShade;
    TMP_Text warehouseHeading, warehouseDetails, warehouseStatus, warehousePages, warehouseReviewText;
    TMP_InputField warehouseSearch, warehouseQuantity;
    readonly Button[] warehouseCells = new Button[24];
    readonly Dictionary<string, int> warehouseSelection = new();
    ItemStackData[] warehouseVisible = Array.Empty<ItemStackData>();
    bool warehouseDeposit = true;
    int warehousePage, warehouseCategory, warehouseReviewPage;
    string warehouseSelected, warehouseExpected;
    InventorySelectionData[] WarehouseSelection => warehouseSelection.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new InventorySelectionData { key = x.Key, count = x.Value }).ToArray();

    void BuildWarehouse()
    {
        warehouseEntry = UIView.Button(panel, "WarehouseEntry", "角色仓库", () =>
        {
            if (!Validate(TownNpcRole.Steward))
                return;
            warehouseDeposit = true;
            warehousePage = warehouseCategory = 0;
            warehouseSelection.Clear();
            warehouseSelected = null;
            warehouseSearch.SetTextWithoutNotify("");
            warehouseStatus.text = "选择物品加入存取清单；锁定状态、词条、强化和宝石全部保留。";
            warehouseShade.gameObject.SetActive(true);
            warehouseShade.SetAsLastSibling();
            RefreshWarehouse();
        });
        warehouseShade = UIView.Panel(transform, "WarehouseShade");
        warehouseWindow = UIView.Panel(warehouseShade, "WarehouseWindow");
        warehouseHeading = UIView.Text(warehouseWindow, "WarehouseHeading", "");
        UIView.Button(warehouseWindow, "CloseWarehouse", "返回交谈", CloseWarehouse);
        UIView.Button(warehouseWindow, "DepositMode", "背包 → 仓库", () => SelectWarehouseMode(true));
        UIView.Button(warehouseWindow, "WithdrawMode", "仓库 → 背包", () => SelectWarehouseMode(false));
        UIView.Button(warehouseWindow, "WarehouseCategory", "切换分类", () =>
        {
            warehouseCategory = (warehouseCategory + 1) % 6;
            warehousePage = 0;
            RefreshWarehouse();
        });
        warehouseSearch = WarehouseInput(warehouseWindow, "WarehouseSearch", 852, 84, 360, 42, "搜索名称", false);
        warehouseSearch.onValueChanged.AddListener(_ =>
        {
            warehousePage = 0;
            RefreshWarehouse();
        });
        for (int i = 0; i < warehouseCells.Length; i++)
        {
            int index = i;
            warehouseCells[i] = UIView.Button(warehouseWindow, "WarehouseItem" + i, "", () =>
            {
                int at = warehousePage * 24 + index;
                if (at >= warehouseVisible.Length)
                    return;
                var entry = warehouseVisible[at];
                warehouseSelected = entry.Key;
                if (warehouseSelection.Remove(entry.Key))
                    warehouseStatus.text = "已从清单移除该物品。";
                else if (!progress.CanWarehouseSelect(entry.Key, warehouseDeposit, out string reason))
                    warehouseStatus.text = reason;
                else
                {
                    warehouseSelection[entry.Key] = entry.count;
                    warehouseStatus.text = "已加入清单，默认为整堆；右侧可输入本项数量。";
                }

                warehouseQuantity.SetTextWithoutNotify(warehouseSelection.TryGetValue(entry.Key, out int n) ? n.ToString() : "1");
                RefreshWarehouse();
            });
        }

        UIView.Button(warehouseWindow, "WarehousePrevious", "上一页", () =>
        {
            warehousePage = Mathf.Max(0, warehousePage - 1);
            RefreshWarehouse();
        });
        warehousePages = UIView.Text(warehouseWindow, "WarehousePages", "");
        UIView.Button(warehouseWindow, "WarehouseNext", "下一页", () =>
        {
            warehousePage = Mathf.Min(Mathf.Max(0, (warehouseVisible.Length - 1) / 24), warehousePage + 1);
            RefreshWarehouse();
        });
        warehouseDetails = UIView.Text(warehouseWindow, "WarehouseDetails", "");
        warehouseQuantity = WarehouseInput(warehouseWindow, "WarehouseQuantity", 680, 428, 250, 44, "数量", true);
        UIView.Button(warehouseWindow, "ApplyWarehouseQuantity", "设置本项数量", () =>
        {
            var entry = progress.FindInstance(warehouseSelected);
            if (entry == null || !warehouseSelection.ContainsKey(warehouseSelected) || !int.TryParse(warehouseQuantity.text, out int n) || n < 1 || n > entry.count)
            {
                warehouseStatus.text = "请选择清单中的物品，并输入有效数量。";
                return;
            }

            warehouseSelection[warehouseSelected] = n;
            warehouseStatus.text = "已更新本项数量；请核对全部清单后确认。";
            RefreshWarehouse();
        });
        UIView.Text(warehouseWindow, "WarehouseRules", "角色独立仓库，不与其他角色共享。\n已装备物品与配装方案引用的装备不能存入。\n锁定装备可以存取，锁定状态保持。\n待领取物品需先领取到背包。\n容量不足或保存失败时，整批保持原位。");
        UIView.Button(warehouseWindow, "ClearWarehouseSelection", "清空选择", () =>
        {
            warehouseSelection.Clear();
            warehouseSelected = warehouseExpected = null;
            warehouseStatus.text = "已清空选择，物品未变化。";
            RefreshWarehouse();
        });
        warehouseReview = UIView.Button(warehouseWindow, "ReviewWarehouse", "核对存取清单", OpenWarehouseReview);
        warehouseStatus = UIView.Text(warehouseWindow, "WarehouseStatus", "");
        warehouseReviewShade = UIView.Panel(warehouseShade, "WarehouseReviewShade");
        var confirmWindow = UIView.Panel(warehouseReviewShade, "WarehouseConfirmWindow");
        UIView.Text(confirmWindow, "WarehouseConfirmTitle", "核 对 存 取 清 单");
        warehouseReviewText = UIView.Text(confirmWindow, "WarehouseReviewText", "");
        UIView.Button(confirmWindow, "WarehouseReviewPrevious", "上一页", () =>
        {
            warehouseReviewPage = Mathf.Max(0, warehouseReviewPage - 1);
            RefreshWarehouseReview();
        });
        UIView.Button(confirmWindow, "WarehouseReviewNext", "下一页", () =>
        {
            warehouseReviewPage = Mathf.Min(Mathf.Max(0, (warehouseSelection.Count - 1) / 8), warehouseReviewPage + 1);
            RefreshWarehouseReview();
        });
        warehouseConfirm = UIView.Button(confirmWindow, "ConfirmWarehouse", "确认整批存取", CommitWarehouse);
        UIView.Button(confirmWindow, "CancelWarehouse", "取消 · 返回选择", () =>
        {
            warehouseReviewShade.gameObject.SetActive(false);
            warehouseExpected = null;
        });
        CloseWarehouse();
    }

    TMP_InputField WarehouseInput(Transform parent, string name, float x, float y, float width, float height, string placeholderText, bool numeric)
    {
        var rect = UIView.Rect(parent, name);
        rect.gameObject.GetComponent<Image>().color = W.Card;
        var input = rect.gameObject.GetComponent<TMP_InputField>();
        var viewport = UIView.Rect(rect, "Viewport");
        var text = UIView.Text(viewport, "Value", "");
        var placeholder = UIView.Text(viewport, "Placeholder", placeholderText);
        placeholder.color = Color.gray;
        input.textViewport = viewport;
        input.textComponent = (TextMeshProUGUI)text;
        input.placeholder = placeholder;
        input.richText = false;
        input.characterLimit = numeric ? 4 : 64;
        if (numeric)
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
        return input;
    }

    void SelectWarehouseMode(bool deposit)
    {
        warehouseDeposit = deposit;
        warehouseSelection.Clear();
        warehouseSelected = null;
        warehouseExpected = null;
        warehousePage = 0;
        warehouseQuantity.SetTextWithoutNotify("");
        warehouseStatus.text = "已切换方向，原清单已取消，物品未变化。";
        RefreshWarehouse();
    }

    void CloseWarehouse()
    {
        warehouseSelection.Clear();
        warehouseSelected = warehouseExpected = null;
        if (warehouseShade != null)
            warehouseShade.gameObject.SetActive(false);
        if (warehouseReviewShade != null)
            warehouseReviewShade.gameObject.SetActive(false);
    }

    public bool HandleServiceBack()
    {
        if (serviceConfirmation != null && serviceConfirmation.IsOpen)
        {
            serviceConfirmation.Dismiss();
            return true;
        }

        if (specializationShade != null && specializationShade.gameObject.activeSelf)
        {
            CloseSpecialization();
            return true;
        }

        if (commerceShade != null && commerceShade.gameObject.activeSelf)
        {
            CloseCommerce();
            return true;
        }

        if (warehouseShade == null || !warehouseShade.gameObject.activeSelf)
            return HandleWorldBack();
        var selectedObject = UnityEngine.EventSystems.EventSystem.current != null ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject : null;
        var field = selectedObject != null ? selectedObject.GetComponentInParent<TMP_InputField>() : null;
        if (field != null && field.isFocused)
        {
            field.DeactivateInputField();
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            return true;
        }

        if (warehouseReviewShade.gameObject.activeSelf)
        {
            warehouseReviewShade.gameObject.SetActive(false);
            warehouseExpected = null;
            return true;
        }

        CloseWarehouse();
        return true;
    }

    void RefreshWarehouseEntry()
    {
        if (warehouseEntry == null)
            return;
        warehouseEntry.gameObject.SetActive(interaction.Conversation.Role == TownNpcRole.Steward);
        if (warehouseShade.gameObject.activeSelf)
            RefreshWarehouse();
    }

    void RefreshWarehouse()
    {
        if (warehouseShade == null || !warehouseShade.gameObject.activeSelf)
            return;
        var rootSize = Vector2.zero;
        InventoryCategory[] categories =
        {
            InventoryCategory.All,
            InventoryCategory.Equipment,
            InventoryCategory.Weapon,
            InventoryCategory.Material,
            InventoryCategory.Consumable,
            InventoryCategory.Gem
        };
        warehouseVisible = warehouseDeposit ? progress.QueryInventory(categories[warehouseCategory], warehouseSearch.text, InventorySort.Quality, true) : progress.QueryWarehouse(categories[warehouseCategory], warehouseSearch.text, InventorySort.Quality, true);
        warehousePage = Mathf.Clamp(warehousePage, 0, Mathf.Max(0, (warehouseVisible.Length - 1) / 24));
        warehouseHeading.text = $"角色仓库 {progress.Warehouse.items.Length}/{WarehouseData.Capacity} · 背包 {progress.Inventory.bag.Length}/{InventoryData.Capacity}";
        UIView.Find(warehouseWindow, "WarehouseCategory").GetComponentInChildren<TMP_Text>().text = new[]
        {
            "全部物品",
            "防具饰品",
            "武器",
            "材料",
            "药品",
            "宝石"
        }[warehouseCategory] + " >";
        UIView.Find(warehouseWindow, "DepositMode").GetComponent<Image>().color = warehouseDeposit ? W.Accent : W.Card;
        UIView.Find(warehouseWindow, "WithdrawMode").GetComponent<Image>().color = warehouseDeposit ? W.Card : W.Accent;
        for (int i = 0; i < warehouseCells.Length; i++)
        {
            int at = warehousePage * 24 + i;
            bool used = at < warehouseVisible.Length;
            warehouseCells[i].interactable = used;
            var label = warehouseCells[i].GetComponentInChildren<TMP_Text>();
            if (!used)
            {
                label.text = "";
                warehouseCells[i].image.color = W.Card;
                continue;
            }

            var entry = warehouseVisible[at];
            var item = progress.FindItem(entry.itemId);
            label.text = item.DisplayName + $"\n{item.RarityLabel}" + (entry.IsInstance ? $" +{entry.upgradeLevel}" : $" ×{entry.count}") + (entry.isLocked ? "\n已锁定" : "") + (warehouseSelection.ContainsKey(entry.Key) ? "\n已选择" : "");
            label.color = item.QualityColor;
            warehouseCells[i].image.color = warehouseSelection.ContainsKey(entry.Key) ? W.Accent : W.Card;
        }

        RefreshV5WarehouseItems();
        var selectedEntry = progress.FindInstance(warehouseSelected);
        var selectedItem = progress.FindItem(warehouseSelected);
        warehouseDetails.text = selectedEntry == null || selectedItem == null ? "选择物品可查看属性并加入清单。\n可以跨页、分类和搜索批量选择。" : $"{selectedItem.DisplayName} · {selectedItem.RarityLabel}\n{selectedItem.SlotLabel} · 数量 {selectedEntry.count} · 强化 +{selectedEntry.upgradeLevel}\n" + $"宝石 {progress.FindItem(selectedEntry.gemId)?.DisplayName ?? "无"} · {(selectedEntry.isLocked ? "锁定保留" : "未锁定")}\n" + string.Join("\n", selectedEntry.affixes.Select(x => new[] { "生命", "能量", "伤害", "减伤" }[(int)x.stat] + " +" + x.value));
        warehousePages.text = $"第 {warehousePage + 1} / {Mathf.Max(1, (warehouseVisible.Length + 23) / 24)} 页";
        warehouseReview.GetComponentInChildren<TMP_Text>().text = $"核对{(warehouseDeposit ? "存入" : "取出")}清单 · {warehouseSelection.Count} 项";
        warehouseReview.interactable = progress.WarehouseQuote(WarehouseSelection, warehouseDeposit) != null;
        if (warehouseReviewShade.gameObject.activeSelf && warehouseExpected != progress.WarehouseQuote(WarehouseSelection, warehouseDeposit))
        {
            warehouseReviewShade.gameObject.SetActive(false);
            warehouseExpected = null;
            warehouseStatus.text = "物品或仓库记录已变化，请重新选择并核对。";
        }
    }

    void OpenWarehouseReview()
    {
        warehouseExpected = progress.WarehouseQuote(WarehouseSelection, warehouseDeposit);
        if (warehouseExpected == null)
        {
            warehouseStatus.text = "清单无效，请重新选择物品。";
            return;
        }

        warehouseReviewPage = 0;
        warehouseReviewShade.gameObject.SetActive(true);
        warehouseReviewShade.SetAsLastSibling();
        RefreshWarehouseReview();
    }

    void RefreshWarehouseReview()
    {
        var all = WarehouseSelection;
        warehouseReviewText.text = (warehouseDeposit ? "将以下物品存入本角色仓库：\n\n" : "将以下物品取回本角色背包：\n\n") + string.Join("\n", all.Skip(warehouseReviewPage * 8).Take(8).Select(x => progress.FindItem(x.key).DisplayName + (progress.FindInstance(x.key).upgradeLevel > 0 ? " +" + progress.FindInstance(x.key).upgradeLevel : "") + $" ×{x.count}")) + $"\n\n第 {warehouseReviewPage + 1} / {Mathf.Max(1, (all.Length + 7) / 8)} 页 · 共 {all.Length} 项／{all.Sum(x => x.count)} 件\n容量不足、确认失效或保存失败时，不会部分存取。";
        warehouseConfirm.interactable = warehouseExpected != null && warehouseExpected == progress.WarehouseQuote(all, warehouseDeposit);
    }

    void CommitWarehouse()
    {
        var selection = WarehouseSelection;
        string quote = warehouseExpected;
        warehouseReviewShade.gameObject.SetActive(false);
        warehouseExpected = null;
        bool success = progress.TryWarehouseTransfer(selection, warehouseDeposit, quote, interaction?.Conversation, out string message);
        if (success)
        {
            warehouseSelection.Clear();
            warehouseSelected = null;
        }

        warehouseStatus.text = message;
        RefreshWarehouse();
    }
}
