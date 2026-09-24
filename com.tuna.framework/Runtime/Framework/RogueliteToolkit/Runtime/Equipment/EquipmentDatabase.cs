using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    [Serializable]
    public sealed class EquipmentRarityRate
    {
        [SerializeField] private EquipmentRarity _rarity;
        [SerializeField, Min(0f)] private float _weight;

        public EquipmentRarity Rarity => _rarity;
        public float Weight => _weight;

        public EquipmentRarityRate(EquipmentRarity rarity, float weight)
        {
            _rarity = rarity;
            _weight = weight;
        }
    }

    [CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Roguelite Toolkit/Equipment/Database")]
    public sealed class EquipmentDatabase : ScriptableObject
    {
        [SerializeField] private EquipmentDefinition[] _items = Array.Empty<EquipmentDefinition>();
        [Tooltip("Relative spawn weights. 0 disables a rarity. Rarities without items are excluded; remaining weights are normalized.")]
        [SerializeField] private EquipmentRarityRate[] _spawnRates =
        {
            new(EquipmentRarity.Common, 0f),
            new(EquipmentRarity.Uncommon, 0f),
            new(EquipmentRarity.Rare, 70f),
            new(EquipmentRarity.Epic, 25f),
            new(EquipmentRarity.Legendary, 4f),
            new(EquipmentRarity.Ultimate, 1f)
        };
        private Dictionary<string, EquipmentDefinition> _lookup;
        private readonly Dictionary<EquipmentRarity, List<EquipmentDefinition>> _spawnPools = new();

        public IReadOnlyList<EquipmentDefinition> Items => _items;
        public IReadOnlyList<EquipmentRarityRate> SpawnRates => _spawnRates;
        public void InvalidateLookup() => _lookup = null;

        public bool TryGet(string id, out EquipmentDefinition definition)
        {
            EnsureLookup();
            return _lookup.TryGetValue(id ?? string.Empty, out definition);
        }

        public bool TryRoll(out EquipmentDefinition definition)
        {
            EnsureLookup();
            definition = null;
            double total = 0;
            for (int i = 0; i < _spawnRates.Length; i++)
                if (_spawnRates[i].Weight > 0f && _spawnPools.ContainsKey(_spawnRates[i].Rarity))
                    total += _spawnRates[i].Weight;
            if (total <= 0)
                return false;

            double roll = UnityEngine.Random.value * total;
            List<EquipmentDefinition> selected = null;
            for (int i = 0; i < _spawnRates.Length; i++)
            {
                EquipmentRarityRate rate = _spawnRates[i];
                if (rate.Weight <= 0f || !_spawnPools.TryGetValue(rate.Rarity, out var pool))
                    continue;
                selected = pool;
                roll -= rate.Weight;
                if (roll < 0)
                    break;
            }
            // Random.value can be 1; in that case the last eligible rarity owns the endpoint.
            definition = selected[UnityEngine.Random.Range(0, selected.Count)];
            return true;
        }

        private void EnsureLookup()
        {
            if (_lookup == null)
            {
                HashSet<EquipmentRarity> rarities = new();
                for (int i = 0; i < _spawnRates.Length; i++)
                {
                    EquipmentRarityRate rate = _spawnRates[i];
                    if (!rarities.Add(rate.Rarity) || !Enum.IsDefined(typeof(EquipmentRarity), rate.Rarity) ||
                        rate.Weight < 0f || float.IsNaN(rate.Weight) || float.IsInfinity(rate.Weight))
                        throw new InvalidOperationException("Equipment spawn rates require unique rarities and finite non-negative weights.");
                }
                _spawnPools.Clear();
                Dictionary<string, EquipmentDefinition> lookup = new(StringComparer.Ordinal);
                for (int i = 0; i < _items.Length; i++)
                {
                    EquipmentDefinition item = _items[i];
                    if (item == null)
                        throw new InvalidOperationException("Equipment database contains an empty entry.");
                    while (item != null)
                    {
                        item.Validate();
                        if (lookup.TryGetValue(item.Id, out EquipmentDefinition existing))
                        {
                            if (existing != item)
                                throw new InvalidOperationException($"Duplicate equipment ID '{item.Id}'.");
                            break;
                        }
                        lookup.Add(item.Id, item);
                        if (!_spawnPools.TryGetValue(item.Rarity, out var pool))
                        {
                            pool = new List<EquipmentDefinition>();
                            _spawnPools.Add(item.Rarity, pool);
                        }
                        pool.Add(item);
                        // Sheet IDs replace the demo IDs; old inventory saves still resolve.
                        if (!string.IsNullOrEmpty(item.LegacyId) && item.LegacyId != item.Id &&
                            !lookup.TryAdd(item.LegacyId, item))
                            throw new InvalidOperationException($"Duplicate legacy equipment ID '{item.LegacyId}'.");
                        item = item.MergeResult;
                    }
                }
                _lookup = lookup;
            }
        }

        private void OnEnable() => _lookup = null;
        private void OnValidate() => _lookup = null;
    }
}
