using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSMultiplyModifier : SSModifier
    {
        public SSFloat multiplier;
        public override T Apply<T>(T typeToApply)
        {
            if (typeToApply is Vector3 vector)
            {
                Vector3 result = vector * multiplier.Get();
                return (T)(object)result;
            }

            if (typeToApply is Quaternion quaternion)
            {
                Quaternion result = Quaternion.Euler(quaternion.eulerAngles * multiplier.Get());
                return (T)(object)result;
            }
            
            if (typeToApply is IConvertible convertible)
            {
                try
                {
                    double result = convertible.ToDouble(null);
                    result *= multiplier.Get();
                    return (T)Convert.ChangeType(result, typeof(T));
                }
                catch
                {
                    Debug.LogError($"[SkillSystem] Cannot multiply type {typeof(T).Name} with MultiplyModifier.");
                }
            }
            
            return typeToApply;
        }
    }
}