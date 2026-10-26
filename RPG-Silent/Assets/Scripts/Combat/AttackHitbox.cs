using System.Collections.Generic;
using RPGSilent.Combat;
using RPGSilent.Domain;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackHitbox : MonoBehaviour
{
    [SerializeField] private string hitboxName;
    [SerializeField] private string hitboxGroup;

    private Collider        hitboxCollider;
    private readonly HashSet<AttackContext> attacks = new();
    private bool            isActive;

    public string HitboxName  => string.IsNullOrEmpty(hitboxName) ? name : hitboxName;
    public string HitboxGroup => hitboxGroup;

    private void Awake()
    {
        EnsureCollider();
        DisableHitbox();
    }

    public void EnableHitbox(AttackContext attack)
    {
        if (attack == null || attack.IsClosed) return;
        EnsureCollider();
        attacks.Add(attack);
        isActive  = true;
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        EnsureCollider();
        isActive               = false;
        hitboxCollider.enabled = false;
        attacks.Clear();
    }

    public void DisableAttack(AttackContext attack)
    {
        attacks.Remove(attack);
        if (attacks.Count == 0) DisableHitbox();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;
        foreach (AttackContext context in attacks)
        {
            if (context.Owner == null || context.Profile == null || context.IsClosed) continue;
            if ((context.Profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0) continue;
            context.Owner.ApplyDamage(target, context, other.ClosestPoint(transform.position));
        }
    }

    // 新攻击开始时目标可能已在判定盒里；上下文负责去重，无需等重新进入。
    private void OnTriggerStay(Collider other) => OnTriggerEnter(other);

    private void EnsureCollider()
    {
        if (hitboxCollider != null) return;
        hitboxCollider           = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
    }
}
