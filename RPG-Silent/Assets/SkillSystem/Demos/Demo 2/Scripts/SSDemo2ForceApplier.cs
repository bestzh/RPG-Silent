using System;
using SkillSystem.Demos.Scripts;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Demos.Demo_2.Scripts
{
    public class SSDemo2ForceApplier : MonoBehaviour
    {
        [Header("Force")]
        [SerializeField] private SSVector3 force;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<Rigidbody>(out var rb))
            {
                ApplyForce(rb);
            }
        }
        
        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.TryGetComponent<Rigidbody>(out var rb))
            {
                ApplyForce(rb);
            }
        }
        private void ApplyForce(Rigidbody rb)
        {
            if (!rb.TryGetComponent<SSDemoHealth>(out var health)) return;
            rb.AddForce(force.Get());
        }
        
    }
}
