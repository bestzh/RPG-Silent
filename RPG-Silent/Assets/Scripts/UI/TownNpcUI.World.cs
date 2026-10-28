using System.Linq;
using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public sealed partial class TownNpcUI
{
    Button storyTab, craftingTab, recipeTab, reforgeTab;
    bool storyPage, craftingPage, reforgePage;
    int dialoguePage;
    string dialogueConfirmation, reforgeTarget, reforgeExpected;
    bool ExtraNpcPage => storyPage || craftingPage;
    void BuildWorldTabs()
    {
        void Place(Button b, float x, float width)
        {
            b.GetComponent<RectTransform>().anchoredPosition = new Vector2(x,-138); b.GetComponent<RectTransform>().sizeDelta = new Vector2(width,40);
            b.GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = new Vector2(width-16,36);
        }
        Place(serviceTab,32,170); Place(questTab,218,230); Place(supplyTab,464,170);
        questCategoryButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(818,-192);
        storyTab = W.Button(panel,Font,"StoryTab","交谈 / 故事",650,138,170,40,() =>
        { storyPage=true; craftingPage=questPage=supplyPage=false; dialoguePage=0; dialogueConfirmation=null; rows.anchoredPosition=Vector2.zero; Refresh(); });
        craftingTab = W.Button(panel,Font,"CraftingTab","锻造 / 重铸",836,138,192,40,() =>
        { craftingPage=true; storyPage=questPage=supplyPage=false; reforgeTarget=null; rows.anchoredPosition=Vector2.zero; Refresh(); });
        recipeTab=W.Button(panel,Font,"RecipeTab","装备配方",32,192,200,40,()=>{reforgePage=false;reforgeTarget=null;rows.anchoredPosition=Vector2.zero;Refresh();});
        reforgeTab=W.Button(panel,Font,"ReforgeTab","词条重铸",248,192,200,40,()=>{reforgePage=true;reforgeTarget=null;rows.anchoredPosition=Vector2.zero;Refresh();});
    }
    void ResetWorldPages() { storyPage=craftingPage=reforgePage=false; dialoguePage=0; dialogueConfirmation=reforgeTarget=reforgeExpected=null; }
    void RefreshWorldTabs()
    {
        var npc=interaction.Conversation;
        storyTab.gameObject.SetActive(progress.DialogueFor(npc)!=null);
        craftingTab.gameObject.SetActive(npc.Role==TownNpcRole.Blacksmith);
        storyTab.image.color=storyPage?W.Accent:W.Card; craftingTab.image.color=craftingPage?W.Accent:W.Card;
        recipeTab.gameObject.SetActive(craftingPage);reforgeTab.gameObject.SetActive(craftingPage);
        recipeTab.image.color=reforgePage?W.Card:W.Accent;reforgeTab.image.color=reforgePage?W.Accent:W.Card;
        if(ExtraNpcPage)serviceTab.image.color=W.Card;
        if(storyPage) details.text=$"好感度 {progress.NpcAffinity(npc.Role)} / 100 · 决定会随角色保存\n阅读故事并选择行动；确认后解锁对应委托，另一分支本角色不再开放。";
        if(craftingPage) details.text=reforgePage?"随机重铸全部词条，数值可能升高或降低。白色装备无随机词条。\n强化、宝石、品质、模型和穿戴状态保留；锁定装备需先解锁。":"使用已有装备与锻造材料制作。提交对应任务后永久解锁配方。\n成品带独立随机词条；背包满时进入待领取。";
    }
    float AddStoryRows(float y)
    {
        var story=progress.DialogueFor(interaction.Conversation);if(story==null)return y;
        string chosen=progress.StoryChoice(story.Id);
        W.Text(rows,Font,"StoryTitle",story.Title,20,y,930,40,28).color=new Color(1,.85f,.5f);y+=52;
        if(chosen.Length>0)
        {
            var c=story.Choices.First(x=>x.Id==chosen);
            W.Text(rows,Font,"StoryOutcome","已选择："+c.Label+"\n\n"+c.Outcome+"\n\n决定已经保存，可在任务页接取后续委托。",20,y,930,210,23);
            return y+230;
        }
        W.Text(rows,Font,"StoryPage",story.Pages[dialoguePage],20,y,930,126,24);y+=144;
        if(dialoguePage<story.Pages.Length-1)
        {
            W.Button(rows,Font,"DialogueNext",$"继续交谈  {dialoguePage+1}/{story.Pages.Length}",680,y,270,48,()=>{dialoguePage++;Refresh();});
            return y+70;
        }
        foreach(var choice in story.Choices)
        {
            var c=choice;
            var b=W.Button(rows,Font,"DialogueChoice:"+c.Id,c.Label,20,y,930,52,()=>{dialogueConfirmation=c.Id;Refresh();});
            b.image.color=dialogueConfirmation==c.Id?W.Accent:W.Card;y+=64;
        }
        if(dialogueConfirmation!=null)
        {
            var c=story.Choices.First(x=>x.Id==dialogueConfirmation);
            W.Text(rows,Font,"ChoiceConsequences",c.Outcome+$"\n金币 +{c.Gold} · 经验 +{c.Experience} · 好感 +{c.Affinity}",20,y,930,118,22);y+=132;
            W.Button(rows,Font,"DialogueConfirm","确认这个决定",20,y,280,48,()=>
            { if(Validate(story.Role)){progress.TryChooseDialogue(story.Id,c.Id,interaction.Conversation,out string m);status.text=m;Refresh();} });
            W.Button(rows,Font,"DialogueCancel","重新考虑",320,y,220,48,()=>{dialogueConfirmation=null;Refresh();});y+=60;
        }
        return y+10;
    }
    static string Affixes(ItemStackData e) => e.affixes.Length==0?"暂无随机词条":string.Join(" · ",e.affixes.Select(a=>
        (a.stat==ItemAffixStat.Health?"生命":a.stat==ItemAffixStat.Energy?"能量":a.stat==ItemAffixStat.AttackPercent?"伤害":"减伤")+" +"+a.value+(a.stat>=ItemAffixStat.AttackPercent?"%":"")));
    float AddCraftingRows(float y)
    {
        var inventory=progress.Inventory;
        if(!reforgePage)
        {
            foreach(var recipe in progress.Recipes)
            {
                var r=recipe;bool unlocked=progress.RecipeUnlocked(r);
                W.Text(rows,Font,"Recipe:"+r.Id,$"{r.Result.DisplayName} · {r.Result.RarityLabel} · Lv.{r.Result.RequiredLevel}",16,y,650,34,23).color=r.Result.QualityColor;
                W.Text(rows,Font,"RecipeCost",$"金币 {r.Gold} · 锻造碎片 {r.ForgeShards} · 晶核碎片 {r.CrystalShards}\n"+
                    (unlocked?"配方已解锁": "先提交："+progress.FindQuest(r.UnlockQuestId)?.Title),16,y+40,650,62,21);
                var b=W.Button(rows,Font,"CraftEquipment:"+r.Id,unlocked?"锻造一件":"配方未解锁",710,y+12,240,50,()=>
                {if(Validate(TownNpcRole.Blacksmith)){progress.TryCraftEquipment(r.Id,interaction.Conversation,out string m);status.text=m;Refresh();}});
                b.interactable=unlocked&&progress.Gold>=r.Gold&&inventory.Count("forge-shard")>=r.ForgeShards&&inventory.Count("crystal-shard")>=r.CrystalShards;y+=124;
            }
            return y;
        }
        if(reforgeTarget!=null)
        {
            var e=progress.FindInstance(reforgeTarget);var item=progress.FindItem(reforgeTarget);
            if(e==null){reforgeTarget=null;return AddCraftingRows(y);}
            W.Text(rows,Font,"ReforgePreview",$"{item.DisplayName} +{e.upgradeLevel}\n\n当前：{Affixes(e)}\n\n将重新随机生成全部 {EquipmentInstances.AffixCount(item.Rarity)} 条词条。\n金币 {progress.ReforgeGold(e.Key)} · 锻造碎片 {progress.ReforgeShards(e.Key)} · 晶核碎片 1",20,y,930,210,23).color=item.QualityColor;
            W.Button(rows,Font,"ConfirmReforge","确认消耗并重铸",20,y+228,340,52,()=>
            {if(Validate(TownNpcRole.Blacksmith)){progress.TryReforge(reforgeTarget,reforgeExpected,interaction.Conversation,out string m);status.text=m;reforgeTarget=null;Refresh();}});
            W.Button(rows,Font,"CancelReforge","取消",380,y+228,220,52,()=>{reforgeTarget=null;Refresh();});return y+302;
        }
        var entries=inventory.bag.Where(e=>e.IsInstance&&!e.legacyReserve&&progress.FindItem(e.itemId)?.Rarity>ItemRarity.Common).ToArray();
        if(entries.Length==0){W.Text(rows,Font,"NoReforge","还没有可重铸的绿、蓝、紫、金或红色装备。",20,y,930,90,23);return y+100;}
        foreach(var e in entries)
        {
            string key=e.Key;var item=progress.FindItem(key);
            W.Text(rows,Font,"ReforgeItem",$"{item.DisplayName} +{e.upgradeLevel}{(e.isLocked?" · 已锁定":"")}\n{Affixes(e)}",16,y,674,72,22).color=item.QualityColor;
            W.Text(rows,Font,"ReforgeCost",$"金币 {progress.ReforgeGold(key)} · 锻造碎片 {progress.ReforgeShards(key)} · 晶核碎片 1",16,y+78,674,32,19);
            var b=W.Button(rows,Font,"SelectReforge:"+key,"查看并确认重铸",710,y+12,240,50,()=>{reforgeTarget=key;reforgeExpected=progress.ReforgeQuote(key);rows.anchoredPosition=Vector2.zero;Refresh();});
            b.interactable=!e.isLocked;y+=128;
        }
        return y;
    }
}
