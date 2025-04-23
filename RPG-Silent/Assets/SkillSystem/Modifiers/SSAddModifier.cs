using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSAddModifier : SSModifier
    {
        public SSFloat value;
        public override T Apply<T>(T typeToApply)
        {
            if (typeToApply is IConvertible convertible)
            {
                try
                {
                    double result = convertible.ToDouble(null);
                    result += value.Get();
                    return (T)(object)result;
                }
                catch
                {
                    Debug.LogError($"[SkillSystem] Cannot add type {typeof(T).Name} with AddModifier.");
                }
            }
            return typeToApply;
        }
    }
}