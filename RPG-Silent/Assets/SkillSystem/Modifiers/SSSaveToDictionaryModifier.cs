using System;
using SkillSystem.Core;

namespace SkillSystem.Modifiers
{
    [Serializable]
    public class SSSaveToDictionaryModifier : SSModifier
    {
        public string key;
        public override T Apply<T>(T typeToApply)
        {
            SSDictionary.ForceAdd(key, typeToApply);
            return typeToApply;
        }
    }
}