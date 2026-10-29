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
            journalButton = UIView.Button(UIView.Find(transform, "TopLeftPanel"), "JournalButton", "任务 [J]", () => _session?.PlayerRig?.GetComponentInChildren<PlayerQuestInteraction>(true)?.Toggle());
            var r = journalButton.GetComponent<RectTransform>();
            questTracker = UIView.Rect(UIView.Find(transform, "TopLeftPanel"), "QuestTracker");
            questTracker.gameObject.GetComponent<Image>().color = new Color(.04f, .07f, .1f, .8f);
            questTracker.GetComponent<Image>().raycastTarget = false;
            questText = UIView.Text(questTracker, "TrackedQuest", "");
        }

        RefreshQuestHud();
    }

    void RefreshQuestHud()
    {
        if (questText == null || _progress == null)
            return;
        var q = _progress.TrackedQuest;
        questText.text = q == null ? "任务追踪 [J]\n广场执事：主线 · 商人：支线\n探险家：特殊委托\n头顶 ! 可接，? 可提交。" : $"{q.CategoryLabel} · {q.Title}\n{_progress.QuestObjectivesText(q.Id)}\n" + (_progress.GetQuestStatus(q.Id) == QuestStatus.Ready ? "可提交 · 返回" + _progress.QuestGiverText(q.Id) : "完成后返回" + _progress.QuestGiverText(q.Id));
    }
}
