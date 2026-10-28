using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueliteToolkit.Stats
{
    [DisallowMultipleComponent]
    public sealed class StatSet : MonoBehaviour
    {
        [SerializeField] private StatBaseValue[] _baseValues = Array.Empty<StatBaseValue>();

        private readonly Dictionary<StatDefinition, RuntimeStat> _stats = new();
        private bool _initialized;

        public event Action<StatChange> StatChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Rebuild()
        {
            _stats.Clear();
            StatBaseValue[] baseValues = _baseValues ?? Array.Empty<StatBaseValue>();
            for (int i = 0; i < baseValues.Length; i++)
            {
                StatBaseValue entry = baseValues[i];
                if (entry?.Definition == null)
                    continue;
                if (!entry.Definition.HasValidId)
                    Debug.LogWarning($"Stat '{entry.Definition.name}' has no stable ID.", entry.Definition);
                if (_stats.ContainsKey(entry.Definition))
                {
                    Debug.LogWarning($"Duplicate stat '{entry.Definition.name}' on {name}.", this);
                    continue;
                }
                _stats.Add(entry.Definition, new RuntimeStat(entry.Definition, entry.Value));
            }
            _initialized = true;
        }

        public float GetValue(StatDefinition definition)
        {
            return GetOrCreate(definition).CalculateValue();
        }

        public float GetBaseValue(StatDefinition definition)
        {
            return GetOrCreate(definition).BaseValue;
        }

        public void SetBaseValue(StatDefinition definition, float value)
        {
            RuntimeStat stat = GetOrCreate(definition);
            float previous = stat.CalculateValue();
            stat.BaseValue = value;
            NotifyIfChanged(stat, previous);
        }

        public void AddOrUpdateModifier(StatDefinition definition, StatModifier modifier)
        {
            RuntimeStat stat = GetOrCreate(definition);
            float previous = stat.CalculateValue();
            stat.AddOrUpdateModifier(modifier);
            NotifyIfChanged(stat, previous);
        }

        public bool RemoveModifier(StatDefinition definition, string modifierId)
        {
            RuntimeStat stat = GetOrCreate(definition);
            float previous = stat.CalculateValue();
            if (!stat.RemoveModifier(modifierId))
                return false;
            NotifyIfChanged(stat, previous);
            return true;
        }

        public int RemoveModifiersFromSource(string sourceId)
        {
            EnsureInitialized();
            int changedStats = 0;
            foreach (RuntimeStat stat in _stats.Values)
            {
                float previous = stat.CalculateValue();
                if (!stat.RemoveModifiersFromSource(sourceId))
                    continue;
                NotifyIfChanged(stat, previous);
                changedStats++;
            }
            return changedStats;
        }

        private RuntimeStat GetOrCreate(StatDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            EnsureInitialized();
            if (_stats.TryGetValue(definition, out RuntimeStat stat))
                return stat;

            stat = new RuntimeStat(definition, definition.DefaultBaseValue);
            _stats.Add(definition, stat);
            return stat;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
                Rebuild();
        }

        private void NotifyIfChanged(RuntimeStat stat, float previous)
        {
            float current = stat.CalculateValue();
            if (Mathf.Approximately(previous, current))
                return;
            StatChanged?.Invoke(new StatChange(stat.Definition, previous, current));
        }
    }
}
