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
        [SerializeField, HideInInspector] private string _legacyId;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private EquipmentType _type;
        [SerializeField] private EquipmentRarity _rarity = EquipmentRarity.Rare;
        [SerializeField] private EquipmentDefinition _mergeResult;
        [SerializeField] private StatGameplayEffect[] _statEffects = Array.Empty<StatGameplayEffect>();
        [SerializeField] private StatGameplayEffect[] _baseStatEffects = Array.Empty<StatGameplayEffect>();

        public string Id => _id;
        internal string LegacyId => _legacyId;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public EquipmentType Type => _type;
        public EquipmentRarity Rarity => _rarity;
        public EquipmentDefinition MergeResult => _mergeResult;
        public IReadOnlyList<StatGameplayEffect> StatEffects => _statEffects;
        public IReadOnlyList<StatGameplayEffect> BaseStatEffects => _baseStatEffects;
        public bool HasValidId => !string.IsNullOrWhiteSpace(_id);

        internal void Validate()
        {
            if (!HasValidId || _type == null)
                throw new InvalidOperationException($"Equipment '{name}' requires a stable ID and type.");
            if (_mergeResult != null && (_mergeResult.Type != _type ||
                                         (int)_mergeResult.Rarity != (int)_rarity + 1))
                throw new InvalidOperationException(
                    $"Equipment '{name}' must merge into the same type at the next rarity.");
            for (int i = 0; i < _statEffects.Length; i++)
                if (_statEffects[i] == null || _statEffects[i].Stat == null)
                    throw new InvalidOperationException($"Equipment '{name}' has an invalid stat effect.");
            for (int i = 0; i < _baseStatEffects.Length; i++)
                if (_baseStatEffects[i] == null || _baseStatEffects[i].Stat == null)
                    throw new InvalidOperationException($"Equipment '{name}' has an invalid base stat effect.");
        }

        internal void Apply(GameplayEffectContext context, string instanceId, bool equipped, int level = 1)
        {
            EffectSource source = new("equipment", instanceId);
            for (int i = 0; i < _statEffects.Length; i++)
                _statEffects[i].OnValueChanged(context, source, i, equipped ? 0 : 1, equipped ? 1 : 0);
            EffectSource baseSource = new("equipment_base", instanceId);
            for (int i = 0; i < _baseStatEffects.Length; i++)
                _baseStatEffects[i].OnValueChanged(context, baseSource, i, equipped ? 0 : level, equipped ? level : 0);
        }

        private void OnValidate() => _id = _id?.Trim();
    }
}