using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class LongChallengeAuthoring
{
    [MenuItem("Tools/RPG/Install Long Challenges")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play Mode。");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Challenges")) AssetDatabase.CreateFolder("Assets/Resources", "Challenges");
        foreach (int dungeon in new[] { 1001, 1002 })
        {
            string scenePath = "Assets/Scenes/" + (dungeon == 1001 ? "Dungeon01" : "Dungeon02") + ".unity";
            var scene = SceneManager.GetSceneByPath(scenePath); bool loaded = scene.isLoaded;
            if (!loaded) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var arena = ScriptableObject.CreateInstance<LongChallengeArena>(); arena.DungeonId = dungeon;
                string folder = dungeon == 1001 ? "Assets/Combat/Enemies/Prefabs/" : "Assets/Combat/CrystalCavern/Prefabs/";
                arena.Melee = AssetDatabase.LoadAssetAtPath<GameObject>(folder + (dungeon == 1001 ? "MeleeGuard" : "CrystalLancer") + ".prefab").GetComponent<EnemyController>();
                arena.Ranged = AssetDatabase.LoadAssetAtPath<GameObject>(folder + (dungeon == 1001 ? "RangedCaster" : "CrystalSeer") + ".prefab").GetComponent<EnemyController>();
                var shadow = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Combat/Enemies/Prefabs/ShadowLord.prefab").GetComponent<EnemyController>();
                var crystal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Combat/CrystalCavern/Prefabs/CrystalWarden.prefab").GetComponent<EnemyController>();
                arena.Bosses = dungeon == 1001 ? new[] { shadow, crystal } : new[] { crystal, shadow };
                var filter = new NavMeshQueryFilter { agentTypeID = arena.AgentType, areaMask = NavMesh.AllAreas };
                Func<Vector3, Vector3> sample = p => NavMesh.SamplePosition(p, out var hit, 2, filter) ? hit.position : throw new InvalidOperationException("挑战位置缺少导航：" + p);
                arena.Center = sample(dungeon == 1001 ? new Vector3(5, 1, -49) : new Vector3(0, .08f, 32));
                arena.PlayerSpawn = sample(dungeon == 1001 ? new Vector3(9, 1, -53) : new Vector3(0, .08f, 28));
                arena.BossSpawn = sample(dungeon == 1001 ? new Vector3(1, 3, -49) : new Vector3(0, .08f, 34));
                arena.GroupSpawns = dungeon == 1001 ? new[] { sample(new Vector3(3, 1, -47)), sample(new Vector3(11, 1, -49)), sample(new Vector3(5.15f, 1, -46.2f)) } :
                    new[] { sample(new Vector3(-3, .08f, 34)), sample(new Vector3(3, .08f, 34)), sample(new Vector3(0, .08f, 36)) };
                if (!arena.ValidateArena(out var error)) throw new InvalidOperationException(error);
                string asset = "Assets/Resources/Challenges/Arena" + dungeon + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<LongChallengeArena>(asset);
                if (existing == null) AssetDatabase.CreateAsset(arena, asset);
                else { EditorUtility.CopySerialized(arena, existing); EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing); UnityEngine.Object.DestroyImmediate(arena); }
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
