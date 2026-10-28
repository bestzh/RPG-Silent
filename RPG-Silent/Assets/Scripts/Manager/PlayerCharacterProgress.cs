using System;
using System.Linq;
using RPGSilent.Domain;

public sealed partial class PlayerProgressService
{
    public string CurrentCharacterKey => store.CurrentCharacterKey;
    public CharacterSaveInfo[] GetCharacters(out string message)
    {
        var characters = store.ListCharacters(out message);
        foreach (var character in characters) character.Progress = ValidateLoadedCharacter(character.Progress);
        return characters;
    }
    public bool BeginContinueCharacter(string key, out string message)
    {
        message = "请先返回主菜单再选择角色。";
        if (attached) return false;
        if (!store.TrySelectCharacter(key, out var result)) { message = result.Message; return false; }
        result = ValidateLoadedCharacter(result); message = result.Message;
        if (!result.CanContinue) return false;
        ApplyLoadedCharacter(result); return true;
    }
    PlayerClassDefinition ValidateNewCharacter(string id, PlayerGender gender)
    {
        if (!classes.TryGetValue(id, out var selected) || selected.IsAdvanced) throw new ArgumentException("请选择基础职业。", nameof(id));
        if ((gender != PlayerGender.Male && gender != PlayerGender.Female) || selected.GetModel(gender) == null)
            throw new ArgumentException("请选择有效的角色外观。", nameof(gender));
        return selected;
    }
    public bool TryBeginNewCharacter(string id, PlayerGender gender, out string message)
    {
        message = "请先返回主菜单再创建角色。";
        if (attached) return false;
        PlayerClassDefinition selected;
        try { selected = ValidateNewCharacter(id, gender); }
        catch (ArgumentException ex) { message = ex.Message; return false; }
        var data = new PlayerProgressData
        {
            profileId = Guid.NewGuid().ToString("N"), classId = id, gender = (int)gender,
            stance = (int)selected.StartingWeapon.Stance,
            skillIds = Enumerable.Repeat(string.Empty, PlayerSkillController.MaxSlots).ToArray(),
            weaponIds = new[] { selected.StartingWeapon.PersistentId, id == "swordsman" ? WeaponDefinition.StarterGreatswordId : WeaponDefinition.StarterSwordId },
            activeWeaponSlot = 0, equipmentVersion = 1,
            learnedSkills = Array.Empty<SkillProgressData>(), weaponUpgrades = Array.Empty<WeaponUpgradeData>(),
            inventory = new InventoryData(), quests = new QuestJournalData(), consumables = new ConsumableLoadoutData()
        };
        data.inventory.AddRewards(data.weaponIds.Distinct().Select(key => EquipmentInstances.Create(FindItem(key), null)));
        data.weaponInstanceIds = data.weaponIds.Select(key => data.inventory.bag.Single(item => item.itemId == key).Key).ToArray();
        if (!store.TryCreateCharacter(data, out message)) { StatusMessage = message; return false; }
        stats.ResetForNewGame();
        ApplyLoadedCharacter(new ProgressLoadResult { Status = ProgressLoadStatus.Loaded, Data = data });
        StatusMessage = message = "新角色已独立保存，已有角色保留。"; return true;
    }
}
