using System;
using SkillSystem.Core;
using SkillSystem.Enums;

namespace SkillSystem.Structs
{
    [Serializable]
    public struct SSVariableWithValue<T>
    {
        public SSVariableTypeWithValue variableType;
        
        public T variableValue;
        public string dictionaryName;

        public T Get()
        {
            return GetVariable();
        }

        private T GetVariable()
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


        private T GetVariableByValue()
        {
            return variableValue;
        }

        private T GetVariableFromDictionary()
        {
            try
            {
                return SSDictionary.Get<T>(dictionaryName);
            }
            catch
            {
                throw new Exception($"[SkillSystem] Key '{dictionaryName}' does not correspond to a valid variable type of {typeof(T).Name}.");
            }
        }
    }
}