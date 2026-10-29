using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class QuestJournalUI
{
    Button abandonQuest;
    RectTransform abandonQuestShade;
    TMP_Text abandonQuestText;
    string abandonId, abandonKey;
    void BuildChapterUI()
    {
        abandonQuest = UIView.Button(panel, "AbandonQuest", "放弃任务…", () =>
        {
            abandonId = selected;
            abandonKey = progress.QuestAbandonKey(selected);
            if (abandonKey == null)
                return;
            abandonQuestText.text = "放弃「" + progress.FindQuest(selected).Title + "」？\n\n" + progress.QuestRulesText(selected);
            abandonQuestShade.gameObject.SetActive(true);
            abandonQuestShade.SetAsLastSibling();
        });
        abandonQuestShade = UIView.Panel(transform, "AbandonQuestShade");
        var window = UIView.Panel(abandonQuestShade, "AbandonQuestWindow");
        abandonQuestText = UIView.Text(window, "AbandonQuestConsequences", "");
        UIView.Button(window, "ConfirmAbandonQuest", "确认放弃并清空目标", () =>
        {
            progress.TryAbandonQuest(abandonId, abandonKey, out string message);
            CloseAbandonQuest();
            status.text = message;
            Refresh();
        });
        UIView.Button(window, "CancelAbandonQuest", "保留任务", CloseAbandonQuest);
        abandonQuestShade.gameObject.SetActive(false);
    }

    void CloseAbandonQuest()
    {
        abandonQuestShade?.gameObject.SetActive(false);
        abandonId = abandonKey = null;
    }

    public bool HandleBack()
    {
        if (abandonQuestShade == null || !abandonQuestShade.gameObject.activeSelf)
            return false;
        CloseAbandonQuest();
        return true;
    }

    void RefreshChapterRows()
    {
        dataRows.Add("主线章节", "章节按成长顺序串联主线；旧档识别已交付记录。已有后续进度无需补做迎新任务；分支委托不会封锁共同主线。");
        foreach (var q in progress.Quests.Where(q => q.Category == QuestCategory.Main))
        {
            string id = q.Id;
            dataRows.Add(q.Chapter + $" · Lv.{q.RequiredLevel} · {StatusLabel(progress.GetQuestStatus(id))}", q.Title + "\n" + q.Description).Action(0, "查看任务与接取条件", () =>
            {
                journalPage = filter = category = 0;
                selected = id;
                rows.anchoredPosition = Vector2.zero;
                Refresh();
            });
        }
    }
}
