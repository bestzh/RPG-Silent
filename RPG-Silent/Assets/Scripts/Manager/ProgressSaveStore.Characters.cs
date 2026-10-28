using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPGSilent.Domain;

public sealed class CharacterSaveInfo
{
    public string Key;
    public DateTime ModifiedUtc;
    public ProgressLoadResult Progress;
    public bool IsLegacy => Key == ProgressSaveStore.LegacyCharacterKey;
}

public sealed partial class ProgressSaveStore
{
    public const string LegacyCharacterKey = "legacy";
    public string CurrentCharacterKey => directory == rootDirectory ? LegacyCharacterKey : Path.GetFileName(directory);
    string CharactersDirectory => Path.Combine(rootDirectory, "characters");

    bool TryCharacterDirectory(string key, out string path)
    {
        path = null;
        if (key == LegacyCharacterKey) { path = rootDirectory; return true; }
        if (!Guid.TryParseExact(key, "N", out var id)) return false;
        path = Path.Combine(CharactersDirectory, id.ToString("N")); return true;
    }

    public ProgressLoadResult ReadCharacter(string key)
    {
        if (!TryCharacterDirectory(key, out string folder)) return new ProgressLoadResult { Status = ProgressLoadStatus.Missing, Message = "角色标识无效。" };
        var result = LoadDirectory(folder);
        if (result.CanContinue && key != LegacyCharacterKey && !string.Equals(result.Data.profileId, key, StringComparison.OrdinalIgnoreCase))
            return new ProgressLoadResult { Status = ProgressLoadStatus.Corrupt, Message = "角色标识与存档目录不一致，原文件已保留。" };
        return result;
    }

    // Enumeration needs no shared index file: an interrupted write cannot hide other characters.
    // The original single save remains in its original location and is never overwritten by creation.
    public CharacterSaveInfo[] ListCharacters(out string message)
    {
        message = string.Empty;
        var keys = new List<string>();
        if (File.Exists(Path.Combine(rootDirectory, "progress.json")) || File.Exists(Path.Combine(rootDirectory, "progress.backup.json"))) keys.Add(LegacyCharacterKey);
        try
        {
            if (Directory.Exists(CharactersDirectory))
                foreach (string folder in Directory.EnumerateDirectories(CharactersDirectory))
                {
                    string key = Path.GetFileName(folder);
                    if (Guid.TryParseExact(key, "N", out _) &&
                        (File.Exists(Path.Combine(folder, "progress.json")) || File.Exists(Path.Combine(folder, "progress.backup.json")))) keys.Add(key);
                }
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        { message = "部分角色目录无法读取，请检查存储权限。已有文件不会被替换。"; }
        return keys.Select(key =>
        {
            TryCharacterDirectory(key, out string folder);
            var result = ReadCharacter(key); DateTime modified = DateTime.MinValue;
            try { modified = File.GetLastWriteTimeUtc(Path.Combine(folder, result.Status == ProgressLoadStatus.Recovered ? "progress.backup.json" : "progress.json")); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { }
            return new CharacterSaveInfo { Key = key, ModifiedUtc = modified, Progress = result };
        }).OrderByDescending(x => x.ModifiedUtc).ThenBy(x => x.Key, StringComparer.Ordinal).ToArray();
    }

    public bool TrySelectCharacter(string key, out ProgressLoadResult result)
    {
        result = ReadCharacter(key);
        if (!result.CanContinue) return false;
        TryCharacterDirectory(key, out directory); return true;
    }

    public bool TryCreateCharacter(PlayerProgressData data, out string message)
    {
        message = "无法创建角色，原有角色已保留。";
        if (data == null || !Guid.TryParseExact(data.profileId, "N", out _) || !TryCharacterDirectory(data.profileId, out string folder)) return false;
        if (Directory.Exists(folder) || File.Exists(folder)) { message = "该角色已经存在，请选择继续。"; return false; }
        string previous = directory; directory = folder;
        // A new character is published by one atomic primary write; no recovery copy is needed yet.
        if (TrySave(data, false, out message)) return true;
        directory = previous; return false;
    }
}
