using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core.Data.Cards;
using Kardx.Core.Logging;

namespace Kardx.Core.Data.States
{
    /// <summary>
    /// Represents the state of a player in the game.
    /// </summary>
    public class PlayerState
    {
        // Constants
        public const int MAX_BATTLEFIELD_SIZE = 5;
        private const int MAX_HAND_SIZE = 10;
        private const int MAX_CREDITS = 9;
        private const int CREDITS_PER_TURN = 5;

        // Core state
        private readonly string playerId;
        private readonly ILogger logger;
        private int credits;

        // Card collections
        private readonly List<Card> handCards = new();
        private readonly List<Card> battlefieldCards = new();
        private readonly Stack<Card> deck = new();
        private readonly Queue<Card> discardPile = new();
        private Card headquarter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerState"/> class.
        /// </summary>
        /// <param name="id">The player's ID.</param>
        /// <param name="initialDeck">The player's initial deck of cards.</param>
        /// <param name="logger">The logger to use for logging messages.</param>
        public PlayerState(string id, List<Card> initialDeck, ILogger logger = null)
        {
            playerId = id;
            this.logger = logger;
            this.credits = MAX_CREDITS;

            // Initialize deck with initial cards
            foreach (var card in initialDeck)
            {
                deck.Push(card);
            }
        }

        // Public properties - all collections are read-only to prevent external modification
        /// <summary>
        /// Gets the player's ID.
        /// </summary>
        public string PlayerId => playerId;

        /// <summary>
        /// Gets the player's hand of cards.
        /// </summary>
        public IReadOnlyList<Card> Hand => handCards;

        /// <summary>
        /// Gets the player's battlefield of cards.
        /// </summary>
        public IReadOnlyList<Card> Battlefield => battlefieldCards;

        /// <summary>
        /// Gets the player's deck of cards.
        /// </summary>
        public IReadOnlyList<Card> Deck => deck.ToList();

        /// <summary>
        /// Gets the player's discard pile of cards.
        /// </summary>
        public IReadOnlyList<Card> DiscardPile => discardPile.ToList();

        /// <summary>
        /// Gets the player's headquarter card.
        /// </summary>
        public Card Headquarter => headquarter;

        /// <summary>
        /// Gets the player's current credits.
        /// </summary>
        public int Credits => credits;

        // Hand management
        /// <summary>
        /// Draws a card from the deck and adds it to the player's hand.
        /// </summary>
        /// <returns>True if the card was drawn successfully, false otherwise.</returns>
        public bool DrawCard()
        {
            if (handCards.Count >= MAX_HAND_SIZE)
            {
                logger?.Log($"[{playerId}] Cannot draw card: Hand is full");
                return false;
            }

            if (!deck.Any())
            {
                logger?.Log($"[{playerId}] Cannot draw card: Deck is empty");
                return false;
            }

            var card = deck.Pop();
            handCards.Add(card);
            logger?.Log($"[{playerId}] Drew card: {card.Name}");
            return true;
        }

        /// <summary>
        /// Discards a card from the player's hand.
        /// </summary>
        /// <param name="card">The card to discard.</param>
        /// <returns>True if the card was discarded successfully, false otherwise.</returns>
        public bool DiscardFromHand(Card card)
        {
            if (!handCards.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot discard card that is not in hand");
                return false;
            }

            handCards.Remove(card);
            discardPile.Enqueue(card);
            logger?.Log($"[{playerId}] Discarded card: {card.Name}");
            return true;
        }

        // Battlefield management
        /// <summary>
        /// Deploys a card from the player's hand to the battlefield.
        /// </summary>
        /// <param name="card">The card to deploy.</param>
        /// <returns>True if the card was deployed successfully, false otherwise.</returns>
        public bool DeployCard(Card card)
        {
            if (!handCards.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot deploy card that is not in hand");
                return false;
            }

            if (battlefieldCards.Count >= MAX_BATTLEFIELD_SIZE)
            {
                logger?.Log($"[{playerId}] Cannot deploy: Battlefield is full");
                return false;
            }

            if (credits < card.DeploymentCost)
            {
                logger?.Log($"[{playerId}] Cannot deploy: Insufficient credits");
                return false;
            }

            handCards.Remove(card);
            battlefieldCards.Add(card);
            credits -= card.DeploymentCost;
            logger?.Log($"[{playerId}] Deployed card: {card.Name}");
            return true;
        }

        /// <summary>
        /// Removes a card from the battlefield and adds it to the discard pile.
        /// </summary>
        /// <param name="card">The card to remove.</param>
        /// <returns>True if the card was removed successfully, false otherwise.</returns>
        public bool RemoveFromBattlefield(Card card)
        {
            if (!battlefieldCards.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot remove card that is not on battlefield");
                return false;
            }

            battlefieldCards.Remove(card);
            discardPile.Enqueue(card);
            logger?.Log($"[{playerId}] Removed card from battlefield: {card.Name}");
            return true;
        }

        // Resource management
        /// <summary>
        /// Adds credits to the player's current credits.
        /// </summary>
        /// <param name="amount">The amount of credits to add.</param>
        public void AddCredits(int amount)
        {
            credits = Math.Min(credits + amount, MAX_CREDITS);
            logger?.Log($"[{playerId}] Credits added: {amount}, Total: {credits}");
        }

        /// <summary>
        /// Spends credits from the player's current credits.
        /// </summary>
        /// <param name="amount">The amount of credits to spend.</param>
        /// <returns>True if the credits were spent successfully, false otherwise.</returns>
        public bool SpendCredits(int amount)
        {
            if (credits < amount)
            {
                logger?.Log($"[{playerId}] Cannot spend credits: Insufficient funds");
                return false;
            }

            credits -= amount;
            logger?.Log($"[{playerId}] Credits spent: {amount}, Remaining: {credits}");
            return true;
        }

        /// <summary>
        /// Starts a new turn for the player.
        /// </summary>
        public void StartTurn()
        {
            AddCredits(CREDITS_PER_TURN);
            DrawCard();
        }
    }
}
