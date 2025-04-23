using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSEventFunction : SSFunction
    {
         public SSVariableWithValue<UnityEvent> targetEvent;
        public override void Play()
        {
            targetEvent.Get().Invoke();
        }
    }
}