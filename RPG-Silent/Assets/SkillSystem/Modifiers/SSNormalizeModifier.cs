using System;
using SkillSystem.Core;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSNormalizeModifier : SSModifier
    {
        public override T Apply<T>(T typeToApply)
        {
            if (typeToApply is Vector3 vector)
            {
                Vector3 result = vector.normalized;
                return (T)(object)result;
            }
            
            if (typeToApply is Quaternion quaternion)
            {
                Quaternion result = quaternion.normalized;
                return (T)(object)result;
            }
            if (typeToApply is float num)
            {
                float result = num / Mathf.Abs(num);
                return (T)(object)result;
            }

            Debug.LogError($"[SkillSystem] Cannot normalize type {typeof(T).Name} with NormalizeModifier.");
            return typeToApply;
        }
    }
}