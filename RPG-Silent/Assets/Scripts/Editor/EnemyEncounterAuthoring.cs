using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>将项目已有模型、动画与特效组装为可编辑资产；不会修改导入资源。</summary>
public static class EnemyEncounterAuthoring
{
    public const string Root = "Assets/Combat/Enemies";
    private const string FxRoot = "Assets/Hovl Studio/Magic effects pack/Prefabs/";
    private const string AnimRoot = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack/Animations/";
    private static readonly Dictionary<Material, Material> effectMaterials = new();

    [MenuItem("Tools/RPG/Build Enemy Encounter Demo")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("退出运行模式后再生成资产。");
        if (AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Root + "/Data/Boss.asset") != null)
            throw new InvalidOperationException("演示资产已存在；请直接编辑现有配置，避免覆盖调整。");
        foreach (string folder in new[] { Root, Root + "/Data", Root + "/Skills", Root + "/Prefabs", Root + "/VFX", Root + "/Materials", Root + "/Animations" })
            EnsureFolder(folder);
        effectMaterials.Clear();
        var impact = CopyEffect("Hits and explosions/Electro hit.prefab", "Impact");
        var death = CopyEffect("Smoke effects/Smoke puff.prefab", "DeathSmoke");
        var slash = CopyEffect("Slash effects/Charge slash red.prefab", "BossSlash");
        var quakeFx = CopyEffect("AoE effects/Ground AOE explosion.prefab", "GroundBurst");
        var meteorFx = CopyEffect("AoE effects/Red energy explosion.prefab", "MeteorBurst");
        var aura = CopyEffect("Character auras/Lightning aura.prefab", "EnrageAura");
        var sparks = CopyEffect("Sparks/Sparks red.prefab", "BoltTrail");
        var warning = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        ConfigureTransparent(warning, false);
        AssetDatabase.CreateAsset(warning, Root + "/Materials/Warning.mat");
        var projectile = CreateProjectile(sparks);

        var melee = Skill("MeleeSlash", "裂甲斩", AttackDeliveryType.MeleeArc, 10, 2.6f, 1.8f, 0.30f, 2.2f, warning);
        melee.Attack.Range = 0f; melee.Attack.Radius = 2.6f; melee.Attack.Angle = 110f; melee.Attack.HitEffectPrefab = impact;
        var bolt = Skill("RangedBolt", "暗焰弹", AttackDeliveryType.Projectile, 12, 14f, 1.7f, 0.55f, 2.4f, null);
        bolt.MinRange = 0f; bolt.Attack.ProjectilePrefab = projectile; bolt.Attack.ProjectileSpeed = 9f;
        bolt.Attack.ProjectileLifeTime = 4f; bolt.Attack.SpawnPointName = "ProjectileOrigin";
        bolt.Attack.HitEffectPrefab = impact;
        var cleave = Skill("BossCleave", "王庭横扫", AttackDeliveryType.MeleeArc, 18, 3.8f, 2.2f, 0.42f, 2.8f, warning);
        cleave.Attack.Range = 0f; cleave.Attack.Radius = 3.8f; cleave.Attack.Angle = 150f;
        cleave.ReleaseEffect = slash; cleave.Attack.HitEffectPrefab = impact;
        var quake = Skill("BossQuake", "震地重击", AttackDeliveryType.SelfAoe, 22, 5.5f, 2.5f, 0.68f, 7f, warning);
        quake.AnimationState = "Quake"; quake.Attack.AoeRadius = 4.5f; quake.SuperArmor = true;
        quake.ReleaseEffect = quakeFx; quake.Attack.HitEffectPrefab = impact;
        var meteor = Skill("BossMeteor", "赤焰烙印", AttackDeliveryType.AoeAtPoint, 25, 16f, 2.4f, 0.65f, 5f, warning);
        meteor.AnimationState = "Meteor"; meteor.RequiredPhase = 2; meteor.Weight = 3f;
        meteor.AimAtTargetGround = true; meteor.Attack.AoeRadius = 2.8f; meteor.SuperArmor = true;
        meteor.ReleaseEffect = meteorFx; meteor.Attack.HitEffectPrefab = impact;

        var meleeDef = Definition("Melee", "暮影剑卫", EnemyBehaviorStyle.Melee, 65, 3.4f, 2.1f, 0f, new[] { melee }, impact, death);
        var rangedDef = Definition("Ranged", "赤焰咒术师", EnemyBehaviorStyle.Ranged, 50, 3.2f, 8f, 4f, new[] { bolt }, impact, death);
        var bossDef = Definition("Boss", "暮影领主 · 维拉斯", EnemyBehaviorStyle.Boss, 600, 2.7f, 3.1f, 0f, new[] { cleave, quake, meteor }, impact, death);
        bossDef.StaggerThreshold = 80; bossDef.RewardGold = 100; bossDef.RewardExp = 200;
        var original = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animator/Enemy/Enemy.controller");
        var baseClips = original.layers[0].stateMachine.states.ToDictionary(s => s.state.name, s => s.state.motion as AnimationClip);
        var meleeController = Controller("Melee", baseClips, new[] { melee }, baseClips["Attack"]);
        var staff = Clip(AnimRoot + "2Hand-Staff/RPG-Character@2Hand-Staff-Attack1.fbx", "2Hand-Staff-Attack1");
        var rangedController = Controller("Ranged", baseClips, new[] { bolt }, staff);
        var bossController = Controller("Boss", baseClips, new[] { cleave, quake, meteor }, baseClips["Attack"]);
        AddState(bossController, "Phase", staff, 1.8f);
        SetStateClip(bossController, "Quake", Clip(AnimRoot + "2Hand-Sword/RPG-Character@2Hand-Sword-Attack2.FBX", "2Hand-Sword-Attack2"), quake.Duration);
        SetStateClip(bossController, "Meteor", staff, meteor.Duration);
        var meleePrefab = EnemyPrefab("MeleeGuard", "Assets/Resources/Enemy.prefab", meleeDef, meleeController, 1f, new Color(0.75f, 0.85f, 1f));
        var rangedPrefab = EnemyPrefab("RangedCaster", "Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_Presets/Chr_FantasyHero_Preset_15.prefab", rangedDef, rangedController, 1f, new Color(0.8f, 0.65f, 1f));
        var bossPrefab = EnemyPrefab("ShadowLord", "Assets/Resources/Enemy.prefab", bossDef, bossController, 1.45f, new Color(1f, 0.35f, 0.26f), aura, meteorFx);
        foreach (string guid in AssetDatabase.FindAssets("", new[] { Root }))
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                if (asset != null) EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        WireScene(meleePrefab, rangedPrefab, bossPrefab);
        WireBossUi();
        AssetDatabase.SaveAssets();
        Debug.Log("[EnemyEncounter] 已创建三类敌人、五个技能及 Dungeon01 两波遭遇。");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static GameObject CopyEffect(string source, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FxRoot + source);
        if (prefab == null) throw new InvalidOperationException("缺少效果 " + source);
        var clone = Object.Instantiate(prefab);
        clone.name = name;
        try
        {
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var old = materials[i];
                    if (old == null) continue;
                    if (!effectMaterials.TryGetValue(old, out var converted))
                    {
                        converted = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                        ConfigureTransparent(converted, old.HasProperty("_DstBlend") && old.GetFloat("_DstBlend") == 1f);
                        if (old.HasProperty("_MainTex")) converted.SetTexture("_BaseMap", old.GetTexture("_MainTex"));
                        if (old.HasProperty("_Color")) converted.SetColor("_BaseColor", old.GetColor("_Color"));
                        else if (old.HasProperty("_TintColor")) converted.SetColor("_BaseColor", old.GetColor("_TintColor"));
                        AssetDatabase.CreateAsset(converted, AssetDatabase.GenerateUniqueAssetPath(Root + "/Materials/FX_" + old.name + ".mat"));
                        effectMaterials.Add(old, converted);
                    }
                    materials[i] = converted;
                }
                renderer.sharedMaterials = materials;
            }
            return PrefabUtility.SaveAsPrefabAsset(clone, Root + "/VFX/" + name + ".prefab");
        }
        finally { Object.DestroyImmediate(clone); }
    }

    private static void ConfigureTransparent(Material material, bool additive)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 2f : 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
    }

    private static GameObject CreateProjectile(GameObject sparks)
    {
        var root = new GameObject("EnemyFlameBolt");
        var sphere = root.AddComponent<SphereCollider>(); sphere.radius = 0.16f; sphere.isTrigger = true;
        var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
        root.AddComponent<AttackProjectile>();
        var trail = Object.Instantiate(sparks, root.transform); trail.transform.localScale = Vector3.one * 0.45f;
        var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(root.transform, false); core.transform.localScale = Vector3.one * 0.24f;
        Object.DestroyImmediate(core.GetComponent<Collider>());
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); mat.SetColor("_BaseColor", new Color(4f, 0.35f, 0.05f));
        AssetDatabase.CreateAsset(mat, Root + "/Materials/BoltCore.mat"); core.GetComponent<Renderer>().sharedMaterial = mat;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/EnemyFlameBolt.prefab");
        Object.DestroyImmediate(root); return prefab;
    }

    private static EnemySkillData Skill(string id, string name, AttackDeliveryType type, int damage,
        float range, float duration, float release, float cooldown, Material warning)
    {
        var attack = ScriptableObject.CreateInstance<AttackProfile>();
        attack.DeliveryType = type; attack.Damage = damage;
        var skill = ScriptableObject.CreateInstance<EnemySkillData>();
        skill.DisplayName = name; skill.Attack = attack; skill.Duration = duration; skill.ReleaseTime = release;
        skill.MaxRange = range; skill.Cooldown = cooldown; skill.TelegraphMaterial = warning;
        AssetDatabase.CreateAsset(attack, Root + "/Skills/" + id + "_Hit.asset");
        AssetDatabase.CreateAsset(skill, Root + "/Skills/" + id + ".asset");
        return skill;
    }

    private static EnemyDefinition Definition(string id, string name, EnemyBehaviorStyle behavior, int health,
        float speed, float distance, float retreat, EnemySkillData[] skills, GameObject hit, GameObject death)
    {
        var data = ScriptableObject.CreateInstance<EnemyDefinition>();
        data.DisplayName = name; data.Behavior = behavior; data.Health = health; data.MoveSpeed = speed;
        data.PreferredDistance = distance; data.RetreatDistance = retreat; data.Skills = skills;
        data.HitEffect = hit; data.DeathEffect = death; data.DetectionDistance = 18f; data.LeashDistance = 60f;
        AssetDatabase.CreateAsset(data, Root + "/Data/" + id + ".asset"); return data;
    }

    private static AnimationClip Clip(string path, string name)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);
        if (clip == null) throw new InvalidOperationException("缺少动画 " + path + " / " + name);
        if (clip.events.Length == 0) return clip;
        var clone = Object.Instantiate(clip); AnimationUtility.SetAnimationEvents(clone, Array.Empty<AnimationEvent>());
        AssetDatabase.CreateAsset(clone, AssetDatabase.GenerateUniqueAssetPath(Root + "/Animations/" + name + ".anim"));
        return clone;
    }

    private static AnimatorController Controller(string name, Dictionary<string, AnimationClip> clips, EnemySkillData[] skills, AnimationClip attack)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Animations/" + name + ".controller");
        foreach (var entry in clips) if (entry.Key != "Attack") AddState(controller, entry.Key, entry.Value, 0f);
        foreach (var skill in skills) AddState(controller, skill.AnimationState, attack, skill.Duration);
        return controller;
    }

    private static void AddState(AnimatorController controller, string name, AnimationClip clip, float duration)
    {
        var state = controller.layers[0].stateMachine.AddState(name);
        state.motion = clip; state.speed = duration > 0f ? clip.length / duration : 1f;
        if (name == "Idle") controller.layers[0].stateMachine.defaultState = state;
    }

    private static void SetStateClip(AnimatorController controller, string name, AnimationClip clip, float duration)
    {
        var state = controller.layers[0].stateMachine.states.First(s => s.state.name == name).state;
        state.motion = clip; state.speed = clip.length / duration;
    }

    private static EnemyController EnemyPrefab(string id, string source, EnemyDefinition data,
        AnimatorController controller, float scale, Color tint, GameObject aura = null, GameObject phaseEffect = null)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source));
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        root.name = id;
        root.transform.localScale = Vector3.one * scale;
        try
        {
            var animator = root.GetComponent<Animator>(); animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var enemy = GetOrAdd<EnemyController>(root);
            enemy.Definition = data; enemy.MaxHealth = data.Health;
            var agent = GetOrAdd<NavMeshAgent>(root);
            agent.agentTypeID = 1479372276; agent.radius = 0.5f; agent.height = 1.9f; agent.speed = data.MoveSpeed;
            var capsule = GetOrAdd<CapsuleCollider>(root);
            capsule.height = 1.9f; capsule.center = Vector3.up * 0.95f; capsule.radius = 0.32f;
            var rb = GetOrAdd<Rigidbody>(root); rb.isKinematic = true; rb.useGravity = false;
            if (root.GetComponent<AttackExecutor>() == null) root.AddComponent<AttackExecutor>();
            var faction = GetOrAdd<CombatFaction>(root); faction.Team = CombatTeam.Enemy;
            var origin = new GameObject("ProjectileOrigin"); origin.transform.SetParent(root.transform, false);
            origin.transform.localPosition = new Vector3(0f, 1.2f, 0.8f);
            if (data.Behavior == EnemyBehaviorStyle.Ranged)
            {
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var staff = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonFantasyHeroCharacters/Prefabs/Weapons/SM_Wep_Staff_01.prefab"), hand);
                staff.transform.localPosition = Vector3.zero; staff.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            var materials = new Dictionary<Material, Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var array = renderer.sharedMaterials;
                for (int i = 0; i < array.Length; i++)
                {
                    var old = array[i]; if (old == null) continue;
                    if (!materials.TryGetValue(old, out var mat))
                    {
                        mat = new Material(old);
                        if (!old.shader.name.StartsWith("Universal Render Pipeline"))
                        {
                            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                            if (old.HasProperty("_MainTex")) mat.SetTexture("_BaseMap", old.GetTexture("_MainTex"));
                        }
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                        AssetDatabase.CreateAsset(mat, AssetDatabase.GenerateUniqueAssetPath(Root + "/Materials/" + id + "_" + old.name + ".mat"));
                        materials.Add(old, mat);
                    }
                    array[i] = mat;
                }
                renderer.sharedMaterials = array;
            }
            if (data.Behavior == EnemyBehaviorStyle.Boss)
            {
                var phases = root.AddComponent<BossPhaseController>();
                phases.EnrageAura = aura; phases.TransitionEffect = phaseEffect;
            }
            return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + id + ".prefab").GetComponent<EnemyController>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void WireScene(EnemyController melee, EnemyController ranged, EnemyController boss)
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Dungeon01.unity", OpenSceneMode.Additive);
        try
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var enemy in root.GetComponentsInChildren<EnemyController>(true))
                {
                    Undo.RecordObject(enemy.gameObject, "Replace demo encounter");
                    enemy.gameObject.SetActive(false);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.gameObject);
                }
            var go = new GameObject("Dungeon Enemy Encounter");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Create enemy encounter");
            var encounter = go.AddComponent<DungeonEncounter>();
            Vector3[] positions = { new(-6.81f, 0.13f, -46.15f), new(-26.27f, 3.3f, -31.96f), new(-34.4f, 4.03f, -32.12f), new(-44.5f, 3.1f, -12.4f) };
            var spawns = new DungeonEncounter.Spawn[4];
            var prefabs = new[] { melee, melee, ranged, boss };
            var filter = new NavMeshQueryFilter { agentTypeID = 1479372276, areaMask = NavMesh.AllAreas };
            for (int i = 0; i < positions.Length; i++)
            {
                if (!NavMesh.SamplePosition(positions[i], out var hit, 2f, filter)) throw new InvalidOperationException("出生点不在导航网格上: " + positions[i]);
                var point = new GameObject("Spawn_" + (i + 1)); point.transform.SetParent(go.transform, false);
                point.transform.position = hit.position;
                spawns[i] = new DungeonEncounter.Spawn { Prefab = prefabs[i], Point = point.transform };
            }
            encounter.Waves = new[] {
                new DungeonEncounter.Wave { Name = "清理暮影守卫与咒术师", Enemies = spawns.Take(3).ToArray() },
                new DungeonEncounter.Wave { Name = "前往北侧平台，击败暮影领主", Enemies = new[] { spawns[3] } }
            };
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    private static T GetOrAdd<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }

    private static void WireBossUi()
    {
        const string path = "Assets/Resources_moved/UI/DungeonUI.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var ui = root.GetComponent<DungeonUI>();
            var so = new SerializedObject(ui);
            var template = (TMP_Text)so.FindProperty("objectiveText").objectReferenceValue;
            var panel = new GameObject("Boss Health", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -174f); rect.sizeDelta = new Vector2(680f, 76f);
            panel.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.07f, 0.85f); panel.GetComponent<Image>().raycastTarget = false;
            var title = Object.Instantiate(template, panel.transform); title.name = "Boss Name";
            title.rectTransform.anchorMin = new Vector2(0f, 0.5f); title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(10f, 0f); title.rectTransform.offsetMax = new Vector2(-10f, -4f);
            title.alignment = TextAlignmentOptions.Center; title.fontSize = 26f; title.enableAutoSizing = true;
            title.fontSizeMin = 20f; title.fontSizeMax = 26f; title.raycastTarget = false;
            var bar = new GameObject("Health", typeof(RectTransform), typeof(Slider)); bar.transform.SetParent(panel.transform, false);
            var br = (RectTransform)bar.transform; br.anchorMin = new Vector2(0f, 0f); br.anchorMax = new Vector2(1f, 0f);
            br.pivot = new Vector2(0.5f, 0f); br.offsetMin = new Vector2(14f, 10f); br.offsetMax = new Vector2(-14f, 22f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)); fill.transform.SetParent(bar.transform, false);
            var fr = (RectTransform)fill.transform; fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = fr.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = new Color(0.85f, 0.16f, 0.12f); fill.GetComponent<Image>().raycastTarget = false;
            var slider = bar.GetComponent<Slider>(); slider.fillRect = fr; slider.interactable = false; slider.value = 1f;
            so.FindProperty("bossPanel").objectReferenceValue = panel;
            so.FindProperty("bossName").objectReferenceValue = title;
            so.FindProperty("bossHealth").objectReferenceValue = slider;
            so.ApplyModifiedPropertiesWithoutUndo(); panel.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
