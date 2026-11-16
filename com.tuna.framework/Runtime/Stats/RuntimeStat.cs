using System;
using System.Collections.Generic;

namespace RogueliteToolkit.Stats
{
    internal sealed class RuntimeStat
    {
        private readonly Dictionary<string, StatModifier> _modifiers =
            new(StringComparer.Ordinal);
        private float _baseValue;
        private float _cachedValue;
        private bool _dirty = true;

        public StatDefinition Definition { get; }
        public float BaseValue
        {
            get => _baseValue;
            set
            {
                _baseValue = value;
                _dirty = true;
            }
        }

        public RuntimeStat(StatDefinition definition, float baseValue)
        {
            Definition = definition;
            BaseValue = baseValue;
        }

        public void AddOrUpdateModifier(StatModifier modifier)
        {
            _modifiers[modifier.ModifierId] = modifier;
            _dirty = true;
        }

        public bool RemoveModifier(string modifierId)
        {
            if (string.IsNullOrWhiteSpace(modifierId) || !_modifiers.Remove(modifierId))
                return false;
            _dirty = true;
            return true;
        }

        public bool RemoveModifiersFromSource(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || _modifiers.Count == 0)
                return false;

            List<string> removals = null;
            foreach (KeyValuePair<string, StatModifier> pair in _modifiers)
            {
                if (!string.Equals(pair.Value.SourceId, sourceId, StringComparison.Ordinal))
                    continue;
                removals ??= new List<string>();
                removals.Add(pair.Key);
            }

            if (removals == null)
                return false;
            for (int i = 0; i < removals.Count; i++)
                _modifiers.Remove(removals[i]);
            _dirty = true;
            return true;
        }

        public float CalculateValue()
        {
            if (_dirty)
            {
                _cachedValue = CalculateUnclampedValue();
                _dirty = false;
            }
            return Definition.Clamp(_cachedValue);
        }

        private float CalculateUnclampedValue()
        {
            float flat = 0f;
            float percent = 0f;
            float multiplier = 1f;
            bool hasOverride = false;
            StatModifier selectedOverride = default;

            foreach (StatModifier modifier in _modifiers.Values)
            {
                switch (modifier.Operation)
                {
                    case StatModifierOperation.AddFlat:
                        flat += modifier.Value;
                        break;
                    case StatModifierOperation.AddPercent:
                        percent += modifier.Value * 0.01f;
                        break;
                    case StatModifierOperation.Multiply:
                        multiplier *= modifier.Value;
                        break;
                    case StatModifierOperation.Override:
                        if (!hasOverride || IsHigherPriority(modifier, selectedOverride))
                        {
                            selectedOverride = modifier;
                            hasOverride = true;
                        }
                        break;
                }
            }

            return hasOverride
                ? selectedOverride.Value
                : (BaseValue + flat) * (1f + percent) * multiplier;
        }

        private static bool IsHigherPriority(StatModifier candidate, StatModifier current)
        {
            if (candidate.Priority != current.Priority)
                return candidate.Priority > current.Priority;
            return string.CompareOrdinal(candidate.ModifierId, current.ModifierId) > 0;
        }
    }
}
