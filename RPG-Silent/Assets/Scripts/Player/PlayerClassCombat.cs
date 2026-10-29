using System.Collections.Generic;
using RPGSilent.Combat;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerClassCombat : MonoBehaviour
{
    readonly Dictionary<EnemyController, ClassCombatRules> marks = new();
    PlayerController player;
    EnemyController companion;
    float feedbackUntil;
    string feedback;
    public EnemyController Companion => companion;
    public string Feedback => Time.time < feedbackUntil ? feedback : "";
    public string PassiveDescription => ClassCombatRules.Description(player?.Progress?.CurrentClass?.FamilyId);
    void Awake() => player = GetComponent<PlayerController>();
    void OnEnable() => SceneManager.activeSceneChanged += SceneChanged;
    void OnDisable() { SceneManager.activeSceneChanged -= SceneChanged; ResetCombat(); }
    void SceneChanged(Scene before, Scene after) => ResetCombat();
    public void ResetCombat()
    {
        marks.Clear(); feedbackUntil = 0;
        if (companion != null) Destroy(companion.gameObject);
        companion = null;
    }
    public int ModifyHit(EnemyController enemy, AttackContext attack, Vector3 source, out float stagger)
    {
        stagger = attack.StaggerMultiplier;
        if (enemy == null || enemy.IsCompanion || player?.Progress?.CurrentClass == null || !attack.TryRegisterClassHit(enemy)) return attack.Damage;
        if (!marks.TryGetValue(enemy, out var mark))
        {
            // Entries are bounded to one encounter, and pruned when a new target is first struck.
            var stale = new List<EnemyController>();
            foreach (var pair in marks) if (pair.Key == null || pair.Key.IsDead) stale.Add(pair.Key);
            foreach (var key in stale) marks.Remove(key);
            mark = new ClassCombatRules(); marks.Add(enemy, mark);
        }
        string family = player.Progress.CurrentClass.FamilyId;
        float scale = mark.Hit(family, attack.IsSkill, attack.Element,
            ClassCombatRules.Behind(enemy.transform.forward, source - enemy.transform.position), enemy.IsVulnerable, Time.time);
        if (mark.IsBroken(Time.time)) stagger *= 1.75f;
        if (!string.IsNullOrEmpty(mark.LastEffect))
        {
            feedback = mark.LastEffect; feedbackUntil = Time.time + 1.5f;
            if (family == "mage" && scale > 1f) player.CombatStats?.RecoverEnergy(5);
        }
        return Mathf.RoundToInt(attack.Damage * scale);
    }
    public bool Summon(SkillData skill, int rank)
    {
        if (player == null || !player.CanReceiveCombatEffects || player.Progress?.CurrentClass?.Id != "beastmaster" || skill.SummonPrefab == null) return false;
        var prefabAgent = skill.SummonPrefab.GetComponent<NavMeshAgent>();
        NavMeshHit hit;
        if (prefabAgent == null || !NavMesh.SamplePosition(transform.position + transform.right * 1.8f, out hit, 3f,
            new NavMeshQueryFilter { agentTypeID = prefabAgent.agentTypeID, areaMask = NavMesh.AllAreas }))
        { feedback = "附近没有可供召唤的落脚点"; feedbackUntil = Time.time + 2; return false; }
        if (companion != null) Destroy(companion.gameObject);
        companion = Instantiate(skill.SummonPrefab, hit.position, transform.rotation);
        SceneManager.MoveGameObjectToScene(companion.gameObject, SceneManager.GetActiveScene());
        companion.ConfigureCompanion(player, skill.SummonDuration + (rank - 1) * 3 + player.Progress.SpecializationSummonDuration,
            (1f + (rank - 1) * .2f) * player.Progress.SpecializationSummonDamage);
        player.EquipmentEffects?.OnSummoned();
        feedback = "兽灵助战 · 技能升级增强伤害与持续时间"; feedbackUntil = Time.time + 2;
        return true;
    }
}
