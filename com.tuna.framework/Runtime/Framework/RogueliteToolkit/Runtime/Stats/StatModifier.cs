using System;

namespace RogueliteToolkit.Stats
{
    [Serializable]
    public readonly struct StatModifier
    {
        public string ModifierId { get; }
        public string SourceId { get; }
        public StatModifierOperation Operation { get; }
        public float Value { get; }
        public int Priority { get; }

        public StatModifier(
            string modifierId,
            string sourceId,
            StatModifierOperation operation,
            float value,
            int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
                throw new ArgumentException("A stat modifier requires a stable modifier ID.", nameof(modifierId));

            ModifierId = modifierId;
            SourceId = sourceId?.Trim() ?? string.Empty;
            Operation = operation;
            Value = value;
            Priority = priority;
        }
    }
}
