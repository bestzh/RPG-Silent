using System;
using RPGSilent.Domain;
using UnityEngine;

// Append values: existing assets and saved objective arrays retain their meaning.
public enum QuestCategory { Main, Side, Profession, Special, Commission }
public enum QuestObjectiveKind { DefeatEnemies, ClearDungeon, DeliverMaterial, LearnSkills, ReachLevel, AdvanceClass,
    TalkToNpc, DiscoverSecret, DefeatElite, CompleteTimed, CompleteBossTrial, OpenChest, ProfessionTrial }
public enum QuestStatus { Locked, Available, Active, Ready, Rewarded, Abandoned }
public enum ProfessionTrialKind { None, PerfectParry, SkillHits, RangedHits, HeavyHits, PerfectDodge, CompanionHits }

[Serializable]
public sealed class QuestObjective
{
    public QuestObjectiveKind Kind;
    public string Label;
    public int DungeonId;
    public string ItemId = string.Empty;
    public TownNpcRole NpcRole;
    [Range(1, 9999)] public int Required = 1;
    public ProfessionTrialKind Trial;
    [Range(1, 9999)] public int TrialRequired = 1;
    public bool IsCounter => Kind == QuestObjectiveKind.DefeatEnemies || Kind == QuestObjectiveKind.ClearDungeon || Kind >= QuestObjectiveKind.TalkToNpc;
    public bool IsDungeonCounter => IsCounter && Kind != QuestObjectiveKind.TalkToNpc;
    public bool MatchesEvent(QuestObjectiveKind kind, int dungeonId, TownNpcRole npcRole) => IsCounter && Kind != QuestObjectiveKind.ProfessionTrial && Kind == kind &&
        (Kind == QuestObjectiveKind.TalkToNpc ? NpcRole == npcRole : DungeonId == dungeonId);
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
    public string RequiredClassId = string.Empty;
    public string Chapter = string.Empty;
    public string PrerequisiteId = string.Empty;
    [Range(1, 50)] public int RequiredLevel = 1;
    public int Order;
    public bool Repeatable;
    [Range(0, 100)] public int RequiredAffinity;
    public string RequiredStoryId = string.Empty;
    public string RequiredChoiceId = string.Empty;
    public QuestObjective[] Objectives = Array.Empty<QuestObjective>();
    [Min(0)] public int RewardGold;
    [Min(0)] public int RewardExp;
    public ItemStackData[] Rewards = Array.Empty<ItemStackData>();
    public string CategoryLabel => Category switch { QuestCategory.Main => "主线", QuestCategory.Side => "支线", QuestCategory.Special => "特殊", QuestCategory.Commission => "委托", _ => "职业" };
}
