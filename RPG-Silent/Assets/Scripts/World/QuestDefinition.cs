using System;
using RPGSilent.Domain;
using UnityEngine;

public enum QuestCategory { Main, Side, Profession }
public enum QuestObjectiveKind { DefeatEnemies, ClearDungeon, DeliverMaterial, LearnSkills, ReachLevel, AdvanceClass }
public enum QuestStatus { Locked, Available, Active, Ready, Rewarded }

[Serializable]
public sealed class QuestObjective
{
    public QuestObjectiveKind Kind;
    public string Label;
    public int DungeonId;
    public string ItemId = string.Empty;
    [Range(1, 9999)] public int Required = 1;
    public bool IsCounter => Kind == QuestObjectiveKind.DefeatEnemies || Kind == QuestObjectiveKind.ClearDungeon;
}

[CreateAssetMenu(menuName = "RPG/Quest")]
public sealed class QuestDefinition : ScriptableObject
{
    public string Id;
    public string Title;
    [TextArea] public string Description;
    public QuestCategory Category;
    public TownNpcRole Giver;
    public string FamilyId = string.Empty;
    public string PrerequisiteId = string.Empty;
    [Range(1, 50)] public int RequiredLevel = 1;
    public int Order;
    public QuestObjective[] Objectives = Array.Empty<QuestObjective>();
    [Min(0)] public int RewardGold;
    [Min(0)] public int RewardExp;
    public ItemStackData[] Rewards = Array.Empty<ItemStackData>();
    public string CategoryLabel => Category == QuestCategory.Main ? "主线" : Category == QuestCategory.Side ? "支线" : "职业";
}
