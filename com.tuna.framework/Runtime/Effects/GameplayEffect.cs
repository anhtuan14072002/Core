using System;
using UnityEngine.Scripting.APIUpdating;

namespace RogueliteToolkit.Effects
{
    [Serializable]
    [MovedFrom(true, "RogueliteToolkit.Cards", null, "CardEffect")]
    public abstract class GameplayEffect
    {
        public abstract void OnValueChanged(
            GameplayEffectContext context,
            EffectSource source,
            int effectIndex,
            int previousValue,
            int currentValue);

        protected static string CreateSourceId(EffectSource source) => source.ToString();

        protected static string CreateModifierId(EffectSource source, int effectIndex) =>
            source.CreateEffectId(effectIndex);
    }
}