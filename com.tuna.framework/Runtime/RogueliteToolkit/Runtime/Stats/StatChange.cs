namespace RogueliteToolkit.Stats
{
    public readonly struct StatChange
    {
        public StatDefinition Definition { get; }
        public float PreviousValue { get; }
        public float CurrentValue { get; }

        public StatChange(StatDefinition definition, float previousValue, float currentValue)
        {
            Definition = definition;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
        }
    }
}
