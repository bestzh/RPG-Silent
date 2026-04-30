using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AttackArea : MonoBehaviour
{
    private readonly HashSet<EnemyController> targets = new HashSet<EnemyController>();
    private AttackProfile profile;
    private AttackExecutor owner;
    private float duration;
    private float tickInterval;

    private void Awake()
    {
        Collider areaCollider = GetComponent<Collider>();
        areaCollider.isTrigger = true;
    }

    public void Initialize(AttackProfile attackProfile, AttackExecutor attackOwner)
    {
        profile = attackProfile;
        owner = attackOwner;
        duration = profile != null ? Mathf.Max(0f, profile.AreaDuration) : 0f;
        tickInterval = profile != null ? Mathf.Max(0.05f, profile.TickInterval) : 1f;

        StartCoroutine(TickDamage());
        Destroy(gameObject, duration);
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyController enemy = ResolveEnemy(other);
        if (enemy != null)
        {
            targets.Add(enemy);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyController enemy = ResolveEnemy(other);
        if (enemy != null)
        {
            targets.Remove(enemy);
        }
    }

    private IEnumerator TickDamage()
    {
        while (profile != null && owner != null)
        {
            foreach (EnemyController enemy in targets)
            {
                if (enemy != null)
                {
                    owner.ApplyDamage(enemy, profile, enemy.transform.position, allowRepeatedDamage: true);
                }
            }

            yield return new WaitForSeconds(tickInterval);
        }
    }

    private EnemyController ResolveEnemy(Collider other)
    {
        if (profile == null || (profile.TargetLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return null;
        }

        return other.GetComponentInParent<EnemyController>();
    }
}
