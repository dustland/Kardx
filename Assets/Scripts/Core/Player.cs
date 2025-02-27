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
        private readonly List<Card> hand = new();
        private readonly Card[] battlefield = new Card[BATTLEFIELD_SLOT_COUNT];
        private readonly Stack<Card> deck = new();
        private readonly Queue<Card> discardPile = new();
        private readonly List<Card> graveyard = new(); // Added a new list to hold destroyed cards
        private Card headquartersCard;

        // Events
        public event Action<Card> OnCardDrawn;
        public event Action<Card> OnCardDiscarded;
        public event Action<Card, int> OnCardDeployed;
        public event Action<Card> OnCardDestroyed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerState"/> class.
        /// </summary>
        /// <param name="playerId">The player's ID.</param>
        /// <param name="initialDeck">The player's initial deck of cards.</param>
        /// <param name="faction">The player's faction.</param>
        /// <param name="logger">The logger to use for logging messages.</param>
        public Player(
            string playerId,
            List<Card> initialDeck,
            Faction faction = Faction.Neutral,
            ILogger logger = null
        )
        {
            Initialize(playerId, initialDeck, faction, logger);
        }

        public void Initialize(
            string playerId,
            List<Card> initialDeck,
            Faction faction = Faction.Neutral,
            ILogger logger = null
        )
        {
            this.playerId = playerId;
            this.logger = logger;
            this.credits = MAX_CREDITS;
            this.faction = faction;

            // Initialize deck with initial cards
            foreach (var card in initialDeck)
            {
                card.SetOwner(this); // Set the owner of the card
                deck.Push(card);
            }
        }

        // Public properties - all collections are read-only to prevent external modification
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
        public IReadOnlyList<Card> Hand => hand;

        /// <summary>
        /// Gets the player's battlefield of cards, preserving slot positions.
        /// </summary>
        public IReadOnlyList<Card> Battlefield => Array.AsReadOnly(battlefield);

        /// <summary>
        /// Gets the player's deck of cards.
        /// </summary>
        public IReadOnlyList<Card> Deck => deck.ToList();

        /// <summary>
        /// Gets the player's discard pile of cards.
        /// </summary>
        public IReadOnlyList<Card> DiscardPile => discardPile.ToList();

        /// <summary>
        /// Gets the player's graveyard of destroyed cards.
        /// </summary>
        public IReadOnlyList<Card> Graveyard => graveyard; // Added a new property to access the graveyard

        /// <summary>
        /// Gets the player's headquarter card.
        /// </summary>
        public Card Headquarter => headquartersCard;

        /// <summary>
        /// Gets the player's current credits.
        /// </summary>
        public int Credits => credits;

        // Hand management
        /// <summary>
        /// Draws a card from the deck and adds it to the player's hand.
        /// </summary>
        /// <param name="faceDown">Whether the card should be drawn face-down.</param>
        /// <returns>The drawn card, or null if no cards could be drawn.</returns>
        public Card DrawCard(bool faceDown = false)
        {
            if (hand.Count >= MAX_HAND_SIZE)
            {
                logger?.Log(
                    $"[{playerId}] Cannot draw card - hand is full ({MAX_HAND_SIZE} cards)"
                );
                return null;
            }

            if (deck.Count == 0)
            {
                logger?.Log($"[{playerId}] Cannot draw card - deck is empty");
                return null;
            }

            var card = deck.Pop();
            card.SetFaceDown(faceDown);
            card.SetOwner(this); // Set the owner of the card
            hand.Add(card);
            logger?.Log($"[{playerId}] Drew card: {card.Title}");
            OnCardDrawn?.Invoke(card);
            return card;
        }

        /// <summary>
        /// Discards a card from the player's hand.
        /// </summary>
        /// <param name="card">The card to discard.</param>
        /// <returns>True if the card was discarded successfully, false otherwise.</returns>
        public bool DiscardFromHand(Card card)
        {
            if (!hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot discard card that is not in hand");
                return false;
            }

            hand.Remove(card);
            discardPile.Enqueue(card);
            logger?.Log($"[{playerId}] Discarded card: {card.Title}");
            OnCardDiscarded?.Invoke(card);
            return true;
        }

        /// <summary>
        /// Deploys a card from the player's hand to the battlefield.
        /// </summary>
        /// <param name="card">The card to deploy.</param>
        /// <returns>True if the card was deployed successfully, false otherwise.</returns>
        public bool DeployCard(Card card, int position)
        {
            if (card == null)
            {
                logger?.Log($"[{playerId}] Cannot deploy null card");
                return false;
            }

            if (!hand.Contains(card))
            {
                logger?.Log($"[{playerId}] Cannot deploy card {card.Title} - not in hand");
                return false;
            }

            if (credits < card.DeploymentCost)
            {
                logger?.Log($"[{playerId}] Cannot deploy card {card.Title} - insufficient credits");
                return false;
            }

            // Find the first empty slot in the battlefield
            if (position < 0 || position >= BATTLEFIELD_SLOT_COUNT)
            {
                logger?.Log($"[{playerId}] Invalid battlefield position: {position}");
                return false;
            }

            if (battlefield[position] != null)
            {
                logger?.Log(
                    $"[{playerId}] Cannot deploy card {card.Title} - slot {position} is already occupied"
                );
                return false;
            }

            // Spend credits first
            if (!SpendCredits(card.DeploymentCost))
            {
                logger?.Log($"[{playerId}] Cannot deploy card {card.Title} - insufficient credits");
                return false;
            }

            // Move card from hand to battlefield and make it face up
            hand.Remove(card);
            card.SetFaceDown(false); // Card becomes visible when deployed
            card.SetOwner(this); // Set the owner of the card
            battlefield[position] = card;

            logger?.Log($"[{playerId}] Deployed card {card.Title} to battlefield slot {position}");
            OnCardDeployed?.Invoke(card, position);
            return true;
        }

        /// <summary>
        /// Removes a card from the battlefield.
        /// </summary>
        /// <param name="card">The card to remove.</param>
        /// <returns>True if the card was removed successfully, false otherwise.</returns>
        public bool RemoveFromBattlefield(Card card)
        {
            if (card == null)
            {
                logger?.Log($"[{playerId}] Cannot remove null card from battlefield");
                return false;
            }

            int slotIndex = Array.FindIndex(battlefield, slot => slot == card);
            if (slotIndex == -1)
            {
                logger?.Log(
                    $"[{playerId}] Failed to remove card {card.Title} from battlefield - not found"
                );
                return false;
            }

            battlefield[slotIndex] = null;
            discardPile.Enqueue(card); // Add to discard pile when removed
            logger?.Log(
                $"[{playerId}] Removed card {card.Title} from battlefield slot {slotIndex}"
            );
            return true;
        }

        // Resource management
        /// <summary>
        /// Spends credits from the player's current credits.
        /// </summary>
        /// <param name="amount">The amount of credits to spend.</param>
        /// <returns>True if the credits were spent successfully, false otherwise.</returns>
        public bool SpendCredits(int amount)
        {
            if (amount < 0)
            {
                logger?.Log($"[{playerId}] Cannot spend negative credits: {amount}");
                return false;
            }

            if (credits < amount)
            {
                logger?.Log($"[{playerId}] Not enough credits. Have: {credits}, Need: {amount}");
                return false;
            }

            credits -= amount;
            logger?.Log($"[{playerId}] Spent {amount} credits. Remaining: {credits}");
            return true;
        }

        /// <summary>
        /// Adds credits to the player's account.
        /// </summary>
        /// <param name="amount">The amount of credits to add.</param>
        public void AddCredits(int amount)
        {
            if (amount < 0)
            {
                logger?.Log($"[{playerId}] Cannot add negative credits: {amount}");
                return;
            }

            credits = Math.Min(credits + amount, MAX_CREDITS);
            logger?.Log($"[{playerId}] Added {amount} credits. Current: {credits}");
        }

        /// <summary>
        /// Gets all cards currently in play for this player (on the battlefield).
        /// </summary>
        /// <returns>A list of cards currently in play, excluding null slots.</returns>
        public List<Card> GetCardsInPlay()
        {
            return battlefield.Where(card => card != null).ToList();
        }

        // Reset all cards' attack status at the start of a turn
        public void ResetCardAttackStatus()
        {
            // Reset attack status for all cards on the battlefield
            foreach (var card in battlefield)
            {
                if (card != null)
                {
                    card.HasAttackedThisTurn = false;
                }
            }
        }

        // Destroy a card and move it to the graveyard
        public void DestroyCard(Card card)
        {
            if (card == null)
                return;

            // Find the card in the battlefield
            int slotIndex = -1;
            for (int i = 0; i < battlefield.Length; i++)
            {
                if (battlefield[i] == card)
                {
                    slotIndex = i;
                    break;
                }
            }

            // If found, remove it and add to graveyard
            if (slotIndex >= 0)
            {
                battlefield[slotIndex] = null;
                // Add the card to the graveyard
                graveyard.Add(card);

                // Notify listeners about the card being destroyed
                OnCardDestroyed?.Invoke(card);
            }
        }
    }
}
