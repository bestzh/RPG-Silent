using System;
using System.Collections.Generic;
using System.Text;
using RPGSilent.Combat;
using RPGSilent.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerEquipmentEffects : MonoBehaviour
{
    readonly EquipmentEffectState timers = new();
    readonly List<EnemyController> candidates = new(32);
    readonly RaycastHit[] sightHits = new RaycastHit[32];
    readonly StringBuilder status = new();
    PlayerController player;
    Comparison<EnemyController> nearest;
    Vector3 sortOrigin;
    float feedbackUntil;
    string feedback = "";
    EquipmentSpecialEffect feedbackEffect;
    bool CanUse => isActiveAndEnabled && player != null && player.CanReceiveCombatEffects && player.Progress != null;
    bool CanTrigger => CanUse && !(player.Weapons != null && player.Weapons.IsBusy);
    public string Feedback => Time.time < feedbackUntil && ActiveLevel(feedbackEffect) > 0 ? feedback : "";
    void Awake()
    {
        player = GetComponent<PlayerController>();
        nearest = (a, b) => {
            int distance = (a.transform.position - sortOrigin).sqrMagnitude.CompareTo((b.transform.position - sortOrigin).sqrMagnitude);
            return distance != 0 ? distance : a.GetInstanceID().CompareTo(b.GetInstanceID());
        };
    }
    void OnEnable() => SceneManager.activeSceneChanged += SceneChanged;
    void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; ResetCombat(); }
    void SceneChanged(Scene before, Scene after) => ResetCombat();
    public void ResetCombat() { timers.Reset(); candidates.Clear(); feedback = ""; feedbackUntil = 0; }
    public int ActiveLevel(EquipmentSpecialEffect effect) => CanUse ? player.Progress.EquipmentEffectLevel(effect) : 0;
    public float RemainingCooldown(EquipmentSpecialEffect effect) => timers.Remaining(effect, Time.time);
    public float SpiritRemaining => ActiveLevel(EquipmentSpecialEffect.SpiritBond) > 0 ? timers.ActiveRemaining(EquipmentSpecialEffect.SpiritBond, Time.time) : 0;
    public void EquipmentChanged()
    {
        if (player?.Progress?.EquipmentEffectLevel(EquipmentSpecialEffect.SpiritBond) == 0)
            timers.Deactivate(EquipmentSpecialEffect.SpiritBond);
    }
    void Show(EquipmentSpecialEffect effect, string detail)
    { feedbackEffect = effect; feedback = EquipmentEffectRules.Name(effect) + " · " + detail; feedbackUntil = Time.time + 2.2f; }
    public void OnPerfectParry()
    {
        if (!CanTrigger) return;
        int level = ActiveLevel(EquipmentSpecialEffect.ParryEnergy);
        if (!timers.TryTrigger(EquipmentSpecialEffect.ParryEnergy, level, Time.time)) return;
        int energy = EquipmentEffectRules.ParryEnergy(level);
        player.CombatStats?.RecoverEnergy(energy); Show(EquipmentSpecialEffect.ParryEnergy, "额外能量 +" + energy);
    }
    public void OnSummoned()
    {
        if (!CanTrigger) return;
        int level = ActiveLevel(EquipmentSpecialEffect.SpiritBond);
        if (player?.Progress?.CurrentClass?.Id != "beastmaster" || player.ClassCombat?.Companion == null ||
            !timers.TryTrigger(EquipmentSpecialEffect.SpiritBond, level, Time.time)) return;
        Show(EquipmentSpecialEffect.SpiritBond, "兽灵强化 8 秒");
    }
    public int CompanionDamage(int damage, EnemyController source)
    {
        if (source == null || player == null || source != player.ClassCombat?.Companion) return damage;
        int level = ActiveLevel(EquipmentSpecialEffect.SpiritBond);
        if (level == 0) { timers.Deactivate(EquipmentSpecialEffect.SpiritBond); return damage; }
        return SpiritRemaining > 0 ? EquipmentEffectRules.Damage(damage, EquipmentSpecialEffect.SpiritBond, level) : damage;
    }
    public void OnHit(EnemyController primary, AttackContext attack, int actualDamage, Vector3 hitPoint)
    {
        if (!CanTrigger || primary == null || attack == null || attack.IsEquipmentProc || attack.EquipmentProcUsed || actualDamage <= 0 ||
            attack.SourceWeaponKey != (player.Weapons?.GetInstanceId(player.Weapons.ActiveSlot) ?? "")) return;
        var effect = attack.IsSkill ? EquipmentSpecialEffect.SpellChain : attack.IsHeavyAttack ? EquipmentSpecialEffect.HeavyShock : EquipmentSpecialEffect.None;
        int level = ActiveLevel(effect);
        if (level == 0 || timers.Remaining(effect, Time.time) > 0) return;
        candidates.Clear(); sortOrigin = primary.transform.position;
        float radius = EquipmentEffectRules.Radius(effect);
        foreach (var target in EnemyController.ActiveEnemies)
            if (target != null && target != primary && target.isActiveAndEnabled && !target.IsCompanion && !target.IsDead && !target.CombatLocked &&
                target.gameObject.scene == primary.gameObject.scene && (target.transform.position - sortOrigin).sqrMagnitude <= radius * radius && ClearRay(primary, target)) candidates.Add(target);
        if (candidates.Count == 0 || !timers.TryTrigger(effect, level, Time.time)) return;
        attack.EquipmentProcUsed = true; candidates.Sort(nearest);
        // Snapshot targets without allocating: death/result callbacks may clear candidates.
        int count = Mathf.Min(candidates.Count, EquipmentEffectRules.TargetLimit(effect));
        var first = candidates[0]; var second = count > 1 ? candidates[1] : null; var third = count > 2 ? candidates[2] : null;
        candidates.Clear();
        var secondary = attack.Owner.CreateEquipmentProc(attack, EquipmentEffectRules.Damage(actualDamage, effect, level), effect == EquipmentSpecialEffect.HeavyShock ? 1.5f : 1f);
        int hit = 0;
        for (int i = 0; i < count; i++)
        {
            var target = i == 0 ? first : i == 1 ? second : third;
            if (target != null && attack.Owner.ApplyDamage(target, secondary, target.transform.position + Vector3.up, sourcePosition: hitPoint)) hit++;
        }
        Show(effect, "波及 " + hit + " 个目标");
    }
    bool ClearRay(EnemyController from, EnemyController target)
    {
        Vector3 origin = from.transform.position + Vector3.up, delta = target.transform.position + Vector3.up - origin;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, sightHits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = sightHits[i].transform;
            if (hit.IsChildOf(player.transform) || hit.GetComponentInParent<EnemyController>() != null) continue;
            return false;
        }
        return true;
    }
    public string StatusText()
    {
        status.Clear(); int count = 0;
        for (int i = 1; i < EquipmentEffectRules.EffectCount; i++)
        {
            var effect = (EquipmentSpecialEffect)i; if (ActiveLevel(effect) == 0) continue;
            if (count > 0) status.Append(count % 2 == 0 ? "\n" : "    ");
            status.Append(EquipmentEffectRules.Name(effect)).Append(' ');
            float remaining = RemainingCooldown(effect);
            status.Append(remaining > 0 ? remaining.ToString("0.0") + "s" : "就绪");
            if (effect == EquipmentSpecialEffect.SpiritBond && SpiritRemaining > 0) status.Append(" · 强化 ").Append(SpiritRemaining.ToString("0.0")).Append('s');
            count++;
        }
        return status.ToString();
    }
}
