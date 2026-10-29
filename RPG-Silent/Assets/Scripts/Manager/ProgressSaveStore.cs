using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using RPGSilent.Domain;
using UnityEngine;

public enum ProgressLoadStatus { Missing, Loaded, Recovered, Corrupt, Unsupported, Unavailable }

public sealed class ProgressLoadResult
{
    public ProgressLoadStatus Status;
    public PlayerProgressData Data;
    public string Message;
    public bool CanContinue => Status == ProgressLoadStatus.Loaded || Status == ProgressLoadStatus.Recovered;
}

/// <summary>Selected local character. Writes a flushed temporary file, then atomically replaces it.</summary>
public sealed partial class ProgressSaveStore
{
    public const int CurrentVersion = 19;
    private const int MaxFileBytes = 512 * 1024;
    private readonly string rootDirectory;
    private string directory;
    public string LastFailureDiagnostic { get; private set; }
    public string PrimaryPath => Path.Combine(directory, "progress.json");
    public string BackupPath => Path.Combine(directory, "progress.backup.json");
    public bool HasFiles => File.Exists(PrimaryPath) || File.Exists(BackupPath);

    [Serializable]
    private sealed class Envelope
    {
        public int version;
        public string savedUtc;
        public string payload;
        public string checksum;
    }

    public ProgressSaveStore(string directory)
    {
        rootDirectory = this.directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
    }

    public ProgressLoadResult Load() => ReadCharacter(CurrentCharacterKey);

    private static ProgressLoadResult LoadDirectory(string folder)
    {
        var primary = Read(Path.Combine(folder, "progress.json"));
        // Never silently downgrade a save written by a newer/different format.
        if (primary.CanContinue || primary.Status == ProgressLoadStatus.Unsupported ||
            primary.Status == ProgressLoadStatus.Unavailable) return primary;
        var backup = Read(Path.Combine(folder, "progress.backup.json"));
        if (backup.CanContinue)
            return new ProgressLoadResult { Status = ProgressLoadStatus.Recovered, Data = backup.Data,
                Message = "主存档不可用，将从上一次备份恢复。" };
        if (backup.Status == ProgressLoadStatus.Unsupported || backup.Status == ProgressLoadStatus.Unavailable) return backup;
        return primary.Status == ProgressLoadStatus.Missing ? backup : primary;
    }

    public bool TrySave(PlayerProgressData data, bool newProfile, out string error)
    {
        error = null;
        LastFailureDiagnostic = null;
        try
        {
            Validate(data);
            if (directory != rootDirectory && !string.Equals(data.profileId, Path.GetFileName(directory), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("角色标识与存档目录不一致，原文件已保留。");
            Directory.CreateDirectory(directory);
            string payload = JsonUtility.ToJson(data);
            string text = JsonUtility.ToJson(new Envelope { version = CurrentVersion,
                savedUtc = DateTime.UtcNow.ToString("O"), payload = payload, checksum = Hash(payload) }, true);
            if (Encoding.UTF8.GetByteCount(text) > MaxFileBytes) throw new InvalidDataException("存档容量已满，原文件已保留。");
            var existing = Read(PrimaryPath);
            if (!newProfile && existing.Status == ProgressLoadStatus.Unsupported)
                throw new InvalidDataException(existing.Message);
            if (!newProfile && existing.Status == ProgressLoadStatus.Unavailable)
                throw new IOException(existing.Message);
            if (newProfile)
            {
                // Explicit new-game confirmation permits replacing the slot. Preserve old
                // files separately; its recovery backup must belong to the new profile.
                Archive(PrimaryPath);
                Archive(BackupPath);
                AtomicWrite(BackupPath, text, null);
                AtomicWrite(PrimaryPath, text, null);
            }
            else
            {
                // Do not rotate a corrupt primary over the good recovery backup.
                AtomicWrite(PrimaryPath, text, existing.CanContinue ? BackupPath : null);
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
        {
            LastFailureDiagnostic = ex.GetType().Name + " (0x" + ex.HResult.ToString("X8") + "): " + ex.Message + "\n" + ex.StackTrace;
            error = "进度未保存，请检查磁盘空间或目录权限后重试。";
            if (ex is InvalidDataException) error = ex.Message;
            return false;
        }
    }

    private void Archive(string path)
    {
        if (!File.Exists(path)) return;
        string folder = Path.Combine(directory, "archives");
        Directory.CreateDirectory(folder);
        File.Copy(path, Path.Combine(folder, DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" +
            Guid.NewGuid().ToString("N") + "-" + Path.GetFileName(path)));
    }

    private static void AtomicWrite(string destination, string text, string backup)
    {
        string temporary = destination + ".tmp";
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        // Recovery displaces the corrupt primary separately from the known-good backup.
        if (File.Exists(destination)) ReplaceFlushedFile(temporary, destination, backup ?? destination + ".replaced", bytes, File.Replace);
        else File.Move(temporary, destination);
    }

    internal static void ReplaceFlushedFile(string temporary, string destination, string backup, byte[] candidate,
        Action<string, string, string> replace)
    {
        byte[] original = File.ReadAllBytes(destination);
        for (int attempt = 0; ; attempt++)
        {
            try { replace(temporary, destination, backup); return; }
            catch (IOException error) when ((error.HResult & 0xffff) == 1175 && attempt < 2)
            {
                // Windows 1175 retains both names. Local probes reproduced this with
                // unchanged contents and immediate retry success. Never retry another
                // error, or commit over files changed by another writer.
                if (!FileContentMatches(destination, original) || !FileContentMatches(temporary, candidate)) throw;
            }
        }
    }
    static bool FileContentMatches(string path, byte[] expected)
    {
        try { return File.ReadAllBytes(path).SequenceEqual(expected); }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private static ProgressLoadResult Read(string path)
    {
        if (!File.Exists(path)) return new ProgressLoadResult { Status = ProgressLoadStatus.Missing, Message = "尚无存档，请开始新游戏。" };
        try
        {
            if (new FileInfo(path).Length > MaxFileBytes) throw new InvalidDataException();
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Encoding.UTF8));
            if (envelope == null) throw new InvalidDataException();
            if (envelope.version < 1 || envelope.version > CurrentVersion)
                return new ProgressLoadResult { Status = ProgressLoadStatus.Unsupported,
                    Message = "存档版本不兼容，原文件已保留，请使用对应版本的游戏。" };
            if (string.IsNullOrEmpty(envelope.payload) || envelope.checksum != Hash(envelope.payload)) throw new InvalidDataException();
            var data = JsonUtility.FromJson<PlayerProgressData>(envelope.payload);
            if (data != null && envelope.version == 1)
            {
                data.weaponIds = new[] { WeaponDefinition.StarterSwordId, WeaponDefinition.StarterGreatswordId };
                data.activeWeaponSlot = data.stance == (int)PlayerStance.TwoHandSword ? 1 : 0;
                if (data.stance == (int)PlayerStance.Unarmed || data.stance == (int)PlayerStance.Relax)
                    data.weaponIds[0] = string.Empty;
            }
            if (data != null && envelope.version < 3)
            {
                data.classId = "swordsman";
                data.learnedSkills = (data.skillIds ?? new string[0]).Where(id => !string.IsNullOrEmpty(id)).Distinct()
                    .Select(id => new SkillProgressData { skillId = id, rank = 1 }).ToArray();
                data.weaponUpgrades = new WeaponUpgradeData[0];
            }
            if (data != null && envelope.version < 4) data.gender = (int)PlayerGender.Legacy;
            if (data != null && envelope.version < 5)
                data.inventory = InventoryData.FromLegacyWeapons((data.weaponIds ?? Array.Empty<string>())
                    .Concat((data.weaponUpgrades ?? Array.Empty<WeaponUpgradeData>()).Where(e => e != null).Select(e => e.weaponId)));
            if (data?.inventory != null && envelope.version < 7)
                foreach (var item in (data.inventory.bag ?? Array.Empty<ItemStackData>()).Concat(data.inventory.overflow ?? Array.Empty<ItemStackData>()))
                    if (item != null) item.gemId = string.Empty;
            if (data?.inventory != null && envelope.version < 8)
            {
                data.inventory.loadouts = Array.Empty<EquipmentLoadoutData>();
                foreach (var item in (data.inventory.bag ?? Array.Empty<ItemStackData>()).Concat(data.inventory.overflow ?? Array.Empty<ItemStackData>()))
                    if (item != null) item.isLocked = false;
            }
            if (data != null && envelope.version < 9) data.quests = new QuestJournalData();
            if (data != null && envelope.version < 10) data.consumables = new ConsumableLoadoutData();
            if (data != null && envelope.version < 11) data.world = new WorldProgressData();
            if (data?.inventory != null && envelope.version < 12)
                foreach (var item in (data.inventory.bag ?? Array.Empty<ItemStackData>()).Concat(data.inventory.overflow ?? Array.Empty<ItemStackData>()))
                    if (item != null) { item.acquiredUtcTicks = 0; item.isNew = false; }
            if (data != null && envelope.version < 13) data.warehouse = new WarehouseData();
            if (data != null && envelope.version < 14) data.commerce = new CommerceData();
            if (data != null && envelope.version < 15) data.tutorial = new TutorialData();
            if (data != null && envelope.version < 16) data.specialization = new SpecializationData();
            if (data?.world != null && envelope.version < 17) data.world.expedition = new ExpeditionData();
            if (data?.world != null && envelope.version < 18) data.world.challenges = new ChallengeCareerData();
            if (data?.quests?.entries != null && envelope.version < 19)
                foreach (var entry in data.quests.entries) if (entry != null) entry.abandoned = false;
            Validate(data);
            return new ProgressLoadResult { Status = ProgressLoadStatus.Loaded, Data = data, Message = "继续游戏将返回主城，恢复成长、装备、技能与任务。" };
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException)
        {
            bool unavailable = ex is UnauthorizedAccessException || (ex is IOException && !(ex is InvalidDataException));
            return new ProgressLoadResult { Status = unavailable ? ProgressLoadStatus.Unavailable : ProgressLoadStatus.Corrupt,
                Message = unavailable ? "无法读取存档，请检查目录权限。" : "存档损坏或不完整，原文件已保留。" };
        }
    }

    public static void Validate(PlayerProgressData data)
    {
        if (data == null || !Guid.TryParseExact(data.profileId, "N", out _) || data.gold < 0 || data.totalExp < 0 ||
            !Enum.IsDefined(typeof(PlayerStance), data.stance) || data.skillIds == null || data.skillIds.Length != PlayerSkillController.MaxSlots)
            throw new InvalidDataException("存档数据不完整或超出允许范围，原文件已保留。");
        foreach (string id in data.skillIds)
            if (id == null || id.Length > 128) throw new InvalidDataException("存档技能标识无效，原文件已保留。");
        if (data.weaponIds == null || data.weaponIds.Length != PlayerWeaponEquipment.SlotCount
            || data.activeWeaponSlot < 0 || data.activeWeaponSlot >= PlayerWeaponEquipment.SlotCount)
            throw new InvalidDataException("存档武器装备无效，原文件已保留。");
        foreach (string id in data.weaponIds)
            if (id == null || id.Length > 128) throw new InvalidDataException("存档武器标识无效，原文件已保留。");
        if (string.IsNullOrEmpty(data.classId) || data.classId.Length > 128 || data.learnedSkills == null ||
            data.weaponUpgrades == null || data.learnedSkills.Length > 128 || data.weaponUpgrades.Length > 128)
            throw new InvalidDataException("存档职业或成长记录不完整，原文件已保留。");
        if (!Enum.IsDefined(typeof(PlayerGender), data.gender))
            throw new InvalidDataException("存档角色外观无效，原文件已保留。");
        var skillSet = new System.Collections.Generic.HashSet<string>();
        foreach (var entry in data.learnedSkills)
            if (entry == null || string.IsNullOrEmpty(entry.skillId) || entry.skillId.Length > 128 || entry.rank < 1 || entry.rank > 5 || !skillSet.Add(entry.skillId))
                throw new InvalidDataException("存档技能等级无效，原文件已保留。");
        foreach (string id in data.skillIds)
            if (id.Length > 0 && !skillSet.Contains(id)) throw new InvalidDataException("装备了尚未学会的技能，原文件已保留。");
        var weaponSet = new System.Collections.Generic.HashSet<string>();
        foreach (var entry in data.weaponUpgrades)
            if (entry == null || string.IsNullOrEmpty(entry.weaponId) || entry.weaponId.Length > 128 || entry.level < 1 || entry.level > 5 || !weaponSet.Add(entry.weaponId))
                throw new InvalidDataException("存档武器强化等级无效，原文件已保留。");
        InventoryData.Validate(data.inventory);
        QuestJournalData.Validate(data.quests);
        ConsumableLoadoutData.Validate(data.consumables);
        WorldProgressData.Validate(data.world);
        WarehouseData.Validate(data.warehouse);
        CommerceData.Validate(data.commerce);
        TutorialData.Validate(data.tutorial);
        SpecializationData.Validate(data.specialization);
        if (data.specialization.rank > 0 && data.totalExp < PlayerStats.ExpAtLevel(SpecializationRules.RequiredLevel(data.specialization.rank)))
            throw new InvalidDataException("专精等级超过角色解锁等级，原存档已保留。");
        if (data.inventory.bag.Concat(data.inventory.overflow).Concat(data.warehouse.items).Concat(data.commerce.buyback.Select(x => x.item)).Where(x => x.IsInstance)
            .GroupBy(x => x.instanceId).Any(x => x.Count() > 1))
            throw new InvalidDataException("装备实例同时出现在多个存储位置，原存档已保留。");
        if (data.equipmentVersion < 0 || data.equipmentVersion > 1) throw new InvalidDataException("装备存档版本无效。");
        if (data.equipmentVersion == 0 && (data.inventory.loadouts.Length > 0 || data.inventory.bag.Concat(data.inventory.overflow).Concat(data.warehouse.items).Concat(data.commerce.buyback.Select(x => x.item)).Any(x => x.IsInstance)))
            throw new InvalidDataException("装备格式标记与实例记录不一致。");
        if (data.equipmentVersion == 1)
        {
            if (data.weaponInstanceIds == null || data.weaponInstanceIds.Length != PlayerWeaponEquipment.SlotCount || data.weaponUpgrades.Length != 0)
                throw new InvalidDataException("武器实例记录不完整。");
            for (int i = 0; i < data.weaponIds.Length; i++)
            {
                string key = data.weaponInstanceIds[i];
                var instance = data.inventory.Find(key);
                if (key == null || (data.weaponIds[i].Length == 0 ? key.Length != 0 :
                    instance == null || !instance.IsInstance || instance.legacyReserve || instance.itemId != data.weaponIds[i]))
                    throw new InvalidDataException("装备了未持有的武器实例。");
            }
        }
        foreach (string id in data.weaponIds)
            if (id.Length > 0 && data.inventory.Count(id) == 0)
                throw new InvalidDataException("装备了未持有的武器，原文件已保留。");
    }

    private static string Hash(string payload)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
