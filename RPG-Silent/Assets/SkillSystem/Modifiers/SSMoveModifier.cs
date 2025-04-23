using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSMoveModifier : SSModifier
    {
        public SSVector3 vector;
        public override T Apply<T>(T typeToApply)
        {
            if (typeToApply is Vector3 vector)
            {
                Vector3 result = vector + this.vector.Get();
                return (T)(object)result;
            }

            Debug.LogError($"[SkillSystem] Cannot move type {typeof(T).Name} with MoveModifier.");
            return typeToApply;
        }
    }
}