using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private string hitboxName;
    [SerializeField] private string hitboxGroup;

    private readonly HashSet<EnemyController> hitEnemies = new HashSet<EnemyController>();
    private Collider hitboxCollider;
    private AttackExecutor owner;
    private AttackProfile profile;
    private bool isActive;

    public string HitboxName => string.IsNullOrEmpty(hitboxName) ? name : hitboxName;
    public string HitboxGroup => hitboxGroup;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    public void EnableHitbox(AttackProfile attackProfile, AttackExecutor attackOwner)
    {
        profile = attackProfile;
        owner = attackOwner;
        isActive = true;
        hitEnemies.Clear();
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        isActive = false;
        hitboxCollider.enabled = false;
        hitEnemies.Clear();
        profile = null;
        owner = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive || profile == null || owner == null)
        {
            return;
        }

        if ((profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        EnemyController enemy = other.GetComponentInParent<EnemyController>();
        if (enemy == null || hitEnemies.Contains(enemy))
        {
            return;
        }

        hitEnemies.Add(enemy);
        owner.ApplyDamage(enemy, profile, other.ClosestPoint(transform.position));
    }
}
