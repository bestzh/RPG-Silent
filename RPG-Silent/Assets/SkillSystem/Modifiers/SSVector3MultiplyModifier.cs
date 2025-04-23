using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSVector3MultiplyModifier : SSModifier
    {
        public SSVector3 multiplier;
        public override T Apply<T>(T typeToApply)
        {
            Vector3 mult = multiplier.Get();
            if (typeToApply is Vector3 vector)
            {
                Vector3 result = new Vector3(vector.x * mult.x, vector.y * mult.y, vector.z * mult.z);
                return (T)(object)result;
            }

            if (typeToApply is Quaternion quaternion)
            {
                Quaternion result = Quaternion.Euler(new Vector3(quaternion.x * mult.x, quaternion.y * mult.y, quaternion.z * mult.z));
                return (T)(object)result;
            }
            
            return typeToApply;
        }
    }
}