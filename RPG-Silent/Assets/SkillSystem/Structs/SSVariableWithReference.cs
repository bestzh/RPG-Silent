using System;
using SkillSystem.Core;
using SkillSystem.Enums;
using UnityEngine;

namespace SkillSystem.Structs
{
    [Serializable]
    public struct SSVariableWithReference<T>
    {
        public SSVariableTypeWithReference variableType;
        
        public T variableReference;
        public string dictionaryName;

        public T Get()
        {
            return GetVariable();
        }

        private T GetVariable()
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


        private T GetVariableByReference()
        {
            try
            {
                return variableReference;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }

        private T GetVariableFromDictionary()
        {
            try
            {
                return SSDictionary.Get<T>(dictionaryName);
            }
            catch
            {
                var obj = SSDictionary.Get<object>(dictionaryName);
                
                if (obj is T directCast)
                {
                    return directCast;
                }
                
                if (typeof(T) == typeof(GameObject))
                {
                    if (obj is GameObject go)
                    {
                        return (T)(object)go;
                    }
                    if (obj is Component comp)
                    {
                        return (T)(object)comp.gameObject;
                    }
                }
                
                if (obj is GameObject gameObject)
                {
                    if (gameObject.TryGetComponent(out T comp))
                    {
                        return comp;
                    }
                    return gameObject.GetComponentInChildren<T>();
                }
                if (obj is Component component)
                {
                    if (component.TryGetComponent(out T com))
                    {
                        return com;
                    }
                    return component.GetComponentInChildren<T>();
                }

                throw new Exception($"[SkillSystem] Key '{dictionaryName}' does not correspond to a valid GameObject, Component, or does not support GetComponentInChildren.");
            }
        }
    }
}
