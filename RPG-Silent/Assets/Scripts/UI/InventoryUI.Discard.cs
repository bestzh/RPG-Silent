using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class InventoryUI
{
    Button discard, discardConfirm, discardLess, discardMore, discardAll;
    RectTransform discardShade;
    TMP_Text discardTitle, discardDetails, discardHint;
    TMP_InputField discardQuantity;
    string discardKey, discardQuote;
    int discardMaximum;

    void BuildDiscard()
    {
        discard = W.Button(panel, Font, "Discard", "丢弃物品", 1200, 704, 452, 38, OpenDiscard);
        discard.image.color = W.Card;
        setText.rectTransform.anchoredPosition = new Vector2(1200, -752);
        setText.rectTransform.sizeDelta = new Vector2(452, 82); setText.fontSize = 16;
        discardShade = W.Panel(panel, "DiscardShade", new Color(0, 0, 0, .92f));
        var window = W.CenterPanel(discardShade, "DiscardWindow", 740, 460);
        W.Text(window, Font, "Heading", "丢 弃 物 品", 28, 22, 684, 48, 30);
        discardTitle = W.Text(window, Font, "DiscardItem", "", 28, 86, 684, 58, 26);
        discardDetails = W.Text(window, Font, "DiscardDetails", "", 28, 150, 684, 92, 21);
        discardLess = W.Button(window, Font, "DiscardLess", "−", 28, 258, 64, 44, () => SetDiscardQuantity(ReadDiscardQuantity() - 1));
        var inputRect = W.Rect(window, "DiscardQuantity", 104, 258, 220, 44);
        inputRect.gameObject.AddComponent<Image>().color = W.Card;
        discardQuantity = inputRect.gameObject.AddComponent<TMP_InputField>();
        var viewport = W.Rect(inputRect, "Viewport", 10, 4, 200, 36); viewport.gameObject.AddComponent<RectMask2D>();
        var text = W.Text(viewport, Font, "Value", "1", 0, 0, 200, 36, 24);
        text.alignment = TextAlignmentOptions.Center;
        discardQuantity.textViewport = viewport;
        discardQuantity.textComponent = (TextMeshProUGUI)text;
        discardQuantity.contentType = TMP_InputField.ContentType.IntegerNumber;
        discardQuantity.characterLimit = 4;
        discardQuantity.onValueChanged.AddListener(_ => RefreshDiscardQuantity());
        discardMore = W.Button(window, Font, "DiscardMore", "+", 336, 258, 64, 44, () => SetDiscardQuantity(ReadDiscardQuantity() + 1));
        discardAll = W.Button(window, Font, "DiscardAll", "全部", 412, 258, 140, 44, () => SetDiscardQuantity(discardMaximum));
        discardHint = W.Text(window, Font, "DiscardHint", "", 28, 316, 684, 42, 20);
        discardConfirm = W.Button(window, Font, "ConfirmDiscard", "确认丢弃", 28, 382, 332, 48, ConfirmDiscard);
        W.Button(window, Font, "CancelDiscard", "取消", 380, 382, 332, 48, CloseDiscard);
        CloseDiscard();
    }

    void OpenDiscard()
    {
        if (batchMode || draft != null || filter == 4 || !progress.CanManageInventory) return;
        if (!progress.CanDiscardItem(selected, out string reason)) { Show(reason); return; }
        var entry = progress.FindInstance(selected); var item = progress.FindItem(selected);
        discardKey = selected; discardQuote = progress.DiscardQuote(selected); discardMaximum = entry.count;
        confirmation = null;
        discardTitle.text = item.DisplayName + (entry.upgradeLevel > 0 ? $" +{entry.upgradeLevel}" : "") + $" · 持有 {entry.count} 件";
        discardTitle.color = item.QualityColor;
        discardDetails.text = "物品会放到角色脚边，关闭背包后可手动拾回。\n离开场景或退出游戏后，地面物品会消失。" +
            (entry.IsInstance ? "\n装备的词条、强化与镶嵌宝石会一起保留。" : "\n输入要丢弃的数量，或选择全部。");
        discardShade.gameObject.SetActive(true); discardShade.SetAsLastSibling();
        SetDiscardQuantity(1);
    }

    int ReadDiscardQuantity() => int.TryParse(discardQuantity.text, out int value) ? value : 0;
    void SetDiscardQuantity(int amount)
    {
        discardQuantity.SetTextWithoutNotify(Mathf.Clamp(amount, 1, Mathf.Max(1, discardMaximum)).ToString());
        RefreshDiscardQuantity();
    }
    void RefreshDiscardQuantity()
    {
        int amount = ReadDiscardQuantity();
        bool valid = amount >= 1 && amount <= discardMaximum;
        discardConfirm.interactable = valid && progress.CanManageInventory;
        discardLess.interactable = amount > 1; discardMore.interactable = amount < discardMaximum;
        discardAll.interactable = discardMaximum > 1;
        discardQuantity.interactable = discardMaximum > 1;
        discardHint.text = valid ? $"将丢弃 {amount} / {discardMaximum} 件；不会获得金币或材料。" : $"请输入 1 至 {discardMaximum} 之间的数量。";
    }
    void CloseDiscard()
    {
        discardKey = discardQuote = null;
        if (discardShade != null) discardShade.gameObject.SetActive(false);
    }
    void ConfirmDiscard()
    {
        int count = ReadDiscardQuantity();
        if (count < 1 || count > discardMaximum) { RefreshDiscardQuantity(); return; }
        string key = discardKey, quote = discardQuote;
        CloseDiscard();
        progress.TryDiscardItem(key, count, quote, out string message);
        Show(message);
    }
    void RefreshDiscard()
    {
        if (discard == null) return;
        bool allowed = progress.CanDiscardItem(selected, out string reason);
        discard.interactable = allowed && filter != 4 && !batchMode && draft == null && progress.CanManageInventory;
        discard.GetComponentInChildren<TMP_Text>().text = batchMode ? "退出批量分解后可丢弃" : draft != null ? "退出整套试穿后可丢弃" :
            filter == 4 ? "待领取物品请先领取" : allowed || selected == null ? "丢弃物品" : reason;
        if (discardShade.gameObject.activeSelf && discardQuote != progress.DiscardQuote(discardKey))
        { CloseDiscard(); status.text = "物品状态已变化，已取消丢弃；请重新选择。"; }
    }
}
