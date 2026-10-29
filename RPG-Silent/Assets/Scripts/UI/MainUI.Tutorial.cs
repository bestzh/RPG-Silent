using RPGSilent.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    RectTransform tutorialSurface, onboardingCard, onboardingReview;
    TMP_Text onboardingTitle, onboardingHint, onboardingState, reviewTitle, reviewDetail, reviewStatus, reviewPageLabel;
    Button onboardingToggle;
    readonly Button[] tutorialRows = new Button[7];
    int tutorialPage, tutorialSelected;
    bool reviewVisible;
    float nextTutorialRefresh;
    string tutorialMessage = "";
    static readonly string[] TutorialTitles =
    {
        "熟悉移动",
        "接取任务",
        "装备或切换武器",
        "进入副本",
        "使用普攻",
        "使用闪避",
        "举起防御",
        "手动拾取物品",
        "返回主城",
        "交付任务",
        "整理背包",
        "向导师学习",
        "强化装备",
        "使用主城仓库"
    };
    string TutorialKey(InputAction action, int binding = 0) => combatInput?.GetDisplayString(action, binding) ?? "未绑定";
    string MovementKeys => combatInput == null ? "未绑定" : string.Join(" / ", new[] { TutorialKey(combatInput.Move, 1), TutorialKey(combatInput.Move, 2), TutorialKey(combatInput.Move, 3), TutorialKey(combatInput.Move, 4) });

    string TutorialInstruction(TutorialStep step)
    {
        string interact = TutorialKey(combatInput?.Interact);
        return step switch
        {
            TutorialStep.Move => $"用 [{MovementKeys}] 移动约 3 米。鼠标控制朝向，Ctrl 显示或收起鼠标。",
            TutorialStep.AcceptQuest => $"靠近主城执事，按 [{interact}] 交谈，在任务页接取「初到静谧城」。完成城内交谈后可继续林地任务；J 可查看目标和导航。",
            TutorialStep.EquipWeapon => $"按 B 打开背包，选择武器并装备到武器槽；也可按 [{TutorialKey(combatInput?.StanceToggle)}] 切换两把初始武器。只在实际装备发生变化后计入。",
            TutorialStep.EnterDungeon => "走入主城传送门的触发范围，在列表中选择普通难度副本并出发。训练场可自由练习，但不计入本步骤。",
            TutorialStep.BasicAttack => $"在副本中按 [{TutorialKey(combatInput?.Attack)}] 普攻。连续点击可衔接连段；主城禁止攻击。",
            TutorialStep.Dodge => $"轻按 [{TutorialKey(combatInput?.Roll)}] 闪避。方向键决定方向；长按为冲刺。副本中闪避消耗能量，不能在菜单或受限状态中使用。",
            TutorialStep.Guard => $"在副本中按住 [{TutorialKey(combatInput?.Guard)}] 举起防御，松开结束。防御需要能量和战斗姿态；命中前瞬间防御可精准格挡。",
            TutorialStep.ManualPickup => $"击败敌人或打开宝箱后靠近地面物品，按 [{interact}] 拾取。按 [{TutorialKey(combatInput?.LootList)}] 查看附近列表和比较；金币、经验继续自动吸附。",
            TutorialStep.ReturnTown => "通关或倒下后，在结算页选择「返回主城」。也可暂停返回主菜单，再继续角色回城。离开前检查掉落，地面物品不会保存。",
            TutorialStep.TurnInQuest => $"完成任务目标后回到对应 NPC，按 [{interact}] 交谈并提交任务。J 中可查看目标、奖励和交付位置。未完成时可再次进入副本继续。",
            TutorialStep.InventorySort => "按 B 打开背包，点击「品质 / 等级 / 类型 / 获得」中的任一排序按钮。搜索和分类可缩小范围；排序不会改变物品本身。",
            TutorialStep.LearnSkill => $"靠近本职业导师，按 [{interact}] 交谈，在技能页学习一项技能。需要达到等级并支付显示的金币；新角色只有普攻，学会后可在技能栏使用。",
            TutorialStep.Enhance => "与主城铁匠交谈，强化已装备武器或背包中的防具一次。武器 +1 需要 100 金币；防具还需锻造碎片。费用与结果会在确认时显示。",
            TutorialStep.WarehouseDeposit => "与主城执事交谈，打开仓库，选择一件材料、药品或空闲装备并确认存入。已穿戴、两武器槽和配装引用物品需先解除引用；仓库按角色保存。",
            _ => "所有引导步骤已完成，之后可以继续探索与成长。"
        };
    }

    void BuildTutorialHud()
    {
        if (tutorialSurface != null || classNameText == null)
            return;
        var font = classNameText.font;
        tutorialSurface = UIView.Rect(transform, "TutorialSurface");
        onboardingCard = UIView.Rect(tutorialSurface, "OnboardingCard");
        onboardingCard.gameObject.GetComponent<Image>().color = W.Ink;
        onboardingTitle = UIView.Text(onboardingCard, "StepTitle", "");
        onboardingHint = UIView.Text(onboardingCard, "StepHint", "");
        onboardingState = UIView.Text(onboardingCard, "StepProgress", "");
        UIView.Button(onboardingCard, "ReviewTutorial", "查看全部步骤", OpenTutorialReview);
        UIView.Button(onboardingCard, "SkipTutorial", "跳过引导", () => SetTutorialEnabled(false));
        onboardingReview = UIView.Panel(tutorialSurface, "OnboardingReview");
        UIView.Text(onboardingReview, "Heading", "新手引导 · 可随时回看");
        UIView.Button(onboardingReview, "CloseTutorialReview", "关闭 [ESC]", CloseTutorialReview);
        reviewStatus = UIView.Text(onboardingReview, "ReviewStatus", "");
        for (int i = 0; i < tutorialRows.Length; i++)
        {
            int row = i;
            tutorialRows[i] = UIView.Button(onboardingReview, "TutorialStep:" + i, "", () =>
            {
                tutorialSelected = tutorialPage * 7 + row;
                RefreshTutorialReview();
            });
        }

        UIView.Button(onboardingReview, "TutorialPrevious", "上一页", () =>
        {
            tutorialPage = Mathf.Max(0, tutorialPage - 1);
            RefreshTutorialReview();
        });
        reviewPageLabel = UIView.Text(onboardingReview, "TutorialPage", "");
        reviewPageLabel.alignment = TextAlignmentOptions.Center;
        UIView.Button(onboardingReview, "TutorialNext", "下一页", () =>
        {
            tutorialPage = Mathf.Min(1, tutorialPage + 1);
            RefreshTutorialReview();
        });
        reviewTitle = UIView.Text(onboardingReview, "ReviewStepTitle", "");
        reviewDetail = UIView.Text(onboardingReview, "ReviewStepDetail", "");
        UIView.Text(onboardingReview, "ReviewHelp", "仅实际完成操作才会记录；允许提前完成后面的步骤。\n引导不发放奖励，不限制菜单和战斗。Ctrl 显示鼠标。");
        UIView.Button(onboardingReview, "TutorialCurrent", "查看当前步骤", () =>
        {
            tutorialSelected = (int)(_progress.Tutorial.NextStep ?? TutorialStep.WarehouseDeposit);
            tutorialPage = tutorialSelected / 7;
            RefreshTutorialReview();
        });
        onboardingToggle = UIView.Button(onboardingReview, "TutorialToggle", "", () => SetTutorialEnabled(!_progress.Tutorial.enabled));
        UIView.Text(onboardingReview, "NoTutorialRewards", "回看、跳过和继续不会重置进度或赠送物品。");
        onboardingReview.gameObject.SetActive(false);
    }

    void OpenTutorialReview()
    {
        reviewVisible = true;
        tutorialMessage = "";
        tutorialSelected = (int)(_progress.Tutorial.NextStep ?? TutorialStep.WarehouseDeposit);
        tutorialPage = tutorialSelected / 7;
        onboardingCard.gameObject.SetActive(false);
        onboardingReview.gameObject.SetActive(true);
        RefreshTutorialReview();
    }

    void CloseTutorialReview()
    {
        reviewVisible = false;
        if (onboardingReview != null)
            onboardingReview.gameObject.SetActive(false);
    }

    public bool HandleTutorialBack()
    {
        if (reviewVisible && onboardingReview != null && onboardingReview.gameObject.activeInHierarchy)
        {
            CloseTutorialReview();
            return true;
        }

        if (!tutorialVisible)
            return false;
        tutorialVisible = false;
        if (tutorialPanel != null)
            tutorialPanel.gameObject.SetActive(false);
        return true;
    }

    void SetTutorialEnabled(bool enabled)
    {
        _progress.TrySetTutorialEnabled(enabled, out tutorialMessage);
        RefreshTutorialReview();
        nextTutorialRefresh = 0;
    }

    bool TutorialCanShow => _session != null && combatPlayer != null && !combatPlayer.IsDead && combatInput != null && !combatInput.IsGameplayBlockedExcept(null);

    void RefreshTutorialHud()
    {
        if (tutorialSurface == null || _progress == null || Time.unscaledTime < nextTutorialRefresh)
            return;
        nextTutorialRefresh = Time.unscaledTime + .1f;
        bool show = TutorialCanShow && (_gameSettings?.CurrentSettings.ShowHud ?? true);
        if (!show)
            CloseTutorialReview();
        var data = _progress.Tutorial;
        bool active = show && data.enabled && !data.IsComplete && !reviewVisible;
        onboardingCard.gameObject.SetActive(active);
        onboardingReview.gameObject.SetActive(show && reviewVisible);
        if (active)
        {
            int step = (int)data.NextStep.Value;
            onboardingTitle.text = $"新手引导 {step + 1}/{TutorialData.StepCount} · {TutorialTitles[step]}";
            onboardingHint.text = TutorialInstruction((TutorialStep)step);
            onboardingState.text = $"已完成 {data.CompletedCount}/{TutorialData.StepCount} · 自动保存 · Ctrl 操作按钮";
        }

        if (reviewVisible)
            RefreshTutorialReview();
    }

    void RefreshTutorialReview()
    {
        if (onboardingReview == null || _progress == null)
            return;
        var data = _progress.Tutorial;
        reviewStatus.text = string.IsNullOrEmpty(tutorialMessage) ? $"已完成 {data.CompletedCount}/{TutorialData.StepCount} · " + (data.IsComplete ? "全部完成" : data.enabled ? "正在引导" : "已跳过，仍可回看或继续") : tutorialMessage;
        reviewPageLabel.text = (tutorialPage + 1) + " / 2";
        for (int i = 0; i < tutorialRows.Length; i++)
        {
            int index = tutorialPage * 7 + i;
            tutorialRows[i].GetComponentInChildren<TMP_Text>().text = $"{(data.Has((TutorialStep)index) ? "已完成" : "待完成")} {index + 1}. {TutorialTitles[index]}";
            tutorialRows[i].GetComponent<Image>().color = index == tutorialSelected ? W.Accent : W.Card;
        }

        reviewTitle.text = $"{tutorialSelected + 1}. {TutorialTitles[tutorialSelected]}";
        reviewDetail.text = TutorialInstruction((TutorialStep)tutorialSelected);
        onboardingToggle.GetComponentInChildren<TMP_Text>().text = data.enabled ? "跳过剩余引导" : "继续未完成步骤";
        onboardingToggle.interactable = !data.IsComplete;
    }
}
