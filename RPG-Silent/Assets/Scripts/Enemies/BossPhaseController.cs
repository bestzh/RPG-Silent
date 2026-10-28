using UnityEngine;

public sealed class BossPhaseController : MonoBehaviour
{
    [Range(0.05f, 0.95f)] public float SecondPhaseThreshold = 0.5f;
    public float TransitionDuration = 1.8f;
    public string TransitionAnimation = "Phase";
    public GameObject TransitionEffect;
    public GameObject EnrageAura;
    [Header("终阶段（现有双阶段预制体默认不启用）")]
    public bool EnableThirdPhase;
    [Range(.01f, .9f)] public float ThirdPhaseThreshold = .25f;
    public string SecondPhaseName = "狂怒";
    public string ThirdPhaseName = "决战";
    public string SecondPhaseHint = "注意连续攻击，收招时反击";
    public string ThirdPhaseHint = "避开终结技，等待破绽";
    [Range(1f, 2f)] public float ThirdPhaseDamageMultiplier = 1.15f;
    public int Phase { get; private set; } = 1;
    public string PhaseName => Phase == 1 ? "试探" : Phase == 2 ? SecondPhaseName : ThirdPhaseName;
    public string Hint => Phase == 1 ? "观察预警，攻击后接技能" : Phase == 2 ? SecondPhaseHint : ThirdPhaseHint;
    public float DamageMultiplier => Phase >= 3 ? ThirdPhaseDamageMultiplier : 1f;
    private GameObject aura;

    public bool TryAdvance(float healthFraction)
    {
        if (float.IsNaN(healthFraction) || float.IsInfinity(healthFraction) || healthFraction <= 0f ||
            (Phase == 1 ? healthFraction > SecondPhaseThreshold : Phase != 2 || !EnableThirdPhase ||
            ThirdPhaseThreshold >= SecondPhaseThreshold || healthFraction > ThirdPhaseThreshold)) return false;
        Phase++;
        EnemySkillRunner.SpawnEffect(TransitionEffect, transform.position, Quaternion.identity, 4f);
        RestoreEffects();
        return true;
    }

    public void RestoreEffects()
    {
        if (Phase >= 2 && EnrageAura != null && aura == null)
        {
            aura = Instantiate(EnrageAura, transform);
            aura.transform.localPosition = Vector3.zero;
        }
    }

    public void ClearEffects() { if (aura != null) Destroy(aura); aura = null; }
    public void ResetEncounter() { ClearEffects(); Phase = 1; }
    private void OnDisable() => ClearEffects();
}
