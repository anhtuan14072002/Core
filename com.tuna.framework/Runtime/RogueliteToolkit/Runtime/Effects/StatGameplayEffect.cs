using System;
using RogueliteToolkit.Stats;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace RogueliteToolkit.Effects
{
    [Serializable]
    [MovedFrom(true, "RogueliteToolkit.Cards", null, "StatCardEffect")]
    public sealed class StatGameplayEffect : GameplayEffect
    {
        [SerializeField] private StatDefinition _stat;
        [SerializeField] private StatModifierOperation _operation;
        [SerializeField] private float _valuePerStack;
        [SerializeField] private float[] _gainsPerLevel = Array.Empty<float>();
        [SerializeField] private int _priority;

        public StatDefinition Stat => _stat;
        public StatModifierOperation Operation => _operation;
        public float ValuePerStack => _valuePerStack;

        public float GetValueAtLevel(int level)
        {
            if (level <= 0)
                return 0f;
            if (_gainsPerLevel == null || _gainsPerLevel.Length == 0)
                return _valuePerStack * level;
            float value = 0f;
            int configuredLevels = Math.Min(level, _gainsPerLevel.Length);
            for (int i = 0; i < configuredLevels; i++)
                value += _gainsPerLevel[i];
            value += (level - configuredLevels) * _valuePerStack;
            return value;
        }

        public override void OnValueChanged(
            GameplayEffectContext context,
            EffectSource source,
            int effectIndex,
            int previousValue,
            int currentValue)
        {
            if (_stat == null)
                throw new InvalidOperationException(
                    $"Effect '{source}' has no StatDefinition.");

            StatSet statSet = context.GetRequired<StatSet>();
            string modifierId = CreateModifierId(source, effectIndex);
            if (currentValue <= 0)
            {
                statSet.RemoveModifier(_stat, modifierId);
                return;
            }

            statSet.AddOrUpdateModifier(
                _stat,
                new StatModifier(
                    modifierId,
                    CreateSourceId(source),
                    _operation,
                    GetValueAtLevel(currentValue),
                    _priority));
        }
    }
}
