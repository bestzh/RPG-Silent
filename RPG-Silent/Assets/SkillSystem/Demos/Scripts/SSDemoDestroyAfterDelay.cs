using UnityEngine;

namespace SkillSystem.Demos.Scripts
{
    public class SSDemoDestroyAfterDelay : MonoBehaviour
    {
        [SerializeField] private float delay;

        private void Start()
        {
            Destroy(gameObject, delay);
        }
    }
}
