using System;

namespace SkillSystem.Core
{
    [Serializable]
    public abstract class SSModifier
    {
        public abstract T Apply<T>(T typeToApply);
    }
}