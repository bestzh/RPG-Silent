using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSSoundFXFunction : SSFunction
    {
        public enum SfxFunctionType { Play, Pause, Stop  }

        public SfxFunctionType sfxType;
        
        public SSVariableWithReference<AudioSource> audioSource;

        public override void Play()
        {
            switch (sfxType)
            {
                case SfxFunctionType.Play:
                    audioSource.Get().Play();
                    break;
                case SfxFunctionType.Pause:
                    audioSource.Get().Pause();
                    break;
                case SfxFunctionType.Stop:
                    audioSource.Get().Stop();
                    break;
            }
        }
    }
}