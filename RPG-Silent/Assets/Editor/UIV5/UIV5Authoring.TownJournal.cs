#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UIV5Authoring
{
    static void AuthorTown(ViewContext c)
    {
        var p=c.R("Conversation");p.name="Group_NpcServicesPage";PageDecor(c,p,"选择服务后直接操作；等级、材料、价格和限制均来自当前角色。");
        Stretch(c.R("Shade"));c.R("Shade").GetComponent<Image>().color=Surface;
        Put(c,p,"Title",72,112,1200,76,null,52);Put(c,p,"Details",435,269,1380,66,null,22);Put(c,p,"Close",1748,29,100,42,null,17);
        var sidebar=c.Group(p,"ServiceNavigation");Box(sidebar,76,261,280,650);Vertical(sidebar,0,16);
        string[] navigation={"Services","AdvancementPage","SpecializationPage","Quests","Supplies","CraftingTab","StoryTab","WarehouseEntry"};
        foreach(var id in navigation)
        {
            var r=id=="AdvancementPage"?c.Button(p,id,"职业进阶"):c.R(p,id);r.SetParent(sidebar,false);Element(r.gameObject,60,280);Stretch(r.GetComponentInChildren<TMP_Text>().rectTransform,20,4,20,4);Size(r.GetComponentInChildren<TMP_Text>(),22);r.GetComponentInChildren<TMP_Text>().alignment=TextAlignmentOptions.MidlineLeft;
        }
        var body=c.Group(p,"ServiceBody");Box(body,408,370,1436,568);Horizontal(body,10,34);
        var viewport=c.R(p,"Viewport");viewport.SetParent(body,false);Element(viewport.gameObject,-1,-1,1);viewport.GetComponent<Image>().color=new Color(.12f,.21f,.22f,.16f);
        var rows=c.R(viewport,"Rows");NativeList(rows);
        var details=c.Group(p,"MentorDetails",body);Element(details.gameObject,-1,925);Vertical(details,8,10).childForceExpandWidth=false;
        foreach(var spec in new[]{("LessonName","选择技能",34f,50f),("LessonGrowth","技能当前等级与下一级效果",23f,106f),("LessonRequirement","等级、金币与学习条件",21f,58f)})
        {var t=c.Text(details,spec.Item1,spec.Item2,spec.Item3);Element(t.gameObject,spec.Item4);}
        var actions=c.Group(details,"LessonActions");Horizontal(actions);Element(actions.gameObject,50);
        foreach(var id in new[]{"TrainSelected","EquipSelected"}){var b=c.Button(details,id,id=="TrainSelected"?"学习 / 升级":"装配到所选位置",actions);Element(b.gameObject,50,-1,1);}
        var hint=c.Text(details,"SlotsHint","装配位置 · 点击 1—0 选择；学习后自动装入第一个空槽。",17);Element(hint.gameObject,28);
        var slots=c.Group(details,"DirectSlots");Horizontal(slots,0,10);Element(slots.gameObject,48);
        for(int i=0;i<10;i++)
        {
            var b=c.Button(details,"SkillSlot"+i,i==9?"0":(i+1).ToString(),slots);Element(b.gameObject,50,50);
            var icon=c.Group(b,"Icon");Stretch(icon,5,4,5,15);var image=icon.GetComponent<Image>()??icon.gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;image.enabled=false;
            var key=c.R(b,"Label");Edge(key,Vector2.zero,Vector2.zero,new Vector2(5,1),new Vector2(40,16));Size(key.GetComponent<TMP_Text>(),11);key.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.BottomLeft;key.SetAsLastSibling();
        }
        var clear=c.R(p,"ClearSlot");clear.SetParent(details,false);Element(clear.gameObject,38,260);Stretch(clear.GetComponentInChildren<TMP_Text>().rectTransform,8,3,8,3);
        foreach(var id in new[]{"PreviousSlot","NextSlot","Slot"})c.R(p,id).gameObject.SetActive(false);
        // Different service sub-tabs share one line and are mutually visible by existing page logic.
        string[][] subtabs={new[]{"WeaponForge","ArmorForge","GemForge"},new[]{"RecipeTab","ReforgeTab"},new[]{"ShopStockTab","ShopBuybackTab"}};
        foreach(var group in subtabs)for(int i=0;i<group.Length;i++)Put(c,p,group[i],434+i*280,326,264,42,null,19);
        Put(c,p,"ForgeMaterials",1260,330,550,36,null,18);Put(c,p,"BuybackRetention",1060,330,750,36,null,18);Put(c,p,"QuestCategory",434,326,300,42,null,19);
        Put(c,p,"Status",434,960,1380,64,null,18);Separator(p,434,944,1380);
        Stretch(c.R("WarehouseShade"));Stretch(c.R("CommerceShade"));Stretch(c.R("SpecializationShade"));
        var view=c.Root.GetComponent<TownNpcUI>();Field(view,"mentorBackground",Resources.Load<Texture2D>("BG/BG2"));Field(view,"forgeBackground",Resources.Load<Texture2D>("BG/BG3"));
        AuthorWarehouse(c);
        AuthorConfirmation(c);
    }
    static void AuthorWarehouse(ViewContext c)
    {
        var page=c.R(c.R("WarehouseShade"),"WarehouseWindow");PageDecor(c,page,"先选存取方向，再点物品加入清单；核对后整批处理。");
        Put(c,page,"WarehouseHeading",72,112,1500,76,null,48);Put(c,page,"CloseWarehouse",1650,29,198,42,null,18);
        Put(c,page,"DepositMode",76,278,280,60,null,22);Put(c,page,"WithdrawMode",76,354,280,60,null,22);
        Put(c,page,"WarehouseCategory",408,278,320,44,null,19);Put(c,page,"WarehouseSearch",750,278,574,44);
        var grid=c.Group(page,"WarehouseGrid");Box(grid,408,350,916,560);var layout=grid.GetComponent<GridLayoutGroup>()??grid.gameObject.AddComponent<GridLayoutGroup>();layout.constraint=GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount=6;layout.cellSize=new Vector2(142,130);layout.spacing=new Vector2(12,12);
        for(int i=0;i<24;i++)
        {
            var cell=c.R(page,"WarehouseItem"+i);cell.SetParent(grid,false);cell.localScale=Vector3.one;
            var label=cell.GetComponentInChildren<TMP_Text>();Box(label.rectTransform,6,72,130,54);Size(label,14);
            var art=c.Group(cell,"Icon");Box(art,43,9,56,56);var image=art.GetComponent<Image>()??art.gameObject.AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;image.enabled=false;
        }
        Put(c,page,"WarehousePrevious",408,928,150,40,null,17);Put(c,page,"WarehousePages",574,928,580,40,null,17);Put(c,page,"WarehouseNext",1174,928,150,40,null,17);
        Put(c,page,"WarehouseDetails",1380,278,464,152,null,19);
        Put(c,page,"WarehouseQuantity",1380,448,160,44);Put(c,page,"ApplyWarehouseQuantity",1556,448,288,44,null,18);
        var cart=c.Text(page,"WarehouseCart","本次清单 · 0 项",19);Box(cart,1380,514,464,160);
        Put(c,page,"WarehouseRules",1380,696,464,128,null,16);
        Put(c,page,"ClearWarehouseSelection",1380,850,464,40,null,17);Put(c,page,"ReviewWarehouse",1380,908,464,60,null,21);
        Put(c,page,"WarehouseStatus",76,981,1768,42,null,18);
    }
    static void AuthorConfirmation(ViewContext c)
    {
        var shade=c.Group(c.Root.transform,"ServiceConfirmation");Stretch(shade);var bg=shade.GetComponent<Image>()??shade.gameObject.AddComponent<Image>();bg.color=new Color(.02f,.07f,.08f,.94f);bg.raycastTarget=true;
        var card=c.Group(shade,"Card");Center(card,1020,620);Back(card,"Image_Surface",Surface);
        var title=c.Text(card,"Title","确认这个决定？",32);Box(title,42,35,936,62);
        var body=c.Text(card,"Details","请核对条件与结果。",23);Box(body,42,116,936,350);
        var confirm=c.Button(card,"Confirm","确认");Box(confirm,42,520,450,56);
        var cancel=c.Button(card,"Cancel","重新考虑");Box(cancel,528,520,450,56);
        var view=shade.GetComponent<UIConfirmationView>()??shade.gameObject.AddComponent<UIConfirmationView>();Field(view,"title",title.GetComponent<TMP_Text>());Field(view,"details",body.GetComponent<TMP_Text>());Field(view,"confirm",confirm.GetComponent<Button>());Field(view,"cancel",cancel.GetComponent<Button>());shade.gameObject.SetActive(false);
        var nested=shade.GetComponent<UIViewReferences>()??shade.gameObject.AddComponent<UIViewReferences>();nested.SetAuthoringEntries(c.Entries.Where(e=>e.Scope==shade||e.Scope==card).ToArray());
        PrefabUtility.SaveAsPrefabAsset(shade.gameObject,Templates+"Dialog_ServiceConfirmation.prefab");
    }
    static void AuthorJournal(ViewContext c)
    {
        var p=c.R("Journal");p.name="Group_JournalPage";PageDecor(c,p,"任务目标、章节去向和收藏进度集中查看；追踪后返回游戏即可跟随指引。");Stretch(c.R("Shade"));c.R("Shade").GetComponent<Image>().color=Surface;
        Put(c,p,"Title",72,112,1000,76,null,52);Put(c,p,"Close",1748,29,100,42,null,17);
        int[] order={0,3,1,2};for(int i=0;i<order.Length;i++)Put(c,p,"JournalPage:"+order[i],76+i*228,257,212,52,null,21);
        var taskFilters=c.Group(p,"TaskFilters");Box(taskFilters,76,331,1770,48);Horizontal(taskFilters,0,12);
        for(int i=0;i<3;i++){var b=c.R(p,"Filter:"+i);b.SetParent(taskFilters,false);Element(b.gameObject,44,164);Stretch(b.GetComponentInChildren<TMP_Text>().rectTransform,8,2,8,2);}
        for(int i=0;i<6;i++){var b=c.R(p,"Category:"+i);b.SetParent(taskFilters,false);Element(b.gameObject,44,112);Stretch(b.GetComponentInChildren<TMP_Text>().rectTransform,8,2,8,2);}
        var filters=c.Group(p,"CodexFilters");Box(filters,76,331,1770,48);Horizontal(filters,0,16);
        for(int i=0;i<4;i++){var b=c.Button(filters,"Codex"+i,new[]{"普通怪物","首领","装备收藏","隐藏区域"}[i]);Element(b.gameObject,44,240);}
        c.R(p,"CodexCategory").gameObject.SetActive(false);
        var body=c.Group(p,"JournalBody");Box(body,76,408,1770,552);Horizontal(body,0,40);
        var viewport=c.R(p,"Viewport");viewport.SetParent(body,false);Element(viewport.gameObject,-1,-1,1);NativeList(c.R(viewport,"Rows"));
        var details=c.Group(p,"TaskDetails",body);Element(details.gameObject,-1,1000);Vertical(details,10,10);
        foreach(var id in new[]{"QuestTitle","Description","Objectives","Rewards","Destination"})
        {var t=c.R(p,id);t.SetParent(details,false);Element(t.gameObject,id=="QuestTitle"?54:id=="Description"?82:id=="Objectives"?112:id=="Rewards"?96:70);Size(t.GetComponent<TMP_Text>(),id=="QuestTitle"?29:21);}
        var actions=c.Group(details,"Actions");Horizontal(actions,0,20);Element(actions.gameObject,52);
        foreach(var id in new[]{"Track","AbandonQuest"}){var b=c.R(p,id);b.SetParent(actions,false);Element(b.gameObject,50,320);Stretch(b.GetComponentInChildren<TMP_Text>().rectTransform,8,3,8,3);}
        Put(c,p,"Status",76,977,1770,42,null,18);Stretch(c.R("AbandonQuestShade"));
        var view=c.Root.GetComponent<QuestJournalUI>();Field(view,"achievementIcon",Icon("glyph-award"));ArrayField(view,"collectionIcons",new UnityEngine.Object[]{Icon("glyph-skull"),Icon("glyph-skull"),Icon("glyph-invchest"),Icon("glyph-map")});
    }
}
#endif
