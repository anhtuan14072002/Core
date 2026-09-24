using System;
using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    [Serializable]
    public struct EquipmentSaveData
    {
        [SerializeField] private string _instanceId;
        [SerializeField] private string _definitionId;
        [SerializeField] private string _slotId;

        public string InstanceId => _instanceId;

        public string DefinitionId => _definitionId;

        // Keep the existing save keys so previously equipped items restore correctly.
        public EquipmentSlotId SlotId => _slotId switch
        {
            null or "" => EquipmentSlotId.None,
            "cutter_blade" => EquipmentSlotId.CutterBlade,
            "gear" => EquipmentSlotId.Gear,
            "reinforced_frame" => EquipmentSlotId.ReinforcedFrame,
            "core" => EquipmentSlotId.Core,
            "nexus_processor" => EquipmentSlotId.NexusProcessor,
            "cable_system" => EquipmentSlotId.CableSystem,
            _ => throw new ArgumentException("Save contains an unknown equipment slot.")
        };

        public EquipmentSaveData(string instanceId, string definitionId, EquipmentSlotId slotId)
        {
            _instanceId = instanceId;
            _definitionId = definitionId;
            _slotId = slotId switch
            {
                EquipmentSlotId.None => null,
                EquipmentSlotId.CutterBlade => "cutter_blade",
                EquipmentSlotId.Gear => "gear",
                EquipmentSlotId.ReinforcedFrame => "reinforced_frame",
                EquipmentSlotId.Core => "core",
                EquipmentSlotId.NexusProcessor => "nexus_processor",
                EquipmentSlotId.CableSystem => "cable_system",
                _ => throw new ArgumentOutOfRangeException(nameof(slotId))
            };
        }
    }
}