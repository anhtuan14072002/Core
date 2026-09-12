using System;

namespace RogueliteToolkit.Cards
{
    [Serializable]
    public struct CardStackData
    {
        public string CardId;
        public int Stacks;

        public CardStackData(string cardId, int stacks)
        {
            CardId = cardId;
            Stacks = stacks;
        }
    }
}
