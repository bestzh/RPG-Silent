using UnityEngine.UI;

public sealed partial class QuestJournalUI
{
    [UnityEngine.SerializeField] UnityEngine.Sprite achievementIcon;
    [UnityEngine.SerializeField] UnityEngine.Sprite[] collectionIcons;
    readonly Button[] codexTabs = new Button[4];
    void BindCodexTabs()
    {
        string[] labels = { "普通怪物", "首领", "装备收藏", "隐藏区域" };
        var filters = UIView.Find(panel, "CodexFilters");
        for (int i = 0; i < codexTabs.Length; i++)
        {
            int index = i;
            codexTabs[i] = UIView.Button(filters, "Codex" + i, labels[i], () => { codexCategory = index; rows.anchoredPosition = UnityEngine.Vector2.zero; Refresh(); });
        }
    }
}
