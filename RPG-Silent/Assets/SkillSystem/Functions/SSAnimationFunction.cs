using System;
using SkillSystem.Core;
using SkillSystem.Structs;
using UnityEngine;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSAnimationFunction : SSFunction
    {
        public SSVariableWithReference<Animator> targetAnimator;
        
        public enum AnimateType { Play, SetTrigger, SetBool, SetInteger, SetFloat, SetSpeed}
        
        public AnimateType animateType = AnimateType.Play;
        public SSVariableWithValue<string> parameterName;
        
        // Play
        public SSFloat crossFadeTime;
        
        public SSVariableWithValue<bool> triggerValue;
        public SSVariableWithValue<bool> boolValue;
        public SSVariableWithValue<int> integerValue;
        public SSVariableWithValue<float> floatValue;
        public SSFloat speedValue;
        
        public override void Play()
        {
            switch (animateType)
            {
                case AnimateType.Play:
                    PlayAnimation();
                    break;
                case AnimateType.SetTrigger:
                    SetTrigger();
                    break;
                case AnimateType.SetBool:
                    SetBool();
                    break;
                case AnimateType.SetInteger:
                    SetInteger();
                    break;
                case AnimateType.SetFloat:
                    SetFloat();
                    break;
                case AnimateType.SetSpeed:
                    SetSpeed();
                    break;
            }
        }

        private void PlayAnimation()
        {
            if (crossFadeTime.Get() <= 0) targetAnimator.Get().Play(parameterName.Get());
            else targetAnimator.Get().CrossFade(parameterName.Get(), crossFadeTime.Get());
        }

        private void SetTrigger()
        {
            if (triggerValue.Get()) targetAnimator.Get().SetTrigger(parameterName.Get());
            else targetAnimator.Get().ResetTrigger(parameterName.Get());
        }

        private void SetBool()
        {
            targetAnimator.Get().SetBool(parameterName.Get(), boolValue.Get());
        }

        private void SetInteger()
        {
            targetAnimator.Get().SetInteger(parameterName.Get(), integerValue.Get());
        }

        private void SetFloat()
        {
            targetAnimator.Get().SetFloat(parameterName.Get(), floatValue.Get());
        }
        private void SetSpeed()
        {
            targetAnimator.Get().speed = speedValue.Get();
        }
    }
}
