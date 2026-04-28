using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    private SkillData currentSkill;
    private float comboTimer = 0f;
    private bool isWaitingForCombo = false;
    private bool isAttacking = false;
    private PlayerController playerController;
    private readonly Dictionary<SkillData, float> cooldownTimers = new();

    [Header("Energy")]
    public float maxEnergy = 100f;
    public float currentEnergy = 0f;
    public float energyRecoveryRate = 10f;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        UpdateComboWindow();

        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }

        UpdateCooldowns();
        RecoverEnergy();
    }

    private void UpdateComboWindow()
    {
        if (!isWaitingForCombo) return;

        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f)
        {
            ResetCombo();
        }
    }

    private void RecoverEnergy()
    {
        if (currentEnergy >= maxEnergy) return;

        currentEnergy += energyRecoveryRate * Time.deltaTime;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
    }

    private void UpdateCooldowns()
    {
        List<SkillData> keys = new(cooldownTimers.Keys);

        foreach (SkillData skill in keys)
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

            return;
        }

        if (SkillDatabase.Instance == null)
        {
            Debug.LogWarning("SkillDatabase is not loaded.");
            TryFallbackAttack();
            return;
        }

        SkillData normalAttack = SkillDatabase.Instance.GetNormalAttackSkill();
        if (normalAttack == null)
        {
            TryFallbackAttack();
            return;
        }

        TryPlaySkill(normalAttack);
    }

    private void TryPlaySkill(SkillData skill)
    {
        if (skill == null)
        {
            Debug.LogWarning("Skill is not configured.");
            return;
        }

        if (IsSkillOnCooldown(skill))
        {
            Debug.Log("Skill is on cooldown: " + skill.name);
            return;
        }

        if (currentEnergy < skill.EnergyCost)
        {
            Debug.Log("Not enough energy to cast skill: " + skill.name);
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

        Debug.Log("Cast skill: " + skill.name);

        if (SkillDatabase.Instance != null && skill == SkillDatabase.Instance.NormalAttackSkill)
        {
            TryFallbackAttack();
        }

        if (skill.CooldownTime > 0f)
        {
            cooldownTimers[skill] = skill.CooldownTime;
        }

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

    private void TryFallbackAttack()
    {
        if (playerController == null || playerController.IsRolling || playerController.IsJumping) return;

        playerController.StateMachine.ChangeState(new AttackState(playerController));
    }

    private void ResetCombo()
    {
        isWaitingForCombo = false;
        isAttacking = false;
        currentSkill = null;
    }

    public void OnAttackAnimationFinished()
    {
        ResetCombo();
    }
}
