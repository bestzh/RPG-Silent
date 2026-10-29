using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class ExpeditionAuthoring
{
    [MenuItem("Tools/RPG/Install Expedition Routes")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play Mode。");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Expeditions")) AssetDatabase.CreateFolder("Assets/Resources", "Expeditions");
        foreach (int id in new[] { 1001, 1002 })
        {
            string path = "Assets/Scenes/" + (id == 1001 ? "Dungeon01" : "Dungeon02") + ".unity";
            var scene = SceneManager.GetSceneByPath(path); bool loaded = scene.isLoaded;
            if (!loaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var filter = new NavMeshQueryFilter { agentTypeID = 1479372276, areaMask = NavMesh.AllAreas };
                Func<Vector3, Vector3> sample = v => NavMesh.SamplePosition(v, out var hit, 2.1f, filter) ? hit.position : throw new InvalidOperationException("无导航：" + v);
                var layout = ScriptableObject.CreateInstance<DungeonVariationDefinition>(); layout.DungeonId = id;
                layout.Melee = AssetDatabase.LoadAssetAtPath<GameObject>(id == 1001 ? "Assets/Combat/Enemies/Prefabs/MeleeGuard.prefab" : "Assets/Combat/CrystalCavern/Prefabs/CrystalLancer.prefab").GetComponent<EnemyController>();
                layout.Ranged = AssetDatabase.LoadAssetAtPath<GameObject>(id == 1001 ? "Assets/Combat/Enemies/Prefabs/RangedCaster.prefab" : "Assets/Combat/CrystalCavern/Prefabs/CrystalSeer.prefab").GetComponent<EnemyController>();
                layout.MarkerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Combat/Enemies/Materials/Warning.mat");
                layout.Route = (id == 1001 ? new[] { new Vector3(9, 1, -53), new Vector3(5, 1, -49), new Vector3(1, 3, -49), new Vector3(7, 1, -46), new Vector3(11, 1, -49) } :
                    new[] { new Vector3(0, .08f, 8), new Vector3(-3, .08f, 8), new Vector3(-5, .08f, 14), new Vector3(0, .08f, 14), new Vector3(4, .08f, 8) }).Select(sample).ToArray();
                layout.AmbushPoints = (id == 1001 ? new[] { new Vector3(3, 1, -47), new Vector3(11, 1, -49) } : new[] { new Vector3(-6, .08f, 16), new Vector3(6, .08f, 16) }).Select(sample).ToArray();
                layout.OptionalNodeIds = id == 1001 ? new[] { "Dungeon01-Chest-1", "Dungeon01-EliteEvent-0" } : new[] { "Dungeon02-Chest-2", "Dungeon02-EliteEvent-1" };
                var nodes = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ExplorationNode>(true)).ToArray();
                if (!layout.ValidateRoute(Resources.Load<DungeonDatabase>("DungeonDatabase").GetById(id), nodes, out var error)) throw new InvalidOperationException(error);
                string asset = "Assets/Resources/Expeditions/Dungeon" + id + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<DungeonVariationDefinition>(asset);
                if (existing == null) AssetDatabase.CreateAsset(layout, asset);
                else { EditorUtility.CopySerialized(layout, existing); EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing); UnityEngine.Object.DestroyImmediate(layout); }
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
