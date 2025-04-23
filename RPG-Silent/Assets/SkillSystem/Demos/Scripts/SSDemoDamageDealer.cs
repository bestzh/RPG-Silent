using UnityEngine;

namespace SkillSystem.Demos.Scripts
{
    public class SSDemoDamageDealer : MonoBehaviour
    {
        [SerializeField] private int damageAmount = 3;
        [SerializeField] private float hitCount = 1;
    
        [SerializeField] private ParticleSystem hitEffect;
    
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out SSDemoHealth health))
            {
                DealDamage(health);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out SSDemoHealth health))
            {
                DealDamage(health);
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.TryGetComponent(out SSDemoHealth health))
            {
                DealDamage(health);
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.TryGetComponent(out SSDemoHealth health))
            {
                DealDamage(health);
            }
        }

        private void DealDamage(SSDemoHealth health)
        {
            if (hitCount <= 0) return;
            health.ChangeHealth(-damageAmount);
            if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
            ReduceHitCount();
        }

        private void ReduceHitCount()
        {
            hitCount--;
            if (hitCount <= 0)
            {
                foreach (Transform child in transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
