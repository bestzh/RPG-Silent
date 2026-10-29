#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UIV5Authoring
{
    static void AuthorHud(ViewContext c)
    {
        var ui=c.Root.GetComponent<MainUI>();var root=c.R("TopLeftPanel");Stretch(root);root.GetComponent<Image>().color=Color.clear;root.GetComponent<Image>().raycastTarget=false;
        var vitals=c.Group(root,"PlayerVitals");Edge(vitals,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-28),new Vector2(490,152));
        Box(ui.avatarImage.rectTransform,0,4,98,98,vitals);Box((RectTransform)ui.hpBar.transform,116,34,340,25,vitals);Box((RectTransform)ui.mpBar.transform,116,70,340,20,vitals);
        ui.avatarImage.sprite=Icon("sy-crest-shield");ui.avatarImage.color=Gold;ui.avatarImage.preserveAspect=true;
        if(ui.hpBar.fillRect!=null)ui.hpBar.fillRect.GetComponent<Image>().color=new Color(.65f,.28f,.22f);if(ui.mpBar.fillRect!=null)ui.mpBar.fillRect.GetComponent<Image>().color=new Color(.24f,.49f,.55f);
        Box(ui.levelText.rectTransform,116,0,340,30,vitals);Size(ui.levelText,22);Box(ui.classNameText.rectTransform,116,122,340,30,vitals);Size(ui.classNameText,20,Gold);
        if(ui.expBar!=null)Box((RectTransform)ui.expBar.transform,116,102,340,7,vitals);if(ui.expProgressText!=null){Box(ui.expProgressText.rectTransform,350,115,106,28,vitals);Size(ui.expProgressText,13);}
        var currency=c.Group(root,"Currency");Edge(currency,Vector2.one,Vector2.one,new Vector2(-28,-24),new Vector2(300,40));Box(ui.goldText.rectTransform,0,0,148,40,currency);Box(ui.moneyText.rectTransform,152,0,148,40,currency);Size(ui.goldText,24,Gold);Size(ui.moneyText,20,Muted);
        ui.miniMap.transform.SetParent(root,false);Edge(ui.miniMap.rectTransform,Vector2.one,Vector2.one,new Vector2(-28,-80),new Vector2(250,250));
        ui.miniMap.color=Color.clear;
        if(ui.weaponEquipment!=null)
        {
            var weapons=ui.weaponEquipment;weapons.transform.SetParent(root,false);Edge((RectTransform)weapons.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-198),new Vector2(400,210));
            Box(weapons.statusText.rectTransform,0,0,400,38,weapons.transform);Size(weapons.statusText,20,Gold);Box(weapons.hintText.rectTransform,0,176,400,34,weapons.transform);Size(weapons.hintText,15,Muted);
            for(int i=0;i<2;i++){Box(weapons.slotTexts[i].rectTransform,0,46+i*58,250,50,weapons.transform);Size(weapons.slotTexts[i],17);Box((RectTransform)weapons.changeButtons[i].transform,260,46+i*58,66,46,weapons.transform);Box((RectTransform)weapons.selectButtons[i].transform,334,46+i*58,66,46,weapons.transform);foreach(var b in new[]{weapons.changeButtons[i],weapons.selectButtons[i]}){Stretch(b.GetComponentInChildren<TMP_Text>().rectTransform,4,2,4,2);Size(b.GetComponentInChildren<TMP_Text>(),14);}}
        }
        var portraitRing=c.Group(vitals,"PortraitFrame");Box(portraitRing,-4,0,106,106);var ringImage=portraitRing.GetComponent<Image>()??portraitRing.gameObject.AddComponent<Image>();ringImage.sprite=Icon("sy-portrait-ring");ringImage.color=Gold;ringImage.raycastTarget=false;ringImage.preserveAspect=true;
        var skills=c.Group(root,"CombatSkillBar");Edge(skills,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,92),new Vector2(792,82));var g=Horizontal(skills,0,8);g.childForceExpandWidth=false;
        foreach(var slot in ui.skillSlots)
        {
            slot.transform.SetParent(skills,false);slot.transform.localScale=Vector3.one;Element(slot.gameObject,82,72);
            var rt=(RectTransform)slot.transform;
            var icon=c.R(rt,"Icon");Stretch(icon,5,5,5,5);Stretch(c.R(rt,"bg"));Stretch(c.R(rt,"CooldownMask"));
            slot.icon.color=Color.white;slot.background.color=new Color(.06f,.14f,.16f,.88f);slot.emptyBackgroundColor=new Color(.06f,.14f,.16f,.72f);slot.emptySlotAlpha=.85f;
            var key=c.R(rt,"HotkeyText");Edge(key,new Vector2(0,0),new Vector2(0,0),new Vector2(5,3),new Vector2(62,24));Size(key.GetComponent<TMP_Text>(),16);
            var seconds=c.R(rt,"CooldownSeconds");Stretch(seconds,4,18,4,18);Size(seconds.GetComponent<TMP_Text>(),24);
            if(slot.GetComponent<CanvasGroup>()==null)slot.gameObject.AddComponent<CanvasGroup>();
        }
        ui.skillBarRoot=skills.gameObject;
        var toolbar=c.Group(root,"AdventureShortcuts");Edge(toolbar,Vector2.one,Vector2.one,new Vector2(-28,-347),new Vector2(250,126));Vertical(toolbar,0,5);
        foreach(var id in new[]{"InventoryButton","JournalButton","NearbyLootButton"}){var r=c.R(root,id);r.SetParent(toolbar,false);Element(r.gameObject,38,250);Stretch(r.GetComponentInChildren<TMP_Text>().rectTransform,8,2,8,2);Size(r.GetComponentInChildren<TMP_Text>(),17);}
        Edge(c.R(root,"QuestTracker"),Vector2.one,Vector2.one,new Vector2(-28,-490),new Vector2(370,270));
        Edge(c.R(root,"CombatGuide"),Vector2.one,Vector2.one,new Vector2(-300,-88),new Vector2(190,40));
        Edge(c.R(root,"CombatTutorial"),new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(550,425));
        Edge(c.R(root,"TrainingPractice"),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-85),new Vector2(620,220));
        Edge(c.R(root,"Consumables"),Vector2.zero,Vector2.zero,new Vector2(28,80),new Vector2(370,152));
        var consumables=c.R(root,"Consumables");for(int i=0;i<3;i++){var button=c.R(consumables,"UseConsumable:"+i);Box(c.R(button,"Label"),4,40,104,38);Size(c.R(button,"Label").GetComponent<TMP_Text>(),14);}
        Edge(c.R(root,"CombatStatuses"),new Vector2(0,1),new Vector2(0,1),new Vector2(28,-440),new Vector2(412,150));
        Edge(c.R(root,"EquipmentEffectsHud"),new Vector2(0,1),new Vector2(0,1),new Vector2(28,-610),new Vector2(560,94));
        Edge(c.R(root,"ClassPassiveFeedback"),new Vector2(.5f,.39f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(640,32));
        Edge(c.R(root,"TownClock"),Vector2.one,Vector2.one,new Vector2(-300,-28),new Vector2(250,36));
        Edge(c.R(root,"JourneyNotice"),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-28),new Vector2(760,42));
        Edge(ui.npcPromptText.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,250),new Vector2(780,62));Size(ui.npcPromptText,23,Gold);
        Edge(ui.saveStatusText.rectTransform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,330),new Vector2(1000,68));Size(ui.saveStatusText,21,new Color(.94f,.61f,.48f));
        var target=c.R(root,"TargetDefenseHud");Stretch(target);
        var charge=c.R(target,"ChargeMeter");var fill=c.R(charge,"Fill").GetComponent<Image>();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;Stretch(fill.rectTransform);
        Edge(c.R(target,"LockedEnemy"),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-395),new Vector2(500,58));
        var tutorial=c.R("TutorialSurface");Stretch(tutorial);
        Edge(c.R(tutorial,"OnboardingCard"),new Vector2(1,0),new Vector2(1,0),new Vector2(-28,80),new Vector2(510,226));
        Stretch(c.R("NearbyLootShade"));
        foreach(var name in new[]{"TopCenterPanel","TopRightPanel","BottomCenterPanel"})c.R(name).gameObject.SetActive(false);
        // The lock reticle and minimap markers continue to follow world objects; their dimensions are authored here.
    }
    static void AuthorDungeon(ViewContext c)
    {
        var ui=c.Root.GetComponent<DungeonUI>();var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        Func<string,UnityEngine.Object> field=name=>(UnityEngine.Object)typeof(DungeonUI).GetField(name,flags).GetValue(ui);
        var objective=(TMP_Text)field("objectiveText");var boss=(GameObject)field("bossPanel");var result=(GameObject)field("resultPanel");
        var header=c.Group(c.Root.transform,"RunHeader");Edge(header,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-85),new Vector2(960,400));Vertical(header,0,12);
        var objectiveCard=(RectTransform)objective.transform.parent;objectiveCard.SetParent(header,false);Vertical(objectiveCard,16,0);Element(objectiveCard.gameObject,132);objective.overflowMode=TextOverflowModes.Overflow;Size(objective,21);
        if(boss!=null)
        {
            boss.transform.SetParent(header,false);Element(boss,132);
            var bossTitle=(TMP_Text)field("bossName");Box(bossTitle.rectTransform,16,12,928,34);Size(bossTitle,23);
            var health=(Slider)field("bossHealth");Box((RectTransform)health.transform,16,52,928,14);
            Put(c,boss.transform,"BossMechanics",16,78,928,44,null,18);
        }
        var actions=c.Group(header,"RunActions");Horizontal(actions,0,14);Element(actions.gameObject,44);
        foreach(var id in new[]{"InspectResult","AbandonEvent","RetryEventSave","EndLongChallenge"})
        {var button=c.R(id);button.SetParent(actions,false);Element(button.gameObject,44,-1,1);Stretch(button.GetComponentInChildren<TMP_Text>().rectTransform,8,2,8,2);Size(button.GetComponentInChildren<TMP_Text>(),18);}
        Stretch((RectTransform)result.transform);
        var title=(TMP_Text)field("titleText");var details=(TMP_Text)field("detailsText");var card=(RectTransform)title.transform.parent;Center(card,1100,790);
        Box(title.rectTransform,36,30,1028,70);Size(title,38,Gold);Box(details.rectTransform,36,128,1028,410);Size(details,23);
        var retry=(Button)field("retryButton");var back=(Button)field("returnButton");Box((RectTransform)retry.transform,36,686,326,60);Box((RectTransform)back.transform,738,686,326,60);
        Put(c,card,"ContinueLooting",387,686,326,60,null,22);Put(c,card,"RetryChallengeReward",36,594,1028,48,null,22);
        foreach(var shade in new[]{"ExitLootShade","AbandonEventShade","LongChallengeShade","LongEndShade"})Stretch(c.R(shade));
        var longCard=c.R(c.R("LongChallengeShade"),"LongChallengeWindow");Center(longCard,1100,790);
        Put(c,longCard,"LongTitle",36,30,1028,66,null,36);Put(c,longCard,"LongDetails",36,120,1028,440,null,23);
        Put(c,longCard,"RetryLongSave",36,594,1028,48,null,22);
        foreach(var id in new[]{"ContinueLongChallenge","RestartLongChallenge"})Put(c,longCard,id,36,686,490,60,null,22);
        foreach(var id in new[]{"RetireLongAtRest","ReturnLongChallenge"})Put(c,longCard,id,574,686,490,60,null,22);
        card.GetComponent<Image>().color=Surface;result.transform.SetAsLastSibling();
    }
}
#endif
