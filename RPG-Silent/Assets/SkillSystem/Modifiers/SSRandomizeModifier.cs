using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSRandomizeModifier : SSModifier
    {
        public SSFloat rangeMin, rangeMax;
        

        public override T Apply<T>(T typeToApply)
        {
            if (typeToApply is Vector3 vector)
            {
                Vector3 result = new Vector3(vector.x * GetRandomMultiplier(), vector.y * GetRandomMultiplier(), vector.z * GetRandomMultiplier());
                return (T)(object)result;
            }

            if (typeToApply is Quaternion quaternion)
            {
                Quaternion result = Quaternion.Euler(quaternion.eulerAngles.x + GetRandomMultiplier(), quaternion.eulerAngles.y + GetRandomMultiplier(), quaternion.eulerAngles.z + GetRandomMultiplier());
                return (T)(object)result;
            }
            
            if (typeToApply is IConvertible convertible)
            {
                try
                {
                    double result = convertible.ToDouble(null);
                    result *= GetRandomMultiplier();
                    return (T)Convert.ChangeType(result, typeof(T));
                }
                catch
                {
                    Debug.LogError($"[SkillSystem] Cannot randomize type {typeof(T).Name} with RandomizeModifier.");
                }
            }
            return typeToApply;
        }
        private float GetRandomMultiplier() => Random.Range(rangeMin.Get(), rangeMax.Get());
    }
}