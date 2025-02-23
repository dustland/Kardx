using System;
using System.Collections.Generic;
using Kardx.Core.Data.Cards;
using Kardx.Core.Logging;

namespace Kardx.Core.Data.States
{
    public class PlayerState
    {
        private readonly string playerId;
        private List<Card> deckCards = new();
        private List<Card> handCards = new();
        private Dictionary<int, Card> battlefieldCards = new();
        private List<Card> discardCards = new();
        private Card headquarter;
        private int credits;
        private readonly ILogger logger;

        // Constants
        private const int MAX_HAND_SIZE = 10;
        private const int MAX_CREDITS = 9;
        private const int CREDITS_PER_TURN = 5;
        private const int MaxBattlefieldSize = 10; // Assuming a constant for max battlefield size

        // Runtime collections
        private Stack<Card> deck;
        private Queue<Card> discardPile;

        public PlayerState(string id, List<Card> initialDeck, ILogger logger = null)
        {
            playerId = id;
            deckCards = new List<Card>(initialDeck);
            this.logger = logger;
            InitializeCollections();
        }

        private void InitializeCollections()
        {
            // Initialize deck
            deck = new Stack<Card>();
            foreach (var card in deckCards)
            {
                deck.Push(card);
            }

            // Initialize discard pile
            discardPile = new Queue<Card>();
            foreach (var card in discardCards)
            {
                discardPile.Enqueue(card);
            }
        }

        // Public properties
        public IReadOnlyCollection<Card> Deck => deck;
        public IReadOnlyList<Card> Hand => handCards;
        public IReadOnlyDictionary<int, Card> Battlefield => battlefieldCards;
        public IReadOnlyCollection<Card> DiscardPile => discardPile;
        public int Credits => credits;
        public Card Headquarter => headquarter;

        // Deck management
        public void SetDeck(IEnumerable<Card> cards)
        {
            deckCards.Clear();
            deck.Clear();
            foreach (var card in cards)
            {
                deckCards.Add(card);
                deck.Push(card);
            }
        }

        public void ShuffleDeck()
        {
            var tempList = new List<Card>(deck);
            deck.Clear();

            // Fisher-Yates shuffle
            var random = new Random();
            for (int i = tempList.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (tempList[i], tempList[j]) = (tempList[j], tempList[i]);
            }

            foreach (var card in tempList)
            {
                deck.Push(card);
            }

            // Update serialized list
            deckCards = tempList;
        }

        // Hand management
        public Card DrawCard()
        {
            logger?.Log($"[{playerId}] Drawing card. Deck: {deck.Count}, Hand: {handCards.Count}");
            if (deck.Count > 0 && handCards.Count < MAX_HAND_SIZE)
            {
                var card = deck.Pop();
                handCards.Add(card);
                // Update serialized deck list
                deckCards.Remove(card);
                return card;
            }
            return null;
        }

        public void DrawCards(int count)
        {
            for (int i = 0; i < count && handCards.Count < MAX_HAND_SIZE && deck.Count > 0; i++)
            {
                DrawCard();
            }
        }

        public bool DiscardCard(Card card)
        {
            if (handCards.Remove(card))
            {
                discardPile.Enqueue(card);
                discardCards.Add(card);
                return true;
            }
            return false;
        }

        // Battlefield management
        public bool IsValidBattlefieldPosition(int position)
        {
            return position >= 0 && position < MaxBattlefieldSize;
        }

        public Card GetCardAtPosition(int position)
        {
            if (!IsValidBattlefieldPosition(position))
                return null;

            battlefieldCards.TryGetValue(position, out Card card);
            return card;
        }

        public bool DeployCard(Card card, int position)
        {
            if (!IsValidBattlefieldPosition(position))
                return false;

            if (GetCardAtPosition(position) != null)
                return false;

            if (!handCards.Contains(card))
                return false;

            handCards.Remove(card);
            battlefieldCards[position] = card;
            return true;
        }

        public bool RemoveFromBattlefield(int position)
        {
            if (battlefieldCards.TryGetValue(position, out Card card))
            {
                battlefieldCards.Remove(position);
                discardPile.Enqueue(card);
                discardCards.Add(card);
                return true;
            }
            return false;
        }

        // Resource management
        public void RefreshCredits()
        {
            credits = Math.Min(credits + CREDITS_PER_TURN, MAX_CREDITS);
        }

        public bool SpendCredits(int amount)
        {
            if (amount <= credits)
            {
                credits -= amount;
                return true;
            }
            return false;
        }

        public void AddCredits(int amount)
        {
            credits = Math.Min(credits + amount, MAX_CREDITS);
        }

        // Headquarter management
        public void SetHeadquarter(Card card)
        {
            if (card.CardType.Category == CardCategory.Headquarter)
            {
                headquarter = card;
            }
        }
    }
}
