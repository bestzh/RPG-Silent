using UnityEngine;

namespace SkillSystem.Demos.Demo_2.Scripts
{
    public class SSDemo2SmoothCamera : MonoBehaviour
    {
        public Transform target;
        public float smoothSpeed;
        public Vector3 offset;
        
        private Vector3 _velocity = Vector3.zero;

        void FixedUpdate()
        {
            if (target == null) return;
            
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, 1f / smoothSpeed);
        }
    }
}
