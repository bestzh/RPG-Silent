using RPGSilent.Domain;
using UnityEngine;

public sealed class EnemyPerception
{
    private readonly Transform owner;
    private readonly RaycastHit[] hits = new RaycastHit[24];
    private float nextSearch;
    public Transform Target { get; private set; }
    public IDamageable Damageable { get; private set; }
    public IRewardable Rewardable { get; private set; }
    public bool IsAlive => Target != null && Target.gameObject.activeInHierarchy && Damageable != null && !Damageable.IsDead;
    public float Distance => IsAlive ? Vector3.Distance(owner.position, Target.position) : float.PositiveInfinity;

    public EnemyPerception(Transform owner) => this.owner = owner;

    public void Refresh()
    {
        if (IsAlive || Time.time < nextSearch) return;
        nextSearch = Time.time + 1f;
        var player = GameObject.FindWithTag("Player");
        Target = player != null ? player.transform : null;
        Damageable = player != null ? player.GetComponent<IDamageable>() : null;
        Rewardable = player != null ? player.GetComponent<IRewardable>() : null;
    }

    public bool HasLineOfSight()
    {
        if (!IsAlive) return false;
        Vector3 origin = owner.position + Vector3.up;
        Vector3 offset = Target.position + Vector3.up - origin;
        int count = Physics.RaycastNonAlloc(origin, offset.normalized, hits, offset.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;
            if (hit.IsChildOf(owner) || hit.IsChildOf(Target) || hit.GetComponentInParent<IDamageable>() != null) continue;
            return false;
        }
        return true;
    }
}
