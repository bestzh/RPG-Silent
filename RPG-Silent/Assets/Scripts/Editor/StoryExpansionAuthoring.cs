using System;
using RPGSilent.Domain;
using UnityEditor;
using UnityEngine;

public static class StoryExpansionAuthoring
{
    [MenuItem("Tools/RPG/Install Story Expansion")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play Mode。");
        MarkChapter("main-arrival", "序章 · 初到静谧城");
        MarkChapter("main-forest", "第一章 · 林地危机");
        MarkChapter("main-crystal", "第二章 · 水晶深处");
        MarkChapter("main-town-defense", "第三章 · 城外威胁");
        Story("forest-afterglow", "林地余烬", TownNpcRole.Scout, "main-town-defense", 8, 100,
            new[] { "米拉把一枚焦黑的路标放在桌上。阴影领主已经倒下，它却仍在无风时轻响。‘城外暂时安全了，森林还没有睡去。’她想让你带着这段旅程的经验，再回旧石碑看看。",
                "旧日巡林人用石碑维系道路，也把危险的回声留在纹路里。修复路标能让商旅安心，研究回声或许能解释水晶洞窟的异动。两种办法都能帮助主城；你的决定只改变后续委托，共同主线仍可继续。" },
            Choice("restore", "修复巡林路标", "米拉请铁匠协助修复路标，开放‘巡林人的旧约’；‘回声勘测’不再开放。两条选择均可继续第四章。"),
            Choice("study", "勘测石碑回声", "米拉请你记录隐藏支路的回声，开放‘回声勘测’；‘巡林人的旧约’不再开放。两条选择均可继续第四章。"));
        Story("crystal-aftertone", "水晶后的来信", TownNpcRole.Steward, "main-forest-afterglow", 12, 100,
            new[] { "阿登收到一封从洞窟送回的信。守望者的碎片已经冷却，裂缝却仍会在入夜时亮起。铁匠认出那道光与森林路标上的细纹相同。城里的人需要的不是又一次捷报，而是一条能长期走下去的道路。",
                "可以先加固裂缝，保障巡路者的安全；也可以趁回声尚存，把最后一段古老记录带回来。阿登愿意尊重你的判断。决定只影响一条后续委托，不会封锁第五章与共同终章，也不能在放弃任务后改选。" },
            Choice("seal", "先加固裂缝", "阿登请铁匠准备晶核，开放‘裂隙的边界’；‘最后的共鸣’不再开放。共同主线仍可继续。"),
            Choice("listen", "先带回共鸣记录", "阿登请米拉协助搜集洞窟记录，开放‘最后的共鸣’；‘裂隙的边界’不再开放。共同主线仍可继续。"));
        Quest("main-forest-afterglow", "余烬回响", "第四章 · 余烬回响", QuestCategory.Main, TownNpcRole.Scout, 8, 50, "main-town-defense", 500, 800,
            "石碑仍在低鸣。以普通探索重访森林支路并通关，再向铁匠说明情况。先与米拉阅读‘林地余烬’并选择行动；两条分支均可推进本章。",
            new[] { Goal(QuestObjectiveKind.DiscoverSecret, "重访森林隐藏支路", 1001), Goal(QuestObjectiveKind.ClearDungeon, "清理森林余患并通关", 1001), Talk(TownNpcRole.Blacksmith, "向铁匠说明路标情况") }, story: "forest-afterglow");
        Quest("main-crystal-aftertone", "裂隙来信", "第五章 · 裂隙来信", QuestCategory.Main, TownNpcRole.Steward, 12, 60, "main-forest-afterglow", 800, 1300,
            "洞窟的光与石碑相连。通过洞窟首领试炼，带回晶核交给阿登，并向米拉核对记录。先阅读‘水晶后的来信’；决定不影响共同主线。",
            new[] { Goal(QuestObjectiveKind.CompleteBossTrial, "完成洞窟首领试炼", 1002), Material("crystal-shard", "交付晶核碎片", 3), Talk(TownNpcRole.Scout, "向米拉核对共鸣记录") }, story: "crystal-aftertone");
        Quest("main-city-oath", "静谧之约", "终章 · 静谧之约", QuestCategory.Main, TownNpcRole.Steward, 18, 70, "main-crystal-aftertone", 1200, 2000,
            "道路重新连起了森林、洞窟与市集。最后巡查两地，再听听罗莎对新商路的打算。阿登会把你的决定记入主城的约书；此前选过的分支不必重做。",
            new[] { Goal(QuestObjectiveKind.ClearDungeon, "巡查并通关森林", 1001), Goal(QuestObjectiveKind.ClearDungeon, "巡查并通关洞窟", 1002), Talk(TownNpcRole.Resident, "听取罗莎的商路计划") });
        Quest("branch-ward-repair", "巡林人的旧约", "", QuestCategory.Side, TownNpcRole.Scout, 8, 410, "main-town-defense", 340, 500,
            "你选择修复路标。替米拉准备锻造碎片，再请铁匠核对旧图纸。放弃只清空目标，修复路标的决定保留，不能借重接改选回声勘测。",
            new[] { Material("forge-shard", "交付锻造碎片", 6), Talk(TownNpcRole.Blacksmith, "向铁匠核对旧图纸") }, story: "forest-afterglow", choice: "restore");
        Quest("branch-echo-map", "回声勘测", "", QuestCategory.Side, TownNpcRole.Scout, 8, 411, "main-town-defense", 340, 500,
            "你选择勘测回声。以普通探索重访森林支路并击败一名精英，替米拉确认危险。另一条修复委托不会因放弃而重新开放。",
            new[] { Goal(QuestObjectiveKind.DiscoverSecret, "记录森林隐藏支路", 1001), Goal(QuestObjectiveKind.DefeatElite, "击败森林精英", 1001) }, story: "forest-afterglow", choice: "study");
        Quest("branch-crystal-seal", "裂隙的边界", "", QuestCategory.Side, TownNpcRole.Steward, 12, 420, "main-forest-afterglow", 500, 700,
            "你选择先加固裂缝。收集晶核碎片并向铁匠确认封边方案。此选择与‘最后的共鸣’互斥，放弃不会撤回已保存的决定。",
            new[] { Material("crystal-shard", "交付封边所需晶核", 4), Talk(TownNpcRole.Blacksmith, "确认裂隙封边方案") }, story: "crystal-aftertone", choice: "seal");
        Quest("branch-final-resonance", "最后的共鸣", "", QuestCategory.Side, TownNpcRole.Steward, 12, 421, "main-forest-afterglow", 500, 700,
            "你选择带回记录。以普通探索重访洞窟支路并检查两处补给箱；首领试炼不开放探索节点。此选择与加固委托互斥，共同主线仍可继续。",
            new[] { Goal(QuestObjectiveKind.DiscoverSecret, "重访洞窟隐藏支路", 1002), Goal(QuestObjectiveKind.OpenChest, "检查洞窟补给箱", 1002, 2) }, story: "crystal-aftertone", choice: "listen");
        Trial("swordsman", "剑锋的分寸", "swordsman", "career-swordsman", ProfessionTrialKind.PerfectParry, 1, "精准格挡", false);
        Trial("mage", "奥术的落点", "mage", "career-mage", ProfessionTrialKind.SkillHits, 4, "技能有效命中", false);
        Trial("ranger", "猎手的准星", "ranger", "career-ranger", ProfessionTrialKind.RangedHits, 6, "远程普攻有效命中", false);
        Trial("sword-saint", "剑圣 · 守势之证", "swordsman", "career-swordsman-advanced", ProfessionTrialKind.PerfectParry, 3, "精准格挡", true);
        Trial("berserker", "狂战士 · 破阵之证", "swordsman", "career-swordsman-advanced", ProfessionTrialKind.HeavyHits, 6, "蓄力重击有效命中", true);
        Trial("sharpshooter", "神射手 · 远望之证", "ranger", "career-ranger-advanced", ProfessionTrialKind.RangedHits, 12, "远程普攻有效命中", true);
        Trial("assassin", "刺客 · 无影之证", "ranger", "career-ranger-advanced", ProfessionTrialKind.PerfectDodge, 3, "精准闪避", true);
        Trial("shaman", "萨满 · 共鸣之证", "mage", "career-mage-advanced", ProfessionTrialKind.SkillHits, 10, "技能有效命中", true);
        Trial("beastmaster", "御兽师 · 同行之证", "mage", "career-mage-advanced", ProfessionTrialKind.CompanionHits, 10, "兽灵有效命中", true);
        Quest("commission-reclaimed-ore", "炉火回收委托", "", QuestCategory.Commission, TownNpcRole.Blacksmith, 6, 540, "side-forge", 200, 220,
            "铁匠需要稳定的回收材料来维护旧路。交付后增加好感 2；再通关一趟正式副本可刷新。好感 40／70 时，补给商品优惠 10%／15%，回购原价不变。",
            new[] { Material("forge-shard", "交付锻造碎片", 4), Material("crystal-shard", "交付晶核碎片", 1) }, repeat: true);
        Quest("commission-echo-survey", "深路勘测委托", "", QuestCategory.Commission, TownNpcRole.Scout, 8, 541, "main-forest-afterglow", 220, 320,
            "米拉只把两地图纸交给熟悉的同行者。重访两地隐藏支路，交付后仍需再通关一趟正式副本才刷新。好感条件只限制本委托，不封锁主线。",
            new[] { Goal(QuestObjectiveKind.DiscoverSecret, "勘测森林支路", 1001), Goal(QuestObjectiveKind.DiscoverSecret, "勘测洞窟支路", 1002) }, repeat: true, affinity: 20);
        Quest("commission-city-watch", "两地巡防委托", "", QuestCategory.Commission, TownNpcRole.Steward, 12, 542, "main-crystal-aftertone", 300, 400,
            "阿登信任与你合作的巡路者。分别通关森林与洞窟，再把巡防结果交回。交付增加好感 2；正式副本通关刷新，训练和长期挑战不刷新。",
            new[] { Goal(QuestObjectiveKind.ClearDungeon, "巡防森林并通关", 1001), Goal(QuestObjectiveKind.ClearDungeon, "巡防洞窟并通关", 1002) }, repeat: true, affinity: 30);
        Quest("commission-market-route", "商路备货委托", "", QuestCategory.Commission, TownNpcRole.Resident, 8, 543, "side-market", 240, 280,
            "罗莎愿意把远途订单交给熟悉的伙伴。清理洞窟沿路威胁并交付材料；交付后再通关一趟正式副本刷新。放弃不会重发历史奖励。",
            new[] { Goal(QuestObjectiveKind.DefeatEnemies, "清理洞窟沿路威胁", 1002, 6), Material("forge-shard", "交付备货材料", 4) }, repeat: true, affinity: 20);
    }
    static QuestObjective Goal(QuestObjectiveKind kind, string label, int dungeon, int count = 1) => new() { Kind = kind, Label = label, DungeonId = dungeon, Required = count };
    static QuestObjective Talk(TownNpcRole role, string label) => new() { Kind = QuestObjectiveKind.TalkToNpc, Label = label, NpcRole = role };
    static QuestObjective Material(string id, string label, int count) => new() { Kind = QuestObjectiveKind.DeliverMaterial, ItemId = id, Label = label, Required = count };
    static TownDialogueChoice Choice(string id, string label, string outcome) => new() { Id = id, Label = label, Outcome = outcome, Gold = 30, Experience = 40, Affinity = 10 };
    static void Trial(string id, string title, string family, string previous, ProfessionTrialKind kind, int count, string action, bool advanced)
    {
        Quest("trial-" + id, title, "", QuestCategory.Profession, TownNpcRole.Mentor, advanced ? 12 : 6, advanced ? 330 : 320, previous, advanced ? 800 : 350, advanced ? 1400 : 600,
            $"在同一趟{(advanced ? "洞窟" : "森林")}完成 {count} 次{action}并成功通关。死亡、退出或读档需重新挑战；放弃可重接，转职与导师学习条件保留。",
            new[] { new QuestObjective { Kind = QuestObjectiveKind.ProfessionTrial, Label = $"同一趟{action} ×{count}，并成功通关", DungeonId = advanced ? 1002 : 1001, Trial = kind, TrialRequired = count } }, family: family, exactClass: advanced ? id : "");
    }
    static void Quest(string id, string title, string chapter, QuestCategory category, TownNpcRole giver, int level, int order, string previous, int gold, int exp, string description,
        QuestObjective[] objectives, string story = "", string choice = "", string family = "", string exactClass = "", bool repeat = false, int affinity = 0)
    {
        var q = ScriptableObject.CreateInstance<QuestDefinition>(); q.Id = id; q.Title = title; q.Chapter = chapter; q.Description = description;
        q.Category = category; q.Giver = giver; q.RequiredLevel = level; q.Order = order; q.PrerequisiteId = previous; q.RewardGold = gold; q.RewardExp = exp;
        q.Objectives = objectives; q.RequiredStoryId = story; q.RequiredChoiceId = choice; q.FamilyId = family; q.RequiredClassId = exactClass; q.Repeatable = repeat; q.RequiredAffinity = affinity;
        Write("Assets/Resources/Quests/" + id + ".asset", q);
    }
    static void Story(string id, string title, TownNpcRole role, string previous, int level, int order, string[] pages, params TownDialogueChoice[] choices)
    {
        var s = ScriptableObject.CreateInstance<TownDialogueDefinition>(); s.Id = id; s.Title = title; s.Role = role; s.RequiredQuestId = previous;
        s.RequiredLevel = level; s.Order = order; s.Pages = pages; s.Choices = choices; Write("Assets/Resources/Adventure/Story-" + id + ".asset", s);
    }
    static void Write<T>(string path, T data) where T : ScriptableObject
    {
        data.name = System.IO.Path.GetFileNameWithoutExtension(path);
        var old = AssetDatabase.LoadAssetAtPath<T>(path);
        if (old == null) AssetDatabase.CreateAsset(data, path);
        else { EditorUtility.CopySerialized(data, old); EditorUtility.SetDirty(old); AssetDatabase.SaveAssetIfDirty(old); UnityEngine.Object.DestroyImmediate(data); }
    }
    static void MarkChapter(string id, string label)
    {
        var q = AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Resources/Quests/" + id + ".asset");
        if (q == null) throw new InvalidOperationException("缺少原主线任务：" + id);
        q.Chapter = label; EditorUtility.SetDirty(q); AssetDatabase.SaveAssetIfDirty(q);
    }
}
