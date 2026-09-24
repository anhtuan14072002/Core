using System;
using RogueliteToolkit.Effects;

namespace RogueliteToolkit.SkillTree
{
    [Serializable]
    public abstract class SkillRequirement
    {
        public abstract bool IsMet(
            SkillTreeState state,
            GameplayEffectContext context);
    }
}
