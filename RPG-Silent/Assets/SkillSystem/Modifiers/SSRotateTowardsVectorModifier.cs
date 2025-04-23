using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSRotateTowardsVectorModifier : SSModifier
    {
        public SSVector3 vectorForward;
        public SSVector3 vectorUp;
        public override T Apply<T>(T typeToApply)
        {
            if (typeToApply is Quaternion quaternion)
            {
                Quaternion result = quaternion * Quaternion.LookRotation(vectorForward.Get(), vectorUp.Get());
                return (T)(object)result;
            }
            
            Debug.LogError($"[SkillSystem] Cannot rotate type {typeof(T).Name} with RotateTowardsVectorModifier.");
            
            return typeToApply;
        }
    }
}