using System;

namespace RogueliteToolkit.Stats
{
    [Serializable]
    public sealed class StatBaseValue
    {
        public StatDefinition Definition;
        public float Value;
    }
}
