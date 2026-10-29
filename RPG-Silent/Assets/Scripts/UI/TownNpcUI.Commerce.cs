using System;
using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    bool shopBuybackPage, commerceBuyingBack;
    Button shopStockTab, shopBuybackTab, commerceConfirm;
    RectTransform commerceShade, commerceWindow;
    TMP_InputField commerceQuantity;
    TMP_Text commerceTitle, commerceDetails, commerceTotals, commerceRetention;
    string commerceItemId, commerceRecordId, commerceExpected, commerceState;
    float nextCommerceTick;
    string visibleBuybackIds;
    void Update()
    {
        if (Time.unscaledTime < nextCommerceTick)
            return;
        nextCommerceTick = Time.unscaledTime + .5f;
        if (warehouseShade != null && warehouseShade.gameObject.activeSelf)
            RefreshWarehouse();
        if (commerceShade != null && commerceShade.gameObject.activeSelf)
            RefreshCommercePreview();
        if (specializationShade != null && specializationShade.gameObject.activeSelf && interaction?.Conversation != null)
            RefreshSpecialization();
        if (supplyPage && shopBuybackPage && interaction?.Conversation != null)
        {
            string ids = string.Join("|", progress.BuybackItems.Select(x => x.id));
            if (ids != visibleBuybackIds)
            {
                visibleBuybackIds = ids;
                Refresh();
            }
        }
    }

    void BuildCommerce()
    {
        shopStockTab = UIView.Button(panel, "ShopStockTab", "购买药品 / 材料", () =>
        {
            shopBuybackPage = false;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
        shopBuybackTab = UIView.Button(panel, "ShopBuybackTab", "误售回购", () =>
        {
            shopBuybackPage = true;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
        commerceRetention = UIView.Text(panel, "BuybackRetention", "原价回购 · 保留 48 小时／最近 24 项");
        commerceShade = UIView.Panel(transform, "CommerceShade");
        commerceWindow = UIView.Panel(commerceShade, "CommerceWindow");
        commerceTitle = UIView.Text(commerceWindow, "CommerceTitle", "");
        commerceDetails = UIView.Text(commerceWindow, "CommerceDetails", "");
        commerceQuantity = WarehouseInput(commerceWindow, "CommerceQuantity", 28, 346, 290, 44, "数量 1–99", true);
        commerceQuantity.onValueChanged.AddListener(_ => RefreshCommercePreview());
        UIView.Button(commerceWindow, "CommerceLess", "−1", () => SetCommerceQuantity(CommerceCount - 1));
        UIView.Button(commerceWindow, "CommerceMore", "+1", () => SetCommerceQuantity(CommerceCount + 1));
        UIView.Button(commerceWindow, "CommerceFive", "数量 5", () => SetCommerceQuantity(5));
        commerceTotals = UIView.Text(commerceWindow, "CommerceTotals", "");
        commerceConfirm = UIView.Button(commerceWindow, "ConfirmCommerce", "确认交易", ConfirmCommerce);
        UIView.Button(commerceWindow, "CancelCommerce", "取消", CloseCommerce);
        CloseCommerce();
    }

    int CommerceCount => int.TryParse(commerceQuantity.text, out int count) ? count : 0;

    void SetCommerceQuantity(int count)
    {
        if (commerceBuyingBack)
            return;
        commerceQuantity.text = Mathf.Clamp(count, 1, 99).ToString();
    }

    void OpenPurchase(string id, int count)
    {
        commerceBuyingBack = false;
        commerceItemId = id;
        commerceRecordId = null;
        commerceState = progress.CommerceStateKey;
        commerceQuantity.SetTextWithoutNotify(count.ToString());
        commerceShade.gameObject.SetActive(true);
        commerceShade.SetAsLastSibling();
        RefreshCommercePreview();
    }

    void OpenBuyback(BuybackRecordData record)
    {
        commerceBuyingBack = true;
        commerceItemId = record.item.itemId;
        commerceRecordId = record.id;
        commerceState = progress.CommerceStateKey;
        commerceQuantity.SetTextWithoutNotify(record.item.count.ToString());
        commerceShade.gameObject.SetActive(true);
        commerceShade.SetAsLastSibling();
        RefreshCommercePreview();
    }

    void CloseCommerce()
    {
        commerceItemId = commerceRecordId = commerceExpected = commerceState = null;
        if (commerceShade != null)
            commerceShade.gameObject.SetActive(false);
    }

    void RefreshCommerce()
    {
        if (shopStockTab == null)
            return;
        bool show = supplyPage && interaction.Conversation.Role == TownNpcRole.Blacksmith;
        shopStockTab.gameObject.SetActive(show);
        shopBuybackTab.gameObject.SetActive(show);
        commerceRetention.gameObject.SetActive(show);
        shopStockTab.image.color = shopBuybackPage ? W.Card : W.Accent;
        shopBuybackTab.image.color = shopBuybackPage ? W.Accent : W.Card;
        shopBuybackTab.GetComponentInChildren<TMP_Text>().text = $"误售回购 · {progress.BuybackItems.Length} 项";
        if (commerceShade.gameObject.activeSelf)
        {
            if (commerceState != progress.CommerceStateKey)
            {
                CloseCommerce();
                status.text = "物品、金币或交易记录已变化，已取消旧确认；请重新核对。";
            }
            else
                RefreshCommercePreview();
        }
    }

    void RefreshCommercePreview()
    {
        if (commerceShade == null || !commerceShade.gameObject.activeSelf)
            return;
        if (commerceState != progress.CommerceStateKey)
        {
            CloseCommerce();
            status.text = "交易状态已变化，请重新打开确认。";
            return;
        }

        var item = progress.FindItem(commerceItemId);
        int count = CommerceCount;
        var record = commerceBuyingBack ? progress.BuybackItems.FirstOrDefault(x => x.id == commerceRecordId) : null;
        long cost = commerceBuyingBack ? record?.price ?? 0 : (long)progress.ShopUnitPrice(commerceItemId) * count;
        commerceTitle.text = commerceBuyingBack ? "核 对 原 价 回 购" : "核 对 购 买 清 单";
        commerceDetails.text = item == null ? "商品不可用，请重新选择。" : $"{item.DisplayName} · {item.RarityLabel}\n{item.Description}\n" + (commerceBuyingBack ? $"原出售数量 {record?.item.count ?? 0}；本次完整取回该条记录。\n原实例、强化、词条和宝石保留。" + (record != null && record.item.IsInstance ? $"\n强化 +{record.item.upgradeLevel} · 词条 {record.item.affixes.Length} · 宝石 {progress.FindItem(record.item.gemId)?.DisplayName ?? "无"}" : "") : progress.ShopRequirement(commerceItemId));
        commerceQuantity.interactable = !commerceBuyingBack;
        foreach (string name in new[]
        {
            "CommerceLess",
            "CommerceMore",
            "CommerceFive"
        }

        )
            UIView.Find(commerceWindow, name).GetComponent<Button>().interactable = !commerceBuyingBack;
        commerceExpected = commerceBuyingBack ? progress.BuybackQuote(commerceRecordId) : progress.ShopQuote(commerceItemId, count);
        commerceTotals.text = $"数量 {count} · 总价 {Math.Max(0, cost)} 金币 · 持有 {progress.Gold} 金币\n" + (commerceBuyingBack ? progress.BuybackCapacityResult(commerceRecordId) : progress.ShopCapacityResult(commerceItemId, count)) + "\n取消不扣款；写盘失败或重复提交不会多扣金币或多发物品。";
        commerceConfirm.interactable = commerceExpected != null && cost >= 0 && cost <= progress.Gold && (!commerceBuyingBack || progress.BuybackFits(commerceRecordId));
    }

    void ConfirmCommerce()
    {
        string id = commerceItemId, record = commerceRecordId, quote = commerceExpected, message;
        int count = CommerceCount;
        bool buyback = commerceBuyingBack;
        CloseCommerce();
        if (buyback)
            progress.TryBuyback(record, quote, interaction?.Conversation, out message);
        else
            progress.TryShopPurchase(id, count, quote, interaction?.Conversation, out message);
        status.text = message;
        Refresh();
    }

    float AddBuybackRows(float y)
    {
        details.text = $"本角色回购 · 金币 {progress.Gold}\n保留 48 小时 / 最近 24 项，支付原出售所得取回整条原记录。";
        var records = progress.BuybackItems;
        if (records.Length == 0)
        {
            AddRow("暂无可回购物品", "背包中出售后，可在这里取回。");
            return 0;
        }

        foreach (var record in records)
        {
            var copy = record;
            var item = progress.FindItem(record.item.itemId);
            double hours = Math.Max(0, 48 - Math.Max(0, DateTime.UtcNow.Ticks - record.soldUtcTicks) / (double)TimeSpan.TicksPerHour);
            var row = AddRow($"{item.DisplayName} ×{record.item.count}" + (record.item.IsInstance ? $" +{record.item.upgradeLevel}" : ""), $"{item.RarityLabel} · 原价 {record.price} 金币 · 剩余 {hours:0.0} 小时\n保留原强化、词条与宝石；需有足够背包空间。", item.Icon);
            row.Title.color = item.QualityColor;
            row.Action(0, "核对并回购", () =>
            {
                if (Validate(TownNpcRole.Blacksmith))
                    OpenBuyback(copy);
            }, progress.Gold >= record.price && progress.CanManageInventory);
        }

        return 0;
    }
}
