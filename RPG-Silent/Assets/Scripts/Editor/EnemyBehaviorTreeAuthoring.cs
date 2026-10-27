using System;
using System.Collections.Generic;
using System.Linq;
using Opsive.BehaviorDesigner.Runtime;
using Opsive.BehaviorDesigner.Runtime.Components;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.BehaviorDesigner.Runtime.Tasks.Composites;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.BehaviorDesigner.Runtime.Tasks.Decorators;
using Opsive.GraphDesigner.Runtime;
using RPGSilent.Enemies.BehaviorDesigner;
using UnityEditor;
using UnityEngine;

/// <summary>首次创建真实的 Pro Subtree 资产并接到预制体。重复运行保留用户编辑过的树。</summary>
public static class EnemyBehaviorTreeAuthoring
{
    public const string Root = "Assets/Combat/Enemies/BehaviorTrees";

    [MenuItem("Tools/RPG/Install Enemy Behavior Designer Trees")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。");
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/Combat/Enemies", "BehaviorTrees");
        var melee = Create("Melee", false, false);
        var ranged = Create("Ranged", true, false);
        var boss = Create("Boss", false, true);
        Wire("Assets/Resources/Enemy.prefab", melee);
        Wire("Assets/Combat/Enemies/Prefabs/MeleeGuard.prefab", melee);
        Wire("Assets/Combat/Enemies/Prefabs/RangedCaster.prefab", ranged);
        Wire("Assets/Combat/Enemies/Prefabs/ShadowLord.prefab", boss);
        AssetDatabase.SaveAssets();
        Debug.Log("[Enemy BT] 三份 Behavior Designer Pro 行为树及四个敌人预制体已接入。");
    }

    private sealed class Branch
    {
        public ITreeLogicNode Node;
        public string Name;
        public Branch[] Children;
        public Vector2 Position;
        public Branch(ITreeLogicNode node, string name, params Branch[] children)
        { Node = node; Name = name; Children = children; }
    }

    private static Branch Action(Task task, string name) =>
        new(new StackedAction { Tasks = new[] { task } }, name);
    private static Branch Condition(Conditional task, string name) =>
        new(new StackedConditional { Tasks = new Task[] { task } }, name);

    private static Subtree Create(string name, bool retreats, bool isBoss)
    {
        string path = Root + "/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Subtree>(path);
        if (existing != null) return existing;

        var combat = new List<Branch>();
        if (isBoss) combat.Add(Action(new TryEnemyAttackSequence(), "第二阶段：连斩与终结重击"));
        if (retreats) combat.Add(new Branch(new Sequence(), "拉开距离",
            Condition(new ShouldEnemyRetreat(), "玩家太近？"), Action(new RetreatFromEnemyTarget(), "撤退 / 寻找侧后方路径")));
        if (!isBoss) combat.Add(Action(new CoordinateEnemyPosition(), "小队：包围站位 / 寻找射击空隙"));
        combat.Add(Action(new TryEnemyAttack(), "按射程、冷却与阶段选招"));
        combat.Add(Action(new ApproachEnemyTarget(), "追击 / 保持攻击距离"));
        var priorities = new List<Branch>
        {
            new(new Sequence(), "脱战归位", Condition(new IsReturningHome(), "需要回家？"), Action(new ReturnEnemyHome(), "返回出生点"))
        };
        if (isBoss) priorities.Add(Action(new TryBossPhaseTransition(), "半血：进入第二阶段"));
        priorities.Add(new Branch(new Sequence(), "与玩家战斗", Condition(new HasEnemyAggro(), "已发现玩家？"),
            new Branch(new Selector(), "战斗优先级", combat.ToArray())));
        priorities.Add(Action(new IdleEnemy(), "待机"));
        var root = new Branch(new Repeater { RepeatForever = true }, "持续决策",
            new Branch(new Sequence(), "一轮决策",
                Action(new WaitForEnemyDecision(), "等待动作完成及 0.15 秒节拍"),
                new Branch(new Selector(), "敌人优先级", priorities.ToArray())));

        var nodes = new List<ITreeLogicNode>();
        var branches = new List<Branch>();
        Flatten(root, ushort.MaxValue, nodes, branches);
        float nextX = 0;
        Layout(root, 0, ref nextX);
        var graph = ScriptableObject.CreateInstance<Opsive.BehaviorDesigner.Runtime.Wrappers.Subtree>();
        graph.Name = name + " Enemy";
        graph.Pooled = false;
        graph.TreeLogicNodes = nodes.ToArray();
        graph.EventNodes = new IEventNode[] { new Opsive.BehaviorDesigner.Runtime.Tasks.Events.Start { Index = 0, ConnectedIndex = 0, Enabled = true } };
        graph.LogicNodeProperties = branches.Select(b =>
        {
            var stacked = b.Node as StackedTask;
            var data = new LogicNodeProperties.NodeData
            {
                ParentIndex = b.Node.ParentIndex, SiblingIndex = b.Node.SiblingIndex,
                IsParent = b.Node is IParentNode,
                ContainedNodeTypes = stacked?.Tasks.Select(t => t.GetType().FullName).ToArray(),
                ContainedNodeNames = stacked == null ? null : new[] { b.Name }
            };
            var properties = new LogicNodeProperties(Guid.NewGuid(), b.Position, 240, data, b.Name, null);
            properties.DisplayName = b.Name;
            return properties;
        }).ToArray();
        graph.EventNodeProperties = new[] { new NodeProperties(Guid.NewGuid(), new Vector2(root.Position.x, -150), 240) };
        graph.Serialize();
        AssetDatabase.CreateAsset(graph, path);
        EditorUtility.SetDirty(graph);
        return graph;
    }

    private static ushort Flatten(Branch branch, ushort parent, List<ITreeLogicNode> nodes, List<Branch> branches)
    {
        ushort index = (ushort)nodes.Count;
        branch.Node.Index = index;
        branch.Node.ParentIndex = parent;
        branch.Node.SiblingIndex = ushort.MaxValue;
        branch.Node.Enabled = true;
        nodes.Add(branch.Node);
        branches.Add(branch);
        ushort previous = ushort.MaxValue;
        foreach (var child in branch.Children)
        {
            ushort childIndex = Flatten(child, index, nodes, branches);
            if (previous != ushort.MaxValue) nodes[previous].SiblingIndex = childIndex;
            previous = childIndex;
        }
        return index;
    }

    private static void Layout(Branch branch, int depth, ref float nextX)
    {
        if (branch.Children.Length == 0) { branch.Position = new Vector2(nextX, depth * 170); nextX += 285; return; }
        foreach (var child in branch.Children) Layout(child, depth + 1, ref nextX);
        branch.Position = new Vector2((branch.Children[0].Position.x + branch.Children[branch.Children.Length - 1].Position.x) * .5f, depth * 170);
    }

    private static void Wire(string path, Subtree graph)
    {
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var tree = prefab.GetComponent<BehaviorTree>();
            if (tree == null) tree = prefab.AddComponent<Opsive.BehaviorDesigner.Runtime.Wrappers.BehaviorTree>();
            tree.StartWhenEnabled = false;
            tree.PauseWhenDisabled = false;
            tree.UpdateMode = UpdateMode.EveryFrame;
            tree.EvaluationType = EvaluationType.EntireTree;
            tree.Subgraph = graph;
            tree.Serialize();
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
}
