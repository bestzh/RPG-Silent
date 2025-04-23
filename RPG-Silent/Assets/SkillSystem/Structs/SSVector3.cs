using System;
using SkillSystem.Core;
using SkillSystem.Enums;
using UnityEngine;

namespace SkillSystem.Structs
{
    [Serializable]
    public struct SSVector3
    {
        public SSVariableType variableType;

        public Transform variableReference;
        public Vector3 variableValue;
        public string dictionaryName;
        
        [SerializeReference] public SSModifier[] modifiers;

        public Vector3 Get()
        {
            return GetModifiedVariable(GetVariable()); 
        }
        
        private Vector3 GetVariable()
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

        private Vector3 GetVariableByValue()
        {
            return variableValue;
        }

        private Vector3 GetVariableByReference()
        {
            try
            {
                return variableReference.position;
            }
            catch
            {
                throw new Exception("[SkillSystem] NullReferenceException on Vector3");
            }
        }

        private Vector3 GetVariableFromDictionary()
        {
            if (SSDictionary.TryGet(dictionaryName, out Vector3 vector3)) return vector3;
            if (SSDictionary.TryGet(dictionaryName, out Vector2 vector2)) return vector2;
            if (SSDictionary.TryGet(dictionaryName, out Transform trans)) return trans.position;
            if (SSDictionary.TryGet(dictionaryName, out Component comp)) return comp.transform.position;
            if (SSDictionary.TryGet(dictionaryName, out GameObject go)) return go.transform.position;
            throw new Exception($"[SkillSystem] Unable to find a valid Vector3 for key: {dictionaryName}");
        }

        private Vector3 GetModifiedVariable(Vector3 unmodifiedVariable)
        {
            Vector3 result = unmodifiedVariable;
            foreach (var modifier in modifiers)
            {
                result = modifier.Apply(result);
            }
            return result;
        }
    }
}