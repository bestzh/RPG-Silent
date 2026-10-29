using System.Collections;
using RPGSilent.Domain;
using UnityEngine;
using VContainer;

/// <summary>Scene-local practice, using real attacks and defenses without loot or progression.</summary>
public sealed class TrainingGround : MonoBehaviour
{
    public EnemyController PracticePrefab;
    public Transform TargetPoint;
    public Transform PlayerPoint;
    EnemyController target;
    PlayerController player;
    IUIService ui;
    PlayerProgressService progress;
    ISceneLoader loader;
    bool defenseMode, leaving;
    int skillGoal;
    float nextRefill;
    public string Mode => defenseMode ? "防御与闪避" : "连段与技能";
    public string Message { get; private set; }
    public EnemyController Target => target;
    public PlayerCombatRecord Record => player?.CombatRecord;
    IEnumerator Start()
    {
        yield return null;
        var container = GameLifetimeScope.Instance?.Container;
        if (container == null) yield break;
        player = container.Resolve<PlayerSessionService>().PlayerRig?.GetComponentInChildren<PlayerController>();
        ui = container.Resolve<IUIService>(); progress = container.Resolve<PlayerProgressService>(); loader = container.Resolve<ISceneLoader>();
        if (player == null) yield break;
        player.Training = this;
        foreach (var node in GetComponentsInChildren<ExplorationNode>(true)) node.InitializeTraining(this);
        Activate(ExplorationKind.TrainingCombo);
    }
    public bool Activate(ExplorationKind kind)
    {
        if (player == null || leaving) return false;
        if (kind == ExplorationKind.ReturnToTown) { ReturnToTown(); return true; }
        if (kind == ExplorationKind.TrainingCombo) defenseMode = false;
        else if (kind == ExplorationKind.TrainingDefense) defenseMode = true;
        else if (kind != ExplorationKind.TrainingReset) return false;
        if (target != null) Destroy(target.gameObject);
        player.ClassCombat?.ResetCombat();
        player.CombatStats.RestoreVitals();
        player.ResetForScene(PlayerPoint.position, PlayerPoint.rotation);
        player.CombatRecord.Reset();
        skillGoal = 0;
        foreach (var skill in progress.LearnedSkills) { skillGoal = 3; break; }
        target = Instantiate(PracticePrefab, TargetPoint.position, TargetPoint.rotation, transform);
        target.ConfigureTraining(defenseMode);
        target.ConfigurePresentation(player.Audio, player.PresentationSettings);
        Message = defenseMode ? "观察起手：右键精准格挡 / 轻按 Shift 闪避。练习目标：精准格挡与精准闪避各 3 次。"
            : "剑 / 法杖完成 3 次连段末击；弓命中 10 次。" + (skillGoal > 0 ? "再释放 3 次已学技能。" : "其他技能需在主城向职业导师学习。") + "重剑与弓可练习蓄力。";
        return true;
    }
    void Update()
    {
        if (player == null || leaving) return;
        if (Time.time >= nextRefill)
        {
            nextRefill = Time.time + 5;
            player.CombatStats.RestoreVitals();
        }
        bool bow = player.CurrentStance == PlayerStance.TwoHandBow || player.CurrentStance == PlayerStance.TwoHandCrossbow;
        if (Record != null && (defenseMode ? Record.PerfectParries >= 3 && Record.PerfectDodges >= 3 : (bow ? Record.Hits >= 10 : Record.ComboFinishers >= 3) && Record.Skills >= skillGoal))
            Message = "练习目标达成！可继续练习，或使用重置机关重新计数。";
    }
    public void ReturnToTown()
    {
        if (leaving || loader == null || loader.IsLoading) return;
        if (!progress.Save()) { Message = "保存失败，请检查存储后重试回城。"; return; }
        leaving = true;
        ui.OpenUI("UI/LoadingUI", "Scenes/Main", "UI/MainUI", (System.Action)(() => leaving = false));
    }
    void OnDestroy() { if (player != null && player.Training == this) player.Training = null; }
}
