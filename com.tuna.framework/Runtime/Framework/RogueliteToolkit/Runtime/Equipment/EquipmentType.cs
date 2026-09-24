using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    public enum EquipmentSlotId
    {
        None = 0,
        CutterBlade = 1,
        Gear = 2,
        ReinforcedFrame = 3,
        Core = 4,
        NexusProcessor = 5,
        CableSystem = 6
    }

    [CreateAssetMenu(fileName = "EquipmentType", menuName = "Roguelite Toolkit/Equipment/Type")]
    public sealed class EquipmentType : ScriptableObject
    {
        [SerializeField] private EquipmentSlotId _id;
        public EquipmentSlotId Id => _id;
        [SerializeField] private Sprite _icon;
        public Sprite Icon => _icon;
    }
}