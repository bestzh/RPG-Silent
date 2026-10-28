using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPGSilent.Domain;
using UnityEngine;

/// <summary>Owns durable progression. Transactions reach disk before charging or granting.</summary>
public sealed partial class PlayerProgressService : IDisposable
{
    readonly PlayerStats stats;
    readonly ProgressSaveStore store;
    readonly Dictionary<string, SkillData> catalog = new();
    readonly Dictionary<string, WeaponDefinition> weaponCatalog = new();
    readonly Dictionary<string, PlayerClassDefinition> classes = new();
    readonly Dictionary<string, int> learned = new();
    PlayerWeaponEquipment weapons;
    PlayerSkillController skills;
    PlayerStanceController stance;
    PlayerClassAppearance appearance;
    PlayerProgressData pending;
    string profileId, lastSavedPayload, classId = "swordsman";
    bool newProfile, applying, attached;
    public const int MaxWeaponLevel = 5;
    public bool HasUnsavedChanges { get; private set; }
    public string StatusMessage { get; private set; } = string.Empty;
    public bool HasStoredProgress => store.ListCharacters(out _).Length > 0;
    public PlayerClassDefinition CurrentClass => classes.TryGetValue(classId, out var c) ? c : null;
    public PlayerGender CurrentGender { get; private set; } = PlayerGender.Male;
    public IEnumerable<PlayerClassDefinition> Classes => classes.Values.OrderBy(c => c.Id);
    public int Gold => stats.Gold;
    public int Level => stats.Level;
    public event Action Changed;

    public PlayerProgressService(PlayerStats stats, ProgressSaveStore store)
    {
        this.stats = stats; this.store = store;
        foreach (var s in Resources.LoadAll<SkillData>(string.Empty))
            if (string.IsNullOrEmpty(s.PersistentId) || !catalog.TryAdd(s.PersistentId, s))
                throw new InvalidOperationException("技能标识缺失或重复：" + s.name);
        foreach (var w in Resources.LoadAll<WeaponDefinition>("Weapons"))
            if (string.IsNullOrEmpty(w.PersistentId) || !weaponCatalog.TryAdd(w.PersistentId, w))
                throw new InvalidOperationException("武器标识缺失或重复：" + w.name);
        foreach (var c in Resources.LoadAll<PlayerClassDefinition>("Classes"))
            if (string.IsNullOrEmpty(c.Id) || !classes.TryAdd(c.Id, c))
                throw new InvalidOperationException("职业标识缺失或重复：" + c.name);
        LoadItemCatalog();
        LoadQuestCatalog();
    }
    public ProgressLoadResult InspectSave() => ValidateLoadedCharacter(store.Load());
    ProgressLoadResult ValidateLoadedCharacter(ProgressLoadResult result)
    {
        if (!result.CanContinue) return result;
        var d = result.Data;
        if (!classes.TryGetValue(d.classId, out var definition) || definition.GetModel((PlayerGender)d.gender) == null || d.learnedSkills.Any(e => !catalog.ContainsKey(e.skillId)) ||
            d.weaponIds.Any(id => id.Length > 0 && !weaponCatalog.ContainsKey(id)) || d.weaponUpgrades.Any(e => !weaponCatalog.ContainsKey(e.weaponId)))
            return new ProgressLoadResult { Status = ProgressLoadStatus.Unsupported, Message = "存档中的职业、技能、武器或物品资源不可用，原文件已保留。" };
        try
        {
            EquipmentInstances.Migrate(d, FindItem);
            ProgressSaveStore.Validate(d);
            if (!ValidInventoryContent(d.inventory)) throw new InvalidDataException();
            if (!ValidQuestContent(d.quests, d.classId)) throw new InvalidDataException();
            if (!ValidConsumableContent(d.consumables)) throw new InvalidDataException();
        }
        catch (InvalidDataException) { return new ProgressLoadResult { Status = ProgressLoadStatus.Unsupported, Message = "装备、任务、药品记录或对应资源不可用，原文件已保留。" }; }
        return result;
    }
    public void BeginNewGame() => BeginNewGameAs("swordsman");
    public void BeginNewGameAs(string id) => BeginNewCharacter(id, PlayerGender.Male);
    public void BeginNewCharacter(string id, PlayerGender gender)
    {
        ValidateNewCharacter(id, gender);
        if (!TryBeginNewCharacter(id, gender, out string message)) throw new InvalidOperationException(message);
    }
    public bool BeginContinue(out string message)
    {
        if (!store.HasFiles)
        {
            var recent = GetCharacters(out _).FirstOrDefault(x => x.Progress.CanContinue);
            if (recent != null) return BeginContinueCharacter(recent.Key, out message);
        }
        var result = InspectSave(); message = result.Message;
        if (!result.CanContinue) return false;
        ApplyLoadedCharacter(result); return true;
    }
    void ApplyLoadedCharacter(ProgressLoadResult result)
    {
        pending = result.Data; profileId = pending.profileId; ApplyRecords(pending);
        lastSavedPayload = null; newProfile = false; HasUnsavedChanges = false;
        StatusMessage = result.Status == ProgressLoadStatus.Recovered ? result.Message : "进度已读取";
        stats.RestoreProgress(pending.gold, pending.totalExp);
    }
    public void Attach(GameObject rig)
    {
        if (attached) return;
        skills = rig.GetComponentInChildren<PlayerSkillController>(true);
        stance = rig.GetComponentInChildren<PlayerStanceController>(true);
        weapons = rig.GetComponentInChildren<PlayerWeaponEquipment>(true);
        appearance = rig.GetComponentInChildren<PlayerClassAppearance>(true);
        if (skills == null || stance == null || weapons == null) throw new InvalidOperationException("玩家缺少技能、姿态或武器组件。");
        applying = true;
        try
        {
            appearance?.ApplyLoadout(CurrentClass, CurrentGender, EquippedItems);
            for (int i = 0; i < PlayerSkillController.MaxSlots; i++) skills.EquipSkill(i,
                pending == null || pending.skillIds[i].Length == 0 ? null : catalog[pending.skillIds[i]]);
            var restored = new WeaponDefinition[PlayerWeaponEquipment.SlotCount];
            if (pending != null)
                for (int i = 0; i < restored.Length; i++) restored[i] = pending.weaponIds[i].Length == 0 ? null : weaponCatalog[pending.weaponIds[i]];
            else
            {
                restored[0] = CurrentClass.StartingWeapon;
                restored[1] = classId == "swordsman" ? weaponCatalog[WeaponDefinition.StarterGreatswordId] : weaponCatalog[WeaponDefinition.StarterSwordId];
            }
            weapons.RestoreInstanceIds(pending?.weaponInstanceIds ?? restored.Select(w => ResolveWeaponKey(w)).ToArray());
            weapons.RestoreEquipment(restored, pending?.activeWeaponSlot ?? 0);
            pending = null; attached = true;
            ApplyEquipmentBonuses();
            stats.OnProgressChanged += OnProgressChanged; skills.OnSlotsChanged += OnProgressChanged; weapons.EquipmentChanged += OnProgressChanged;
        }
        finally { applying = false; }
        Save();
    }
    public int GetSkillRank(SkillData s) => s != null && learned.TryGetValue(s.PersistentId, out int rank) ? rank : 0;
    public int GetWeaponLevel(WeaponDefinition w) => GetInstanceLevel(ResolveWeaponKey(w));
    public float SkillDamageMultiplier(SkillData s) => (1f + Mathf.Max(0, GetSkillRank(s) - 1) * s.DamagePerRank) * (CurrentClass?.DamageMultiplier ?? 1f) * EquipmentDamageMultiplier;
    public float WeaponDamageMultiplier(WeaponDefinition w) => (1f + GetWeaponLevel(w) * 0.1f) * (CurrentClass?.DamageMultiplier ?? 1f) * EquipmentDamageMultiplier * (w != null ? w.DamageMultiplier : 1f);
    public IEnumerable<SkillData> AvailableLessons => CurrentClass == null ? Enumerable.Empty<SkillData>() :
        (CurrentClass.IsAdvanced ? CurrentClass.BaseClass.Lessons : Array.Empty<SkillData>()).Concat(CurrentClass.Lessons).Where(s => s != null).Distinct();
    public IEnumerable<SkillData> LearnedSkills => learned.Keys.Select(id => catalog[id]);
    public bool CanStudy(SkillData s) => s != null && AvailableLessons.Contains(s);
    public int SkillRankCap(SkillData s) => Mathf.Min(s.MaxRank, CurrentClass != null && CurrentClass.IsAdvanced ? 5 : 3);
    public int WeaponUpgradeCost(int nextLevel) => 100 * nextLevel;
    public WeaponDefinition EquippedWeapon(int slot) => weapons != null ? weapons.GetWeapon(slot) : null;

    public bool TryTrainSkill(SkillData s, int expectedRank, out string message)
    {
        message = "该导师无法教授这个技能。";
        if (!attached || !CanStudy(s) || !catalog.TryGetValue(s.PersistentId, out var known) || known != s) return false;
        int rank = GetSkillRank(s);
        if (rank != expectedRank) { message = "技能等级已变化，请重新选择。"; return false; }
        if (rank >= SkillRankCap(s)) { message = "已达到当前职业的技能等级上限。"; return false; }
        int next = rank + 1;
        if (stats.Level < s.LevelForRank(next)) { message = $"需要角色等级 {s.LevelForRank(next)}。"; return false; }
        var d = Capture();
        d.learnedSkills = d.learnedSkills.Where(e => e.skillId != s.PersistentId).Append(new SkillProgressData { skillId = s.PersistentId, rank = next }).ToArray();
        if (rank == 0) { int empty = Array.IndexOf(d.skillIds, string.Empty); if (empty >= 0) d.skillIds[empty] = s.PersistentId; }
        return CommitPurchase(d, s.CostForRank(next), $"{s.SkillName} 已提升至 {next} 级。", out message);
    }
    public bool TryEquipLearnedSkill(SkillData s, int slot, out string message)
    {
        message = "尚未学会该技能或槽位无效。";
        if (!attached || slot < 0 || slot >= PlayerSkillController.MaxSlots ||
            (s != null && (GetSkillRank(s) == 0 || !catalog.TryGetValue(s.PersistentId, out var known) || known != s))) return false;
        var d = Capture();
        if (s != null) for (int i = 0; i < d.skillIds.Length; i++) if (d.skillIds[i] == s.PersistentId) d.skillIds[i] = string.Empty;
        d.skillIds[slot] = s == null ? string.Empty : s.PersistentId;
        return CommitPurchase(d, 0, $"已更新技能槽 {(slot + 1) % 10}。", out message);
    }
    public bool TryUpgradeWeapon(WeaponDefinition w, int expectedLevel, out string message)
    {
        return TryUpgradeWeaponInstance(ResolveWeaponKey(w), expectedLevel, out message);
    }
    public bool TryUpgradeWeaponInstance(string key, int expectedLevel, out string message)
    {
        message = "请先装备要强化的武器。";
        var item = FindItem(key);
        if (!attached || item?.Kind != ItemKind.Weapon || inventory.Find(key) == null || weapons.IsBusy ||
            !Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Any(i => weapons.GetInstanceId(i) == key)) return false;
        int level = GetInstanceLevel(key);
        if (level != expectedLevel) { message = "强化等级已变化，请重新选择。"; return false; }
        if (level >= MaxWeaponLevel) { message = "武器已达到强化上限。"; return false; }
        var d = Capture();
        d.inventory.Find(key).upgradeLevel = level + 1;
        return CommitPurchase(d, WeaponUpgradeCost(level + 1), $"{item.DisplayName} 强化至 +{level + 1}。", out message);
    }
    public bool TryAdvanceClass(PlayerClassDefinition target, out string message)
    {
        message = "请选择当前基础职业的转职分支。";
        if (!attached || CurrentClass == null || CurrentClass.IsAdvanced || target == null || target.BaseClass != CurrentClass ||
            !classes.TryGetValue(target.Id, out var known) || known != target) return false;
        if (stats.Level < target.RequiredLevel) { message = $"转职需要 {target.RequiredLevel} 级。"; return false; }
        var d = Capture(); d.classId = target.Id;
        return CommitPurchase(d, target.AdvancementCost, $"已转职为{target.DisplayName}，开放专属技能和 5 级技能上限。", out message);
    }
    bool CommitPurchase(PlayerProgressData d, int cost, string success, out string message)
    {
        message = "请在主城导师或铁匠处操作。";
        if (!attached || skills.GetComponent<PlayerController>().CombatAllowed || stats.IsDead || cost < 0) return false;
        if (stats.Gold < cost) { message = $"金币不足，需要 {cost} 金币。"; return false; }
        d.gold = stats.Gold - cost;
        if (!store.TrySave(d, newProfile, out message)) { StatusMessage = message; HasUnsavedChanges = true; Changed?.Invoke(); return false; }
        applying = true;
        try
        {
            bool changedClass = classId != d.classId;
            ApplyRecords(d); stats.TrySpendGold(cost);
            for (int i = 0; i < d.skillIds.Length; i++)
                if ((skills.GetEquippedSkill(i)?.PersistentId ?? string.Empty) != d.skillIds[i])
                    skills.EquipSkill(i, d.skillIds[i].Length == 0 ? null : catalog[d.skillIds[i]]);
            if (changedClass)
            {
                weapons.CancelTransition(); appearance?.ApplyLoadout(CurrentClass, CurrentGender, EquippedItems);
                weapons.RestoreEquipment(Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Select(weapons.GetWeapon).ToArray(), weapons.ActiveSlot);
            }
            newProfile = false; lastSavedPayload = JsonUtility.ToJson(d); HasUnsavedChanges = false; message = StatusMessage = success;
        }
        finally { applying = false; }
        Changed?.Invoke(); return true;
    }
    void ApplyRecords(PlayerProgressData d)
    {
        classId = d.classId; CurrentGender = (PlayerGender)d.gender; learned.Clear();
        foreach (var e in d.learnedSkills) learned.Add(e.skillId, e.rank);
        inventory = d.inventory.Copy();
        journal = d.quests.Copy();
        consumables = d.consumables.Copy();
        ApplyEquipmentBonuses();
    }
    PlayerProgressData Capture()
    {
        var d = new PlayerProgressData { profileId = profileId, gold = stats.Gold, totalExp = stats.Exp,
            stance = (int)stance.CurrentStance, classId = classId, gender = (int)CurrentGender, activeWeaponSlot = weapons.ActiveSlot,
            skillIds = new string[PlayerSkillController.MaxSlots], weaponIds = new string[PlayerWeaponEquipment.SlotCount],
            learnedSkills = learned.OrderBy(p => p.Key).Select(p => new SkillProgressData { skillId = p.Key, rank = p.Value }).ToArray(),
            weaponUpgrades = Array.Empty<WeaponUpgradeData>(), equipmentVersion = 1, quests = journal.Copy(), consumables = consumables.Copy(),
            weaponInstanceIds = Enumerable.Range(0, PlayerWeaponEquipment.SlotCount).Select(weapons.GetInstanceId).ToArray(), inventory = inventory.Copy() };
        for (int i = 0; i < d.weaponIds.Length; i++)
        {
            var w = weapons.GetWeapon(i); d.weaponIds[i] = w?.PersistentId ?? string.Empty;
            if (w != null && (!weaponCatalog.TryGetValue(d.weaponIds[i], out var known) || known != w)) throw new InvalidDataException("武器资源无效，请恢复有效装备后重试保存。");
        }
        for (int i = 0; i < d.skillIds.Length; i++)
        {
            var s = skills.GetEquippedSkill(i); d.skillIds[i] = s?.PersistentId ?? string.Empty;
            if (s != null && (!catalog.TryGetValue(d.skillIds[i], out var known) || known != s || GetSkillRank(s) == 0)) throw new InvalidDataException("技能尚未学会或资源无效，请恢复有效技能后重试保存。");
        }
        return d;
    }
    public bool Save()
    {
        if (!attached || applying) return true;
        try
        {
            var d = Capture(); string payload = JsonUtility.ToJson(d);
            if (payload == lastSavedPayload && !HasUnsavedChanges) { Changed?.Invoke(); return true; }
            if (!store.TrySave(d, newProfile, out string error)) throw new InvalidDataException(error);
            newProfile = false; lastSavedPayload = payload; HasUnsavedChanges = false; StatusMessage = "进度已保存";
        }
        catch (InvalidDataException ex) { HasUnsavedChanges = true; StatusMessage = ex.Message; }
        Changed?.Invoke(); return !HasUnsavedChanges;
    }
    void OnProgressChanged() { if (!applying) { ApplyEquipmentBonuses(); Save(); } }
    public void Detach()
    {
        if (!attached) { pending = null; return; }
        Save(); stats.OnProgressChanged -= OnProgressChanged;
        if (skills != null) skills.OnSlotsChanged -= OnProgressChanged;
        if (weapons != null) weapons.EquipmentChanged -= OnProgressChanged;
        skills = null; stance = null; weapons = null; appearance = null; attached = false; pending = null;
    }
    public void Dispose() => Detach();
}
