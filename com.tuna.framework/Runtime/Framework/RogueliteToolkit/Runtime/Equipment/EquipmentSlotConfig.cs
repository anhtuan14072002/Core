using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    [Serializable]
    public sealed class EquipmentSlotDefinition
    {
        [SerializeField] private EquipmentSlotId _id;
        [SerializeField] private EquipmentType _type;

        public EquipmentSlotId Id => _id;
        public EquipmentType Type => _type;

        public EquipmentSlotDefinition(EquipmentSlotId id, EquipmentType type)
        {
            _id = id;
            _type = type;
        }
    }

    [CreateAssetMenu(fileName = "EquipmentSlots", menuName = "Roguelite Toolkit/Equipment/Slot Config")]
    public sealed class EquipmentSlotConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _maxLevel = 100;
        [SerializeField] private EquipmentSlotDefinition[] _slots = Array.Empty<EquipmentSlotDefinition>();

        public int MaxLevel => Mathf.Max(1, _maxLevel);
        public IReadOnlyList<EquipmentSlotDefinition> Slots => _slots;
    }
}
