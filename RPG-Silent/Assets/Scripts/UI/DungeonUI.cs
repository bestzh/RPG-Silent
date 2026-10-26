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

    protected override void OnInit()
    {
        retryButton.onClick.AddListener(() => boundRun?.Retry());
        returnButton.onClick.AddListener(() => boundRun?.ReturnToMain());
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
        objectiveText.text = $"{name} · {difficulty}\n{boundRun.Objective}  {boundRun.DefeatedEnemies} / {boundRun.TotalEnemies}";
        var boss = boundRun.Boss;
        if (bossPanel != null) bossPanel.SetActive(boss != null && !boss.IsDead && !boundRun.HasResult);
        if (boss != null)
        {
            if (bossName != null) bossName.text = $"{boss.DisplayName} · 阶段 {boss.Phase}  {boss.CurrentHealth}/{boss.MaxHealth}";
            if (bossHealth != null) bossHealth.value = (float)boss.CurrentHealth / boss.MaxHealth;
        }
        resultPanel.SetActive(boundRun.HasResult);
        if (!boundRun.HasResult) return;
        titleText.text = boundRun.Succeeded ? "副本通关" : "挑战失败";
        detailsText.text = $"{name} · {difficulty}\n击败 {boundRun.DefeatedEnemies} / {boundRun.TotalEnemies}\n" +
            (boundRun.Succeeded ? $"已获得 {boundRun.AwardedGold} 金币 · {boundRun.AwardedExp} 经验" :
                $"{boundRun.FailureReason}\n本次未通关，不发放奖励。");
        bool canLeave = boundRun.State != DungeonRunState.Leaving;
        retryButton.interactable = canLeave && boundRun.Dungeon?.GetDifficultyTier(boundRun.Difficulty) != null;
        returnButton.interactable = canLeave;
    }
}
