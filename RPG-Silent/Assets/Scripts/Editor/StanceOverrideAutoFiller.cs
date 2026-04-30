using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 用途：给定一个 AnimatorOverrideController + 一个姿态动画所在的文件夹 + 姿态标签（如 "Armed"），
//      自动按命名规则 "RPG-Character@<StanceTag>-<Action>" 匹配 Clip 并填入 Override。
// 打开方式：菜单 Tools > Stance > Override Auto Filler
public class StanceOverrideAutoFiller : EditorWindow
{
    private AnimatorOverrideController overrideController;
    private DefaultAsset stanceFolder;
    private string stanceTag = "Armed";
    private bool fuzzyMatch = true;

    private List<MatchRow> rows = new List<MatchRow>();
    private Vector2 scroll;

    private class MatchRow
    {
        public AnimationClip Original;
        public AnimationClip Match;
        public string Action;
        public string Status;
    }

    [MenuItem("Tools/Stance/Override Auto Filler")]
    public static void ShowWindow()
    {
        GetWindow<StanceOverrideAutoFiller>("Override Auto Filler");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("姿态 Override 自动填充", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1) 选择目标 OverrideController（其 Controller 字段必须先设为基础 PlayerAnim）\n" +
            "2) 选择姿态动画文件夹（例如 Animations/Armed）\n" +
            "3) 设置 Stance Tag（例如 Armed），文件名格式为 RPG-Character@<StanceTag>-<Action>\n" +
            "4) 点 Preview 查看匹配，确认后点 Apply 写入",
            MessageType.Info);

        overrideController = (AnimatorOverrideController)EditorGUILayout.ObjectField(
            "Override Controller", overrideController, typeof(AnimatorOverrideController), false);
        stanceFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Stance Folder", stanceFolder, typeof(DefaultAsset), false);
        stanceTag = EditorGUILayout.TextField("Stance Tag", stanceTag);
        fuzzyMatch = EditorGUILayout.Toggle("Fuzzy Match (尾部包含匹配)", fuzzyMatch);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(overrideController == null || stanceFolder == null))
        {
            if (GUILayout.Button("Preview Match"))
            {
                BuildPreview();
            }
        }

        if (rows.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"匹配预览（共 {rows.Count} 项）", EditorStyles.boldLabel);

            int matched = rows.Count(r => r.Match != null);
            EditorGUILayout.LabelField($"成功 {matched} / 失败 {rows.Count - matched}");

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(360));
            foreach (var row in rows)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color prev = GUI.color;
                    GUI.color = row.Match != null ? Color.green : Color.yellow;
                    EditorGUILayout.LabelField(row.Status, GUILayout.Width(40));
                    GUI.color = prev;

                    EditorGUILayout.ObjectField(row.Original, typeof(AnimationClip), false, GUILayout.Width(220));
                    EditorGUILayout.LabelField("→", GUILayout.Width(20));
                    row.Match = (AnimationClip)EditorGUILayout.ObjectField(row.Match, typeof(AnimationClip), false);
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply (写入 OverrideController)"))
                {
                    Apply();
                }
                if (GUILayout.Button("Clear"))
                {
                    rows.Clear();
                }
            }
        }
    }

    private void BuildPreview()
    {
        rows.Clear();
        if (overrideController == null || stanceFolder == null) return;

        string folderPath = AssetDatabase.GetAssetPath(stanceFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogError($"{folderPath} 不是文件夹");
            return;
        }

        var folderClips = LoadAllClipsUnder(folderPath);
        var prefix = $"{stanceTag}-";

        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(pairs);

        foreach (var pair in pairs)
        {
            var orig = pair.Key;
            if (orig == null) continue;

            string action = StripStancePrefix(orig.name);
            var match = FindBestMatch(folderClips, prefix, action);

            rows.Add(new MatchRow
            {
                Original = orig,
                Action = action,
                Match = match,
                Status = match != null ? "OK" : "MISS",
            });
        }
    }

    private void Apply()
    {
        if (overrideController == null) return;

        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(pairs);

        var newPairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        for (int i = 0; i < pairs.Count; i++)
        {
            var orig = pairs[i].Key;
            AnimationClip overrideClip = pairs[i].Value;

            var row = rows.FirstOrDefault(r => r.Original == orig);
            if (row != null && row.Match != null)
            {
                overrideClip = row.Match;
            }

            newPairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(orig, overrideClip));
        }

        Undo.RecordObject(overrideController, "Apply Stance Override");
        overrideController.ApplyOverrides(newPairs);
        EditorUtility.SetDirty(overrideController);
        AssetDatabase.SaveAssets();

        int written = rows.Count(r => r.Match != null);
        Debug.Log($"[Override Auto Filler] 已写入 {written} 项到 {overrideController.name}");
    }

    private List<AnimationClip> LoadAllClipsUnder(string folderPath)
    {
        var result = new List<AnimationClip>();
        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folderPath });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in subAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    result.Add(clip);
                }
            }
        }
        return result;
    }

    // 把 "Relax-Walk-Forward" / "Armed-Walk-Forward" / "Walk-Forward" 这样的名字统一去掉姿态前缀，
    // 返回纯动作名（"Walk-Forward"）。
    private static readonly string[] KnownStancePrefixes = new[]
    {
        "Relax-", "Unarmed-", "Armed-", "Armed-Shield-",
        "1Hand-Dagger-", "1Hand-Item-", "1Hand-Mace-", "1Hand-Pistol-", "1Hand-Spear-", "1Hand-Sword-",
        "2Hand-Axe-", "2Hand-Bow-", "2Hand-Crossbow-", "2Hand-Shooting-", "2Hand-Spear-", "2Hand-Staff-", "2Hand-Sword-",
        "Climb-Ladder-", "Climb-Ledge-", "Crawl-", "Swimming-",
    };

    private string StripStancePrefix(string clipName)
    {
        foreach (var p in KnownStancePrefixes)
        {
            if (clipName.StartsWith(p, System.StringComparison.OrdinalIgnoreCase))
            {
                return clipName.Substring(p.Length);
            }
        }
        return clipName;
    }

    private AnimationClip FindBestMatch(List<AnimationClip> candidates, string stancePrefix, string action)
    {
        // 1) 严格相等：<stancePrefix><action>
        string targetExact = stancePrefix + action;
        var exact = candidates.FirstOrDefault(c => string.Equals(c.name, targetExact, System.StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        if (!fuzzyMatch) return null;

        // 2) 模糊：以 -<action> 结尾（任何子姿态）
        var endsWith = candidates
            .Where(c => c.name.EndsWith("-" + action, System.StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        if (endsWith != null) return endsWith;

        // 3) 退化：包含 action
        var contains = candidates
            .Where(c => c.name.IndexOf(action, System.StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(c => c.name.Length)
            .FirstOrDefault();
        return contains;
    }
}
