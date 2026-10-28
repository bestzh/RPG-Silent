using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public sealed class DungeonUI : UIBase
{
    [Inject] private DungeonRunService run;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private GameObject bossPanel;
    [SerializeField] private TMP_Text bossName;
    [SerializeField] private Slider bossHealth;
    private DungeonRunService boundRun;
    private Button continueButton, resultButton;
    private TMP_Text bossMechanics;
    private Button rewardRetry;
    float nextRefresh;
    void Update() { if (boundRun != null && Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .2f; Refresh(); } }

    protected override void OnInit()
    {
        if (bossPanel != null && bossName != null)
        {
            bossMechanics = ProgressionUIWidgets.Text(bossPanel.transform, bossName.font, "BossMechanics", "", 0, 0, 900, 36, 22);
            var hintRect = bossMechanics.rectTransform; hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0, -40); bossMechanics.alignment = TextAlignmentOptions.Center;
        }
        var panelRect = resultPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        var card = (RectTransform)titleText.transform.parent;
        card.sizeDelta = new Vector2(940, 650);
        detailsText.rectTransform.sizeDelta = new Vector2(820, 340); detailsText.fontSize = 22;
        detailsText.rectTransform.anchorMin = detailsText.rectTransform.anchorMax = detailsText.rectTransform.pivot = new Vector2(.5f,.5f);
        detailsText.rectTransform.anchoredPosition = new Vector2(0,5);
        titleText.rectTransform.anchoredPosition = new Vector2(0,255);
        rewardRetry = ProgressionUIWidgets.Button(card, titleText.font, "RetryChallengeReward", "重试领取挑战奖励", 0, 0, 280, 42, () => boundRun?.ClaimChallengeReward());
        var rewardRect = rewardRetry.GetComponent<RectTransform>(); rewardRect.anchorMin = rewardRect.anchorMax = rewardRect.pivot = new Vector2(.5f,.5f); rewardRect.anchoredPosition = new Vector2(0,-185);
        retryButton.onClick.AddListener(() => boundRun?.Retry());
        returnButton.onClick.AddListener(() => boundRun?.ReturnToMain());
        continueButton = ProgressionUIWidgets.Button(card, titleText.font, "ContinueLooting", "继续拾取", 0, 0, 230, 60, () => boundRun?.ContinueLooting());
        var r = continueButton.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = new Vector2(0, -255);
        retryButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-285, -255);
        returnButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(285, -255);
        resultButton = ProgressionUIWidgets.Button(transform, titleText.font, "InspectResult", "查看结算 [ESC]", 0, 0, 240, 48, () => boundRun?.ShowResult());
        r = resultButton.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, 1); r.anchoredPosition = new Vector2(0, -180);
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
        if (boundRun != null) boundRun.Changed -= Refresh;
        boundRun = null;
    }

    private void Refresh()
    {
        if (boundRun == null) return;
        string name = boundRun.Dungeon?.DisplayName ?? "副本";
        string difficulty = DungeonDatabase.GetDifficultyLabel(boundRun.Difficulty);
        objectiveText.text = $"{name} · {difficulty} · {boundRun.ChallengeSummary}\n{boundRun.Objective}  {boundRun.DefeatedEnemies} / {boundRun.TotalEnemies} · 探索 {boundRun.ExplorationCount}";
        var boss = boundRun.Boss;
        if (bossPanel != null) bossPanel.SetActive(boss != null && !boss.IsDead && !boundRun.HasResult);
        if (boss != null)
        {
            if (bossName != null) bossName.text = $"{boss.DisplayName} · {boss.Phase}阶段 {boss.PhaseName}  {boss.CurrentHealth}/{boss.MaxHealth}" +
                (string.IsNullOrEmpty(boss.CombatHint) ? "" : $" · <color=#FFD66B>{boss.CombatHint}</color>");
            if (bossHealth != null) bossHealth.value = (float)boss.CurrentHealth / boss.MaxHealth;
            if (bossMechanics != null) { bossMechanics.text = boss.PhaseHint; bossMechanics.color = boss.IsVulnerable ? new Color(.5f, 1f, .7f) : new Color(1f, .85f, .55f); }
        }
        resultPanel.SetActive(boundRun.ResultVisible);
        resultButton.gameObject.SetActive(boundRun.Succeeded && !boundRun.ResultVisible);
        if (!boundRun.HasResult) return;
        titleText.text = boundRun.Succeeded ? "副本通关" : "挑战失败";
        detailsText.text = $"{name} · {difficulty}\n击败 {boundRun.DefeatedEnemies} / {boundRun.TotalEnemies}\n" +
            $"已拾取 {boundRun.AwardedGold} 金币 · {boundRun.AwardedExp} 经验 · {boundRun.CollectedItems} 件物品\n" +
            (boundRun.Succeeded ? $"地面还有 {boundRun.RemainingDrops} 份战利品，靠近后按交互键拾取。\n离开或重试会放弃未拾取的物品。" : $"{boundRun.FailureReason}\n已拾取物品保留；未拾取物品将在离开时消失。");
        var record = boundRun.CombatRecord;
        detailsText.text += $"\n用时 {DungeonChallengeRules.Duration(boundRun.Elapsed)} · 探索互动 {boundRun.ExplorationCount}";
        if (record != null) detailsText.text += $"\n输出 {record.DamageDealt} · 承伤 {record.DamageTaken} · 精准格挡 {record.PerfectParries} · 精准闪避 {record.PerfectDodges}";
        if (boundRun.Challenge != DungeonChallenge.Exploration)
            detailsText.text += boundRun.ChallengeCompleted ? $"\n挑战达成 · 额外 {boundRun.ChallengeGold} 金币 / {boundRun.ChallengeGold} 经验" : "\n挑战目标未达成，已拾取战利品保留。";
        rewardRetry.gameObject.SetActive(boundRun.ChallengeRewardPending);
        if (!string.IsNullOrEmpty(boundRun.RewardError)) detailsText.text += "\n奖励待领取：" + boundRun.RewardError;
        bool canLeave = boundRun.State != DungeonRunState.Leaving;
        retryButton.interactable = canLeave && boundRun.Dungeon?.GetDifficultyTier(boundRun.Difficulty) != null;
        returnButton.interactable = canLeave;
        continueButton.gameObject.SetActive(boundRun.Succeeded); continueButton.interactable = canLeave;
        retryButton.GetComponentInChildren<TMP_Text>().text = boundRun.Succeeded ? "再次挑战" : "复活并重试";
        returnButton.GetComponentInChildren<TMP_Text>().text = boundRun.Succeeded ? "返回主城" : "复活回主城";
        if (!string.IsNullOrEmpty(boundRun.TravelError)) detailsText.text += "\n" + boundRun.TravelError;
    }
}
