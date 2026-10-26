using RPGSilent.Combat;
using RPGSilent.Domain;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackProjectile : MonoBehaviour
{
    private AttackContext context;
    private AttackProfile profile => context?.Profile;
    private AttackExecutor owner => context?.Owner;
    private bool hasHit;
    private Vector3        direction;
    private float          lifeTimer;
    private readonly RaycastHit[] sweepHits = new RaycastHit[24];
    private float radius = 0.12f;

    public void Initialize(AttackContext attack, Vector3 fireDirection)
    {
        context = attack;
        hasHit = false;
        direction = fireDirection.normalized;
        lifeTimer = profile != null ? profile.ProjectileLifeTime : 5f;
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null) radius = Mathf.Max(0.03f, sphere.radius * transform.lossyScale.x);
    }

    private void Update()
    {
        if (profile == null || owner == null || !owner.IsContextValid(context)) { Destroy(gameObject); return; }

        float step = profile.ProjectileSpeed * Time.deltaTime;
        // 扫掠覆盖两帧之间的路程，避免低帧率下高速弹体穿过目标。
        int count = Physics.SphereCastNonAlloc(transform.position, radius, direction, sweepHits,
            step, ~0, QueryTriggerInteraction.Ignore);
        int nearest = -1;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider col = sweepHits[i].collider;
            if (col.transform.IsChildOf(transform) || col.transform.IsChildOf(owner.transform)) continue;
            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null && (!owner.CanDamage(target) ||
                (profile.TargetLayers.value & (1 << col.gameObject.layer)) == 0)) continue;
            if (sweepHits[i].distance < distance) { distance = sweepHits[i].distance; nearest = i; }
        }
        if (nearest >= 0)
        {
            var hit = sweepHits[nearest];
            var target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null) owner.ApplyDamage(target, context, hit.point);
            hasHit = true;
            Destroy(gameObject);
            return;
        }
        transform.position += direction * step;
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || profile == null || owner == null) return;

        if ((profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;

        if (!owner.ApplyDamage(target, context, other.ClosestPoint(transform.position))) return;
        hasHit = true; // Destroy 延迟到帧末，复合碰撞体回调不能再次命中。
        Destroy(gameObject);
    }
}
