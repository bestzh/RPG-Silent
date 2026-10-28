using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Retargets the existing player Animator without replacing the persistent player rig.</summary>
[RequireComponent(typeof(Animator))]
public sealed class PlayerClassAppearance : MonoBehaviour
{
    public Transform[] LegacyVisuals;
    public float VisualScale = 1.428571f;
    readonly List<GameObject> roots = new();
    ModularArmorAppearance armor;
    GameObject currentPrefab;
    public PlayerClassDefinition Definition { get; private set; }
    public PlayerGender Gender { get; private set; }
    public CharacterRigProfile RigProfile { get; private set; }
    public void Apply(PlayerClassDefinition definition) => ApplySelection(definition, PlayerGender.Legacy);
    public void ApplySelection(PlayerClassDefinition definition, PlayerGender gender)
        => ApplyLoadout(definition, gender, System.Array.Empty<ItemDefinition>());
    public bool ApplyLoadout(PlayerClassDefinition definition, PlayerGender gender, IEnumerable<ItemDefinition> equipped)
    {
        if (definition == null) return false;
        var items = equipped.Where(x => x != null && x.Kind == ItemKind.Equipment).ToArray();
        // Pre-selection saves keep their original whole-body model until visible armor is worn.
        var visualGender = definition.GetVisualGender(gender, items.Any(x => x.Appearance != null));
        var prefab = definition.GetModel(visualGender);
        if (Definition == definition && Gender == gender && currentPrefab == prefab)
        { armor?.Apply(items); return false; }
        if (prefab == null) throw new System.InvalidOperationException("职业外观资源未配置。");
        var source = prefab.GetComponent<Animator>();
        if (source == null || source.avatar == null || !source.avatar.isHuman)
            throw new System.InvalidOperationException("职业模型需要有效的人形 Avatar。");
        GetComponent<PlayerWeaponEquipment>()?.PrepareAppearanceChange();
        armor?.Dispose(); armor = null;
        RigProfile = prefab.GetComponent<CharacterRigProfile>();
        foreach (var old in roots) if (old != null) { old.SetActive(false); old.transform.SetParent(null); Release(old); }
        roots.Clear();
        if (LegacyVisuals != null) foreach (var visual in LegacyVisuals) if (visual != null) visual.gameObject.SetActive(false);
        var instance = Instantiate(prefab);
        instance.SetActive(false);
        while (instance.transform.childCount > 0)
        {
            var child = instance.transform.GetChild(0);
            child.SetParent(transform, false);
            child.localPosition *= VisualScale;
            child.localScale *= VisualScale;
            roots.Add(child.gameObject);
        }
        Release(instance);
        var animator = GetComponent<Animator>();
        animator.avatar = source.avatar;
        animator.Rebind();
        animator.Update(0f);
        Definition = definition;
        Gender = gender;
        currentPrefab = prefab;
        if (RigProfile != null && !string.IsNullOrEmpty(RigProfile.ArmorMeshRoot))
        {
            armor = new ModularArmorAppearance(transform, RigProfile, visualGender);
            armor.Apply(items);
        }
        return true;
    }
    static void Release(GameObject root) { if (Application.isPlaying) Destroy(root); else DestroyImmediate(root); }
    void OnDestroy() { armor?.Dispose(); armor = null; }
}
