using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSInstantiateFunction: SSFunction
    {
        public SSVariableWithReference<GameObject> objectToInstantiate;
        
        public SSTransform parentTransform;
        
        public SSVector3 position;
        
        public SSQuaternion rotation;

        public bool saveObjectToDictionary;
        public string dictionaryKeyName;
        
        public override void Play()
        {
            var clone = Object.Instantiate(objectToInstantiate.Get(), position.Get(), rotation.Get(), parentTransform.Get());
            if (saveObjectToDictionary) SSDictionary.ForceAdd(dictionaryKeyName, clone);
        }
    }
}
