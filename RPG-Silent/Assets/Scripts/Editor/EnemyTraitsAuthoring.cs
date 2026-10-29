using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Wire existing actors and material; preserve maps, spawn counts and behavior trees.</summary>
public static class EnemyTraitsAuthoring
{
    [MenuItem("Tools/RPG/Install Enemy Traits")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("先退出 Play Mode。");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Combat/Enemies/Materials/Warning.mat");
        if (material == null) throw new InvalidOperationException("缺少现有攻击预警材质。");
        foreach (string path in new[] { "Assets/Combat/Enemies/Prefabs/MeleeGuard.prefab", "Assets/Combat/Enemies/Prefabs/RangedCaster.prefab",
            "Assets/Combat/Enemies/Prefabs/ShadowLord.prefab", "Assets/Resources/Enemy.prefab", "Assets/Combat/CrystalCavern/Prefabs/CrystalLancer.prefab",
            "Assets/Combat/CrystalCavern/Prefabs/CrystalSeer.prefab", "Assets/Combat/CrystalCavern/Prefabs/CrystalWarden.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { root.GetComponent<EnemyController>().CombatWarningMaterial = material; PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in new[] { "Assets/Scenes/Dungeon01.unity", "Assets/Scenes/Dungeon02.unity" })
        {
            bool loaded = SceneManager.GetSceneByPath(path).isLoaded;
            var scene = loaded ? SceneManager.GetSceneByPath(path) : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("场景有未保存改动，未修改：" + path);
                var encounter = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<DungeonEncounter>(true)).Single();
                var guards = encounter.Waves.SelectMany(w => w.Enemies).Where(s => s.Prefab.Definition != null && s.Prefab.Definition.Behavior == EnemyBehaviorStyle.Melee).ToArray();
                var casters = encounter.Waves.SelectMany(w => w.Enemies).Where(s => s.Prefab.Definition != null && s.Prefab.Definition.Behavior == EnemyBehaviorStyle.Ranged).ToArray();
                if (guards.Length < 1 || casters.Length < 1) throw new InvalidOperationException("现有敌人配置不完整：" + path);
                guards[0].Trait = path.EndsWith("Dungeon01.unity", StringComparison.Ordinal) ? EnemyEliteTrait.Shield : EnemyEliteTrait.Frenzy;
                casters[0].Trait = EnemyEliteTrait.Mender;
                EditorUtility.SetDirty(encounter); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            finally { if (!loaded) EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
