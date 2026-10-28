using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class QuestJournalUI
{
    int journalPage, codexCategory;
    readonly Button[] collectionTabs = new Button[3];
    Button codexFilter;
    void BuildCollectionTabs()
    {
        string[] labels={"任务","成就奖励","冒险图鉴"};
        for(int i=0;i<3;i++)
        {
            int page=i;
            collectionTabs[i]=W.Button(panel,Font,"JournalPage:"+i,labels[i],452+i*182,26,170,42,()=>{journalPage=page;rows.anchoredPosition=Vector2.zero;Refresh();});
        }
        codexFilter=W.Button(panel,Font,"CodexCategory","怪物",960,100,228,40,()=>{codexCategory=(codexCategory+1)%4;rows.anchoredPosition=Vector2.zero;Refresh();});
    }
    void RefreshCollectionTabs()
    {
        for(int i=0;i<3;i++)collectionTabs[i].image.color=journalPage==i?W.Accent:W.Card;
        bool tasks=journalPage==0;
        foreach(var b in tabs)b.gameObject.SetActive(tasks);
        foreach(var b in categoryTabs)b.gameObject.SetActive(tasks);
        foreach(var t in new[]{heading,description,objectives,rewards,destination})t.gameObject.SetActive(tasks);
        track.gameObject.SetActive(tasks);
        var viewport=rows.parent.GetComponent<RectTransform>();
        viewport.anchoredPosition=new Vector2(32,tasks?-208:-160);viewport.sizeDelta=tasks?new Vector2(386,502):new Vector2(1156,550);
        codexFilter.gameObject.SetActive(journalPage==2);
        codexFilter.GetComponentInChildren<TMP_Text>().text=new[]{"普通怪物","首领","装备收藏","隐藏区域"}[codexCategory]+"  >";
    }
    void RefreshCollectionRows()
    {
        float y=10;
        if(journalPage==1)
        {
            foreach(var a in progress.Achievements)
            {
                string id=a.Id;bool claimed=progress.AchievementClaimed(id),ready=progress.AchievementReady(id);
                W.Text(rows,Font,"Achievement:"+id,$"{a.Title} · {progress.AchievementCount(id)}/{a.Required}\n{a.Description}",18,y,820,66,22).color=ready?new Color(1,.85f,.4f):Color.white;
                W.Text(rows,Font,"AchievementReward",$"金币 {a.Gold} · 经验 {a.Experience} · 锻造碎片 {a.ForgeShards}",18,y+70,820,30,19);
                var b=W.Button(rows,Font,"ClaimAchievement:"+id,claimed?"奖励已领取":ready?"领取奖励":"尚未达成",882,y+12,240,52,()=>{progress.TryClaimAchievement(id,out string message);status.text=message;Refresh();});
                b.interactable=ready;y+=118;
            }
        }
        else
        {
            var kind=new[]{CollectionKind.Enemy,CollectionKind.Boss,CollectionKind.Equipment,CollectionKind.Secret}[codexCategory];
            var entries=progress.CodexEntries(kind).ToArray();
            W.Text(rows,Font,"CollectionCount",$"已收录 {entries.Count(x=>x.count>0)} / {entries.Length} · 首次取得后永久保留",18,y,1100,40,24);y+=52;
            foreach(var e in entries)
            {
                W.Text(rows,Font,"Codex:"+e.id,progress.CodexDescription(e),18,y,1100,94,22).color=e.count>0?new Color(.84f,.92f,1):new Color(.58f,.63f,.66f);
                y+=108;
            }
        }
        rows.sizeDelta=new Vector2(1156,Mathf.Max(550,y+10));
    }
}
