using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Changes only clothing renderers. The player's Animator, bones and weapon mounts remain live.</summary>
public sealed class ModularArmorAppearance : IDisposable
{
    readonly Transform owner, meshRoot;
    readonly PlayerGender gender;
    readonly Dictionary<string, Transform> bones = new();
    readonly List<(Renderer renderer, ArmorBodyRegion region, bool enabled)> defaults = new();
    readonly ArmorAppearanceDefinition[] current = new ArmorAppearanceDefinition[3];
    readonly GameObject[] visuals = new GameObject[3];
    static readonly int Primary = Shader.PropertyToID("_Color_Primary"), Secondary = Shader.PropertyToID("_Color_Secondary"),
        Metal = Shader.PropertyToID("_Color_Metal_Primary"), MetalSecondary = Shader.PropertyToID("_Color_Metal_Secondary");

    public ModularArmorAppearance(Transform owner, CharacterRigProfile profile, PlayerGender gender)
    {
        this.owner = owner; this.gender = gender;
        meshRoot = owner.Find(profile.ArmorMeshRoot);
        if (meshRoot == null) throw new InvalidOperationException("角色缺少换装模型根节点。");
        foreach (var binding in profile.ArmorBodyParts)
        {
            var target = owner.Find(binding.RendererPath);
            var renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer == null) throw new InvalidOperationException("角色缺少换装部件：" + binding.RendererPath);
            defaults.Add((renderer, binding.Region, renderer.enabled));
        }
    }
    Transform Bone(string path)
    {
        if (!bones.TryGetValue(path, out var bone))
        {
            bone = owner.Find(path);
            if (bone == null) throw new InvalidOperationException("防具缺少绑定骨骼：" + path);
            bones.Add(path, bone);
        }
        return bone;
    }
    public void Apply(IReadOnlyList<ItemDefinition> equipped)
    {
        for (int slot = 0; slot < current.Length; slot++)
        {
            ArmorAppearanceDefinition next = null;
            for (int i = 0; i < equipped.Count; i++)
                if ((int)equipped[i].Slot == slot) { next = equipped[i].Appearance; break; }
            if (next == current[slot]) continue;
            if (next != null && (int)next.Slot != slot) throw new InvalidOperationException("防具外观与装备槽不匹配。");
            // Construct and validate first so a bad asset cannot hide the current outfit.
            var replacement = next != null ? Create(next) : null;
            Release(visuals[slot]); visuals[slot] = replacement; current[slot] = next;
        }
        var hidden = ArmorBodyRegion.None;
        foreach (var appearance in current) if (appearance != null) hidden |= appearance.ForGender(gender).HiddenRegions;
        foreach (var part in defaults) if (part.renderer != null) part.renderer.enabled = part.enabled && (part.region & hidden) == 0;
    }
    GameObject Create(ArmorAppearanceDefinition appearance)
    {
        var variant = appearance.ForGender(gender);
        if (variant == null || variant.Parts == null || variant.Parts.Length == 0) throw new InvalidOperationException("防具缺少男女外观配置。");
        foreach (var part in variant.Parts)
        {
            if (part.Mesh == null || part.Materials == null || part.Materials.Length == 0 ||
                part.BonePaths == null || part.BonePaths.Length != part.Mesh.bindposes.Length)
                throw new InvalidOperationException("防具网格或骨骼配置无效。");
            Bone(part.RootBonePath); foreach (var path in part.BonePaths) Bone(path);
        }
        var root = new GameObject("Armor visual " + appearance.Slot); root.transform.SetParent(meshRoot, false); root.layer = meshRoot.gameObject.layer;
        try
        {
            var colors = new MaterialPropertyBlock();
            colors.SetColor(Primary, appearance.Primary); colors.SetColor(Secondary, appearance.Secondary);
            colors.SetColor(Metal, appearance.Metal); colors.SetColor(MetalSecondary, appearance.Secondary);
            foreach (var part in variant.Parts)
            {
                var go = new GameObject(part.Mesh.name); go.layer = root.layer; go.transform.SetParent(root.transform, false);
                var renderer = go.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh = part.Mesh;
                var mapped = new Transform[part.BonePaths.Length];
                for (int i = 0; i < mapped.Length; i++) mapped[i] = Bone(part.BonePaths[i]);
                renderer.bones = mapped; renderer.rootBone = Bone(part.RootBonePath); renderer.localBounds = part.Bounds;
                renderer.sharedMaterials = part.Materials; renderer.skinnedMotionVectors = false;
                renderer.SetPropertyBlock(colors);
            }
            return root;
        }
        catch { Release(root); throw; }
    }
    static void Release(GameObject root)
    {
        if (root == null) return;
        root.SetActive(false);
        if (Application.isPlaying) UnityEngine.Object.Destroy(root); else UnityEngine.Object.DestroyImmediate(root);
    }
    public void Dispose()
    {
        for (int i = 0; i < visuals.Length; i++) { Release(visuals[i]); visuals[i] = null; current[i] = null; }
        foreach (var part in defaults) if (part.renderer != null) part.renderer.enabled = part.enabled;
        defaults.Clear(); bones.Clear();
    }
}
