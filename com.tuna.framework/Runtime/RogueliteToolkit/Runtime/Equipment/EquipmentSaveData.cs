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
        public string SlotId => _slotId;

        public EquipmentSaveData(string instanceId, string definitionId, string slotId)
        {
            _instanceId = instanceId;
            _definitionId = definitionId;
            _slotId = slotId;
        }
    }
}
