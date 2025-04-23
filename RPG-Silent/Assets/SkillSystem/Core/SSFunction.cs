using System;
using UnityEngine;

namespace SkillSystem.Core
{
    [Serializable]
    public abstract class SSFunction
    {
        public float delay;
        public abstract void Play();
    }
}