using UnityEngine;
using System.Collections.Generic;
public class PlayerSkillController : MonoBehaviour
{
    private SkillData currentSkill;
    private float comboTimer = 0f;
    private bool isWaitingForCombo = false;
    private bool isAttacking = false;
    private Dictionary<SkillData, float> cooldownTimers = new Dictionary<SkillData, float>();
    [Header("能量槽系统")]
    public float maxEnergy = 100f; // 最大能量
    public float currentEnergy = 0f; // 当前能量
    public float energyRecoveryRate = 10f; // 每秒回复能量速度
    private void Update()
    {
        if (isWaitingForCombo)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse0)) // 左键攻击
        {
            TryAttack();
        }
        UpdateCooldowns();
        RecoverEnergy();
    }
    private void RecoverEnergy()
    {
        if (currentEnergy < maxEnergy)
        {
            currentEnergy += energyRecoveryRate * Time.deltaTime;
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
        }
    }
    private void UpdateCooldowns()
    {
        List<SkillData> keys = new List<SkillData>(cooldownTimers.Keys);

        foreach (var skill in keys)
        {
            cooldownTimers[skill] -= Time.deltaTime;
            if (cooldownTimers[skill] <= 0f)
            {
                cooldownTimers.Remove(skill);
            }
        }
    }
    private void TryAttack()
    {
        if (isAttacking)
        {
            if (isWaitingForCombo && currentSkill != null && currentSkill.NextComboSkill != null)
            {
                TryPlaySkill(currentSkill.NextComboSkill);
            }
        }
        else
        {
            SkillData normalAttack = SkillDatabase.Instance.GetNormalAttackSkill();
            TryPlaySkill(normalAttack);
        }
    }

    private void TryPlaySkill(SkillData skill)
    {
        if (IsSkillOnCooldown(skill))
        {
            Debug.Log("技能冷却中：" + skill.name);
            return;
        }

        if (currentEnergy < skill.EnergyCost)
        {
            Debug.Log("能量不足，无法释放技能：" + skill.name);
            return;
        }

        currentEnergy -= skill.EnergyCost;
        PlaySkill(skill);
    }

    private bool IsSkillOnCooldown(SkillData skill)
    {
        return cooldownTimers.ContainsKey(skill);
    }

    private void PlaySkill(SkillData skill)
    {
        isAttacking = true;
        currentSkill = skill;
        isWaitingForCombo = false;

        Debug.Log("释放技能：" + skill.name);

        // 开始冷却
        if (skill.CooldownTime > 0f)
        {
            cooldownTimers[skill] = skill.CooldownTime;
        }

        // Combo连招窗口处理
        if (skill.CanCombo && skill.NextComboSkill != null)
        {
            comboTimer = skill.ComboInputWindow;
            isWaitingForCombo = true;
        }
        else
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        isWaitingForCombo = false;
        isAttacking = false;
        currentSkill = null;
    }

    // 动画事件调用（打完动作某一帧后）
    public void OnAttackAnimationFinished()
    {
        ResetCombo();
    }
}
