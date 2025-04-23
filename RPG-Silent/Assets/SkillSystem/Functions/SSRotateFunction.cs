using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSRotateFunction : SSFunction
    {
        public enum RotateType {SetRotation, AddRotation, SetTorque, AddTorque, SetTorque2D, AddTorque2D}
        public RotateType rotateType;
        
        public SSTransform targetTransform;
        
        public SSVariableWithReference<Rigidbody> targetRigidbody;
        
        public SSVariableWithReference<Rigidbody2D> targetRigidbody2D;
        
        public SSQuaternion rotation;
        
        public SSVector3 vector;
        
        public SSFloat value;
        
        public override void Play()
        {
            switch (rotateType)
            {
                case RotateType.SetRotation:
                    SetRotation();
                    break;
                case RotateType.AddRotation:
                    AddRotation();
                    break;
                case RotateType.SetTorque:
                    SetTorque();
                    break;
                case RotateType.AddTorque:
                    AddTorque();
                    break;
                case RotateType.SetTorque2D:
                    SetTorque2D();
                    break;
                case RotateType.AddTorque2D:
                    AddTorque2D();
                    break;
            }
        }

        private void SetRotation()
        {
            targetTransform.Get().rotation = rotation.Get();
        }

        private void AddRotation()
        {
            targetTransform.Get().rotation *= rotation.Get();
        }

        private void SetTorque()
        {
            targetRigidbody.Get().angularVelocity = vector.Get();
        }

        private void AddTorque()
        {
            targetRigidbody.Get().AddTorque(vector.Get());
        }
        
        private void SetTorque2D()
        {
            targetRigidbody2D.Get().angularVelocity = value.Get();
        }

        private void AddTorque2D()
        {
            targetRigidbody2D.Get().AddTorque(value.Get());
        }
    }
}