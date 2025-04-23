using System;
using SkillSystem.Core;
using SkillSystem.Enums;
using UnityEngine;

namespace SkillSystem.Structs
{
    [Serializable]
    public struct SSFloat
    {
        public SSVariableTypeWithValue variableType;
        
        public float variableValue;
        public string dictionaryName;
        
        [SerializeReference] public SSModifier[] modifiers;
        
        public float Get()
        {
            return GetModifiedVariable(GetVariable()); 
        }
        
        private float GetVariable()
        {
            switch (variableType)
            {
                case SSVariableTypeWithValue.ByValue:
                    return GetVariableByValue();
                case SSVariableTypeWithValue.ByDictionaryKeyName:
                    return GetVariableFromDictionary();
                default:
                    return default;
            }
        }

        private float GetVariableByValue()
        {
            return variableValue;
        }

        private float GetVariableFromDictionary()
        {
            return SSDictionary.Get<float>(dictionaryName);
        }

        private float GetModifiedVariable(float unmodifiedVariable)
        {
            float result = unmodifiedVariable;
            foreach (var modifier in modifiers)
            {
                result = modifier.Apply(result);
            }
            return result;
        }
    }
}