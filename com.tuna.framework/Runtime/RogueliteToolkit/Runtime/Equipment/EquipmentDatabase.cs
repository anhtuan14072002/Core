using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    [CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "Roguelite Toolkit/Equipment/Database")]
    public sealed class EquipmentDatabase : ScriptableObject
    {
        [SerializeField] private EquipmentDefinition[] _items = Array.Empty<EquipmentDefinition>();
        private Dictionary<string, EquipmentDefinition> _lookup;

        public IReadOnlyList<EquipmentDefinition> Items => _items;

        public bool TryGet(string id, out EquipmentDefinition definition)
        {
            if (_lookup == null)
            {
                Dictionary<string, EquipmentDefinition> lookup = new(StringComparer.Ordinal);
                for (int i = 0; i < _items.Length; i++)
                {
                    EquipmentDefinition item = _items[i];
                    if (item == null)
                        throw new InvalidOperationException("Equipment database contains an empty entry.");
                    item.Validate();
                    if (!lookup.TryAdd(item.Id, item))
                        throw new InvalidOperationException($"Duplicate equipment ID '{item.Id}'.");
                }
                _lookup = lookup;
            }
            return _lookup.TryGetValue(id ?? string.Empty, out definition);
        }

        private void OnEnable() => _lookup = null;
        private void OnValidate() => _lookup = null;
    }
}
