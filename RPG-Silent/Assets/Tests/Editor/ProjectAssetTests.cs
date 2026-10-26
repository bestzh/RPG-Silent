using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RPGSilent.Tests
{
    public class ProjectAssetTests
    {
        [TestCase("Assets/Scenes/StartScene.unity")]
        [TestCase("Assets/Scenes/Main.unity")]
        [TestCase("Assets/Scenes/Dungeon01.unity")]
        public void MainScenesHaveNoMissingScripts(string path)
        {
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                Assert.That(scene.IsValid(), Is.True, path);
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                        Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),
                            Is.Zero, path + ": " + transform.name);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test] public void EntrySceneIsFirstEnabledBuildScene()
        {
            var enabled = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            Assert.That(enabled, Is.Not.Empty);
            Assert.That(enabled[0].path, Is.EqualTo("Assets/Scenes/StartScene.unity"));
            foreach (var scene in enabled) Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path), Is.Not.Null, scene.path);
        }

        [TestCase("Assets/Scenes/StartScene.unity", "Scenes/StartScene")]
        [TestCase("Assets/Scenes/Main.unity", "Scenes/Main")]
        [TestCase("Assets/Scenes/Dungeon01.unity", "Scenes/Dungeon01")]
        public void MainSceneAddressableKeysRemainStable(string path, string key)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(settings, Is.Not.Null);
            var entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Assert.That(entry, Is.Not.Null, path);
            Assert.That(entry.address, Is.EqualTo(key));
        }

        [Test] public void ResourcesPlayerAndGameplayDatabasesCanBeLoaded()
        {
            Assert.That(Resources.Load<GameObject>("RPG-Character"), Is.Not.Null);
            Assert.That(Resources.Load("DungeonDatabase"), Is.Not.Null);
            Assert.That(Resources.Load("PortalDatabase"), Is.Not.Null);
            Assert.That(Resources.LoadAll("Skills").Length, Is.GreaterThanOrEqualTo(6));
        }
    }
}
