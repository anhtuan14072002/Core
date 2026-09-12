using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    [Serializable]
    public sealed class EquipmentSlotDefinition
    {
        [SerializeField] private string _id;
        [SerializeField] private EquipmentType _type;

        public string Id => _id;
        public EquipmentType Type => _type;

        public EquipmentSlotDefinition(string id, EquipmentType type)
        {
            _id = id;
            _type = type;
        }
    }

    [CreateAssetMenu(fileName = "EquipmentSlots", menuName = "Roguelite Toolkit/Equipment/Slot Config")]
    public sealed class EquipmentSlotConfig : ScriptableObject
    {
        [SerializeField] private EquipmentSlotDefinition[] _slots = Array.Empty<EquipmentSlotDefinition>();

        public IReadOnlyList<EquipmentSlotDefinition> Slots => _slots;
    }
}
