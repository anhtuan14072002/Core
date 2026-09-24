using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueliteToolkit.Cards
{
    public enum CardRateType
    {
        Default = 0,
        LevelUp = 1,
        Reroll = 2,
        Special = 3
    }

    [Serializable]
    public sealed class CardRarityRate
    {
        [SerializeField] private CardRarity _rarity;
        [SerializeField, Min(0f)] private float _weight;

        public CardRarity Rarity => _rarity;
        public float Weight => _weight;
    }

    [Serializable]
    public sealed class CardLevelRate
    {
        [SerializeField, Min(1)] private int _level = 1;
        [SerializeField] private CardRarityRate[] _rarityRates = Array.Empty<CardRarityRate>();

        public int Level => _level;

        public IReadOnlyList<CardRarityRate> RarityRates =>
            _rarityRates ?? Array.Empty<CardRarityRate>();
    }

    [Serializable]
    public sealed class CardRateProfile
    {
        [SerializeField] private CardRateType _type;
        [SerializeField] private CardLevelRate[] _levels = Array.Empty<CardLevelRate>();

        public CardRateType Type => _type;

        public IReadOnlyList<CardLevelRate> Levels =>
            _levels ?? Array.Empty<CardLevelRate>();
    }

    [Serializable]
    public sealed class CardRarityVisualStyle
    {
        [SerializeField] private CardRarity _rarity;
        [SerializeField] private Color _frameColor = Color.white;

        [Tooltip("Optional frame art. Leave empty to keep the frame already assigned in the card view.")]
        [SerializeField]
        private Sprite _frameSprite;

        public CardRarity Rarity => _rarity;
        public Color FrameColor => _frameColor;
        public Sprite FrameSprite => _frameSprite;
    }

    [CreateAssetMenu(fileName = "CardDatabase", menuName = "Roguelite Toolkit/Card Database")]
    public sealed class CardDatabase : ScriptableObject
    {
        [SerializeField] private CardDefinition[] _cards = Array.Empty<CardDefinition>();
        [SerializeField] private CardRateProfile[] _rateProfiles = Array.Empty<CardRateProfile>();

        [Header("Card Visuals")] [SerializeField]
        private CardRarityVisualStyle[] _rarityVisualStyles =
            Array.Empty<CardRarityVisualStyle>();

        [Tooltip("Allow cards already selected by the player to appear again while below Max Stacks.")] [SerializeField]
        private bool _allowSelectedCards = true;

        private Dictionary<string, CardDefinition> _lookup;

        public IReadOnlyList<CardDefinition> Cards =>
            _cards ?? Array.Empty<CardDefinition>();

        public IReadOnlyList<CardRateProfile> RateProfiles =>
            _rateProfiles ?? Array.Empty<CardRateProfile>();

        public IReadOnlyList<CardRarityVisualStyle> RarityVisualStyles =>
            _rarityVisualStyles ?? Array.Empty<CardRarityVisualStyle>();

        public bool TryGetRarityVisualStyle(
            CardRarity rarity, out CardRarityVisualStyle visualStyle)
        {
            for (int i = 0; i < RarityVisualStyles.Count; i++)
            {
                CardRarityVisualStyle candidate = RarityVisualStyles[i];
                if (candidate == null || candidate.Rarity != rarity)
                    continue;

                visualStyle = candidate;
                return true;
            }

            visualStyle = null;
            return false;
        }

        public bool TryGet(string cardId, out CardDefinition card)
        {
            EnsureLookup();
            return _lookup.TryGetValue(cardId ?? string.Empty, out card);
        }

        public bool TryGetCard(string cardId, out CardDefinition card)
        {
            return TryGet(cardId, out card);
        }

        public int Roll(
            CardRateType rateType,
            int level,
            int count,
            List<CardDefinition> output,
            CardCollection ownedCards = null,
            Predicate<CardDefinition> cardFilter = null)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (count <= 0)
                return 0;

            CardLevelRate levelRate = GetLevelRate(rateType, level);
            if (levelRate == null)
                return Roll(count, output, ownedCards, cardFilter: cardFilter);

            while (output.Count < count &&
                   TryRollCard(levelRate, output, ownedCards, cardFilter, out CardDefinition card))
                output.Add(card);
            return output.Count;
        }

        public int Roll(
            int count,
            List<CardDefinition> output,
            CardCollection ownedCards = null,
            CardRarity? rarity = null,
            Predicate<CardDefinition> cardFilter = null)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            if (count <= 0)
                return 0;

            List<CardDefinition> pool = new(Cards.Count);
            for (int i = 0; i < Cards.Count; i++)
            {
                CardDefinition card = Cards[i];
                if (card == null || !card.HasValidId || card.Weight <= 0f)
                    continue;
                if (rarity.HasValue && card.Rarity != rarity.Value)
                    continue;
                if (!IsOwnedCardEligible(card, ownedCards) ||
                    (cardFilter != null && !cardFilter(card)))
                    continue;
                pool.Add(card);
            }

            while (output.Count < count && pool.Count > 0)
            {
                int selectedIndex = SelectWeightedIndex(pool);
                output.Add(pool[selectedIndex]);
                pool.RemoveAt(selectedIndex);
            }

            return output.Count;
        }

        private static int SelectWeightedIndex(IReadOnlyList<CardDefinition> pool)
        {
            float total = 0f;
            for (int i = 0; i < pool.Count; i++)
                total += pool[i].Weight;

            float roll = UnityEngine.Random.value * total;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= pool[i].Weight;
                if (roll <= 0f)
                    return i;
            }

            return pool.Count - 1;
        }

        private CardLevelRate GetLevelRate(CardRateType rateType, int level)
        {
            CardRateProfile selectedProfile = null;
            for (int i = 0; i < RateProfiles.Count; i++)
            {
                CardRateProfile profile = RateProfiles[i];
                if (profile == null || profile.Type != rateType)
                    continue;
                if (selectedProfile != null)
                    throw new InvalidOperationException(
                        $"Duplicate rate profile '{rateType}' in '{name}'.");
                selectedProfile = profile;
            }

            if (selectedProfile == null)
                return null;

            for (int i = 0; i < selectedProfile.Levels.Count; i++)
            {
                CardLevelRate rate = selectedProfile.Levels[i];
                if (rate != null && rate.Level == level)
                    return rate;
            }

            return null;
        }

        private bool TryRollCard(
            CardLevelRate levelRate,
            List<CardDefinition> selectedCards,
            CardCollection ownedCards,
            Predicate<CardDefinition> cardFilter,
            out CardDefinition selectedCard)
        {
            float rarityWeight = 0f;
            for (int i = 0; i < levelRate.RarityRates.Count; i++)
            {
                CardRarityRate rate = levelRate.RarityRates[i];
                if (rate.Weight > 0f && HasEligibleCard(rate.Rarity, selectedCards, ownedCards, cardFilter))
                    rarityWeight += rate.Weight;
            }

            if (rarityWeight <= 0f)
            {
                selectedCard = null;
                return false;
            }

            float rarityRoll = UnityEngine.Random.Range(0f, rarityWeight);
            CardRarity selectedRarity = default;
            for (int i = 0; i < levelRate.RarityRates.Count; i++)
            {
                CardRarityRate rate = levelRate.RarityRates[i];
                if (rate.Weight <= 0f || !HasEligibleCard(rate.Rarity, selectedCards, ownedCards, cardFilter))
                    continue;
                rarityRoll -= rate.Weight;
                selectedRarity = rate.Rarity;
                if (rarityRoll <= 0f)
                    break;
            }

            float cardWeight = 0f;
            for (int i = 0; i < Cards.Count; i++)
                if (IsEligible(Cards[i], selectedRarity, selectedCards, ownedCards, cardFilter))
                    cardWeight += Cards[i].Weight;

            float cardRoll = UnityEngine.Random.Range(0f, cardWeight);
            for (int i = 0; i < Cards.Count; i++)
            {
                CardDefinition card = Cards[i];
                if (!IsEligible(card, selectedRarity, selectedCards, ownedCards, cardFilter))
                    continue;
                cardRoll -= card.Weight;
                if (cardRoll <= 0f)
                {
                    selectedCard = card;
                    return true;
                }
            }

            selectedCard = null;
            return false;
        }

        private bool HasEligibleCard(
            CardRarity rarity,
            List<CardDefinition> selectedCards,
            CardCollection ownedCards,
            Predicate<CardDefinition> cardFilter)
        {
            for (int i = 0; i < Cards.Count; i++)
                if (IsEligible(Cards[i], rarity, selectedCards, ownedCards, cardFilter))
                    return true;
            return false;
        }

        private bool IsEligible(
            CardDefinition card,
            CardRarity rarity,
            List<CardDefinition> selectedCards,
            CardCollection ownedCards,
            Predicate<CardDefinition> cardFilter)
        {
            return card != null && card.Rarity == rarity && card.Weight > 0f &&
                   !selectedCards.Contains(card) &&
                   IsOwnedCardEligible(card, ownedCards) &&
                   (cardFilter == null || cardFilter(card));
        }

        private bool IsOwnedCardEligible(CardDefinition card, CardCollection ownedCards)
        {
            if (ownedCards == null)
                return true;

            int stacks = ownedCards.GetStacks(card.Id);
            return stacks < card.MaxStacks && (_allowSelectedCards || stacks == 0);
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;
            _lookup = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
            for (int i = 0; i < Cards.Count; i++)
            {
                CardDefinition card = Cards[i];
                if (card == null || !card.HasValidId)
                    continue;
                if (!_lookup.TryAdd(card.Id, card))
                    Debug.LogError($"Duplicate card ID '{card.Id}' in {name}.", this);
            }
        }

        private void OnValidate()
        {
            _lookup = null;
        }
    }
}