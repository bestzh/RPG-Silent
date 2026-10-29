using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class DungeonUI
{
    RectTransform longShade, longWindow, longEndShade, longEndWindow;
    TMP_Text longTitle, longDetails;
    Button longContinue, longSave, longRetry, longReturn, longEnd, longRestEnd;
    void BuildLongChallengeUI()
    {
        longEnd = UIView.Button(transform, "EndLongChallenge", "结束挑战…", () =>
        {
            longEndShade.gameObject.SetActive(true);
            longEndShade.SetAsLastSibling();
            input.SetGameplayInputBlocked(this, true);
            cursor.EnterUICursor();
        });
        var endRect = longEnd.GetComponent<RectTransform>();
        longShade = UIView.Panel(transform, "LongChallengeShade");
        longWindow = UIView.Panel(longShade, "LongChallengeWindow");
        longTitle = UIView.Text(longWindow, "LongTitle", "");
        longDetails = UIView.Text(longWindow, "LongDetails", "");
        longContinue = UIView.Button(longWindow, "ContinueLongChallenge", "继续下一轮", () => boundRun?.ContinueLongChallenge());
        longSave = UIView.Button(longWindow, "RetryLongSave", "重试保存", () =>
        {
            if (boundRun?.LongChallenge != null && !boundRun.LongChallengeSavePending && boundRun.LongChallenge.Phase == LongChallengePhase.Resting)
                boundRun.ContinueLongChallenge();
            else
                boundRun?.RetryLongChallengeSave();
        });
        longRestEnd = UIView.Button(longWindow, "RetireLongAtRest", "结束并结算", () => boundRun?.EndLongChallenge());
        longRetry = UIView.Button(longWindow, "RestartLongChallenge", "从第1轮重新挑战", () => boundRun?.Retry());
        longReturn = UIView.Button(longWindow, "ReturnLongChallenge", "返回主城", () => boundRun?.ReturnToMain());
        longEndShade = UIView.Panel(transform, "LongEndShade");
        longEndWindow = UIView.Panel(longEndShade, "LongEndWindow");
        UIView.Text(longEndWindow, "LongEndText", "结束本次挑战？\n已保存的轮次奖励与纪录保留，当前未清轮次不发奖。\n重新挑战从第1轮开始，首次奖励标记不重置。\n确认期间战斗继续。");
        UIView.Button(longEndWindow, "ConfirmLongEnd", "确认结束", () =>
        {
            CloseLongEndPrompt();
            boundRun?.EndLongChallenge();
        });
        UIView.Button(longEndWindow, "CancelLongEnd", "返回战斗", CloseLongEndPrompt);
        longShade.gameObject.SetActive(false);
        longEndShade.gameObject.SetActive(false);
        longEnd.gameObject.SetActive(false);
    }

    void CloseLongEndPrompt()
    {
        if (longEndShade == null || !longEndShade.gameObject.activeSelf)
            return;
        longEndShade.gameObject.SetActive(false);
        input.SetGameplayInputBlocked(this, false);
        if (input.GameplayInputEnabled)
            cursor.EnterGameplayCursor();
        else
            cursor.EnterUICursor();
    }

    bool RefreshLongChallengeUI()
    {
        if (boundRun == null || !boundRun.IsLongChallenge)
        {
            longShade.gameObject.SetActive(false);
            longEnd.gameObject.SetActive(false);
            CloseLongEndPrompt();
            return false;
        }

        var runtime = boundRun.LongChallenge;
        objectiveText.text = $"{boundRun.Dungeon?.DisplayName ?? "副本"} · {DungeonDatabase.GetDifficultyLabel(boundRun.Difficulty)} · {RPGSilent.Domain.LongChallengeRules.Label((RPGSilent.Domain.LongChallengeMode)(int)boundRun.Challenge)}\n{boundRun.Objective}";
        bool rest = runtime != null && runtime.Phase != LongChallengePhase.Fighting;
        bool overlay = rest || boundRun.HasResult;
        resultPanel.SetActive(false);
        resultButton.gameObject.SetActive(false);
        longEnd.gameObject.SetActive(!overlay && boundRun.State == DungeonRunState.Running);
        longShade.gameObject.SetActive(overlay);
        var objectiveCard = (RectTransform)objectiveText.transform.parent;
        if (!overlay)
            return true;
        CloseLongEndPrompt();
        longShade.SetAsLastSibling();
        bool canLeave = boundRun.State != DungeonRunState.Leaving;
        bool pending = boundRun.LongChallengeSavePending;
        bool canContinue = canLeave && !boundRun.HasResult && runtime != null && runtime.Phase == LongChallengePhase.Resting && !pending;
        longTitle.text = boundRun.HasResult ? (boundRun.Succeeded ? "长期挑战通关" : "本次挑战结束") : pending ? "奖励与纪录尚未保存" : "本轮完成 · 休整";
        longDetails.text = boundRun.LongChallengeSummary + (boundRun.HasResult && !string.IsNullOrEmpty(boundRun.FailureReason) ? "\n" + boundRun.FailureReason : "") + (!string.IsNullOrEmpty(boundRun.TravelError) ? "\n" + boundRun.TravelError : "");
        longContinue.gameObject.SetActive(!boundRun.HasResult);
        longContinue.interactable = canContinue;
        longRestEnd.gameObject.SetActive(!boundRun.HasResult);
        longRestEnd.interactable = canLeave && !pending;
        longSave.gameObject.SetActive(pending || runtime != null && !string.IsNullOrEmpty(runtime.SaveError));
        longSave.interactable = canLeave;
        longRetry.gameObject.SetActive(boundRun.HasResult);
        longRetry.interactable = canLeave && !pending;
        longReturn.gameObject.SetActive(boundRun.HasResult);
        longReturn.interactable = canLeave && !pending;
        return true;
    }
}
