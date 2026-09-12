namespace RogueliteToolkit.Equipment
{
    public sealed class EquipmentInstance
    {
        public string Id { get; }
        public EquipmentDefinition Definition { get; }
        public string SlotId { get; internal set; }
        public bool IsEquipped => SlotId != null;

        internal EquipmentInstance(string id, EquipmentDefinition definition)
        {
            Id = id;
            Definition = definition;
        }
    }
}
