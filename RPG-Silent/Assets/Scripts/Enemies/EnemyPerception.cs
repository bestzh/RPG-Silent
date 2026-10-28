using RPGSilent.Domain;
using UnityEngine;

public sealed class EnemyPerception
{
    private readonly Transform owner;
    private readonly RaycastHit[] hits = new RaycastHit[24];
    private float nextSearch;
    private PlayerController companionOwner;
    public void ConfigureCompanion(PlayerController player) { companionOwner = player; Target = null; Damageable = null; Rewardable = null; nextSearch = 0; }
    public Transform Target { get; private set; }
    public IDamageable Damageable { get; private set; }
    public IRewardable Rewardable { get; private set; }
    public bool IsAlive => Target != null && Target.gameObject.activeInHierarchy && Damageable != null && !Damageable.IsDead;
    public float Distance => IsAlive ? Vector3.Distance(owner.position, Target.position) : float.PositiveInfinity;

    public EnemyPerception(Transform owner) => this.owner = owner;

    public void Refresh()
    {
        if (companionOwner != null)
        {
            if (Time.time < nextSearch) return;
            nextSearch = Time.time + .3f;
            EnemyController best = null; float distance = 16f * 16f;
            foreach (var enemy in EnemyController.ActiveEnemies)
            {
                if (enemy == null || enemy.IsCompanion || enemy.IsDead || enemy.CombatLocked || !enemy.isActiveAndEnabled) continue;
                float d = (enemy.transform.position - companionOwner.transform.position).sqrMagnitude;
                if (d < distance) { best = enemy; distance = d; }
            }
            Target = best != null ? best.transform : null; Damageable = best; Rewardable = null;
            return;
        }
        if (IsAlive || Time.time < nextSearch) return;
        nextSearch = Time.time + 1f;
        var player = GameObject.FindWithTag("Player");
        Target = player != null ? player.transform : null;
        Damageable = player != null ? player.GetComponent<IDamageable>() : null;
        Rewardable = player != null ? player.GetComponent<IRewardable>() : null;
    }

    public bool HasLineOfSight() => HasLineOfSightFrom(owner.position + Vector3.up);
    public bool HasClearShot() => HasLineOfSightFrom(owner.position + Vector3.up, true);

    public bool HasLineOfSightFrom(Vector3 origin, bool alliesBlock = false)
    {
        if (!IsAlive) return false;
        Vector3 offset = Target.position + Vector3.up - origin;
        int count = alliesBlock ? Physics.SphereCastNonAlloc(origin, 0.2f, offset.normalized, hits, offset.magnitude, ~0, QueryTriggerInteraction.Ignore) :
            Physics.RaycastNonAlloc(origin, offset.normalized, hits, offset.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;
            if (hit.IsChildOf(owner) || hit.IsChildOf(Target) || (!alliesBlock && hit.GetComponentInParent<IDamageable>() != null)) continue;
            return false;
        }
        return true;
    }
}
