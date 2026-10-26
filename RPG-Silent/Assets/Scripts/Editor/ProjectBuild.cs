using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RPGSilent.Editor
{
    public static class ProjectBuild
    {
        [Serializable]
        private sealed class BuildRecord
        {
            public string status, stage, startedUtc, finishedUtc, unityVersion, version, output, error;
            public string[] scenes, messages;
            public long totalBytes;
            public int errors, warnings;
            public double seconds, playerBuildSeconds;
        }

        // Both the menu and batch mode use this entry point. Never build from an
        // unsaved scene or silently switch platform in the middle of compilation.
        [MenuItem("Tools/RPG Silent/Build Windows64")]
        public static void BuildWindows()
        {
            string project = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string output = Path.Combine(project, "Builds", "Windows64");
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-rpgBuildOutput");
            if (option >= 0)
            {
                if (option + 1 >= args.Length) throw new BuildFailedException("Missing -rpgBuildOutput value.");
                output = Path.GetFullPath(args[option + 1]);
                string buildRoot = Path.Combine(project, "Builds") + Path.DirectorySeparatorChar;
                if (!output.StartsWith(buildRoot, StringComparison.OrdinalIgnoreCase))
                    throw new BuildFailedException("Build output must be inside this project's Builds directory.");
            }
            Directory.CreateDirectory(output);
            string reportPath = Path.Combine(output, "build-report.json");
            string pipelineSettingsPath = Path.Combine(project, "ProjectSettings", "ScriptableBuildPipeline.json");
            byte[] pipelineSettings = File.Exists(pipelineSettingsPath) ? File.ReadAllBytes(pipelineSettingsPath) : null;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var record = new BuildRecord
            {
                status = "Running", stage = "Preflight", startedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = UnityEngine.Application.unityVersion, version = PlayerSettings.bundleVersion,
                output = Path.Combine(output, "RPG-Silent.exe")
            };
            WriteReport(reportPath, record);
            try
            {
                string expectedVersion = File.ReadLines(Path.Combine(project, "ProjectSettings", "ProjectVersion.txt"))
                    .First(line => line.StartsWith("m_EditorVersion: ")).Substring("m_EditorVersion: ".Length).Trim();
                if (UnityEngine.Application.unityVersion != expectedVersion)
                    throw new BuildFailedException("Use Unity " + expectedVersion + ".");
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    throw new BuildFailedException("Stop Play Mode and wait for compilation before building.");
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                        throw new BuildFailedException("Save or discard scene changes before building.");
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                    throw new BuildFailedException("Select Windows64 first (batch mode: -buildTarget Win64).");
                if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                    throw new BuildFailedException("Install the Unity Windows build support module.");
                record.scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
                if (record.scenes.Length == 0 || record.scenes[0] != "Assets/Scenes/StartScene.unity")
                    throw new BuildFailedException("StartScene must be the first enabled build scene.");

                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null) throw new BuildFailedException("Addressables settings are missing.");
                record.stage = "Addressables";
                WriteReport(reportPath, record);
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
                if (content == null || !string.IsNullOrEmpty(content.Error))
                    throw new BuildFailedException("Addressables build failed: " + content?.Error);

                // A fresh batch process can report an untitled scene as loaded,
                // but that scene has no restorable SceneManagerSetup. Opsive's
                // prefab validator needs a loaded, saved scene after Addressables.
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (UnityEngine.Application.isBatchMode || !activeScene.isLoaded || string.IsNullOrEmpty(activeScene.path))
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(record.scenes[0]);

                // Explicitly built above; avoid a second build based on machine preferences.
                var previous = settings.BuildAddressablesWithPlayerBuild;
                BuildReport report;
                try
                {
                    settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                    record.stage = "Player";
                    WriteReport(reportPath, record);
                    report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = record.scenes, locationPathName = record.output,
                        target = BuildTarget.StandaloneWindows64,
                        options = BuildOptions.DetailedBuildReport | BuildOptions.CompressWithLz4
                    });
                }
                finally { settings.BuildAddressablesWithPlayerBuild = previous; }
                record.status = report.summary.result.ToString();
                record.totalBytes = (long)report.summary.totalSize;
                record.errors = (int)report.summary.totalErrors;
                record.warnings = (int)report.summary.totalWarnings;
                record.playerBuildSeconds = report.summary.totalTime.TotalSeconds;
                record.messages = report.steps.SelectMany(step => step.messages)
                    .Where(message => message.type != LogType.Log)
                    .Select(message => message.content).ToArray();
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new BuildFailedException("Windows build failed. See the Editor log and build-report.json.");
            }
            catch (Exception error)
            {
                record.status = "Failed";
                record.error = error.ToString();
                throw;
            }
            finally
            {
                // Addressables can set SBP's slimWriteResults=false while building.
                // Restore the pre-build settings so validation does not dirty source.
                if (pipelineSettings != null && (!File.Exists(pipelineSettingsPath) ||
                    !pipelineSettings.SequenceEqual(File.ReadAllBytes(pipelineSettingsPath))))
                    File.WriteAllBytes(pipelineSettingsPath, pipelineSettings);
                record.seconds = timer.Elapsed.TotalSeconds;
                record.finishedUtc = DateTime.UtcNow.ToString("O");
                WriteReport(reportPath, record);
            }
        }

        private static void WriteReport(string path, BuildRecord record) => File.WriteAllText(path, JsonUtility.ToJson(record, true));
    }
}
