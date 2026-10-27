using System;
using System.Collections.Generic;
using System.Linq;
using Opsive.BehaviorDesigner.Runtime;
using Opsive.GraphDesigner.Runtime;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>首次组装第二关；所有派生资源独立保存，不覆盖导入包和已编辑的第一关。</summary>
public static class CrystalCavernAuthoring
{
    public const string Root = "Assets/Combat/CrystalCavern";
    public const string ScenePath = "Assets/Scenes/Dungeon02.unity";
    private const string Kingdom = "Assets/PolygonFantasyKingdom/Prefabs/";
    private const string Nature = "Assets/PolygonNatureBiomes/PNB_Enchanted_Forest/Prefabs/Props/";
    private const string Anim = "Assets/ExplosiveLLC/RPG Character Mecanim Animation Pack/Animations/";
    private static readonly Dictionary<string, Material> materials = new();
    private static readonly Color Cyan = new(0.25f, 0.8f, 1f, 0.9f);

    [MenuItem("Tools/RPG/Create Crystal Cavern")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出 Play Mode。");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null || AssetDatabase.IsValidFolder(Root))
            throw new InvalidOperationException("第二关资源已存在，请直接编辑现有资产，避免覆盖调整。");
        foreach (var path in new[] { Root, Root + "/Data", Root + "/Skills", Root + "/Animations", Root + "/BehaviorTrees", Root + "/Prefabs", Root + "/VFX", Root + "/Materials", "Assets/Scenes/Dungeon02" }) Folder(path);
        materials.Clear();
        var warning = new Material(Asset<Material>("Assets/Combat/Enemies/Materials/Warning.mat"));
        AssetDatabase.CreateAsset(warning, Root + "/Materials/CrystalWarning.mat");
        var burst = Effect("AoE effects/Crystals crossfade.prefab", "CrystalBurst");
        var shock = Effect("AoE effects/AoE slash blue.prefab", "CrystalShock");
        var glow = Effect("Environment/Crystal effect blue.prefab", "CrystalGlow");
        var aura = Effect("Character auras/Plexus.prefab", "ResonanceAura");
        var impact = Asset<GameObject>("Assets/Combat/Enemies/VFX/Impact.prefab");
        var death = Asset<GameObject>("Assets/Combat/Enemies/VFX/DeathSmoke.prefab");

        var thrust = Skill("CrystalThrust", "贯晶突刺", AttackDeliveryType.MeleeArc, 16, 3.4f, 1.9f, .55f, 2.8f, warning);
        thrust.Attack.Range = 0; thrust.Attack.Radius = 3.4f; thrust.Attack.Angle = 55;
        thrust.RecoveryDuration = .6f;
        var field = Skill("CrystalField", "晶蚀领域", AttackDeliveryType.PersistentAoe, 6, 14, 2.1f, .65f, 6, warning);
        field.AnimationState = "Cast"; field.AimAtTargetGround = true; field.Attack.AoeRadius = 2.3f;
        field.Attack.AreaDuration = 4; field.Attack.TickInterval = 1; field.Attack.AreaPrefab = Area("CrystalFieldArea", 2.3f, glow, warning);
        var smash = Skill("WardenSmash", "晶锤重劈", AttackDeliveryType.MeleeArc, 24, 4.3f, 2.2f, .6f, 3.5f, warning);
        smash.Attack.Range = 0; smash.Attack.Radius = 4.3f; smash.Attack.Angle = 80;
        smash.AnimationState = "Smash"; smash.RecoveryDuration = .8f;
        var quake = Skill("WardenShock", "晶环震荡", AttackDeliveryType.SelfAoe, 28, 5.5f, 2.4f, .68f, 6, warning);
        quake.Attack.AoeRadius = 4.6f; quake.AnimationState = "Quake"; quake.SuperArmor = true; quake.ReleaseEffect = shock;
        var eruption = Skill("WardenEruption", "晶刺爆发", AttackDeliveryType.AoeAtPoint, 26, 18, 2.2f, .65f, 5, warning);
        eruption.AimAtTargetGround = true; eruption.Attack.AoeRadius = 2.7f; eruption.AnimationState = "Cast";
        eruption.SuperArmor = true; eruption.ReleaseEffect = burst; eruption.Weight = 2;
        var rift = Skill("WardenRift", "碎晶领域", AttackDeliveryType.PersistentAoe, 9, 18, 2.4f, .65f, 10, warning);
        rift.RequiredPhase = 2; rift.AimAtTargetGround = true; rift.Attack.AoeRadius = 3;
        rift.Attack.AreaDuration = 5; rift.Attack.TickInterval = 1; rift.Attack.AreaPrefab = Area("WardenRiftArea", 3, glow, warning);
        rift.AnimationState = "Rift"; rift.SuperArmor = true; rift.Weight = 2;
        foreach (var s in new[] { thrust, field, smash, quake, eruption, rift }) s.Attack.HitEffectPrefab = impact;
        var combo = ScriptableObject.CreateInstance<EnemyAttackSequence>();
        combo.DisplayName = "共鸣崩裂"; combo.Steps = new[] { quake, rift }; combo.RequiredPhase = 2;
        combo.Cooldown = 12; combo.BetweenAttacks = .4f; combo.RecoveryDuration = 2.2f;
        AssetDatabase.CreateAsset(combo, Root + "/Skills/ResonanceCollapse.asset");

        var guard = Definition("CrystalLancer", "晶刺枪卫", EnemyBehaviorStyle.Melee, 90, 3.2f, 2.7f, 18, 40, new[] { thrust }, impact, death);
        guard.AttackFacingAngle = 18;
        var caster = Definition("CrystalSeer", "晶咒祭司", EnemyBehaviorStyle.Ranged, 70, 3, 9, 22, 55, new[] { field }, impact, death);
        caster.RetreatDistance = 4.5f;
        var boss = Definition("CrystalWarden", "晶核守望者 · 瑟莱恩", EnemyBehaviorStyle.Boss, 900, 2.5f, 3.5f, 160, 360, new[] { smash, quake, eruption, rift }, impact, death);
        boss.StaggerThreshold = 90; boss.AttackSequences = new[] { combo }; boss.RecoveryDamageMultiplier = 1.3f;
        boss.DetectionDistance = 22; boss.LeashDistance = 32;
        var guardPrefab = Enemy("CrystalLancer", 2, guard, Controller("CrystalLancer", "2Hand-Spear", new[] { thrust }), "SM_Wep_Spear_01", 1.05f);
        var casterPrefab = Enemy("CrystalSeer", 12, caster, Controller("CrystalSeer", "2Hand-Staff", new[] { field }), "SM_Wep_Staff_09", 1.05f);
        var bossPrefab = Enemy("CrystalWarden", 3, boss, Controller("CrystalWarden", "2Hand-Axe", new[] { smash, quake, eruption, rift }), "SM_Wep_Hammer_06", 1.65f, aura, burst);
        SaveCreatedAssets();
        CreateScene(guardPrefab, casterPrefab, bossPrefab);
        WireDatabase();
        SaveCreatedAssets();
        Debug.Log("[Crystal Cavern] 第二关、三类专属敌人、六种技能及三波遭遇已创建。");
    }

    private static T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("缺少资源：" + path);
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/'); Folder(path[..split]); AssetDatabase.CreateFolder(path[..split], path[(split + 1)..]);
    }
    private static void SaveCreatedAssets()
    {
        foreach (var guid in AssetDatabase.FindAssets("", new[] { Root }))
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid))) if (asset != null) EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
    private static EnemySkillData Skill(string id, string name, AttackDeliveryType type, int damage, float range, float duration, float release, float cooldown, Material warning)
    {
        var hit = ScriptableObject.CreateInstance<AttackProfile>(); hit.DeliveryType = type; hit.Damage = damage;
        var skill = ScriptableObject.CreateInstance<EnemySkillData>(); skill.DisplayName = name; skill.Attack = hit;
        skill.MaxRange = range; skill.Duration = duration; skill.ReleaseTime = release; skill.Cooldown = cooldown;
        skill.TelegraphMaterial = warning; skill.TelegraphColor = Cyan;
        AssetDatabase.CreateAsset(hit, Root + "/Skills/" + id + "Attack.asset");
        AssetDatabase.CreateAsset(skill, Root + "/Skills/" + id + ".asset");
        return skill;
    }
    private static EnemyDefinition Definition(string id, string name, EnemyBehaviorStyle style, int health, float speed, float distance, int gold, int exp, EnemySkillData[] skills, GameObject hit, GameObject death)
    {
        var data = ScriptableObject.CreateInstance<EnemyDefinition>(); data.DisplayName = name; data.Behavior = style;
        data.Health = health; data.MoveSpeed = speed; data.PreferredDistance = distance; data.DetectionDistance = 15; data.LeashDistance = 28;
        data.RewardGold = gold; data.RewardExp = exp; data.Skills = skills; data.HitEffect = hit; data.DeathEffect = death;
        AssetDatabase.CreateAsset(data, Root + "/Data/" + id + ".asset"); return data;
    }
    private static AnimationClip Clip(string folder, string name)
    {
        string path = Anim + folder + "/RPG-Character@" + name + ".FBX";
        var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        if (clip == null) throw new InvalidOperationException("缺少动画：" + path);
        if (clip.events.Length == 0) return clip;
        string copyPath = Root + "/Animations/" + name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(copyPath); if (existing != null) return existing;
        var clean = Object.Instantiate(clip); AnimationUtility.SetAnimationEvents(clean, Array.Empty<AnimationEvent>());
        AssetDatabase.CreateAsset(clean, copyPath); return clean;
    }
    private static AnimatorController Controller(string id, string stance, EnemySkillData[] skills)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Animations/" + id + ".controller");
        var original = Asset<AnimatorController>("Assets/Combat/Enemies/Animations/Melee.controller");
        foreach (var item in original.layers[0].stateMachine.states.Where(s => s.state.name != "Attack"))
            State(controller, item.state.name, (AnimationClip)item.state.motion, 0);
        var idle = controller.layers[0].stateMachine.states.First(s => s.state.name == "Idle").state;
        idle.motion = Clip(stance, stance + "-Idle");
        foreach (var s in skills.GroupBy(s => s.AnimationState).Select(g => g.First()))
        {
            var clip = s.AnimationState == "Attack" ? Clip(stance, stance + "-Attack1") :
                s.AnimationState == "Smash" || s.AnimationState == "Quake" ? Clip("2Hand-Axe", "2Hand-Axe-Attack2") :
                Clip("Unarmed", "Unarmed-Cast-Dual-AOE1");
            State(controller, s.AnimationState, clip, s.Duration);
        }
        if (id == "CrystalWarden") State(controller, "Phase", Clip("Unarmed", "Unarmed-Cast-Dual-Summon1"), 2);
        return controller;
    }
    private static void State(AnimatorController controller, string name, AnimationClip clip, float duration)
    {
        var state = controller.layers[0].stateMachine.AddState(name); state.motion = clip; state.speed = duration > 0 ? clip.length / duration : 1;
        if (name == "Idle") controller.layers[0].stateMachine.defaultState = state;
    }
    private static EnemyController Enemy(string id, int preset, EnemyDefinition data, AnimatorController controller, string weapon, float scale, GameObject aura = null, GameObject transition = null)
    {
        var root = Object.Instantiate(Asset<GameObject>("Assets/PolygonFantasyHeroCharacters/Prefabs/Characters_Presets/Chr_FantasyHero_Preset_" + preset + ".prefab"));
        root.name = id; root.transform.localScale = Vector3.one * scale;
        try
        {
            var animator = root.GetComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var enemy = root.AddComponent<EnemyController>(); enemy.Definition = data; enemy.MaxHealth = data.Health;
            var agent = root.AddComponent<NavMeshAgent>(); agent.agentTypeID = 1479372276; agent.radius = .5f; agent.height = 1.9f; agent.speed = data.MoveSpeed;
            var capsule = root.AddComponent<CapsuleCollider>(); capsule.height = 1.9f; capsule.center = Vector3.up * .95f; capsule.radius = .32f;
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            root.AddComponent<AttackExecutor>(); root.AddComponent<CombatFaction>().Team = CombatTeam.Enemy;
            var held = Object.Instantiate(Asset<GameObject>(Kingdom + "Weapons/" + weapon + ".prefab"), animator.GetBoneTransform(HumanBodyBones.RightHand));
            held.transform.localPosition = Vector3.zero; held.transform.localRotation = Quaternion.Euler(0, 0, 90);
            FitBoneAttachment(held.transform, root.transform, Vector3.zero, 1);
            foreach (var collider in held.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            Remap(root, "Character", new Color(.72f, .86f, 1f));
            if (data.Behavior == EnemyBehaviorStyle.Boss)
            {
                var phase = root.AddComponent<BossPhaseController>(); phase.EnrageAura = aura; phase.TransitionEffect = transition; phase.TransitionDuration = 2;
                var shoulder = Object.Instantiate(Asset<GameObject>(Nature + "SM_Prop_Crystal_03.prefab"), animator.GetBoneTransform(HumanBodyBones.Spine));
                FitBoneAttachment(shoulder.transform, root.transform, new Vector3(0, .25f, -.2f), .23f);
                foreach (var collider in shoulder.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
                Remap(shoulder, "Crystal", new Color(.3f, .85f, 1));
            }
            string original = data.Behavior == EnemyBehaviorStyle.Boss ? "Boss" : data.Behavior == EnemyBehaviorStyle.Ranged ? "Ranged" : "Melee";
            string treePath = Root + "/BehaviorTrees/" + id + ".asset";
            if (!AssetDatabase.CopyAsset(EnemyBehaviorTreeAuthoring.Root + "/" + original + ".asset", treePath)) throw new InvalidOperationException("复制 Pro 树失败。");
            var graph = Asset<Subtree>(treePath); graph.Deserialize(true); graph.Name = id;
            foreach (var property in graph.LogicNodeProperties)
                if (property.DisplayName != null && property.DisplayName.Contains("连斩")) property.DisplayName = "第二阶段：晶环震荡 → 碎晶领域";
            graph.Serialize(); EditorUtility.SetDirty(graph);
            var tree = root.AddComponent<Opsive.BehaviorDesigner.Runtime.Wrappers.BehaviorTree>();
            tree.StartWhenEnabled = false; tree.PauseWhenDisabled = false; tree.Subgraph = graph;
            tree.UpdateMode = Opsive.BehaviorDesigner.Runtime.Components.UpdateMode.EveryFrame;
            tree.EvaluationType = Opsive.BehaviorDesigner.Runtime.Components.EvaluationType.EntireTree; tree.Serialize();
            return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + id + ".prefab").GetComponent<EnemyController>();
        }
        finally { Object.DestroyImmediate(root); }
    }
    private static void FitBoneAttachment(Transform attachment, Transform root, Vector3 offset, float scale)
    {
        // Synty 骨骼采用厘米单位；挂件按角色根节点的米制尺寸缩放。
        var boneScale = attachment.parent.lossyScale;
        var modelScale = root.lossyScale;
        var units = new Vector3(modelScale.x / boneScale.x, modelScale.y / boneScale.y, modelScale.z / boneScale.z);
        attachment.localScale = Vector3.Scale(Vector3.one * scale, units);
        attachment.localPosition = Vector3.Scale(offset, units);
    }
    private static GameObject Effect(string source, string id)
    {
        var root = Object.Instantiate(Asset<GameObject>("Assets/Hovl Studio/Magic effects pack/Prefabs/" + source)); root.name = id;
        try { Remap(root, "FX", Color.white); return PrefabUtility.SaveAsPrefabAsset(root, Root + "/VFX/" + id + ".prefab"); }
        finally { Object.DestroyImmediate(root); }
    }
    private static GameObject Area(string id, float radius, GameObject fx, Material lineMaterial)
    {
        var root = new GameObject(id);
        try
        {
            var col = root.AddComponent<SphereCollider>(); col.radius = radius; col.isTrigger = true;
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false; root.AddComponent<AttackArea>();
            var visual = Object.Instantiate(fx, root.transform); visual.transform.localScale = Vector3.one * .4f;
            var ring = root.AddComponent<LineRenderer>(); ring.sharedMaterial = lineMaterial; ring.useWorldSpace = false;
            ring.loop = true; ring.positionCount = 64; ring.startWidth = ring.endWidth = .08f; ring.startColor = ring.endColor = Cyan;
            for (int i = 0; i < 64; i++) { float angle = i * Mathf.PI * 2 / 64; ring.SetPosition(i, new Vector3(Mathf.Sin(angle) * radius, .08f, Mathf.Cos(angle) * radius)); }
            return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + id + ".prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }
    private static void Remap(GameObject root, string style, Color tint)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var array = renderer.sharedMaterials;
            for (int i = 0; i < array.Length; i++)
            {
                var old = array[i]; if (old == null) continue;
                string key = style + "_" + old.GetInstanceID();
                if (!materials.TryGetValue(key, out var mat))
                {
                    bool fx = style == "FX";
                    bool characterShader = style == "Character" && old.shader.isSupported && old.shader.name.Contains("URP");
                    mat = characterShader ? new Material(old) : new Material(Shader.Find(fx ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit"));
                    var texture = old.HasProperty("_BaseMap") ? old.GetTexture("_BaseMap") : old.HasProperty("_MainTex") ? old.GetTexture("_MainTex") : null;
                    if (!characterShader) mat.SetTexture("_BaseMap", texture);
                    Color baseColor = fx && old.HasProperty("_TintColor") ? old.GetColor("_TintColor") : fx && old.HasProperty("_Color") ? old.GetColor("_Color") : tint;
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .2f);
                    if (fx)
                    {
                        bool additive = old.HasProperty("_DstBlend") && old.GetFloat("_DstBlend") == 1;
                        mat.SetFloat("_Surface", 1); mat.SetFloat("_Blend", additive ? 2 : 0); mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                        mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                        mat.SetFloat("_ZWrite", 0); mat.SetFloat("_Cull", 0); mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
                    }
                    if (style == "Crystal") { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", tint * .7f); mat.SetFloat("_Smoothness", .65f); }
                    AssetDatabase.CreateAsset(mat, AssetDatabase.GenerateUniqueAssetPath(Root + "/Materials/" + style + "_" + old.name.Replace('/', '_') + ".mat")); materials.Add(key, mat);
                }
                array[i] = mat;
            }
            renderer.sharedMaterials = array;
        }
    }
    private static GameObject Fit(string asset, Transform parent, string name, Vector3 center, Vector3 size, string style)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Asset<GameObject>(asset)); go.name = name; go.transform.SetParent(parent, false);
        var bounds = go.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        var scale = new Vector3(size.x / bounds.size.x, size.y / bounds.size.y, size.z / bounds.size.z);
        go.transform.localScale = scale; go.transform.position = center - Vector3.Scale(bounds.center, scale);
        Remap(go, style, style == "Crystal" ? new Color(.28f, .76f, .95f) : style == "Floor" ? new Color(.48f, .58f, .67f) : new Color(.3f, .38f, .48f));
        return go;
    }
    private static void CreateScene(EnemyController guard, EnemyController caster, EnemyController boss)
    {
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); SceneManager.SetActiveScene(scene);
        try
        {
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .6f, .75f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.055f, .09f, .15f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .008f;
            var sun = new GameObject("Cavern soft light").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(.68f, .8f, 1); sun.intensity = 1.3f; sun.transform.rotation = Quaternion.Euler(55, -25, 0); sun.shadows = LightShadows.Soft;
            var env = new GameObject("Crystal Cavern Environment");
            var floors = new GameObject("Stone chambers and connecting path"); floors.transform.SetParent(env.transform);
            for (int z = -25; z <= 40; z += 5)
            {
                int width = z < 0 ? 10 : z < 15 ? 0 : 15;
                for (int x = -width; x <= width; x += 5) Fit(Kingdom + "Castle/SM_Bld_Castle_Floor_Stone_01.prefab", floors.transform, "Stone floor", new Vector3(x, -.14f, z), new Vector3(5, .28f, 5), "Floor");
            }
            var walls = new GameObject("Cavern walls"); walls.transform.SetParent(env.transform);
            for (int side = -1; side <= 1; side += 2)
            {
                for (int z = -26; z <= -2; z += 6) Fit(Kingdom + "Environments/SM_Env_Rock_Cliff_01.prefab", walls.transform, "Entry rock wall", new Vector3(side * 16, 4, z), new Vector3(9, 14, 9), "Rock");
                for (int z = 2; z <= 10; z += 4) Fit(Kingdom + "Environments/SM_Env_Rock_Cliff_02.prefab", walls.transform, "Narrow passage wall", new Vector3(side * 8, 4, z), new Vector3(8, 14, 7), "Rock");
                for (int z = 16; z <= 40; z += 6) Fit(Kingdom + "Environments/SM_Env_Rock_Cliff_01.prefab", walls.transform, "Sanctum rock wall", new Vector3(side * 21, 5, z), new Vector3(10, 16, 9), "Rock");
            }
            for (int x = -15; x <= 15; x += 6) Fit(Kingdom + "Environments/SM_Env_Rock_Cliff_02.prefab", walls.transform, "Sanctum rear wall", new Vector3(x, 5, 46), new Vector3(9, 16, 9), "Rock");
            for (int x = -10; x <= 10; x += 5) Fit(Kingdom + "Environments/SM_Env_Rock_Cliff_02.prefab", walls.transform, "Entrance wall", new Vector3(x, 4, -31), new Vector3(8, 14, 8), "Rock");
            AddVault(env.transform);
            // 用现有桥栏限定狭道，桥面下沉到石板下，避免多层导航表面。
            Fit(Kingdom + "Environments/SM_Env_Bridge_Stone_Path_01.prefab", env.transform, "Ancient stone bridge", new Vector3(0, -1.4f, 7), new Vector3(6.5f, 3, 14), "Floor");
            var crystals = new GameObject("Crystal veins"); crystals.transform.SetParent(env.transform);
            Vector3[] positions = { new(-10, 1.6f, -21), new(10, 2, -11), new(-10, 2, -3), new(5, 1.5f, 5), new(-5, 1.5f, 11), new(-15, 2, 21), new(15, 2, 25), new(-15, 2.5f, 35), new(14, 2, 39), new(0, 3.5f, 41) };
            for (int i = 0; i < positions.Length; i++)
            {
                float size = i == positions.Length - 1 ? 6 : 3;
                Fit(Nature + "SM_Prop_Crystal_0" + (i % 3 + 1) + ".prefab", crystals.transform, "Crystal vein " + i, positions[i], new Vector3(size, size * 1.5f, size), "Crystal");
                if (i % 2 == 0) { var light = new GameObject("Crystal light").AddComponent<Light>(); light.transform.SetParent(crystals.transform); light.transform.position = positions[i] + Vector3.up * 2; light.type = LightType.Point; light.color = new Color(.2f, .7f, 1); light.range = 12; light.intensity = 3; }
            }
            var surface = env.AddComponent<NavMeshSurface>(); surface.agentTypeID = 1479372276; surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("第二关导航烘焙失败。");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Scenes/Dungeon02/NavMesh-Dungeon02.asset");
            var services = new GameObject("Scene services");
            var scope = services.AddComponent<SceneLifetimeScope>();
            var scopeData = new SerializedObject(scope); scopeData.FindProperty("parentReference.TypeName").stringValue = "GameLifetimeScope"; scopeData.ApplyModifiedPropertiesWithoutUndo();
            var spawner = services.AddComponent<PlayerRigSpawner>(); var spawnerData = new SerializedObject(spawner);
            spawnerData.FindProperty("playerRigPrefab").objectReferenceValue = Asset<GameObject>("Assets/Resources/RPG-Character.prefab"); spawnerData.FindProperty("allowCombat").boolValue = true; spawnerData.ApplyModifiedPropertiesWithoutUndo();
            var entry = new GameObject("Entry - Crystal Cavern"); entry.transform.position = new Vector3(0, .05f, -23);
            var marker = entry.AddComponent<DungeonSpawnAuthoring>(); marker.database = Asset<DungeonDatabase>("Assets/Resources/DungeonDatabase.asset"); marker.dungeonId = 1002;
            var encounter = new GameObject("Crystal Cavern Encounter").AddComponent<DungeonEncounter>();
            encounter.GroupCombat.AllyAlertRadius = 18; encounter.GroupCombat.AttackStartInterval = .9f; encounter.GroupCombat.BurstPause = 1.6f;
            var points = new[] { new Vector3(-4, 0, -10), new Vector3(4, 0, -7), new Vector3(0, 0, -2), new Vector3(-6, 0, 22), new Vector3(6, 0, 25), new Vector3(0, 0, 34) };
            var prefabs = new[] { guard, guard, caster, guard, caster, boss }; var spawns = new DungeonEncounter.Spawn[6];
            var filter = new NavMeshQueryFilter { agentTypeID = 1479372276, areaMask = NavMesh.AllAreas };
            for (int i = 0; i < points.Length; i++)
            {
                if (!NavMesh.SamplePosition(points[i], out var hit, 1, filter)) throw new InvalidOperationException("出生点不在导航上 " + i);
                var path = new NavMeshPath(); if (!NavMesh.CalculatePath(entry.transform.position, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete) throw new InvalidOperationException("入口不能到达出生点 " + i);
                var point = new GameObject("Spawn_" + (i + 1)); point.transform.SetParent(encounter.transform); point.transform.position = hit.position; point.transform.rotation = Quaternion.Euler(0, 180, 0);
                spawns[i] = new DungeonEncounter.Spawn { Prefab = prefabs[i], Point = point.transform };
            }
            encounter.Waves = new[] {
                new DungeonEncounter.Wave { Name = "清理入口守卫", Enemies = spawns.Take(3).ToArray() },
                new DungeonEncounter.Wave { Name = "清理水晶内殿", Enemies = spawns.Skip(3).Take(2).ToArray() },
                new DungeonEncounter.Wave { Name = "击败守望者", Enemies = spawns.Skip(5).ToArray() }
            };
            EditorSceneManager.SaveScene(scene, ScenePath);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var address = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(ScenePath), settings.FindGroup("Scene")); address.address = "Scenes/Dungeon02";
            EditorUtility.SetDirty(settings); EditorUtility.SetDirty(address.parentGroup);
        }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
    }
    private static void WireDatabase()
    {
        var database = Asset<DungeonDatabase>("Assets/Resources/DungeonDatabase.asset"); var entry = database.GetById(1002);
        Undo.RecordObject(database, "Connect Crystal Cavern");
        entry.SceneKey = "Scenes/Dungeon02"; entry.SpawnPosition = new Vector3(0, .05f, -23); entry.SpawnEulerAngles = Vector3.zero;
        entry.Description = "穿过蓝晶照亮的古老石桥，清理枪卫与施放晶蚀领域的祭司，深入内殿挑战晶核守望者。侧移躲开窄角突刺，及时离开地面领域；守望者半血后将释放共鸣连招，收招时可以反击。";
        EditorUtility.SetDirty(database);
    }
    private static void AddVault(Transform environment)
    {
        SealVault(environment);
        if (environment.Find("High rock vault") != null) return;
        var roof = new GameObject("High rock vault"); roof.transform.SetParent(environment);
        for (int z = -24; z <= 42; z += 6)
        {
            float width = z < 0 ? 32 : z < 15 ? 17 : 42;
            Fit(Kingdom + "Environments/SM_Env_Rock_Cliff_02.prefab", roof.transform, "Vault rock", new Vector3(0, 14, z), new Vector3(width, 4, 12), "Rock");
        }
    }
    private static void SealVault(Transform environment)
    {
        if (environment.Find("Cavern foundation") == null)
            Fit(Kingdom + "Castle/SM_Bld_Castle_Floor_Stone_01.prefab", environment, "Cavern foundation", new Vector3(0, -.75f, 7), new Vector3(48, 1, 82), "Floor");
        if (environment.Find("Cavern backing") != null) return;
        var backing = new GameObject("Cavern backing"); backing.transform.SetParent(environment);
        const string stone = Kingdom + "Castle/SM_Bld_Castle_Floor_Stone_01.prefab";
        // 连续外壳封住岩壁轮廓和宽窄房间接缝，避免从低角度看到背景。
        var ceilingCenter = new Vector3(0, 17, 7);
        var ceiling = Fit(stone, backing.transform, "Ceiling backing", ceilingCenter, new Vector3(48, .4f, 84), "Rock");
        ceiling.transform.RotateAround(ceilingCenter, Vector3.right, 180);
        foreach (int side in new[] { -1, 1 })
        {
            var center = new Vector3(24 * side, 8, 7);
            var panel = Fit(stone, backing.transform, "Wall backing", center, new Vector3(18, .4f, 84), "Rock");
            panel.transform.RotateAround(center, Vector3.forward, side * 90);
        }
        foreach (var end in new[] { new Vector3(0, 8, -33), new Vector3(0, 8, 48) })
        {
            var panel = Fit(stone, backing.transform, "End backing", end, new Vector3(48, .4f, 18), "Rock");
            panel.transform.RotateAround(end, Vector3.right, end.z < 0 ? 90 : -90);
        }
    }
}
