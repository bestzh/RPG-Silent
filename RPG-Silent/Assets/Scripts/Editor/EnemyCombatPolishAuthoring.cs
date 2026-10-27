using System;
using System.Linq;
using Opsive.BehaviorDesigner.Runtime;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime;
using RPGSilent.Enemies.BehaviorDesigner;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>为现有演示追加连招资源，保留原资产 GUID 及用户已编辑的行为树分支。</summary>
public static class EnemyCombatPolishAuthoring
{
    private const string Root = "Assets/Combat/Enemies/";

    [MenuItem("Tools/RPG/Apply Enemy Combat Polish")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出运行模式。");
        var cleave = Load<EnemySkillData>("Skills/BossCleave.asset");
        var quake = Load<EnemySkillData>("Skills/BossQuake.asset");
        var first = ComboSkill(cleave, "BossComboSlashA", "狂怒连斩·一式", 1.15f, .48f, 14, "ComboSlashA");
        var second = ComboSkill(cleave, "BossComboSlashB", "狂怒连斩·二式", 1.25f, .5f, 16, "ComboSlashB");
        var finisher = ComboSkill(quake, "BossComboFinisher", "狂怒连斩·碎地", 1.5f, .65f, 22, "ComboFinisher");
        string sequencePath = Root + "Skills/BossEnragedCombo.asset";
        var sequence = AssetDatabase.LoadAssetAtPath<EnemyAttackSequence>(sequencePath);
        if (sequence == null)
        {
            sequence = ScriptableObject.CreateInstance<EnemyAttackSequence>();
            sequence.DisplayName = "狂怒三连击";
            sequence.Steps = new[] { first, second, finisher };
            sequence.RequiredPhase = 2;
            sequence.Cooldown = 10f;
            sequence.BetweenAttacks = .3f;
            sequence.RecoveryDuration = 1.8f;
            AssetDatabase.CreateAsset(sequence, sequencePath);
        }
        var boss = Load<EnemyDefinition>("Data/Boss.asset");
        Undo.RecordObject(boss, "Configure boss combo");
        if (boss.AttackSequences == null || !boss.AttackSequences.Contains(sequence))
            boss.AttackSequences = (boss.AttackSequences ?? Array.Empty<EnemyAttackSequence>()).Append(sequence).ToArray();
        boss.AttackFacingAngle = 20f;
        boss.RecoveryDamageMultiplier = 1.25f;
        EditorUtility.SetDirty(boss);
        SetRecovery("MeleeSlash", .45f);
        SetRecovery("RangedBolt", .55f);
        SetRecovery("BossCleave", .75f);
        SetRecovery("BossQuake", 1.1f);
        SetRecovery("BossMeteor", 1.25f);

        var controller = Load<AnimatorController>("Animations/Boss.controller");
        AddAnimation(controller, first, "Attack");
        AddAnimation(controller, second, "Attack");
        AddAnimation(controller, finisher, "Quake");
        AddSequenceNode();
        AssetDatabase.SaveAssets();
        Debug.Log("[Enemy Polish] 已配置狂怒连招、动作恢复与 Boss 反击窗口。");
    }

    private static T Load<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(Root + path) ?? throw new InvalidOperationException("缺少资源 " + Root + path);

    private static EnemySkillData ComboSkill(EnemySkillData source, string id, string displayName,
        float duration, float releaseTime, int damage, string animation)
    {
        string path = Root + "Skills/" + id + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<EnemySkillData>(path);
        if (existing != null) return existing;
        var skill = UnityEngine.Object.Instantiate(source);
        skill.DisplayName = displayName;
        skill.Duration = duration;
        skill.ReleaseTime = releaseTime;
        skill.RequiredPhase = 2;
        skill.AnimationState = animation;
        skill.SuperArmor = true;
        skill.Cooldown = 0f; // 整套连招使用实例级的独立冷却。
        skill.Attack = UnityEngine.Object.Instantiate(source.Attack);
        skill.Attack.Damage = damage;
        AssetDatabase.CreateAsset(skill.Attack, Root + "Skills/" + id + "Attack.asset");
        AssetDatabase.CreateAsset(skill, path);
        return skill;
    }

    private static void SetRecovery(string id, float duration)
    {
        var skill = Load<EnemySkillData>("Skills/" + id + ".asset");
        Undo.RecordObject(skill, "Set enemy recovery");
        skill.RecoveryDuration = duration;
        EditorUtility.SetDirty(skill);
    }

    private static void AddAnimation(AnimatorController controller, EnemySkillData skill, string source)
    {
        var machine = controller.layers[0].stateMachine;
        if (machine.states.Any(s => s.state.name == skill.AnimationState)) return;
        Undo.RecordObject(machine, "Add combo animation");
        var state = machine.AddState(skill.AnimationState);
        state.motion = machine.states.First(s => s.state.name == source).state.motion;
        var clip = state.motion as AnimationClip;
        state.speed = clip != null ? clip.length / skill.Duration : 1f;
        EditorUtility.SetDirty(controller);
    }

    private static void AddSequenceNode()
    {
        var graph = Load<Subtree>("BehaviorTrees/Boss.asset");
        graph.Deserialize(true);
        var nodes = graph.TreeLogicNodes.ToList();
        if (nodes.OfType<StackedTask>().Any(t => t.Tasks.Any(a => a is TryEnemyAttackSequence))) return;
        int index = nodes.FindIndex(n => n is StackedTask s && s.Tasks.Any(t => t is TryEnemyAttack));
        if (index < 0) throw new InvalidOperationException("Boss 树未找到选招节点，无法确定连招插入位置。");
        Undo.RecordObject(graph, "Add boss combo branch");
        ushort parent = nodes[index].ParentIndex;
        var properties = graph.LogicNodeProperties.ToList();
        Vector2 position = properties[index].Position;
        foreach (var node in nodes)
        {
            if (node.Index >= index) node.Index++;
            if (node.ParentIndex != ushort.MaxValue && node.ParentIndex >= index) node.ParentIndex++;
            // 指向原选招节点的兄弟连接应进入新连招节点；其余索引照常顺移。
            if (node.SiblingIndex != ushort.MaxValue && node.SiblingIndex > index) node.SiblingIndex++;
        }
        var added = new StackedAction { Index = (ushort)index, ParentIndex = parent, SiblingIndex = (ushort)(index + 1),
            Tasks = new Task[] { new TryEnemyAttackSequence() }, Enabled = true };
        nodes.Insert(index, added);
        foreach (var property in properties) if (property.Position.x >= position.x) property.Position += Vector2.right * 285f;
        var data = new LogicNodeProperties.NodeData { ParentIndex = parent, SiblingIndex = (ushort)(index + 1),
            ContainedNodeTypes = new[] { typeof(TryEnemyAttackSequence).FullName }, ContainedNodeNames = new[] { "第二阶段：连斩与终结重击" } };
        var inserted = new LogicNodeProperties(Guid.NewGuid(), position, 240, data, "", null) { DisplayName = "第二阶段：连斩与终结重击" };
        properties.Insert(index, inserted);
        for (int i = 0; i < nodes.Count; i++)
        {
            var updated = properties[i].Data;
            updated.ParentIndex = nodes[i].ParentIndex;
            updated.SiblingIndex = nodes[i].SiblingIndex;
            properties[i].Data = updated;
        }
        if (graph.DisabledLogicNodes != null)
            graph.DisabledLogicNodes = graph.DisabledLogicNodes.Select(i => i >= index ? (ushort)(i + 1) : i).ToArray();
        graph.TreeLogicNodes = nodes.ToArray();
        graph.LogicNodeProperties = properties.ToArray();
        graph.Serialize();
        EditorUtility.SetDirty(graph);
    }
}
