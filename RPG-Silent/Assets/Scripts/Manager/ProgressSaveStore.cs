using System;
using System.IO;
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

/// <summary>Single local slot. Writes a flushed temporary file, then atomically replaces it.</summary>
public sealed class ProgressSaveStore
{
    public const int CurrentVersion = 2;
    private const int MaxFileBytes = 64 * 1024;
    private readonly string directory;
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
        this.directory = Path.GetFullPath(directory ?? throw new ArgumentNullException(nameof(directory)));
    }

    public ProgressLoadResult Load()
    {
        var primary = Read(PrimaryPath);
        // Never silently downgrade a save written by a newer/different format.
        if (primary.CanContinue || primary.Status == ProgressLoadStatus.Unsupported ||
            primary.Status == ProgressLoadStatus.Unavailable) return primary;
        var backup = Read(BackupPath);
        if (backup.CanContinue)
            return new ProgressLoadResult { Status = ProgressLoadStatus.Recovered, Data = backup.Data,
                Message = "主存档不可用，将从上一次备份恢复。" };
        if (backup.Status == ProgressLoadStatus.Unsupported || backup.Status == ProgressLoadStatus.Unavailable) return backup;
        return primary.Status == ProgressLoadStatus.Missing ? backup : primary;
    }

    public bool TrySave(PlayerProgressData data, bool newProfile, out string error)
    {
        error = null;
        try
        {
            Validate(data);
            Directory.CreateDirectory(directory);
            string payload = JsonUtility.ToJson(data);
            string text = JsonUtility.ToJson(new Envelope { version = CurrentVersion,
                savedUtc = DateTime.UtcNow.ToString("O"), payload = payload, checksum = Hash(payload) }, true);
            var existing = Read(PrimaryPath);
            if (!newProfile && existing.Status == ProgressLoadStatus.Unsupported)
                throw new InvalidDataException(existing.Message);
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
        if (File.Exists(destination)) File.Replace(temporary, destination, backup);
        else File.Move(temporary, destination);
    }

    private static ProgressLoadResult Read(string path)
    {
        if (!File.Exists(path)) return new ProgressLoadResult { Status = ProgressLoadStatus.Missing, Message = "尚无存档，请开始新游戏。" };
        try
        {
            if (new FileInfo(path).Length > MaxFileBytes) throw new InvalidDataException();
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path, Encoding.UTF8));
            if (envelope == null) throw new InvalidDataException();
            if (envelope.version != 1 && envelope.version != CurrentVersion)
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
            Validate(data);
            return new ProgressLoadResult { Status = ProgressLoadStatus.Loaded, Data = data, Message = "继续游戏将返回主城，恢复成长、武器和技能装配。" };
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
    }

    private static string Hash(string payload)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
