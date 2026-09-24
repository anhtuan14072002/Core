namespace RogueliteToolkit.Equipment
{
    public sealed class EquipmentInstance
    {
        public string Id { get; }
        public EquipmentDefinition Definition { get; internal set; }
        public EquipmentSlotId SlotId { get; internal set; }
        public bool IsEquipped => SlotId != EquipmentSlotId.None;

        internal EquipmentInstance(string id, EquipmentDefinition definition)
        {
            Id = id;
            Definition = definition;
        }
    }
}