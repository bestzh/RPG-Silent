using System;
using System.Collections.Generic;
using System.IO;
using RPGSilent.Domain;
using UnityEngine;

/// <summary>Connects durable progress to the session; never serializes a live dungeon.</summary>
public sealed class PlayerProgressService : IDisposable
{
    private readonly PlayerStats stats;
    private readonly ProgressSaveStore store;
    private readonly Dictionary<string, SkillData> catalog = new();
    private readonly Dictionary<string, WeaponDefinition> weaponCatalog = new();
    private PlayerWeaponEquipment weapons;
    private PlayerSkillController skills;
    private PlayerStanceController stance;
    private PlayerProgressData pending;
    private string profileId, lastSavedPayload;
    private bool newProfile, applying, attached;
    public bool HasUnsavedChanges { get; private set; }
    public string StatusMessage { get; private set; } = string.Empty;
    public bool HasStoredProgress => store.HasFiles;
    public event Action Changed;

    public PlayerProgressService(PlayerStats stats, ProgressSaveStore store)
    {
        this.stats = stats;
        this.store = store;
        foreach (var skill in Resources.LoadAll<SkillData>(string.Empty))
        {
            if (string.IsNullOrEmpty(skill.PersistentId) || catalog.ContainsKey(skill.PersistentId))
                throw new InvalidOperationException("技能存档标识缺失或重复：" + skill.name);
            catalog.Add(skill.PersistentId, skill);
        }
        foreach (var weapon in Resources.LoadAll<WeaponDefinition>("Weapons"))
        {
            if (string.IsNullOrEmpty(weapon.PersistentId) || weaponCatalog.ContainsKey(weapon.PersistentId))
                throw new InvalidOperationException("武器存档标识缺失或重复：" + weapon.name);
            weaponCatalog.Add(weapon.PersistentId, weapon);
        }
    }

    public ProgressLoadResult InspectSave()
    {
        var result = store.Load();
        if (!result.CanContinue) return result;
        foreach (string id in result.Data.skillIds)
            if (id.Length > 0 && !catalog.ContainsKey(id))
                return new ProgressLoadResult { Status = ProgressLoadStatus.Unsupported,
                    Message = "存档中的技能资源不可用，原文件已保留，请恢复对应游戏内容。" };
        foreach (string id in result.Data.weaponIds)
            if (id.Length > 0 && !weaponCatalog.ContainsKey(id))
                return new ProgressLoadResult { Status = ProgressLoadStatus.Unsupported,
                    Message = "存档中的武器资源不可用，原文件已保留，请恢复对应游戏内容。" };
        return result;
    }

    public void BeginNewGame()
    {
        pending = null;
        profileId = Guid.NewGuid().ToString("N");
        lastSavedPayload = null;
        newProfile = true;
        HasUnsavedChanges = false;
        StatusMessage = string.Empty;
        stats.ResetForNewGame();
    }

    public bool BeginContinue(out string message)
    {
        var result = InspectSave();
        message = result.Message;
        if (!result.CanContinue) return false;
        pending = result.Data;
        profileId = pending.profileId;
        // Commit once after restoration, including any version migration or recovery.
        lastSavedPayload = null;
        newProfile = false;
        HasUnsavedChanges = false;
        StatusMessage = result.Status == ProgressLoadStatus.Recovered ? result.Message : "进度已读取";
        stats.RestoreProgress(pending.gold, pending.totalExp);
        return true;
    }

    public void Attach(GameObject rig)
    {
        if (attached) return;
        skills = rig.GetComponentInChildren<PlayerSkillController>(true);
        stance = rig.GetComponentInChildren<PlayerStanceController>(true);
        weapons = rig.GetComponentInChildren<PlayerWeaponEquipment>(true);
        if (skills == null || stance == null || weapons == null) throw new InvalidOperationException("玩家缺少技能、姿态或武器组件。");
        applying = true;
        try
        {
            if (pending != null)
            {
                for (int i = 0; i < PlayerSkillController.MaxSlots; i++)
                    skills.EquipSkill(i, pending.skillIds[i].Length == 0 ? null : catalog[pending.skillIds[i]]);
                var restoredWeapons = new WeaponDefinition[PlayerWeaponEquipment.SlotCount];
                for (int i = 0; i < restoredWeapons.Length; i++)
                    restoredWeapons[i] = pending.weaponIds[i].Length == 0 ? null : weaponCatalog[pending.weaponIds[i]];
                weapons.RestoreEquipment(restoredWeapons, pending.activeWeaponSlot);
                pending = null;
            }
            attached = true;
            stats.OnProgressChanged += OnProgressChanged;
            skills.OnSlotsChanged += OnProgressChanged;
            weapons.EquipmentChanged += OnProgressChanged;
        }
        finally { applying = false; }
        Save(); // New games commit only after a real player is created in the loaded scene.
    }

    public bool Save()
    {
        if (!attached || applying) return true;
        try
        {
            var data = new PlayerProgressData { profileId = profileId, gold = stats.Gold,
                totalExp = stats.Exp, stance = (int)stance.CurrentStance, skillIds = new string[PlayerSkillController.MaxSlots],
                weaponIds = new string[PlayerWeaponEquipment.SlotCount], activeWeaponSlot = weapons.ActiveSlot };
            for (int i = 0; i < data.weaponIds.Length; i++)
            {
                var weapon = weapons.GetWeapon(i);
                if (weapon == null) { data.weaponIds[i] = string.Empty; continue; }
                if (string.IsNullOrEmpty(weapon.PersistentId) || !weaponCatalog.TryGetValue(weapon.PersistentId, out var known) || known != weapon)
                    throw new InvalidDataException("武器尚未配置可保存的标识，请恢复有效武器后重试保存。");
                data.weaponIds[i] = weapon.PersistentId;
            }
            for (int i = 0; i < data.skillIds.Length; i++)
            {
                var skill = skills.GetEquippedSkill(i);
                if (skill == null) { data.skillIds[i] = string.Empty; continue; }
                if (string.IsNullOrEmpty(skill.PersistentId) || !catalog.TryGetValue(skill.PersistentId, out var known) || known != skill)
                    throw new InvalidDataException("技能尚未配置可保存的标识，请恢复有效技能后重试保存。");
                data.skillIds[i] = skill.PersistentId;
            }
            string payload = JsonUtility.ToJson(data);
            if (payload == lastSavedPayload && !HasUnsavedChanges) return true;
            if (!store.TrySave(data, newProfile, out string error)) throw new InvalidDataException(error);
            newProfile = false;
            lastSavedPayload = payload;
            HasUnsavedChanges = false;
            StatusMessage = "进度已保存";
        }
        catch (InvalidDataException ex)
        {
            HasUnsavedChanges = true;
            StatusMessage = ex.Message;
        }
        Changed?.Invoke();
        return !HasUnsavedChanges;
    }

    private void OnProgressChanged() { if (!applying) Save(); }

    public void Detach()
    {
        if (!attached) { pending = null; return; }
        Save();
        stats.OnProgressChanged -= OnProgressChanged;
        if (skills != null) skills.OnSlotsChanged -= OnProgressChanged;
        if (weapons != null) weapons.EquipmentChanged -= OnProgressChanged;
        skills = null;
        stance = null;
        weapons = null;
        attached = false;
        pending = null;
    }

    public void Dispose() => Detach();
}
