using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SkillSystem.Demos.Scripts
{
    public class SSDemoHealth : MonoBehaviour
    {
        [SerializeField] [Min(1)] private int maxHealth = 100;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Image healthFillImage;
    
        private int _currentHealth = 1;
        
        private Coroutine _damageRoutine;

        private void Start()
        {
            _currentHealth = maxHealth;
        }

        public void ChangeHealth(int health)
        {
            _currentHealth += health;
            _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);
            healthFillImage.fillAmount = (float)_currentHealth / maxHealth;
            
            if (_damageRoutine != null) StopCoroutine(_damageRoutine);
            _damageRoutine = StartCoroutine(DamageRoutine());
            
            
            if (_currentHealth <= 0) Die();
        }

        private IEnumerator DamageRoutine()
        {
            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                yield return new WaitForEndOfFrame();
                timer += Time.deltaTime;
        
                float t = timer / duration;
                if (sprite != null) sprite.color = Color.Lerp(Color.red, Color.white, t);
            }

            if (sprite != null) sprite.color = Color.white;
        }

        private void Die()
        {
            Instantiate(gameObject, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
