using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSDestroyFunction : SSFunction
    {
        public SSVariableWithReference<GameObject> targetObject;

        public SSFloat destroyDelay;
        public override void Play()
        {
            Object.Destroy(targetObject.Get(), destroyDelay.Get());
        }
    }
}