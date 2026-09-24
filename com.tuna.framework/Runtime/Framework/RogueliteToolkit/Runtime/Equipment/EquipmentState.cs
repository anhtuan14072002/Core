using System;
using System.Collections.Generic;
using RogueliteToolkit.Effects;
using RogueliteToolkit.Stats;

namespace RogueliteToolkit.Equipment
{
    public sealed class EquipmentState
    {
        private readonly GameplayEffectContext _context;
        private readonly List<EquipmentInstance> _items = new();
        private readonly Dictionary<string, EquipmentInstance> _instances = new(StringComparer.Ordinal);
        private readonly Dictionary<EquipmentSlotId, int> _slotIndices = new();
        private readonly EquipmentInstance[] _equipped;
        private readonly int[] _slotLevels;

        public EquipmentSlotConfig Config { get; }
        public IReadOnlyList<EquipmentInstance> Items { get; }
        public event Action Changed;

        public EquipmentState(EquipmentSlotConfig config, GameplayEffectContext context)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.GetRequired<StatSet>();
            Config = config;
            Items = _items.AsReadOnly();
            _equipped = new EquipmentInstance[config.Slots.Count];
            _slotLevels = new int[config.Slots.Count];
            for (int i = 0; i < config.Slots.Count; i++)
            {
                EquipmentSlotDefinition slot = config.Slots[i];
                _slotLevels[i] = 1;
                if (slot == null || slot.Id == EquipmentSlotId.None || slot.Type == null || slot.Type.Id != slot.Id ||
                    !_slotIndices.TryAdd(slot.Id, i))
                    throw new ArgumentException("Equipment slots require unique IDs and types.", nameof(config));
            }
        }

        public EquipmentInstance GetEquipped(EquipmentSlotId slotId) =>
            _slotIndices.TryGetValue(slotId, out int index) ? _equipped[index] : null;

        public int GetSlotLevel(EquipmentSlotId slotId) => _slotLevels[_slotIndices[slotId]];

        public bool UpgradeSlot(EquipmentSlotId slotId, bool maximum = false)
        {
            EquipmentInstance item = GetEquipped(slotId);
            if (item == null || GetSlotLevel(slotId) >= Config.MaxLevel)
                return false;
            int index = _slotIndices[slotId];
            // Upgrades are free until an equipment upgrade cost is configured.
            _slotLevels[index] = maximum ? Config.MaxLevel : _slotLevels[index] + 1;
            item.Definition.Apply(_context, item.Id, true, _slotLevels[index]);
            Changed?.Invoke();
            return true;
        }

        public int[] CopySlotLevels() => (int[])_slotLevels.Clone();

        public void RestoreSlotLevels(int[] levels)
        {
            if (levels == null)
                return;
            for (int i = 0; i < Math.Min(levels.Length, _slotLevels.Length); i++)
            {
                _slotLevels[i] = Math.Clamp(levels[i], 1, Config.MaxLevel);
                EquipmentInstance item = _equipped[i];
                if (item != null)
                    item.Definition.Apply(_context, item.Id, true, _slotLevels[i]);
            }

            Changed?.Invoke();
        }

        public EquipmentInstance Add(EquipmentDefinition definition, string instanceId = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            definition.Validate();
            string id = instanceId == null ? Guid.NewGuid().ToString("N") : instanceId.Trim();
            if (string.IsNullOrWhiteSpace(id) || _instances.ContainsKey(id))
                throw new ArgumentException("Equipment requires a unique instance ID.", nameof(instanceId));
            EquipmentInstance item = new(id, definition);
            _instances.Add(id, item);
            _items.Add(item);
            Changed?.Invoke();
            return item;
        }

        public bool CanEquip(EquipmentInstance item, EquipmentSlotId slotId) =>
            Owns(item) && _slotIndices.TryGetValue(slotId, out int index) &&
            Config.Slots[index].Id == item.Definition.Type.Id && item.SlotId != slotId;

        public bool Equip(EquipmentInstance item, EquipmentSlotId slotId)
        {
            if (!CanEquip(item, slotId))
                return false;
            int index = _slotIndices[slotId];
            EquipmentInstance previous = _equipped[index];
            if (previous != null)
                previous.SlotId = EquipmentSlotId.None;
            bool wasEquipped = item.IsEquipped;
            if (wasEquipped)
                _equipped[_slotIndices[item.SlotId]] = null;
            _equipped[index] = item;
            item.SlotId = slotId;
            if (previous != null)
                previous.Definition.Apply(_context, previous.Id, false);
            if (!wasEquipped)
                item.Definition.Apply(_context, item.Id, true, GetSlotLevel(slotId));
            Changed?.Invoke();
            return true;
        }

        public bool Unequip(EquipmentInstance item)
        {
            if (!Owns(item) || !item.IsEquipped)
                return false;
            _equipped[_slotIndices[item.SlotId]] = null;
            item.SlotId = EquipmentSlotId.None;
            item.Definition.Apply(_context, item.Id, false);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(EquipmentInstance item)
        {
            if (!Owns(item))
                return false;
            bool wasEquipped = item.IsEquipped;
            if (wasEquipped)
            {
                _equipped[_slotIndices[item.SlotId]] = null;
                item.SlotId = EquipmentSlotId.None;
            }

            _instances.Remove(item.Id);
            _items.Remove(item);
            if (wasEquipped)
                item.Definition.Apply(_context, item.Id, false);
            Changed?.Invoke();
            return true;
        }

        public void WriteSaveData(List<EquipmentSaveData> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            for (int i = 0; i < _items.Count; i++)
            {
                EquipmentInstance item = _items[i];
                output.Add(new EquipmentSaveData(item.Id, item.Definition.Id, item.SlotId));
            }
        }

        public int GetMergeCount(EquipmentInstance item)
        {
            if (!Owns(item))
                return 0;
            int count = 1;
            for (int i = 0; i < _items.Count; i++)
                if (_items[i] != item && !_items[i].IsEquipped && _items[i].Definition == item.Definition)
                    count++;
            return count;
        }

        public bool CanMerge(EquipmentInstance item) =>
            Owns(item) && item.Definition.MergeResult != null && GetMergeCount(item) >= 3;

        public bool Merge(EquipmentInstance item)
        {
            if (!MergeWithoutNotify(item))
                return false;
            Changed?.Invoke();
            return true;
        }

        public bool Merge(EquipmentInstance item, EquipmentInstance first, EquipmentInstance second)
        {
            if (!MergeWithoutNotify(item, first, second))
                return false;
            Changed?.Invoke();
            return true;
        }

        private bool MergeWithoutNotify(EquipmentInstance item, EquipmentInstance first, EquipmentInstance second)
        {
            if (!Owns(item) || item.Definition.MergeResult == null || first == second ||
                !IsMergeMaterial(item, first) || !IsMergeMaterial(item, second))
                return false;
            EquipmentDefinition source = item.Definition;
            EquipmentDefinition result = source.MergeResult;
            source.Validate();
            result.Validate();
            if (item.IsEquipped)
                source.Apply(_context, item.Id, false);
            _instances.Remove(first.Id);
            _instances.Remove(second.Id);
            _items.Remove(first);
            _items.Remove(second);
            item.Definition = result;
            if (item.IsEquipped)
                result.Apply(_context, item.Id, true, GetSlotLevel(item.SlotId));
            return true;
        }

        public bool IsMergeMaterial(EquipmentInstance source, EquipmentInstance item) =>
            Owns(source) && Owns(item) && source != item && !item.IsEquipped &&
            source.Definition.MergeResult != null && source.Definition == item.Definition;

        public bool CanMergeAny()
        {
            for (int i = 0; i < _items.Count; i++)
                if (CanMerge(_items[i]))
                    return true;
            return false;
        }

        public int MergeAll()
        {
            int merged = 0;
            // ponytail: click-only inventory scans; index recipes if bags grow to thousands of items.
            while (true)
            {
                EquipmentInstance candidate = null;
                for (int i = 0; i < _equipped.Length && candidate == null; i++)
                    if (CanMerge(_equipped[i]))
                        candidate = _equipped[i];
                for (int i = 0; i < _items.Count && candidate == null; i++)
                    if (CanMerge(_items[i]))
                        candidate = _items[i];
                if (candidate == null)
                    break;
                MergeWithoutNotify(candidate);
                merged++;
            }

            if (merged > 0)
                Changed?.Invoke();
            return merged;
        }

        private bool MergeWithoutNotify(EquipmentInstance item)
        {
            if (!CanMerge(item))
                return false;
            EquipmentInstance first = null;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                EquipmentInstance material = _items[i];
                if (!IsMergeMaterial(item, material))
                    continue;
                if (first != null)
                    return MergeWithoutNotify(item, first, material);
                first = material;
            }

            return false;
        }

        public void Restore(IReadOnlyList<EquipmentSaveData> data, EquipmentDatabase database)
        {
            if (_items.Count != 0)
                throw new InvalidOperationException("Restore requires an empty EquipmentState.");
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (data == null)
                return;

            // Validate the whole save before applying any stats or changing ownership.
            HashSet<string> ids = new(StringComparer.Ordinal);
            HashSet<EquipmentSlotId> slots = new();
            EquipmentDefinition[] definitions = new EquipmentDefinition[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                EquipmentSaveData entry = data[i];
                if (string.IsNullOrWhiteSpace(entry.InstanceId) || !ids.Add(entry.InstanceId.Trim()) ||
                    !database.TryGet(entry.DefinitionId, out definitions[i]))
                    throw new ArgumentException("Save contains invalid instances or unknown equipment.", nameof(data));
                if (entry.SlotId != EquipmentSlotId.None &&
                    (!_slotIndices.TryGetValue(entry.SlotId, out int index) ||
                     Config.Slots[index].Id != definitions[i].Type.Id || !slots.Add(entry.SlotId)))
                    throw new ArgumentException("Save contains invalid equipment slots.", nameof(data));
            }

            for (int i = 0; i < data.Count; i++)
            {
                EquipmentInstance item = Add(definitions[i], data[i].InstanceId);
                if (data[i].SlotId != EquipmentSlotId.None)
                    Equip(item, data[i].SlotId);
            }
        }

        private bool Owns(EquipmentInstance item) => item != null &&
                                                     _instances.TryGetValue(item.Id, out EquipmentInstance owned) &&
                                                     ReferenceEquals(owned, item);
    }
}