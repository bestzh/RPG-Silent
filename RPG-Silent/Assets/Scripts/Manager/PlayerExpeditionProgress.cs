using System;
using RPGSilent.Domain;
using UnityEngine;

public sealed partial class PlayerProgressService
{
    public ExpeditionData Expedition => world.expedition.Copy();
    public ExpeditionData PreviewExpedition(int dungeonId, DungeonDifficulty difficulty, bool replace)
    {
        var current = world.expedition;
        if (!replace && current.HasValue && current.dungeonId == dungeonId && current.difficulty == (int)difficulty) return current.Copy();
        if (current.sequence == int.MaxValue) return null;
        var bytes = Guid.NewGuid().ToByteArray();
        return new ExpeditionData { id = Guid.NewGuid().ToString("N"), sequence = current.sequence + 1, seed = (BitConverter.ToInt32(bytes, 0) & int.MaxValue) | 1,
            dungeonId = dungeonId, difficulty = (int)difficulty, attempt = 1, status = ExpeditionEventStatus.Available };
    }
    public bool TryBeginExpedition(ExpeditionData preview, out string message)
    {
        message = "请在主城重新选择远征。";
        var player = weapons != null ? weapons.GetComponent<PlayerController>() : null;
        if (!attached || !CanManageInventory || player == null || player.CombatAllowed || preview == null) return false;
        try { ExpeditionData.Validate(preview); } catch (System.IO.InvalidDataException) { return false; }
        var database = Resources.Load<DungeonDatabase>("DungeonDatabase"); var entry = database != null ? database.GetById(preview.dungeonId) : null;
        if (entry == null || !entry.IsEnabled || entry.IsTraining || entry.GetDifficultyTier((DungeonDifficulty)preview.difficulty) == null) return false;
        var current = world.expedition;
        bool same = current.HasValue && current.SamePlan(preview);
        if (same ? preview.attempt != current.attempt || preview.rewardClaimed != current.rewardClaimed || current.attempt >= 1000000 :
            current.sequence == int.MaxValue || preview.sequence != current.sequence + 1 || preview.attempt != 1 || preview.rewardClaimed || preview.status != ExpeditionEventStatus.Available)
        { message = "远征记录已经变化，请重新打开传送门。"; return false; }
        var data = Capture(); var next = same ? current.Copy() : preview.Copy();
        if (same) next.attempt++;
        next.status = ExpeditionEventStatus.Available;
        data.world.expedition = next;
        return CommitQuest(data, 0, 0, "远征种子与本次尝试已保存。", out message);
    }
    bool IsCurrentExpedition(ExpeditionData token) => attached && world.expedition.SamePlan(token) && world.expedition.attempt == token.attempt &&
        DungeonLaunchContext.HasValue && DungeonLaunchContext.Challenge == DungeonChallenge.Expedition && DungeonLaunchContext.ExpeditionId == token.id &&
        DungeonLaunchContext.DungeonId == token.dungeonId && (int)DungeonLaunchContext.Difficulty == token.difficulty;
    public bool TrySetExpeditionState(ExpeditionData token, ExpeditionEventStatus status, out string message)
    {
        message = "远征记录已变化，不能重复结算。";
        if (token == null || !IsCurrentExpedition(token)) return false;
        var current = world.expedition;
        if (status == ExpeditionEventStatus.Active ? current.status != ExpeditionEventStatus.Available :
            status == ExpeditionEventStatus.Completed || status == ExpeditionEventStatus.Failed ? current.status != ExpeditionEventStatus.Active :
            status == ExpeditionEventStatus.Abandoned ? current.status != ExpeditionEventStatus.Available && current.status != ExpeditionEventStatus.Active : true) return false;
        var data = Capture(); var next = data.world.expedition; next.status = status;
        int reward = status == ExpeditionEventStatus.Completed && !next.rewardClaimed ? new ExpeditionPlan(next).Reward : 0;
        if (status == ExpeditionEventStatus.Completed) next.rewardClaimed = true;
        return CommitQuest(data, reward, reward, reward > 0 ? $"事件完成，{reward} 金币 / {reward} 经验已保存。" : "事件状态已保存。", out message);
    }
    public bool TryRetryExpedition(ExpeditionData token, out string message)
    {
        message = "远征记录已经变化，无法重试。";
        if (token == null || !IsCurrentExpedition(token) || world.expedition.attempt >= 1000000) return false;
        var data = Capture(); data.world.expedition.attempt++; data.world.expedition.status = ExpeditionEventStatus.Available;
        return CommitQuest(data, 0, 0, "沿用种子，重新开始本次远征。", out message);
    }
}
