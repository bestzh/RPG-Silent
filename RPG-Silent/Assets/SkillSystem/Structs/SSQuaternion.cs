using System;
using SkillSystem.Core;
using SkillSystem.Enums;
using UnityEngine;

namespace SkillSystem.Structs
{
    [Serializable]
    public struct SSQuaternion
    {
        public SSVariableType variableType;

        public Transform variableReference;
        public Quaternion variableValue;
        public string dictionaryName;
        
        [SerializeReference] public SSModifier[] modifiers;

        public Quaternion Get()
        {
            return GetModifiedVariable(GetVariable());
        }

        private Quaternion GetVariable()
        {
            switch (variableType)
            {
                case SSVariableType.ByValue:
                    return GetVariableByValue();
                case SSVariableType.ByReference:
                    return GetVariableByReference();
                case SSVariableType.ByDictionaryKeyName:
                    return GetVariableFromDictionary();
                default:
                    return default;
            }
        }

        private Quaternion GetVariableByValue()
        {
            return variableValue;
        }

        private Quaternion GetVariableByReference()
        {
            try
            {
                return variableReference.rotation;
            }
            catch
            {
                throw new Exception("[SkillSystem] NullReferenceException on Quaternion");
            }
        }

        private Quaternion GetVariableFromDictionary()
        {
            if (SSDictionary.TryGet(dictionaryName, out Quaternion quat)) return quat;
            if (SSDictionary.TryGet(dictionaryName, out Transform trans)) return trans.rotation;
            if (SSDictionary.TryGet(dictionaryName, out Component comp)) return comp.transform.rotation;
            if (SSDictionary.TryGet(dictionaryName, out GameObject go)) return go.transform.rotation;
            throw new Exception($"[SkillSystem] Unable to find a valid Quaternion for key: {dictionaryName}");
        }
        
        private Quaternion GetModifiedVariable(Quaternion unmodifiedVariable)
        {
            Quaternion result = unmodifiedVariable;
            foreach (var modifier in modifiers)
            {
                result = modifier.Apply(result);
            }
            return result;
        }
    }
}