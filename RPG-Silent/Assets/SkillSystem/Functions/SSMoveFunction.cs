using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSMoveFunction : SSFunction
    {
        public enum MoveType { SetPosition, AddPosition, SetVelocity, AddForce, SetVelocity2D, AddForce2D  }
        public MoveType moveType;
        
        public SSTransform targetTransform;
        
        public SSVariableWithReference<Rigidbody> targetRigidbody;
        
        public SSVariableWithReference<Rigidbody2D> targetRigidbody2D;
        
        public SSVector3 vector;
        
        public override void Play()
        {
            switch (moveType)
            {
                case MoveType.SetPosition:
                    SetPosition();
                    break;
                case MoveType.AddPosition:
                    AddPosition();
                    break;
                case MoveType.SetVelocity:
                    SetVelocity();
                    break;
                case MoveType.AddForce:
                    AddForce();
                    break;
                case MoveType.SetVelocity2D:
                    SetVelocity2D();
                    break;
                case MoveType.AddForce2D:
                    AddForce2D();
                    break;
            }
        }

        private void SetPosition()
        {
            targetTransform.Get().position = vector.Get();
        }

        private void AddPosition()
        {
            targetTransform.Get().position += vector.Get();
        }

        private void SetVelocity()
        { 
            targetRigidbody.Get().linearVelocity = vector.Get();
        }
        
        private void AddForce()
        { 
            targetRigidbody.Get().AddForce(vector.Get());
        }
        
        private void SetVelocity2D()
        { 
            targetRigidbody2D.Get().linearVelocity = vector.Get();
        }
        
        private void AddForce2D()
        { 
            targetRigidbody2D.Get().AddForce(vector.Get());
        }
        
    }
}
