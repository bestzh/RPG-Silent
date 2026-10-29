#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static partial class UIV5Authoring
{
    static void AuthorStart(ViewContext c)
    {
        var ui=c.Root.GetComponent<StartUI>();var frame=c.Group(c.Root.transform,"MenuFrame");PageDecor(c,frame,"一段属于你的冒险，从静谧城开始。","BG");frame.SetAsFirstSibling();
        var title=c.Text(frame,"GameTitle","SILENT",96);Box(title,150,240,1000,160);Size(title.GetComponent<TMP_Text>(),96,Paper);
        var subtitle=c.Text(frame,"GameSubtitle","静 谧 之 境",30);Box(subtitle,158,398,900,60);Size(subtitle.GetComponent<TMP_Text>(),30,Gold);
        Button[] buttons={ui.StartButton,ui.ContinueButton,ui.SettingsButton,ui.QuiteButton};
        for(int i=0;i<buttons.Length;i++){Box((RectTransform)buttons[i].transform,160,495+i*82,370,64,frame);Stretch(buttons[i].GetComponentInChildren<TMP_Text>().rectTransform,18,5,18,5);Size(buttons[i].GetComponentInChildren<TMP_Text>(),25);}
        string[] menuIcons={"glyph-flag","glyph-book","glyph-settings","glyph-exit"};for(int i=0;i<buttons.Length;i++)ButtonGlyph(c,(RectTransform)buttons[i].transform,menuIcons[i]);
        Box(ui.SaveStatusText.rectTransform,160,857,1240,90,frame);Size(ui.SaveStatusText,19,Muted);
        ui.SaveStatusText.alignment=TextAlignmentOptions.Left;
        foreach(Transform child in c.Root.transform)
            if(child!=frame && child.name!="ClassSelection" && child.name!="CharacterSelection")child.gameObject.SetActive(false);
        var creation=c.R("ClassSelection");Stretch(creation);var page=c.R(creation,"CreationPage");PageDecor(c,page,"每一种选择，都有自己的冒险方式。","BG");
        c.R(page,"V5Brand").gameObject.SetActive(false);c.R(page,"V5Subtitle").gameObject.SetActive(false);
        foreach(Transform rule in page)if(rule.name=="Rule"||rule.name.StartsWith("Image_Rule"))rule.gameObject.SetActive(false);
        Put(c,page,"Brand",76,30,1200,42,null,22);Put(c,page,"Heading",76,110,1100,82,null,52);Put(c,page,"Subtitle",76,198,1600,42,null,19);Put(c,page,"Step",1460,135,380,42,null,20);
        Put(c,page,"ClassCaption",76,270,300,40,null,24);
        string[] ids={"swordsman","mage","ranger"};
        // Definitions decide the actual profession IDs; their captured buttons are the source of truth.
        var classCards=Resources.LoadAll<PlayerClassDefinition>("Classes").Where(x=>!x.IsAdvanced).OrderBy(x=>x.Id=="swordsman"?0:x.Id=="mage"?1:2).ToArray();
        for(int i=0;i<classCards.Length;i++){var r=Put(c,page,classCards[i].Id,76,326+i*144,326,120);Box(c.R(r,"Number"),18,18,50,36);Box(c.R(r,"Name"),78,13,230,44);Box(c.R(r,"Role"),78,68,230,36);}
        Put(c,page,"BeginnerNote",76,805,326,90,null,21);Put(c,page,"Portrait",440,276,702,560);
        Put(c,page,"Male",624,855,164,52);Put(c,page,"Female",812,855,164,52);
        Put(c,page,"Role",1220,277,620,40,null,22);Put(c,page,"ClassName",1216,326,620,88,null,56);Put(c,page,"Description",1220,442,620,150,null,25);
        Put(c,page,"WeaponCaption",1220,614,620,36,null,21);Put(c,page,"Weapon",1220,659,620,50,null,28);Put(c,page,"BranchCaption",1220,746,620,36,null,21);Put(c,page,"Branches",1220,793,620,90,null,26);
        Put(c,page,"Back",76,957,150,54);Put(c,page,"Selected",264,963,820,48,null,22);Put(c,page,"StartAdventure",1220,950,620,64);Put(c,page,"Error",440,913,702,34,null,19);creation.gameObject.SetActive(false);
        var selection=c.R("CharacterSelection");Stretch(selection);var characters=c.R(selection,"CharactersPage");PageDecor(c,characters,"每个角色独立成长；创建新角色会保留已有进度。","BG");
        c.R(characters,"V5Brand").gameObject.SetActive(false);c.R(characters,"V5Subtitle").gameObject.SetActive(false);
        Put(c,characters,"Brand",76,30,1200,42,null,22);Put(c,characters,"Title",76,110,1300,82,null,52);Put(c,characters,"Hint",76,198,1600,42,null,19);
        for(int i=0;i<6;i++)Put(c,characters,"Character:"+i,76,278+i*92,640,82,null,22);
        Put(c,characters,"Portrait",772,270,1072,550);Put(c,characters,"SelectedCharacter",772,833,1072,48,null,28);Put(c,characters,"CharacterDetails",772,887,1072,54,null,19);
        Put(c,characters,"PreviousPage",76,844,160,44);Put(c,characters,"Page",252,850,280,40,null,20);Put(c,characters,"NextPage",550,844,166,44);
        Put(c,characters,"Feedback",76,912,640,42,null,18);Put(c,characters,"Back",76,969,144,50);Put(c,characters,"Refresh",238,969,180,50);Put(c,characters,"CreateCharacter",1150,960,250,60);Put(c,characters,"ContinueCharacter",1430,960,414,60);selection.gameObject.SetActive(false);
        SaveSubView(c,creation,"Page_CharacterCreation");SaveSubView(c,selection,"Page_CharacterSelection");
    }
    static void SaveSubView(ViewContext c,RectTransform root,string name)
    {
        var refs=root.GetComponent<UIViewReferences>()??root.gameObject.AddComponent<UIViewReferences>();
        refs.SetAuthoringEntries(c.Entries.Where(e=>e.Scope==root||e.Scope.IsChildOf(root)).ToArray());
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,Templates+name+".prefab");
    }
    static void AuthorPause(ViewContext c)
    {
        var ui=c.Root.GetComponent<PauseUI>();var frame=c.Group(c.Root.transform,"PauseFrame");PageDecor(c,frame,"休息片刻，再继续前行。");frame.SetAsFirstSibling();
        var title=c.Text(frame,"PauseTitle","旅途暂歇",64);Box(title,160,235,1000,94);
        Button[] buttons={ui.ResumeButton,ui.SaveButton,ui.SettingsButton,ui.MainMenuButton,ui.QuiteButton};
        for(int i=0;i<buttons.Length;i++){var b=buttons[i];Box((RectTransform)b.transform,160,400+i*88,440,66,frame);Stretch(b.GetComponentInChildren<TMP_Text>().rectTransform,14,4,14,4);Size(b.GetComponentInChildren<TMP_Text>(),25);}
        Box(ui.SaveStatusText.rectTransform,770,446,950,300,frame);Size(ui.SaveStatusText,25,Muted);
        foreach(Transform child in c.Root.transform)if(child!=frame)child.gameObject.SetActive(false);
    }
    static void AuthorLoading(ViewContext c)
    {
        var ui=c.Root.GetComponent<LoadingUI>();var frame=c.Group(c.Root.transform,"LoadingFrame");PageDecor(c,frame,"风会指引下一段旅程。","Loding");frame.SetAsFirstSibling();
        var title=c.Text(frame,"LoadingTitle","正前往新的旅途",52);Box(title,96,710,1500,90);
        Box((RectTransform)ui.progressBar.transform,96,842,1728,12,frame);Box(ui.progressText.rectTransform,96,877,1728,70,frame);Size(ui.progressText,25,Gold);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;var failure=(GameObject)typeof(LoadingUI).GetField("failurePanel",flags).GetValue(ui);
        Stretch((RectTransform)failure.transform);failure.transform.SetAsLastSibling();failure.gameObject.SetActive(false);
        foreach(Transform child in c.Root.transform)if(child!=frame&&child!=failure.transform)child.gameObject.SetActive(false);
    }
    static void AuthorPortal(ViewContext c)
    {
        var frame=c.Group(c.Root.transform,"PortalFrame");PageDecor(c,frame,"先选目的地，再选择模式与难度。出发前可查看奖励、纪录和继续规则。");frame.SetAsFirstSibling();
        var heading=c.Text(frame,"PortalHeading","出发，向未知之地",52);Box(heading,76,112,1300,82);
        var scroll=c.R("Scroll View");Box(scroll,76,275,410,665,frame);
        var scrollBackground=scroll.GetComponent<Image>();if(scrollBackground!=null){scrollBackground.sprite=null;scrollBackground.color=Color.clear;}
        var viewport=c.R(scroll,"Viewport");Stretch(viewport);var content=c.R(viewport,"Content");
        content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.sizeDelta=Vector2.zero;content.anchoredPosition=Vector2.zero;
        Vertical(content,4,16);var fit=content.GetComponent<ContentSizeFitter>()??content.gameObject.AddComponent<ContentSizeFitter>();fit.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var template=c.R(content,"Item");Element(template.gameObject,110,402);foreach(var text in template.GetComponentsInChildren<TMP_Text>(true)){Size(text,25);Stretch(text.rectTransform,18,8,18,8);}
        template.GetComponent<Image>().sprite=null;template.GetComponent<Image>().color=ProgressionUIWidgets.Card;foreach(var text in template.GetComponentsInChildren<TMP_Text>(true))text.color=Paper;
        var portalView=c.Root.GetComponent<PortalUI>();var portalValues=new SerializedObject(portalView);portalValues.FindProperty("normalColor").colorValue=ProgressionUIWidgets.Card;portalValues.FindProperty("selectedColor").colorValue=ProgressionUIWidgets.Accent;portalValues.ApplyModifiedPropertiesWithoutUndo();
        var templateRefs=template.GetComponent<UIViewReferences>()??template.gameObject.AddComponent<UIViewReferences>();templateRefs.SetAuthoringEntries(c.Entries.Where(e=>e.Scope==template||e.Scope.IsChildOf(template)).ToArray());
        PrefabUtility.SaveAsPrefabAsset(template.gameObject,Templates+"Row_Destination.prefab");
        var bg=c.R("BG");Box(bg,526,275,1318,665,frame);bg.GetComponent<RawImage>().color=new Color(.45f,.57f,.52f);
        var card=c.R(bg,"DescriptionCard");Stretch(card);card.GetComponent<Image>().color=new Color(.04f,.10f,.11f,.8f);
        var modes=c.Group(c.Root.transform,"ChallengeModes",frame);Box(modes,552,300,1266,110);var grid=modes.GetComponent<GridLayoutGroup>()??modes.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=3;grid.cellSize=new Vector2(410,48);grid.spacing=new Vector2(12,10);
        for(int i=0;i<6;i++)c.Button(modes,"Mode"+i,DungeonChallengeRules.Label((DungeonChallenge)i));
        Put(c,bg,"Description",34,160,1250,430,null,23);Put(c,bg,"ChallengeMode",0,0,100,40);c.R(bg,"ChallengeMode").gameObject.SetActive(false);Put(c,bg,"NewExpedition",970,580,310,44,null,17);
        var bottom=c.R("Bottom");Box(bottom,526,962,1318,60,frame);var bottomImage=bottom.GetComponent<Image>();if(bottomImage!=null){bottomImage.sprite=null;bottomImage.color=Color.clear;}
        var drop=c.R(bottom,"Dropdown");Box(drop,0,0,320,58);var ok=Put(c,bottom,"OK",850,0,300,58,null,23);ok.name="Button_EnterDungeon";ok.GetComponentInChildren<TMP_Text>().text="进入副本";var close=Put(c,bottom,"Quite",1168,0,150,58,null,23);
        foreach(Transform child in c.Root.transform)if(child!=frame)child.gameObject.SetActive(false);
        template.gameObject.SetActive(false);
    }
    static void AuthorSettings(ViewContext c)
    {
        var ui=c.Root.GetComponent<SettingsUI>();var page=c.R("Image");page.name="Group_SettingsPage";PageDecor(c,page,"选项立即预览；应用后保存，返回撤销未应用修改。","SettingBG");Stretch(c.R("SettingsShade"));
        c.R("SettingsShade").GetComponent<Image>().color=Surface;
        foreach(Transform child in c.Root.transform)if(child!=page&&child!=c.R("SettingsShade"))child.gameObject.SetActive(false);
        var title=c.Text(page,"SettingsHeading","调整你的冒险",52);Box(title,76,112,1300,82);
        var navigation=c.Group(page,"SettingsNavigation");Box(navigation,76,278,280,600);Vertical(navigation,0,16);
        foreach(var button in new[]{ui.Screen,ui.Sound,ui.Controller,ui.Game,ui.Back}){button.transform.SetParent(navigation,false);Element(button.gameObject,64,280);Stretch(button.GetComponentInChildren<TMP_Text>().rectTransform,20,4,20,4);Size(button.GetComponentInChildren<TMP_Text>(),23);if(button!=ui.Back){var selected=c.R(button.transform,"select");Stretch(selected);}}
        var right=c.R(page,"Right");Box(right,420,275,1424,720);
        foreach(var component in right.GetComponentsInChildren<MonoBehaviour>(true).Where(x=>x is ScreenPage||x is SoundPage||x is ControllerPage||x is GamePage).ToArray())
            AuthorSettingsPage(c,component);
        Put(c,right,"SettingsSaveHint",0,744,1424,42,null,18);
        // Original left decoration is no longer interactive; buttons retain serialized references.
        foreach(Transform child in page)if(child.name is "Lift" or "Left")child.gameObject.SetActive(false);
        right.SetAsLastSibling();
    }
    static void AuthorSettingsPage(ViewContext c,MonoBehaviour page)
    {
        Stretch((RectTransform)page.transform);
        if(page is ControllerPage){EnsureControllerButtons(c,page);NormalizeKeyButtons(page);}
        var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        var values=page.GetType().GetFields(flags).Where(f=>typeof(TMP_Text).IsAssignableFrom(f.FieldType)).Select(f=>f.GetValue(page)as TMP_Text).Where(t=>t!=null).ToHashSet();
        var fieldControls=page.GetType().GetFields(flags).Where(f=>typeof(Selectable).IsAssignableFrom(f.FieldType)).Select(f=>new{Field=f,Control=f.GetValue(page)as Selectable}).Where(x=>x.Control!=null).ToArray();
        var actionButtons=fieldControls.Where(x=>x.Field.Name.IndexOf("okButton",StringComparison.OrdinalIgnoreCase)>=0||x.Field.Name.IndexOf("resetButton",StringComparison.OrdinalIgnoreCase)>=0).Select(x=>(Button)x.Control).Distinct().ToArray();
        var controls=fieldControls.Select(x=>x.Control).Where(x=>!actionButtons.Contains(x as Button)).Distinct().ToList();
        if(page is GamePage)
        {
            controls.Add(c.R(page.transform,"DamageNumbers").GetComponentInChildren<Toggle>(true));
            var feedback=c.R(page.transform,"FeedbackIntensity");controls.Add(feedback.GetComponentInChildren<Slider>(true));
            var original=(TMP_Text)page.GetType().GetField("screenShakeLabel",flags).GetValue(page);
            if(original!=null)foreach(var text in feedback.GetComponentsInChildren<TMP_Text>(true))if(text.name==original.name)values.Add(text);
        }
        var rows=controls.Select(x=>OptionRow(x.transform,page.transform)).Distinct().Where(x=>!actionButtons.Any(b=>b.transform==x||b.transform.IsChildOf(x))).ToArray();
        var content=c.Group(page.transform,"OptionsContent");Box(content,24,24,1376,610);
        var grid=content.gameObject.GetComponent<GridLayoutGroup>()??content.gameObject.AddComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=page is ControllerPage?2:1;grid.cellSize=new Vector2(page is ControllerPage?674:1376,page is ControllerPage?54:86);grid.spacing=new Vector2(28,page is ControllerPage?12:18);
        foreach(var row in rows)
        {
            row.SetParent(content,false);row.localScale=Vector3.one;
            foreach(var control in controls.Where(x=>x.transform==row||x.transform.IsChildOf(row)))
            {
                if(control.transform!=row)Box((RectTransform)control.transform,page is ControllerPage?340:696,8,control is Slider?(page is ControllerPage?222:500):(page is ControllerPage?310:620),page is ControllerPage?40:62);
                if(control is TMP_Dropdown dropdown&&dropdown.captionText!=null){Size(dropdown.captionText,22);Stretch(dropdown.captionText.rectTransform,14,4,32,4);}
                if(control is Button b){var label=b.GetComponentInChildren<TMP_Text>();Stretch(label.rectTransform,8,3,8,3);Size(label,20);}
            }
            foreach(var text in row.GetComponentsInChildren<TMP_Text>(true))
                if(text.GetComponentInParent<Selectable>()==null){bool value=values.Contains(text);text.name=value?"Text_Value":"Text_Action";Box(text.rectTransform,value?(page is ControllerPage?570:1206):8,5,value?100:(page is ControllerPage?320:656),page is ControllerPage?44:68);Size(text,value?18:page is ControllerPage?21:25);}
        }
        for(int i=0;i<actionButtons.Length;i++){Box((RectTransform)actionButtons[i].transform,810+i*300,642,276,54,page.transform);Stretch(actionButtons[i].GetComponentInChildren<TMP_Text>().rectTransform,8,3,8,3);Size(actionButtons[i].GetComponentInChildren<TMP_Text>(),22);}
        foreach(Transform child in page.transform)if(child!=content&&!actionButtons.Any(b=>b.transform==child))child.gameObject.SetActive(false);
        SaveSubView(c,(RectTransform)page.transform,"Page_Settings"+page.GetType().Name.Replace("Page",""));
    }
    static Transform OptionRow(Transform node,Transform page)
    {while(node.parent!=page&&node.parent!=null){if(node.parent.name.Contains("Content")||node.parent.GetComponent<LayoutGroup>()!=null)break;node=node.parent;}return node;}
    static void NormalizeKeyButtons(MonoBehaviour page)
    {
        // The legacy prefab placed Button directly on its TMP label. Give the
        // selectable its own background and child label so each rect has one job.
        foreach(var field in page.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.Name.EndsWith("KeyButton")))
        {
            var button=field.GetValue(page) as Button;if(button==null)continue;button.name="Button_"+field.Name.Replace("KeyButton","");
            var old=button.GetComponent<TMP_Text>();if(old==null)continue;
            string value=old.text;UnityEngine.Object.DestroyImmediate(old);
            var image=button.gameObject.GetComponent<Image>()??button.gameObject.AddComponent<Image>();image.color=ProgressionUIWidgets.Card;button.targetGraphic=image;
            var label=NewText(button.transform,"Text_Key",value,20);Stretch(label.rectTransform,8,3,8,3);label.alignment=TextAlignmentOptions.Center;
        }
    }
    static void EnsureControllerButtons(ViewContext c,MonoBehaviour page)
    {
        string[] fields={"forwardKeyButton","backwardKeyButton","toLeftKeyButton","toRightKeyButton","attackKeyButton","sprintKeyButton","rollKeyButton","jumpKeyButton","walkKeyButton","interactKeyButton","lockTargetKeyButton","guardKeyButton","nextTargetKeyButton","previousTargetKeyButton"};
        string[] labels={"向前","向后","向左","向右","普攻 / 蓄力","冲刺","闪避","跳跃","行走","交互","锁定目标","防御","下个目标","上个目标"};
        for(int i=0;i<fields.Length;i++)
        {
            var f=page.GetType().GetField(fields[i],BindingFlags.Instance|BindingFlags.NonPublic);if(f.GetValue(page)!=null)continue;
            var row=c.Group(page.transform,"Control_"+fields[i]);var label=c.Text(row,"Action",labels[i],22);Box(label,0,0,320,44);
            var button=c.Button(row,"Binding","按键");Box(button,350,0,320,44);Field(page,fields[i],button.GetComponent<Button>());
        }
    }
}
#endif
