using System.Collections.Generic;
using UnityEngine;

/// <summary>Retargets the existing player Animator without replacing the persistent player rig.</summary>
[RequireComponent(typeof(Animator))]
public sealed class PlayerClassAppearance : MonoBehaviour
{
    public Transform[] LegacyVisuals;
    public float VisualScale = 1.428571f;
    readonly List<GameObject> roots = new();
    public PlayerClassDefinition Definition { get; private set; }
    public PlayerGender Gender { get; private set; }
    public CharacterRigProfile RigProfile { get; private set; }
    public void Apply(PlayerClassDefinition definition) => ApplySelection(definition, PlayerGender.Legacy);
    public void ApplySelection(PlayerClassDefinition definition, PlayerGender gender)
    {
        if (definition == null || (Definition == definition && Gender == gender)) return;
        var prefab = definition.GetModel(gender);
        if (prefab == null) throw new System.InvalidOperationException("职业外观资源未配置。");
        var source = prefab.GetComponent<Animator>();
        if (source == null || source.avatar == null || !source.avatar.isHuman)
            throw new System.InvalidOperationException("职业模型需要有效的人形 Avatar。");
        GetComponent<PlayerWeaponEquipment>()?.PrepareAppearanceChange();
        RigProfile = prefab.GetComponent<CharacterRigProfile>();
        foreach (var old in roots) if (old != null) { old.SetActive(false); old.transform.SetParent(null); Destroy(old); }
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
        Destroy(instance);
        var animator = GetComponent<Animator>();
        animator.avatar = source.avatar;
        animator.Rebind();
        animator.Update(0f);
        Definition = definition;
        Gender = gender;
    }
}
