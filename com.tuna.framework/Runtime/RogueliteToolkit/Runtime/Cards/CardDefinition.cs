using System;
using System.Collections.Generic;
using RogueliteToolkit.Effects;
using UnityEngine;
using UnityEngine.Serialization;

namespace RogueliteToolkit.Cards
{
    [CreateAssetMenu(fileName = "Card", menuName = "Roguelite Toolkit/Card Definition")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _icon;
        [SerializeField] private CardRarity _rarity;
        [SerializeField, Min(0f)] private float _weight = 1f;
        [SerializeField, Min(1)] private int _maxStacks = 1;
        [FormerlySerializedAs("_stats")]
        [SerializeField] private StatGameplayEffect[] _statEffects = Array.Empty<StatGameplayEffect>();
        [SerializeReference] private GameplayEffect[] _effects = Array.Empty<GameplayEffect>();

        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public CardRarity Rarity => _rarity;
        public float Weight => _weight;
        public int MaxStacks => _maxStacks;
        public IReadOnlyList<StatGameplayEffect> StatEffects =>
            _statEffects ?? Array.Empty<StatGameplayEffect>();
        public IReadOnlyList<GameplayEffect> Effects =>
            _effects ?? Array.Empty<GameplayEffect>();
        public bool HasValidId => !string.IsNullOrWhiteSpace(_id);

        public void NotifyStacksChanged(
            GameplayEffectContext context, int previousStacks, int currentStacks)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            EffectSource source = new("card", _id);
            int effectIndex = 0;
            if (_statEffects != null)
                for (int i = 0; i < _statEffects.Length; i++, effectIndex++)
                    _statEffects[i]?.OnValueChanged(
                        context, source, effectIndex, previousStacks, currentStacks);

            if (_effects != null)
                for (int i = 0; i < _effects.Length; i++, effectIndex++)
                    _effects[i]?.OnValueChanged(
                        context, source, effectIndex, previousStacks, currentStacks);
        }

        private void OnValidate()
        {
            _id = _id?.Trim();
            _weight = Mathf.Max(0f, _weight);
            _maxStacks = Mathf.Max(1, _maxStacks);
        }
    }
}
