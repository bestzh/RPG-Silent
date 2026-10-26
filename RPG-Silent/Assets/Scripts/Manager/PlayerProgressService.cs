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
    }

    public ProgressLoadResult InspectSave()
    {
        var result = store.Load();
        if (!result.CanContinue) return result;
        foreach (string id in result.Data.skillIds)
            if (id.Length > 0 && !catalog.ContainsKey(id))
                return new ProgressLoadResult { Status = ProgressLoadStatus.Unsupported,
                    Message = "存档中的技能资源不可用，原文件已保留，请恢复对应游戏内容。" };
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
        lastSavedPayload = result.Status == ProgressLoadStatus.Loaded ? JsonUtility.ToJson(pending) : null;
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
        if (skills == null || stance == null) throw new InvalidOperationException("玩家缺少技能或姿态组件。");
        applying = true;
        try
        {
            if (pending != null)
            {
                for (int i = 0; i < PlayerSkillController.MaxSlots; i++)
                    skills.EquipSkill(i, pending.skillIds[i].Length == 0 ? null : catalog[pending.skillIds[i]]);
                stance.SetStance((PlayerStance)pending.stance, true);
                pending = null;
            }
            attached = true;
            stats.OnProgressChanged += OnProgressChanged;
            skills.OnSlotsChanged += OnProgressChanged;
            stance.OnStanceChanged += OnStanceChanged;
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
                totalExp = stats.Exp, stance = (int)stance.CurrentStance, skillIds = new string[PlayerSkillController.MaxSlots] };
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
    private void OnStanceChanged(PlayerStance previous, PlayerStance current) => OnProgressChanged();

    public void Detach()
    {
        if (!attached) { pending = null; return; }
        Save();
        stats.OnProgressChanged -= OnProgressChanged;
        if (skills != null) skills.OnSlotsChanged -= OnProgressChanged;
        if (stance != null) stance.OnStanceChanged -= OnStanceChanged;
        skills = null;
        stance = null;
        attached = false;
        pending = null;
    }

    public void Dispose() => Detach();
}
