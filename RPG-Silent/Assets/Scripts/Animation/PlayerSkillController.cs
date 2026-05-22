using UnityEngine;

public class PlayerSkillController : MonoBehaviour
{
    [Header("Energy")]
    public float maxEnergy = 100f;
    public float currentEnergy = 0f;
    public float energyRecoveryRate = 10f;

    private void Update()
    {
        RecoverEnergy();
    }

    private void RecoverEnergy()
    {
        if (currentEnergy >= maxEnergy) return;

        currentEnergy += energyRecoveryRate * Time.deltaTime;
        currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
    }

    public void OnAttackAnimationFinished()
    {
        // Kept for animation event compatibility. Combo flow is handled by AttackState.
    }
}
