using UnityEngine;

public sealed class BossPhaseController : MonoBehaviour
{
    [Range(0.05f, 0.95f)] public float SecondPhaseThreshold = 0.5f;
    public float TransitionDuration = 1.8f;
    public string TransitionAnimation = "Phase";
    public GameObject TransitionEffect;
    public GameObject EnrageAura;
    public int Phase { get; private set; } = 1;
    private GameObject aura;

    public bool TryAdvance(float healthFraction)
    {
        if (Phase != 1 || healthFraction > SecondPhaseThreshold) return false;
        Phase = 2;
        EnemySkillRunner.SpawnEffect(TransitionEffect, transform.position, Quaternion.identity, 4f);
        if (EnrageAura != null)
        {
            aura = Instantiate(EnrageAura, transform);
            aura.transform.localPosition = Vector3.zero;
        }
        return true;
    }

    public void ClearEffects() { if (aura != null) Destroy(aura); }
    private void OnDisable() => ClearEffects();
}
