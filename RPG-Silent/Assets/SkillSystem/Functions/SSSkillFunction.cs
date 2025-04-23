using System;
using SkillSystem.Core;
using SkillSystem.Structs;

namespace SkillSystem.Functions
{
    [Serializable]
    public class SSSkillFunction : SSFunction
    {
        public SSVariableWithReference<SSSkill> targetSkill;
        public override void Play()
        {
            targetSkill.Get().PlayNextAction();
        }
    }
}