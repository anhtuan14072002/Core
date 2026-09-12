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
        private readonly Dictionary<string, int> _slotIndices = new(StringComparer.Ordinal);
        private readonly EquipmentInstance[] _equipped;

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
            for (int i = 0; i < config.Slots.Count; i++)
            {
                EquipmentSlotDefinition slot = config.Slots[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.Id) || slot.Type == null ||
                    !_slotIndices.TryAdd(slot.Id, i))
                    throw new ArgumentException("Equipment slots require unique IDs and types.", nameof(config));
            }
        }

        public EquipmentInstance GetEquipped(string slotId) =>
            slotId != null && _slotIndices.TryGetValue(slotId, out int index) ? _equipped[index] : null;

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

        public bool CanEquip(EquipmentInstance item, string slotId) =>
            Owns(item) && slotId != null && _slotIndices.TryGetValue(slotId, out int index) &&
            Config.Slots[index].Type == item.Definition.Type && item.SlotId != slotId;

        public bool Equip(EquipmentInstance item, string slotId)
        {
            if (!CanEquip(item, slotId))
                return false;
            int index = _slotIndices[slotId];
            EquipmentInstance previous = _equipped[index];
            if (previous != null)
                previous.SlotId = null;
            bool wasEquipped = item.IsEquipped;
            if (wasEquipped)
                _equipped[_slotIndices[item.SlotId]] = null;
            _equipped[index] = item;
            item.SlotId = slotId;
            if (previous != null)
                previous.Definition.Apply(_context, previous.Id, false);
            if (!wasEquipped)
                item.Definition.Apply(_context, item.Id, true);
            Changed?.Invoke();
            return true;
        }

        public bool Unequip(EquipmentInstance item)
        {
            if (!Owns(item) || !item.IsEquipped)
                return false;
            _equipped[_slotIndices[item.SlotId]] = null;
            item.SlotId = null;
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
                item.SlotId = null;
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
            HashSet<string> slots = new(StringComparer.Ordinal);
            EquipmentDefinition[] definitions = new EquipmentDefinition[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                EquipmentSaveData entry = data[i];
                if (string.IsNullOrWhiteSpace(entry.InstanceId) || !ids.Add(entry.InstanceId.Trim()) ||
                    !database.TryGet(entry.DefinitionId, out definitions[i]))
                    throw new ArgumentException("Save contains invalid instances or unknown equipment.", nameof(data));
                if (!string.IsNullOrEmpty(entry.SlotId) &&
                    (!_slotIndices.TryGetValue(entry.SlotId, out int index) ||
                     Config.Slots[index].Type != definitions[i].Type || !slots.Add(entry.SlotId)))
                    throw new ArgumentException("Save contains invalid equipment slots.", nameof(data));
            }
            for (int i = 0; i < data.Count; i++)
            {
                EquipmentInstance item = Add(definitions[i], data[i].InstanceId);
                if (!string.IsNullOrEmpty(data[i].SlotId))
                    Equip(item, data[i].SlotId);
            }
        }

        private bool Owns(EquipmentInstance item) => item != null &&
            _instances.TryGetValue(item.Id, out EquipmentInstance owned) && ReferenceEquals(owned, item);
    }
}
