using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class QuestJournalUI
{
    int journalPage, codexCategory;
    readonly Button[] collectionTabs = new Button[4];
    Button codexFilter;
    void BuildCollectionTabs()
    {
        string[] labels =
        {
            "任务",
            "成就奖励",
            "冒险图鉴",
            "主线章节"
        };
        for (int i = 0; i < 4; i++)
        {
            int page = i;
            collectionTabs[i] = UIView.Button(panel, "JournalPage:" + i, labels[i], () =>
            {
                journalPage = page;
                rows.anchoredPosition = Vector2.zero;
                Refresh();
            });
        }

        codexFilter = UIView.Button(panel, "CodexCategory", "怪物", () =>
        {
            codexCategory = (codexCategory + 1) % 4;
            rows.anchoredPosition = Vector2.zero;
            Refresh();
        });
    }

    void RefreshCollectionTabs()
    {
        for (int i = 0; i < 4; i++)
            collectionTabs[i].image.color = journalPage == i ? W.Accent : W.Card;
        bool tasks = journalPage == 0;
        UIView.Find(panel, "TaskDetails").gameObject.SetActive(tasks);
        UIView.Find(panel, "TaskFilters").gameObject.SetActive(tasks);
        UIView.Find(panel, "CodexFilters").gameObject.SetActive(journalPage == 2);
        codexFilter.gameObject.SetActive(false);
        foreach (var b in tabs)
            b.gameObject.SetActive(tasks);
        foreach (var b in categoryTabs)
            b.gameObject.SetActive(tasks);
        for (int i = 0; i < codexTabs.Length; i++)
            codexTabs[i].image.color = codexCategory == i ? W.Accent : W.Card;
    }

    void RefreshCollectionRows()
    {
        if (journalPage == 3)
        {
            RefreshChapterRows();
            return;
        }

        if (journalPage == 1)
        {
            foreach (var a in progress.Achievements)
            {
                string id = a.Id;
                bool claimed = progress.AchievementClaimed(id), ready = progress.AchievementReady(id);
                var row = dataRows.Add($"{a.Title} · {progress.AchievementCount(id)}/{a.Required}", a.Description + $"\n金币 {a.Gold} · 经验 {a.Experience} · 锻造碎片 {a.ForgeShards}", achievementIcon);
                row.SetSelected(ready);
                row.Action(0, claimed ? "奖励已领取" : ready ? "领取奖励" : "尚未达成", () =>
                {
                    progress.TryClaimAchievement(id, out string message);
                    status.text = message;
                    Refresh();
                }, ready);
            }
        }
        else
        {
            var kind = new[]
            {
                CollectionKind.Enemy,
                CollectionKind.Boss,
                CollectionKind.Equipment,
                CollectionKind.Secret
            }[codexCategory];
            var entries = progress.CodexEntries(kind).ToArray();
            dataRows.Add($"已收录 {entries.Count(x => x.count > 0)} / {entries.Length}", "首次取得后永久保留");
            foreach (var e in entries)
            {
                string copy = progress.CodexDescription(e);
                int split = copy.IndexOf('\n');
                var row = dataRows.Add(e.count > 0 && split >= 0 ? copy.Substring(0, split) : e.count > 0 ? copy : "尚未发现", split >= 0 ? copy.Substring(split + 1) : e.count > 0 ? "" : copy, e.count > 0 && kind == CollectionKind.Equipment ? progress.FindItem(e.id)?.Icon : collectionIcons[codexCategory]);
                row.Title.color = e.count > 0 ? new Color(.84f, .92f, 1) : new Color(.58f, .63f, .66f);
            }
        }
    }
}
