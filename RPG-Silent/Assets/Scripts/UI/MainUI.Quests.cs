using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    RectTransform questTracker;
    TMP_Text questText;
    Button journalButton;
    void BuildQuestHud()
    {
        if (journalButton == null)
        {
            journalButton = W.Button(transform.Find("TopLeftPanel"), classNameText.font, "JournalButton", "任务 [J]", 0, 0, 156, 44,
                () => _session?.PlayerRig?.GetComponentInChildren<PlayerQuestInteraction>(true)?.Toggle());
            var r = journalButton.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = Vector2.one; r.anchoredPosition = new Vector2(-196, -330);
            questTracker = W.Rect(transform.Find("TopLeftPanel"), "QuestTracker", 0, 0, 390, 206);
            questTracker.anchorMin = questTracker.anchorMax = questTracker.pivot = Vector2.one; questTracker.anchoredPosition = new Vector2(-24, -404);
            questTracker.gameObject.AddComponent<Image>().color = new Color(.04f, .07f, .1f, .8f);
            questTracker.GetComponent<Image>().raycastTarget = false;
            questText = W.Text(questTracker, classNameText.font, "TrackedQuest", "", 16, 12, 358, 182, 20);
        }
        RefreshQuestHud();
    }
    void RefreshQuestHud()
    {
        if (questText == null || _progress == null) return;
        var q = _progress.TrackedQuest;
        questText.text = q == null ? "任务追踪 [J]\n前往主城职业导师或铁匠接取委托。" :
            $"{q.CategoryLabel} · {q.Title}\n{_progress.QuestObjectivesText(q.Id)}\n" +
            (_progress.GetQuestStatus(q.Id) == QuestStatus.Ready ? "可提交 · 返回" + _progress.QuestGiverText(q.Id) : "完成后返回" + _progress.QuestGiverText(q.Id));
    }
}
