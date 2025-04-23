using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSVisualFXFunction : SSFunction
    {
        public enum VfxFunctionType { Play, Pause, Stop  }

        public VfxFunctionType vfxType;
        
        public SSVariableWithReference<ParticleSystem> particleSystem;
        public override void Play()
        {
            switch (vfxType)
            {
                case VfxFunctionType.Play:
                    particleSystem.Get().Play();
                    break;
                case VfxFunctionType.Pause:
                    particleSystem.Get().Pause();
                    break;
                case VfxFunctionType.Stop:
                    particleSystem.Get().Stop();
                    break;
            }
        }
    }
}