using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public sealed partial class DungeonUI : UIBase
{
    private const float ObjectiveWidth = 704;
    [Inject]
    private DungeonRunService run;
    [Inject]
    private RPGSilent.Domain.IPlayerInputActions input;
    [Inject]
    private RPGSilent.Domain.ICursorService cursor;
    [SerializeField]
    private TMP_Text objectiveText;
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TMP_Text titleText;
    [SerializeField]
    private TMP_Text detailsText;
    [SerializeField]
    private Button retryButton;
    [SerializeField]
    private Button returnButton;
    [SerializeField]
    private GameObject bossPanel;
    [SerializeField]
    private TMP_Text bossName;
    [SerializeField]
    private Slider bossHealth;
    private DungeonRunService boundRun;
    private Button continueButton, resultButton;
    private TMP_Text bossMechanics;
    private Button rewardRetry;
    private Button eventSaveRetry, abandonEvent, confirmAbandon, cancelAbandon;
    RectTransform abandonShade;
    RectTransform exitLootShade, exitLootWindow;
    TMP_Text exitLootDetails;
    float nextRefresh;
    RectTransform runHeader;
    void Update()
    {
        if (boundRun != null && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + .2f;
            Refresh();
        }
    }

    protected override void OnInit()
    {
        runHeader = UIView.Rect(transform, "RunHeader");
        if (bossPanel != null && bossName != null)
        {
            bossMechanics = UIView.Text(bossPanel.transform, "BossMechanics", "");
            var hintRect = bossMechanics.rectTransform;
            bossMechanics.alignment = TextAlignmentOptions.Center;
        }

        var panelRect = resultPanel.GetComponent<RectTransform>();
        var card = (RectTransform)titleText.transform.parent;
        rewardRetry = UIView.Button(card, "RetryChallengeReward", "重试保存奖励", () =>
        {
            boundRun?.ClaimChallengeReward();
            boundRun?.RetryExpeditionSave();
        });
        var rewardRect = rewardRetry.GetComponent<RectTransform>();
        retryButton.onClick.AddListener(() => boundRun?.Retry());
        returnButton.onClick.AddListener(() => boundRun?.ReturnToMain());
        continueButton = UIView.Button(card, "ContinueLooting", "继续拾取", () => boundRun?.ContinueLooting());
        var r = continueButton.GetComponent<RectTransform>();
        resultButton = UIView.Button(transform, "InspectResult", "查看结算 [ESC]", () => boundRun?.ShowResult());
        r = resultButton.GetComponent<RectTransform>();
        exitLootShade = UIView.Panel(transform, "ExitLootShade");
        exitLootWindow = UIView.Panel(exitLootShade, "ExitLootWindow");
        UIView.Text(exitLootWindow, "ExitLootTitle", "尚有高品质物品未拾取");
        exitLootDetails = UIView.Text(exitLootWindow, "ExitLootDetails", "");
        UIView.Button(exitLootWindow, "ConfirmLeaveLoot", "确认放弃并离开", () => boundRun?.ConfirmLeave());
        UIView.Button(exitLootWindow, "CancelLeaveLoot", "取消 · 保留本次副本", () => boundRun?.CancelLeave());
        exitLootShade.gameObject.SetActive(false);
        eventSaveRetry = UIView.Button(transform, "RetryEventSave", "重试保存事件", () => boundRun?.RetryExpeditionSave());
        abandonEvent = UIView.Button(transform, "AbandonEvent", "放弃额外事件…", () =>
        {
            abandonShade.gameObject.SetActive(true);
            abandonShade.SetAsLastSibling();
            input.SetGameplayInputBlocked(this, true);
            cursor.EnterUICursor();
        });
        foreach (var button in new[]
        {
            abandonEvent,
            eventSaveRetry
        }

        )
        {
            var rect = button.GetComponent<RectTransform>();
        }

        abandonShade = UIView.Panel(transform, "AbandonEventShade");
        var abandonWindow = UIView.Panel(abandonShade, "AbandonEventWindow");
        UIView.Text(abandonWindow, "AbandonEventText", "放弃本次额外事件？\n本次不发放事件奖励，已拾取物品保留。\n清除普通波次后仍可通关；重试可再次体验同一种子。");
        confirmAbandon = UIView.Button(abandonWindow, "ConfirmAbandonEvent", "确认放弃", () =>
        {
            CloseAbandonPrompt();
            boundRun?.AbandonExpeditionEvent();
        });
        cancelAbandon = UIView.Button(abandonWindow, "CancelAbandonEvent", "返回事件", CloseAbandonPrompt);
        abandonShade.gameObject.SetActive(false);
        BuildLongChallengeUI();
    }

    public override void OnOpen(params object[] args)
    {
        Unbind();
        boundRun = run;
        boundRun.Changed += Refresh;
        Refresh();
    }

    public override void OnClose() => Unbind();
    private void OnDestroy() => Unbind();
    private void Unbind()
    {
        CloseAbandonPrompt();
        CloseLongEndPrompt();
        if (boundRun != null)
            boundRun.Changed -= Refresh;
        boundRun = null;
    }

    void CloseAbandonPrompt()
    {
        if (abandonShade == null || !abandonShade.gameObject.activeSelf)
            return;
        abandonShade.gameObject.SetActive(false);
        input.SetGameplayInputBlocked(this, false);
        if (input.GameplayInputEnabled)
            cursor.EnterGameplayCursor();
        else
            cursor.EnterUICursor();
    }

    public bool HandleBack()
    {
        if (longEndShade != null && longEndShade.gameObject.activeSelf)
        {
            CloseLongEndPrompt();
            return true;
        }

        if (abandonShade == null || !abandonShade.gameObject.activeSelf)
            return false;
        CloseAbandonPrompt();
        return true;
    }

    private void Refresh()
    {
        if (boundRun == null)
            return;
        runHeader.gameObject.SetActive(!input.IsGameplayBlockedExcept(boundRun));
        abandonEvent.gameObject.SetActive(boundRun.CanAbandonExpedition && !boundRun.ResultVisible);
        eventSaveRetry.gameObject.SetActive(boundRun.ExpeditionSavePending && !boundRun.ResultVisible);
        if (!boundRun.CanAbandonExpedition)
            CloseAbandonPrompt();
        exitLootShade.gameObject.SetActive(boundRun.LeaveConfirmationPending);
        if (boundRun.LeaveConfirmationPending)
        {
            exitLootShade.SetAsLastSibling();
            exitLootDetails.text = boundRun.HighQualityDepartureWarning;
        }

        string name = boundRun.Dungeon?.DisplayName ?? "副本";
        string difficulty = DungeonDatabase.GetDifficultyLabel(boundRun.Difficulty);
        objectiveText.text = $"{name} · {difficulty} · {boundRun.ChallengeSummary}\n{boundRun.Objective}  {boundRun.DefeatedEnemies} / {boundRun.TotalEnemies} · 探索 {boundRun.ExplorationCount}";
        if (boundRun.IsExpedition)
        {
            objectiveText.text += "\n" + boundRun.ExpeditionSummary;
        }

        var boss = boundRun.Boss;
        string trial = boundRun.IsLongChallenge ? "" : run.ProfessionTrialSummary;
        if (!string.IsNullOrEmpty(trial))
        {
            objectiveText.text += "\n" + trial;
        }

        bool hasTrial = !string.IsNullOrEmpty(trial);
        bool expanded = boundRun.IsExpedition || hasTrial;
        bool combined = boundRun.IsExpedition && hasTrial;
        float objectiveHeight = combined ? 188 : expanded ? 162 : 130;
        float objectiveY = combined ? 168 : expanded ? 155 : 140;
        var objectiveCard = (RectTransform)objectiveText.transform.parent;
        if (bossPanel != null)
            bossPanel.SetActive(boss != null && !boss.IsDead && !boundRun.HasResult);
        if (boss != null)
        {
            if (bossName != null)
                bossName.text = $"{boss.DisplayName} · {boss.Phase}阶段 {boss.PhaseName}  {boss.CurrentHealth}/{boss.MaxHealth}" + (string.IsNullOrEmpty(boss.CombatHint) ? "" : $" · <color=#FFD66B>{boss.CombatHint}</color>");
            if (bossHealth != null)
                bossHealth.value = (float)boss.CurrentHealth / boss.MaxHealth;
            if (bossMechanics != null)
            {
                bossMechanics.text = boss.PhaseHint;
                bossMechanics.color = boss.IsVulnerable ? new Color(.5f, 1f, .7f) : new Color(1f, .85f, .55f);
            }
        }

        if (RefreshLongChallengeUI())
            return;
        resultPanel.SetActive(boundRun.ResultVisible);
        resultButton.gameObject.SetActive(boundRun.Succeeded && !boundRun.ResultVisible);
        if (!boundRun.HasResult)
            return;
        titleText.text = boundRun.Succeeded ? "副本通关" : "挑战失败";
        detailsText.text = $"{name} · {difficulty}\n击败 {boundRun.DefeatedEnemies} / {boundRun.TotalEnemies}\n" + $"已拾取 {boundRun.AwardedGold} 金币 · {boundRun.AwardedExp} 经验 · {boundRun.CollectedItems} 件物品\n" + (boundRun.Succeeded ? $"地面还有 {boundRun.RemainingDrops} 份战利品，靠近后按交互键拾取。\n离开或重试会放弃未拾取的物品。" : $"{boundRun.FailureReason}\n已拾取物品保留；未拾取物品将在离开时消失。");
        var record = boundRun.CombatRecord;
        detailsText.text += $"\n用时 {DungeonChallengeRules.Duration(boundRun.Elapsed)} · 探索互动 {boundRun.ExplorationCount}";
        if (record != null)
            detailsText.text += $"\n输出 {record.DamageDealt} · 承伤 {record.DamageTaken} · 精准格挡 {record.PerfectParries} · 精准闪避 {record.PerfectDodges}";
        if (boundRun.Challenge == DungeonChallenge.Timed || boundRun.Challenge == DungeonChallenge.BossTrial)
            detailsText.text += boundRun.ChallengeCompleted ? $"\n挑战达成 · 额外 {boundRun.ChallengeGold} 金币 / {boundRun.ChallengeGold} 经验" : "\n挑战目标未达成，已拾取战利品保留。";
        if (boundRun.IsExpedition)
            detailsText.text += "\n" + boundRun.ExpeditionSummary;
        rewardRetry.gameObject.SetActive(boundRun.ChallengeRewardPending || boundRun.ExpeditionSavePending);
        if (!string.IsNullOrEmpty(boundRun.RewardError))
            detailsText.text += "\n奖励待领取：" + boundRun.RewardError;
        bool canLeave = boundRun.State != DungeonRunState.Leaving;
        retryButton.interactable = canLeave && boundRun.Dungeon?.GetDifficultyTier(boundRun.Difficulty) != null;
        returnButton.interactable = canLeave;
        continueButton.gameObject.SetActive(boundRun.Succeeded);
        continueButton.interactable = canLeave;
        retryButton.GetComponentInChildren<TMP_Text>().text = boundRun.Succeeded ? "再次挑战" : "复活并重试";
        returnButton.GetComponentInChildren<TMP_Text>().text = boundRun.Succeeded ? "返回主城" : "复活回主城";
        if (!string.IsNullOrEmpty(boundRun.TravelError))
            detailsText.text += "\n" + boundRun.TravelError;
    }
}
