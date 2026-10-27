using System;
using System.Linq;
using Opsive.BehaviorDesigner.Runtime;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime;
using RPGSilent.Enemies.BehaviorDesigner;
using UnityEditor;
using UnityEngine;

/// <summary>只追加协作叶节点；不重建用户已经编辑的树。</summary>
public static class EnemyGroupAuthoring
{
    [MenuItem("Tools/RPG/Install Enemy Group Coordination")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。");
        InsertPositionTask("Melee");
        InsertPositionTask("Ranged");
        AssetDatabase.SaveAssets();
    }

    private static void InsertPositionTask(string name)
    {
        var graph = AssetDatabase.LoadAssetAtPath<Subtree>(EnemyBehaviorTreeAuthoring.Root + "/" + name + ".asset");
        if (graph == null) throw new InvalidOperationException("缺少行为树 " + name);
        graph.Deserialize(true);
        var nodes = graph.TreeLogicNodes.ToList();
        if (nodes.OfType<StackedTask>().Any(t => t.Tasks.Any(a => a is CoordinateEnemyPosition))) return;
        int index = nodes.FindIndex(n => n is StackedTask s && s.Tasks.Any(t => t is TryEnemyAttack));
        if (index < 0) throw new InvalidOperationException(name + " 树缺少选招节点。");
        Undo.RecordObject(graph, "Add enemy group positioning");
        ushort parent = nodes[index].ParentIndex;
        var properties = graph.LogicNodeProperties.ToList();
        Vector2 position = properties[index].Position;
        foreach (var node in nodes)
        {
            if (node.Index >= index) node.Index++;
            if (node.ParentIndex != ushort.MaxValue && node.ParentIndex >= index) node.ParentIndex++;
            if (node.SiblingIndex != ushort.MaxValue && node.SiblingIndex > index) node.SiblingIndex++;
        }
        nodes.Insert(index, new StackedAction { Index = (ushort)index, ParentIndex = parent, SiblingIndex = (ushort)(index + 1),
            Tasks = new Task[] { new CoordinateEnemyPosition() }, Enabled = true });
        foreach (var property in properties) if (property.Position.x >= position.x) property.Position += Vector2.right * 285f;
        const string label = "小队：包围站位 / 寻找射击空隙";
        var data = new LogicNodeProperties.NodeData { ParentIndex = parent, SiblingIndex = (ushort)(index + 1),
            ContainedNodeTypes = new[] { typeof(CoordinateEnemyPosition).FullName }, ContainedNodeNames = new[] { label } };
        properties.Insert(index, new LogicNodeProperties(Guid.NewGuid(), position, 240, data, label, null) { DisplayName = label });
        for (int i = 0; i < nodes.Count; i++)
        {
            var updated = properties[i].Data;
            updated.ParentIndex = nodes[i].ParentIndex;
            updated.SiblingIndex = nodes[i].SiblingIndex;
            properties[i].Data = updated;
        }
        if (graph.DisabledLogicNodes != null)
            graph.DisabledLogicNodes = graph.DisabledLogicNodes.Select(i => i >= index ? (ushort)(i + 1) : i).ToArray();
        foreach (var evt in graph.EventNodes)
            if (evt.ConnectedIndex != ushort.MaxValue && evt.ConnectedIndex >= index) evt.ConnectedIndex++;
        graph.TreeLogicNodes = nodes.ToArray();
        graph.LogicNodeProperties = properties.ToArray();
        graph.Serialize();
        EditorUtility.SetDirty(graph);
    }
}
