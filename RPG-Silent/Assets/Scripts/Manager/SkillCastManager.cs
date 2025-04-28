using UnityEngine;
using System.Collections.Generic;

public class SkillCastManager : MonoBehaviour
{
    private PlayerAnimationController animationController;

    private bool isCasting = false;
    private float castTimer = 0f;
    private float castDuration = 0f;
    private SkillData currentCastingSkill;

    private Dictionary<string, float> cooldownTimers = new Dictionary<string, float>();

    [Header("技能表")]
    public List<SkillData> skillList;

    private void Awake()
    {
        animationController = GetComponent<PlayerAnimationController>();
        foreach (var skill in skillList)
        {
            cooldownTimers.Add(skill.SkillName, 0f);
        }
    }

    private void Update()
    {
        // 冷却计时
        List<string> keys = new List<string>(cooldownTimers.Keys);
        foreach (var key in keys)
        {
            if (cooldownTimers[key] > 0f)
                cooldownTimers[key] -= Time.deltaTime;
        }

        if (isCasting)
        {
            castTimer += Time.deltaTime;
            if (castTimer >= castDuration)
            {
                CompleteSkillCast();
            }
        }

        // 示例：按 Q 释放技能1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryCastSkill("Fireball");
        }
        {
            TryCastSkill("Fireball");
        }
        // 示例：按 E 释放技能2
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryCastSkill("SwordSlash");
        }
    }

    public bool TryCastSkill(string skillName)
    {
        if (isCasting) return false; // 正在施法中
        if (!cooldownTimers.ContainsKey(skillName)) return false;
        if (cooldownTimers[skillName] > 0f) return false; // 冷却中

        var skill = GetSkillData(skillName);
        if (skill == null)
        {
            Debug.LogWarning($"技能 {skillName} 没找到！");
            return false;
        }

        animationController.PlayAnimation(skill.Animation.AnimationName);

        currentCastingSkill = skill;
        castDuration = skill.CastTime;
        castTimer = 0f;
        isCasting = true;

        Debug.Log($"开始施法：{skill.SkillName}，吟唱时间：{skill.CastTime}秒");
        return true;
    }

    private void CompleteSkillCast()
    {
        Debug.Log($"技能释放成功：{currentCastingSkill.SkillName}");

        SpawnSkillEffect(currentCastingSkill);

        cooldownTimers[currentCastingSkill.SkillName] = currentCastingSkill.CooldownTime;
        ResetCastingState();
    }

    private void SpawnSkillEffect(SkillData skill)
    {
        if (skill.EffectPrefab == null)
            return;

        Vector3 spawnPos = transform.position + transform.rotation * skill.EffectOffset;
        Quaternion spawnRot = transform.rotation;

        GameObject effectObj = Instantiate(skill.EffectPrefab, spawnPos, spawnRot);
        
        Destroy(effectObj, 5f); // 5秒后自动销毁
    }
    public void InterruptSkill()
    {
        if (isCasting && currentCastingSkill != null && currentCastingSkill.CanBeInterrupted)
        {
            Debug.Log($"施法中断：{currentCastingSkill.SkillName}");
            ResetCastingState();
        }
    }
    public void HandleSkillHit(GameObject target, SkillData skill)
    {
        if (skill.HitEffectPrefab != null)
        {
            Vector3 hitPos = target.transform.position + target.transform.rotation * skill.HitEffectOffset;
            Quaternion hitRot = Quaternion.LookRotation(transform.forward);
            GameObject hitEffectObj = Instantiate(skill.HitEffectPrefab, hitPos, hitRot);
            Destroy(hitEffectObj, 5f);
        }

        if (skill.HasChainEffect)
        {
            TriggerChainEffect(target, skill);
        }
    }
    private void TriggerChainEffect(GameObject startTarget, SkillData skill)
    {
        GameObject currentTarget = startTarget;
        int chainTimes = skill.ChainCount;

        for (int i = 0; i < chainTimes; i++)
        {
            Collider[] hits = Physics.OverlapSphere(currentTarget.transform.position, skill.ChainRadius, LayerMask.GetMask("Enemy"));

            GameObject nextTarget = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.gameObject != currentTarget)
                {
                    float dist = Vector3.Distance(currentTarget.transform.position, hit.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nextTarget = hit.gameObject;
                    }
                }
            }

            if (nextTarget != null)
            {
                // 生成连锁特效
                Vector3 from = currentTarget.transform.position;
                Vector3 to = nextTarget.transform.position;
                Vector3 midPoint = (from + to) / 2;

                if (skill.ChainEffectPrefab != null)
                {
                    GameObject chainObj = Instantiate(skill.ChainEffectPrefab, midPoint, Quaternion.identity);
                    Destroy(chainObj, 5f);
                }

                // 可以在这里对 nextTarget 造成伤害
                // nextTarget.GetComponent<EnemyController>().TakeDamage(chainDamage);

                currentTarget = nextTarget;
            }
            else
            {
                break;
            }
        }
    }
    private void ResetCastingState()
    {
        currentCastingSkill = null;
        isCasting = false;
        castTimer = 0f;
        castDuration = 0f;
    }

    private SkillData GetSkillData(string skillName)
    {
        return skillList.Find(skill => skill.SkillName == skillName);
    }

    public bool IsSkillOnCooldown(string skillName)
    {
        return cooldownTimers.ContainsKey(skillName) && cooldownTimers[skillName] > 0f;
    }
}
