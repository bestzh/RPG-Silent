using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    TMP_Text hpNumbers, energyNumbers, passiveFeedback, trainingText, tutorialText;
    RectTransform trainingPanel, tutorialPanel, mapOverlay;
    RectTransform playerMapArrow;
    Button tutorialButton;
    readonly TMP_Text[] mapMarkers = new TMP_Text[40];
    float nextAdventureRefresh;
    bool tutorialVisible;
    void BuildAdventureHud()
    {
        if (classNameText == null || tutorialButton != null) return;
        var font = classNameText.font;
        if (hpBar != null)
        {
            hpNumbers = W.Text(hpBar.transform, font, "HealthNumbers", "", 0, 0, 240, 28, 18);
            Anchor(hpNumbers.rectTransform, new Vector2(.5f,.5f), Vector2.zero); hpNumbers.alignment = TextAlignmentOptions.Center;
        }
        if (mpBar != null)
        {
            energyNumbers = W.Text(mpBar.transform, font, "EnergyNumbers", "", 0, 0, 240, 28, 18);
            Anchor(energyNumbers.rectTransform, new Vector2(.5f,.5f), Vector2.zero); energyNumbers.alignment = TextAlignmentOptions.Center;
        }
        var root = transform.Find("TopLeftPanel");
        tutorialButton = W.Button(root,font,"CombatGuide","操作 / 职业被动",0,0,190,40,() => tutorialVisible = !tutorialVisible);
        var rect = tutorialButton.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(1,1); rect.anchoredPosition = new Vector2(-220,-624);
        tutorialPanel = W.Rect(root,"CombatTutorial",0,0,550,425);
        tutorialPanel.anchorMin = tutorialPanel.anchorMax = new Vector2(1,1); tutorialPanel.anchoredPosition = new Vector2(-970,-350);
        tutorialPanel.gameObject.AddComponent<Image>().color = W.Ink;
        tutorialText = W.Text(tutorialPanel,font,"Instructions","",18,14,514,395,20);
        trainingPanel = W.Rect(root,"TrainingPractice",650,105,620,220);
        trainingPanel.gameObject.AddComponent<Image>().color = W.Ink;
        trainingText = W.Text(trainingPanel,font,"PracticeStats","",14,12,592,162,21);
        W.Button(trainingPanel,font,"ResetPractice","重置练习",14,180,150,36,() => combatPlayer?.Training?.Activate(ExplorationKind.TrainingReset));
        W.Button(trainingPanel,font,"TrainingReturn","返回主城",178,180,150,36,() => combatPlayer?.Training?.ReturnToTown());
        passiveFeedback = W.Text(root,font,"ClassPassiveFeedback","",0,0,640,30,22);
        Anchor(passiveFeedback.rectTransform,new Vector2(.5f,.39f),Vector2.zero); passiveFeedback.alignment=TextAlignmentOptions.Center;
        passiveFeedback.color=new Color(.6f,.9f,1);
        if (miniMap != null)
        {
            mapOverlay=W.Panel(miniMap.transform,"ObjectiveMap",new Color(.04f,.08f,.10f,.9f)); mapOverlay.GetComponent<Image>().raycastTarget=false;
            var heading=W.Text(mapOverlay,font,"MapLegend","北 ↑ · 45m\n箱 宝箱  机 机关  ! 精英",8,5,220,44,15);
            var center=W.Text(mapOverlay,font,"PlayerMarker","▲",0,0,22,22,18); Anchor(center.rectTransform,new Vector2(.5f,.5f),Vector2.zero);
            playerMapArrow = center.rectTransform;
            for(int i=0;i<mapMarkers.Length;i++)
            {
                mapMarkers[i]=W.Text(mapOverlay,font,"Marker"+i,"",0,0,24,24,17);
                Anchor(mapMarkers[i].rectTransform,new Vector2(.5f,.5f),Vector2.zero);
                mapMarkers[i].alignment=TextAlignmentOptions.Center;
            }
        }
    }
    void RefreshAdventureHud()
    {
        if (tutorialButton == null || Time.unscaledTime < nextAdventureRefresh) return;
        nextAdventureRefresh=Time.unscaledTime+.1f;
        bool show=_gameSettings?.CurrentSettings.ShowHud ?? true;
        tutorialButton.gameObject.SetActive(show); tutorialPanel.gameObject.SetActive(show && tutorialVisible);
        if(hpNumbers!=null) hpNumbers.text=$"{_stats.CurrentHealth} / {_stats.MaxHealth}";
        if(energyNumbers!=null) energyNumbers.text=$"{_stats.CurrentEnergy:0} / {_stats.MaxEnergy:0}";
        passiveFeedback.gameObject.SetActive(show && combatPlayer != null && combatPlayer.CombatAllowed);
        passiveFeedback.text=combatPlayer?.ClassCombat?.Feedback ?? "";
        var training=combatPlayer?.Training;
        trainingPanel.gameObject.SetActive(show && training!=null);
        if(training!=null && training.Record!=null)
        {
            var r=training.Record;
            trainingText.text=$"训练场 · {training.Mode}  |  每 5 秒恢复生命 / 能量\n输出 {r.DamageDealt}  承伤 {r.DamageTaken}  连段末击 {r.ComboFinishers}  技能 {r.Skills}\n精准格挡 {r.PerfectParries}  精准闪避 {r.PerfectDodges}\n{training.Message}";
        }
        if(tutorialVisible && combatInput!=null)
        {
            string Key(UnityEngine.InputSystem.InputAction action) => combatInput.GetDisplayString(action,0);
            tutorialText.text=$"操作教学 · 按 Ctrl 显示鼠标操作界面\n移动 WASD / 鼠标转身 · 普攻 [{Key(combatInput.Attack)}]\n连续点击普攻接连段，技能键 {combatInput.GetSkillSlotDisplay(0)}–{combatInput.GetSkillSlotDisplay(9)} 可衔接技能。\n闪避 [轻按 {Key(combatInput.Roll)}] / 防御 [{Key(combatInput.Guard)}]\n在攻击命中前瞬间防御或闪避，可触发精准判定。\n锁定 [{Key(combatInput.LockTarget)}] / 切武器 [{Key(combatInput.StanceToggle)}]\n交互 [{Key(combatInput.Interact)}] · B 背包 · J 手札\n" + (combatPlayer?.ClassCombat?.PassiveDescription ?? "") +
                (_progress?.CurrentClass?.Id=="beastmaster" ? "\n兽灵召唤：导师学习兽灵技能，同一时间保留一只，离场或死亡后消失。" : "") +
                (combatPlayer!=null && !combatPlayer.CombatAllowed ? "\n主城为安全区；从传送门可进入训练场。" : "");
        }
        if(mapOverlay==null || combatPlayer==null) return;
        playerMapArrow.localRotation=Quaternion.Euler(0,0,-combatPlayer.transform.eulerAngles.y);
        int index=0;
        AddTrackedMapMarker(ref index);
        foreach(var node in ExplorationNode.Active)
        { node.Discover(combatPlayer.transform.position); if(node.VisibleOnMap) AddMapMarker(ref index,node.transform.position,node.Symbol,new Color(1,.83f,.3f)); }
        foreach(var enemy in EnemyController.ActiveEnemies)
            if(enemy!=null && !enemy.IsDead && !enemy.CombatLocked) AddMapMarker(ref index,enemy.transform.position,enemy.IsCompanion ? "+" : "•",enemy.IsCompanion ? Color.green : new Color(1,.35f,.3f));
        foreach(var npc in TownNpc.Active)
            if(npc!=null) AddMapMarker(ref index,npc.transform.position,string.IsNullOrEmpty(npc.QuestMarker) ? npc.MapSymbol : npc.QuestMarker,new Color(.5f,1,1));
        foreach(var portal in PortalTrigger.Active)
            if(portal!=null) AddMapMarker(ref index,portal.transform.position,"门",new Color(.4f,.75f,1));
        foreach(var drop in WorldLootPickup.Active)
            if(drop!=null && drop.Item!=null && !drop.IsCollected) AddMapMarker(ref index,drop.transform.position,"物",drop.QualityColor);
        for(int i=index;i<mapMarkers.Length;i++) mapMarkers[i].gameObject.SetActive(false);
    }
    void AddMapMarker(ref int index, Vector3 position, string symbol, Color color)
    {
        if(index>=mapMarkers.Length) return;
        Vector3 delta=position-combatPlayer.transform.position;
        if(delta.x*delta.x+delta.z*delta.z>45*45) return;
        var marker=mapMarkers[index++]; marker.gameObject.SetActive(true); marker.text=symbol; marker.color=color;
        Vector2 half=mapOverlay.rect.size*.38f;
        marker.rectTransform.anchoredPosition=new Vector2(delta.x/45*half.x,delta.z/45*half.y);
    }
}
