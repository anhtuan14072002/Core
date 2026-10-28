using System;

namespace RogueliteToolkit.Effects
{
    [Serializable]
    public readonly struct EffectSource : IEquatable<EffectSource>
    {
        public string Type { get; }
        public string Id { get; }

        public EffectSource(string type, string id)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("An effect source requires a type.", nameof(type));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("An effect source requires a stable ID.", nameof(id));

            Type = type.Trim();
            Id = id.Trim();
        }

        public string CreateEffectId(int effectIndex)
        {
            if (effectIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(effectIndex));
            return $"{Type}:{Id}:effect:{effectIndex}";
        }

        public bool Equals(EffectSource other) =>
            string.Equals(Type, other.Type, StringComparison.Ordinal) &&
            string.Equals(Id, other.Id, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is EffectSource other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Type, Id);

        public override string ToString() => $"{Type}:{Id}";

        public static bool operator ==(EffectSource left, EffectSource right) => left.Equals(right);
        public static bool operator !=(EffectSource left, EffectSource right) => !left.Equals(right);
    }
}