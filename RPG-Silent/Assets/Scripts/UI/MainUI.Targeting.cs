using TMPro;
using UnityEngine;
using UnityEngine.UI;
using W = ProgressionUIWidgets;

public partial class MainUI
{
    RectTransform targetHud, lockReticle;
    TMP_Text targetCaption, defenseCaption, combatControls;
    Camera targetViewCamera;
    Canvas targetCanvas;
    float nextTargetTextAt;
    TMP_Text impactCaption, weaponCaption;
    Image[] impactEdges;
    RectTransform chargeMeter;
    Image chargeFill;
    void BuildTargetHud()
    {
        if (targetHud != null || classNameText == null) return;
        targetHud = W.Panel(transform.Find("TopLeftPanel"), "TargetDefenseHud", Color.clear);
        targetHud.GetComponent<Image>().raycastTarget = false;
        targetCanvas = GetComponentInParent<Canvas>();
        var font = classNameText.font;
        targetCaption = W.Text(targetHud, font, "LockedEnemy", "", 0, 0, 400, 54, 22);
        Anchor(targetCaption.rectTransform, new Vector2(.5f, 1), new Vector2(0, -316));
        targetCaption.alignment = TextAlignmentOptions.Center;
        lockReticle = W.Rect(targetHud, "LockReticle", 0, 0, 36, 36);
        Anchor(lockReticle, Vector2.zero, Vector2.zero);
        for (int i = 0; i < 4; i++)
        {
            var edge = W.Rect(lockReticle, "Edge" + i, 0, 0, i < 2 ? 3 : 12, i < 2 ? 12 : 3);
            Anchor(edge, new Vector2(.5f,.5f), i == 0 ? new Vector2(-18,0) : i == 1 ? new Vector2(18,0) : i == 2 ? new Vector2(0,18) : new Vector2(0,-18));
            var image = edge.gameObject.AddComponent<Image>(); image.color = new Color(1f,.79f,.32f); image.raycastTarget = false;
        }
        defenseCaption = W.Text(targetHud, font, "DefenseFeedback", "", 0, 0, 540, 38, 24);
        Anchor(defenseCaption.rectTransform, new Vector2(.5f,.34f), Vector2.zero);
        defenseCaption.alignment = TextAlignmentOptions.Center;
        combatControls = W.Text(targetHud, font, "CombatControls", "", 0, 0, 740, 30, 18);
        Anchor(combatControls.rectTransform, new Vector2(.5f,0), new Vector2(0,24));
        combatControls.alignment = TextAlignmentOptions.Center;
        combatControls.rectTransform.sizeDelta = new Vector2(980, 30);
        weaponCaption = W.Text(targetHud, font, "WeaponCombatHint", "", 0, 0, 980, 30, 19);
        Anchor(weaponCaption.rectTransform, new Vector2(.5f,0), new Vector2(0,57));
        weaponCaption.alignment = TextAlignmentOptions.Center;
        impactCaption = W.Text(targetHud, font, "PlayerImpact", "", 0, 0, 360, 34, 23);
        Anchor(impactCaption.rectTransform, new Vector2(.5f,.62f), Vector2.zero);
        impactCaption.alignment = TextAlignmentOptions.Center;
        impactCaption.color = new Color(1,.32f,.25f);
        impactEdges = new Image[4];
        Vector2[] anchors = { new Vector2(.5f,.73f), new Vector2(.5f,.4f), new Vector2(.38f,.56f), new Vector2(.62f,.56f) };
        for (int i = 0; i < impactEdges.Length; i++)
        {
            var edge = W.Rect(targetHud, "IncomingDamage" + i, 0, 0, i < 2 ? 120 : 6, i < 2 ? 6 : 90);
            Anchor(edge, anchors[i], Vector2.zero);
            impactEdges[i] = edge.gameObject.AddComponent<Image>(); impactEdges[i].raycastTarget = false;
        }
        chargeMeter = W.Rect(targetHud, "ChargeMeter", 0, 0, 240, 7);
        Anchor(chargeMeter, new Vector2(.5f,.3f), Vector2.zero);
        var background = chargeMeter.gameObject.AddComponent<Image>(); background.color = new Color(.08f,.09f,.12f,.8f); background.raycastTarget = false;
        var fill = W.Rect(chargeMeter, "Fill", 0, 0, 240, 7);
        fill.anchorMin = fill.anchorMax = new Vector2(0,.5f); fill.pivot = new Vector2(0,.5f); fill.anchoredPosition = Vector2.zero;
        chargeFill = fill.gameObject.AddComponent<Image>(); chargeFill.raycastTarget = false;
        chargeMeter.gameObject.SetActive(false);
        lockReticle.gameObject.SetActive(false);
    }
    static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position)
    { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f,.5f); rect.anchoredPosition = position; }
    void LateUpdate()
    {
        if (targetHud == null) return;
        bool visible = combatPlayer != null && combatPlayer.isActiveAndEnabled && combatPlayer.GameplayInputEnabled
            && combatPlayer.CanReceiveCombatEffects && (_gameSettings?.CurrentSettings.ShowHud ?? true);
        targetHud.gameObject.SetActive(visible);
        if (!visible) return;
        UpdateImpactAndCharge();
        var targeting = combatPlayer.TargetLock;
        bool locked = targeting != null && targeting.IsLocked;
        if (targetViewCamera == null) targetViewCamera = Camera.main;
        Vector3 screen = locked && targetViewCamera != null ? targetViewCamera.WorldToScreenPoint(targeting.AimPoint) : Vector3.back;
        bool onScreen = locked && screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height;
        lockReticle.gameObject.SetActive(onScreen);
        if (onScreen)
        {
            Camera uiCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? targetCanvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetHud, screen, uiCamera, out var local);
            lockReticle.localPosition = new Vector3(local.x, local.y, 0);
        }
        if (Time.unscaledTime < nextTargetTextAt) return;
        nextTargetTextAt = Time.unscaledTime + .1f;
        // Boss encounters already own a phase/name/health bar at the top of the screen.
        targetCaption.text = locked && !targeting.Target.IsBoss ? targeting.Target.DisplayName + "\n" + targeting.Target.CurrentHealth + " / " + targeting.Target.MaxHealth : "";
        var defense = combatPlayer.Defense;
        defenseCaption.text = defense == null ? "" : !string.IsNullOrEmpty(defense.Feedback) ? defense.Feedback
            : defense.CounterReady ? "反击就绪 · 下一次普攻增强" : defense.IsGuarding ? "防御中 · 正面减伤" : "";
        if (combatInput != null) combatControls.text = "锁定 [" + combatInput.GetDisplayString(combatInput.LockTarget,0)
            + "]  切换目标 [滚轮 / " + combatInput.GetDisplayString(combatInput.PreviousTarget,0) + " "
            + combatInput.GetDisplayString(combatInput.NextTarget,0) + "]  防御 [" + combatInput.GetDisplayString(combatInput.Guard,0)
            + "]  闪避 [轻按 " + combatInput.GetDisplayString(combatInput.Roll,0) + "]";
        string attackKey = combatInput != null ? combatInput.GetDisplayString(combatInput.Attack,0) : "普攻";
        weaponCaption.text = combatPlayer.IsCharging ? (combatPlayer.ChargeRatio >= .999f ? "蓄力完成" : "蓄力 " + Mathf.RoundToInt(combatPlayer.ChargeRatio * 100) + "%") + " · 松开 [" + attackKey + "] 释放 / 防御取消"
            : combatPlayer.CurrentStance switch
            {
                PlayerStance.OneHandSword => combatPlayer.DodgeFollowupReady ? "闪避追击就绪 · 按 [" + attackKey + "]" : "单手剑 · 快速三连击 · 闪避后普攻追击",
                PlayerStance.TwoHandSword => "重剑 · 点按连击 / 按住 [" + attackKey + "] 蓄力 · 满蓄力强化削韧",
                PlayerStance.TwoHandBow => "弓 · 点按速射 / 按住 [" + attackKey + "] 蓄力 · 强化伤害与箭速",
                PlayerStance.TwoHandStaff => "法杖 · 按住 [" + attackKey + "] 连发 · 第三发命中回复能量",
                _ => ""
            };
    }

    void UpdateImpactAndCharge()
    {
        var hit = combatPlayer.HitFeedback;
        float alpha = hit != null ? hit.Visibility : 0;
        impactCaption.text = alpha > 0 ? (hit.Periodic ? "持续伤害 " : hit.Impact == AttackImpact.Heavy ? "重击 " : "受击 ") + "−" + hit.Damage : "";
        impactCaption.alpha = alpha;
        int edge = -1;
        if (alpha > 0 && !hit.Periodic)
        {
            Quaternion view = targetViewCamera != null ? targetViewCamera.transform.rotation : combatPlayer.ViewRotation;
            Vector3 local = Quaternion.Inverse(view) * hit.SourceDirection;
            edge = Mathf.Abs(local.x) > Mathf.Abs(local.z) ? local.x > 0 ? 3 : 2 : local.z > 0 ? 0 : 1;
        }
        for (int i = 0; i < impactEdges.Length; i++) impactEdges[i].color = new Color(1,.18f,.1f, i == edge ? alpha * .8f : 0);
        chargeMeter.gameObject.SetActive(combatPlayer.IsCharging);
        if (combatPlayer.IsCharging)
        {
            float ratio = combatPlayer.ChargeRatio;
            chargeFill.rectTransform.sizeDelta = new Vector2(240 * ratio, 7);
            chargeFill.color = ratio >= .999f ? new Color(1,.8f,.3f) : new Color(.4f,.75f,1);
        }
    }
}
