using System;
using System.Collections.Generic;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.Equipment
{
    [CreateAssetMenu(fileName = "Equipment", menuName = "Roguelite Toolkit/Equipment/Definition")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private EquipmentType _type;
        [SerializeField] private StatGameplayEffect[] _statEffects = Array.Empty<StatGameplayEffect>();

        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public EquipmentType Type => _type;
        public IReadOnlyList<StatGameplayEffect> StatEffects => _statEffects;
        public bool HasValidId => !string.IsNullOrWhiteSpace(_id);

        internal void Validate()
        {
            if (!HasValidId || _type == null)
                throw new InvalidOperationException($"Equipment '{name}' requires a stable ID and type.");
            for (int i = 0; i < _statEffects.Length; i++)
                if (_statEffects[i] == null || _statEffects[i].Stat == null)
                    throw new InvalidOperationException($"Equipment '{name}' has an invalid stat effect.");
        }

        internal void Apply(GameplayEffectContext context, string instanceId, bool equipped)
        {
            EffectSource source = new("equipment", instanceId);
            for (int i = 0; i < _statEffects.Length; i++)
                _statEffects[i].OnValueChanged(context, source, i, equipped ? 0 : 1, equipped ? 1 : 0);
        }

        private void OnValidate() => _id = _id?.Trim();
    }
}
