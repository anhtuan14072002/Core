using System;
using System.Collections.Generic;
using RogueliteToolkit.Effects;
using UnityEngine;

namespace RogueliteToolkit.Cards
{
    public sealed class CardCollection
    {
        private readonly Dictionary<string, int> _stacks =
            new(StringComparer.Ordinal);

        public int GetStacks(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) && _stacks.TryGetValue(cardId, out int stacks) ? stacks : 0;
        }

        public bool Add(CardDefinition card, GameplayEffectContext context, int amount = 1)
        {
            Validate(card, context);
            if (amount <= 0)
                return false;

            int previous = GetStacks(card.Id);
            int current = Mathf.Min(card.MaxStacks, previous + amount);
            if (current == previous)
                return false;

            _stacks[card.Id] = current;
            card.NotifyStacksChanged(context, previous, current);
            return true;
        }

        public bool Remove(
            CardDefinition card, GameplayEffectContext context, int amount = 1)
        {
            Validate(card, context);
            if (amount <= 0)
                return false;

            int previous = GetStacks(card.Id);
            int current = Mathf.Max(0, previous - amount);
            if (current == previous)
                return false;

            if (current == 0)
                _stacks.Remove(card.Id);
            else
                _stacks[card.Id] = current;
            card.NotifyStacksChanged(context, previous, current);
            return true;
        }

        public void Clear(CardDatabase database, GameplayEffectContext context)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            List<CardStackData> cards = new();
            WriteSaveData(cards);
            for (int i = 0; i < cards.Count; i++)
                if (database.TryGet(cards[i].CardId, out CardDefinition card))
                    Remove(card, context, cards[i].Stacks);
        }

        public void Restore(
            IReadOnlyList<CardStackData> data,
            CardDatabase database,
            GameplayEffectContext context)
        {
            if (_stacks.Count != 0)
                throw new InvalidOperationException("Restore requires an empty CardCollection.");
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            if (data == null)
                return;

            for (int i = 0; i < data.Count; i++)
                if (data[i].Stacks > 0 && database.TryGet(data[i].CardId, out CardDefinition card))
                    Add(card, context, data[i].Stacks);
        }

        public void WriteSaveData(List<CardStackData> output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            output.Clear();
            foreach (KeyValuePair<string, int> pair in _stacks)
                output.Add(new CardStackData(pair.Key, pair.Value));
        }

        private static void Validate(CardDefinition card, GameplayEffectContext context)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!card.HasValidId)
                throw new ArgumentException("Card requires a stable ID.", nameof(card));
        }
    }
}