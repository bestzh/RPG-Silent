using System;
using SkillSystem.Core;
using SkillSystem.Enums;
using UnityEngine;

namespace SkillSystem.Structs
{
    [Serializable]
    public struct SSTransform
    {
        public SSVariableTypeWithReference variableType;
        
        public Transform variableReference;
        public string dictionaryName;

        public Transform Get()
        {
            return GetVariable();
        }

        private Transform GetVariable()
        {
            switch (variableType)
            {
                case SSVariableTypeWithReference.ByReference:
                    return GetVariableByReference();
                case SSVariableTypeWithReference.ByDictionaryKeyName:
                    return GetVariableFromDictionary();
                default:
                    return default;
            }
        }


        private Transform GetVariableByReference()
        {
            try
            {
                return variableReference;
            }
            catch
            {
                throw new Exception("[SkillSystem] NullReferenceException on Transform");
            }
        }

        private Transform GetVariableFromDictionary()
        {
            if (SSDictionary.TryGet(dictionaryName, out Transform trans)) return trans;
            if (SSDictionary.TryGet(dictionaryName, out Component comp)) return comp.transform;
            if (SSDictionary.TryGet(dictionaryName, out GameObject go)) return go.transform;
            throw new Exception($"[SkillSystem] Unable to find a valid Transform for key: {dictionaryName}");
        }
    }
}