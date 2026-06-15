using RPGSilent.Domain;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackProjectile : MonoBehaviour
{
    private AttackProfile  profile;
    private AttackExecutor owner;
    private Vector3        direction;
    private float          lifeTimer;

    public void Initialize(AttackProfile attackProfile, AttackExecutor attackOwner, Vector3 fireDirection)
    {
        profile   = attackProfile;
        owner     = attackOwner;
        direction = fireDirection.normalized;
        lifeTimer = profile != null ? profile.ProjectileLifeTime : 5f;
    }

    private void Update()
    {
        if (profile == null) { Destroy(gameObject); return; }

        transform.position += direction * profile.ProjectileSpeed * Time.deltaTime;
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (profile == null || owner == null) return;

        if ((profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;

        owner.ApplyDamage(target, profile, other.ClosestPoint(transform.position));
        Destroy(gameObject);
    }
}
