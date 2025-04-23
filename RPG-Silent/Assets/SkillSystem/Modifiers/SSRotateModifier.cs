using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Modifiers
{
    public class SSRotateModifier : SSModifier
    {
        public SSQuaternion rotation;
        public override T Apply<T>(T typeToApply)
        {
            if (typeToApply is Vector3 vector)
            {
                Vector3 result = rotation.Get() * vector;
                return (T)(object)result;
            }

            if (typeToApply is Quaternion quaternion)
            {
                Quaternion result = rotation.Get() * quaternion;
                return (T)(object)result;
            }

            Debug.LogError($"[SkillSystem] Cannot rotate type {typeof(T).Name} with RotateModifier.");
            return typeToApply;
        }
    }
}