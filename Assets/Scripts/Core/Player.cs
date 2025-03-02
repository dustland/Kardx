using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Utils;

namespace Kardx.Core
{
    /// <summary>
    /// Represents the state of a player in the game.
    /// </summary>
    public class Player
    {
        // Constants
        public const int BATTLEFIELD_SLOT_COUNT = 5;
        private const int MAX_HAND_SIZE = 10;
        private const int MAX_CREDITS = 9;
        public const int CREDITS_PER_TURN = 1;

        // Core state
        private string playerId;
        private ILogger logger;
        private int credits;
        private Faction faction;

        // Card collections
        private Hand hand;
        private Battlefield battlefield;
        private Deck deck;
        private Card headquartersCard;
        private Board board; // Reference to the game board

        // Events
        public event Action<Card> OnCardDrawn;
        public event Action<Card, int> OnCardDeployed;
        public event Action<Card> OnCardDestroyed;

        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class.
        /// </summary>
        /// <param name="playerId">The player's ID.</param>
        /// <param name="initialDeck">The player's initial deck of cards.</param>
        /// <param name="faction">The player's faction.</param>
        /// <param name="logger">The logger to use for logging messages.</param>
        public Player(
            string playerId,
            List<Card> initialDeck,
            Faction faction = Faction.Neutral,
            ILogger logger = null,
            Board board = null
        )
        {
            Initialize(playerId, initialDeck, faction, logger, board);
        }

        public void Initialize(
            string playerId,
            List<Card> initialDeck,
            Faction faction = Faction.Neutral,
            ILogger logger = null,
            Board board = null
        )
        {
            this.playerId = playerId;
            this.logger = logger;
            this.credits = MAX_CREDITS;
            this.faction = faction;
            this.board = board;

            // Initialize collections
            hand = new Hand(this);
            battlefield = new Battlefield(this);
            deck = new Deck(this);

            // Wire up events
            battlefield.OnCardDeployed += (card, slot) => OnCardDeployed?.Invoke(card, slot);
            hand.OnCardAdded += (card, source) => OnCardDrawn?.Invoke(card);

            // Initialize deck with initial cards
            foreach (var card in initialDeck)
            {
                card.SetOwner(this); // Set the owner of the card
                deck.AddCard(card);
            }

            // Shuffle the deck
            deck.Shuffle();
        }

        // Public properties
        /// <summary>
        /// Gets the player's ID.
        /// </summary>
        public string Id => playerId;

        /// <summary>
        /// Gets the player's faction.
        /// </summary>
        public Faction Faction => faction;

        /// <summary>
        /// Gets the player's hand of cards.
        /// </summary>
        public Hand Hand => hand;

        /// <summary>
        /// Gets the player's battlefield.
        /// </summary>
        public Battlefield Battlefield => battlefield;

        /// <summary>
        /// Gets the player's deck of cards.
        /// </summary>
        public Deck Deck => deck;

        /// <summary>
        /// Gets the player's headquarter card.
        /// </summary>
        public Card Headquarter => headquartersCard;

        /// <summary>
        /// Gets the player's current credits.
        /// </summary>
        public int Credits => credits;

        /// <summary>
        /// Gets a value indicating whether the player is an opponent.
        /// </summary>
        public bool IsOpponent => board.IsOpponent(this);

        // Hand management
        /// <summary>
        /// Draws a card from the deck and adds it to the player's hand.
        /// </summary>
        /// <param name="faceDown">Whether the card should be drawn face-down.</param>
        /// <returns>The drawn card, or null if no cards could be drawn.</returns>
        public Card DrawCard(bool faceDown = false)
        {
            if (hand.IsFull)
            {
                logger?.Log(
                    $"[{playerId}] Cannot draw card - hand is full ({MAX_HAND_SIZE} cards)"
                );
                return null;
            }

            var card = deck.DrawCard();
            if (card == null)
            {
                logger?.Log($"[{playerId}] Cannot draw card - deck is empty");
                return null;
            }

            card.SetFaceDown(faceDown);
            hand.AddCard(card);
            return card;
        }

        /// <summary>
        /// Discards a card from the player's hand.
        /// </summary>
        /// <param name="card">The card to discard.</param>
        /// <returns>True if the card was discarded, false otherwise.</returns>
        public bool DiscardCard(Card card)
        {
            if (card == null)
                return false;

            if (!hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot discard card - card not in hand");
                return false;
            }

            hand.RemoveCard(card);
            return true;
        }

        /// <summary>
        /// Deploys a unit card to the battlefield.
        /// </summary>
        /// <param name="card">The card to deploy.</param>
        /// <param name="slotIndex">The slot index to deploy the card to.</param>
        /// <returns>True if the card was deployed, false otherwise.</returns>
        public bool DeployUnitCard(Card card, int slotIndex)
        {
            if (card == null)
                return false;

            if (!hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot deploy card - card not in hand");
                return false;
            }

            if (card.CardType.Category != CardCategory.Unit)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not a unit card");
                return false;
            }

            if (credits < card.CardType.Cost)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not enough credits");
                return false;
            }

            if (!battlefield.IsSlotEmpty(slotIndex))
            {
                logger?.Log($"[{playerId}] Cannot deploy card - slot {slotIndex} is not empty");
                return false;
            }

            // Remove from hand
            hand.RemoveCard(card);

            // Pay the cost
            SpendCredits(card.CardType.Cost);

            // Deploy to battlefield
            battlefield.DeployCard(card, slotIndex);

            return true;
        }

        /// <summary>
        /// Deploys an order card.
        /// </summary>
        /// <param name="card">The card to deploy.</param>
        /// <returns>True if the card was deployed, false otherwise.</returns>
        public bool DeployOrderCard(Card card)
        {
            if (card == null)
                return false;

            if (!hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot deploy card - card not in hand");
                return false;
            }

            if (card.CardType.Category != CardCategory.Order)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not an order card");
                return false;
            }

            if (credits < card.CardType.Cost)
            {
                logger?.Log($"[{playerId}] Cannot deploy card - not enough credits");
                return false;
            }

            // Remove from hand
            hand.RemoveCard(card);

            // Pay the cost
            SpendCredits(card.CardType.Cost);

            // Order cards go directly to the discard pile after being played
            return true;
        }

        /// <summary>
        /// Deploys a card (for backward compatibility).
        /// </summary>
        /// <param name="card">The card to deploy.</param>
        /// <param name="position">The position to deploy to (ignored for order cards).</param>
        /// <returns>True if the card was deployed, false otherwise.</returns>
        public bool DeployCard(Card card, int position)
        {
            if (card == null)
                return false;
                
            return card.CardType.Category == CardCategory.Unit ? DeployUnitCard(card, position)
                : card.CardType.Category == CardCategory.Order ? DeployOrderCard(card)
                : false;
        }

        /// <summary>
        /// Destroys a card on the battlefield.
        /// </summary>
        /// <param name="card">The card to destroy.</param>
        /// <returns>True if the card was destroyed, false otherwise.</returns>
        public bool DestroyCard(Card card)
        {
            if (card == null)
                return false;

            if (!battlefield.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot destroy card - card not on battlefield");
                return false;
            }

            battlefield.RemoveCard(card);
            // Card is destroyed - we're not keeping track of destroyed cards for now
            return true;
        }

        /// <summary>
        /// Removes a card from the battlefield.
        /// </summary>
        /// <param name="card">The card to remove.</param>
        /// <returns>True if the card was removed, false otherwise.</returns>
        public bool RemoveFromBattlefield(Card card)
        {
            if (card == null)
                return false;
                
            if (!battlefield.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot remove card from battlefield - card not on battlefield");
                return false;
            }
            
            // Remove from battlefield
            battlefield.RemoveCard(card);
            
            // Card is removed - we're not tracking removed cards for now
            logger?.Log($"[{playerId}] Card {card.Title} removed from battlefield");
            
            return true;
        }

        /// <summary>
        /// Spends credits.
        /// </summary>
        /// <param name="amount">The amount of credits to spend.</param>
        /// <returns>True if the credits were spent, false otherwise.</returns>
        public bool SpendCredits(int amount)
        {
            if (amount <= 0)
                return false;

            if (credits < amount)
            {
                logger?.Log($"[{playerId}] Cannot spend {amount} credits - only have {credits}");
                return false;
            }

            credits -= amount;
            return true;
        }

        /// <summary>
        /// Adds credits.
        /// </summary>
        /// <param name="amount">The amount of credits to add.</param>
        public void AddCredits(int amount)
        {
            if (amount <= 0)
                return;

            credits += amount;
            if (credits > MAX_CREDITS)
            {
                credits = MAX_CREDITS;
            }
        }

        /// <summary>
        /// Sets the board reference.
        /// </summary>
        /// <param name="board">The board reference.</param>
        public void SetBoard(Board board)
        {
            this.board = board;
        }

        /// <summary>
        /// Gets all cards currently in play (on the battlefield and headquarters).
        /// </summary>
        /// <returns>A list of all cards in play for this player.</returns>
        public List<Card> GetCardsInPlay()
        {
            var result = new List<Card>();

            // Add cards from the battlefield
            if (battlefield != null)
            {
                result.AddRange(battlefield.Cards);
            }

            // Add headquarters card if it exists
            if (headquartersCard != null)
            {
                result.Add(headquartersCard);
            }

            return result;
        }

        /// <summary>
        /// Sets the headquarters card.
        /// </summary>
        /// <param name="card">The headquarters card.</param>
        public void SetHeadquarters(Card card)
        {
            if (card == null || card.CardType.Category != CardCategory.Headquarters)
                return;

            headquartersCard = card;
            card.SetOwner(this);
        }

        /// <summary>
        /// Resets the attack status for all cards on the battlefield.
        /// </summary>
        public void ResetCardAttackStatus()
        {
            foreach (var card in battlefield.Cards)
            {
                // Reset the card's attack status for the new turn
                if (card is Card unitCard)
                {
                    // Reset hasAttackedThisTurn flag (this property might be internal to the Card class)
                    // Call a method on the card to reset its attack status
                    unitCard.ProcessStartOfTurnEffects(); // This should reset hasAttackedThisTurn
                }
            }
            
            logger?.Log($"[{playerId}] Reset attack status for all cards on the battlefield");
        }
    }
}
