using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Utils;
using Kardx.UI;
using UnityEngine;
using Kardx.Managers;
using Kardx.Planning;
using Kardx.Models.Cards;
using Kardx.Views.Cards;

namespace Kardx.Models.Match
{
    /// <summary>
    /// Manages the game match, handling turns and player actions.
    /// </summary>
    public class MatchManager : MonoBehaviour
    {
        private Kardx.Utils.ILogger logger;
        private Board board;
        private StrategyPlanner strategyPlanner;
        private const int CREDITS_PER_TURN = 1;

        // Public properties
        public bool IsMatchInProgress { get; private set; }
        public string CurrentPlayerId => board?.CurrentTurnPlayer?.Id;
        public int TurnNumber => board.TurnNumber;
        public Player Player => board.Player;
        public Player Opponent => board.Opponent;
        public Board Board => board;
        public Player CurrentPlayer => board.CurrentTurnPlayer;

        // Essential events for UI updates
        public event Action<Card, int> OnCardDeployed;
        public event Action<Card> OnCardDrawn;
        public event Action<Card> OnCardDied;
        public event Action<Card> OnCardDiscarded;
        public event Action<Card> OnCardReturned;
        public event Action<Strategy> OnStrategyDetermined;
        public event Action<Decision> OnDecisionExecuting;
        public event Action<Decision> OnDecisionExecuted;
        public event EventHandler<Player> OnTurnStarted;
        public event EventHandler<Player> OnTurnEnded;
        public event Action<string> OnMatchStarted;
        public event Action<string> OnMatchEnded;

        // Ability-related events
        public event Action<Card, Card, int, int> OnAttackCompleted;

        // Event for signaling when AI needs to process its turn
        public event Action<Board, StrategyPlanner> OnProcessAITurn;

        // Generic event to trigger UI updates when needed
        public event System.Action OnUIUpdateNeeded;

        // Reference to ViewManager instead of ViewRegistry directly
        private ViewManager viewManager;

        // Reference to the CardRegistry for tracking card views
        private ViewRegistry viewRegistry;

        public ViewManager ViewManager => viewManager;
        public ViewRegistry ViewRegistry => viewRegistry;

        /// <summary>
        /// Creates a new instance of the MatchManager class.
        /// </summary>
        public MatchManager(Kardx.Utils.ILogger logger = null)
        {
            // Use the SimpleLogger if no logger is provided
            this.logger = logger ?? new SimpleLogger("MatchManager");
        }

        /// <summary>
        /// Initialize the ViewManager reference
        /// </summary>
        public void SetViewManager(ViewManager viewManager)
        {
            this.viewManager = viewManager;
            this.viewRegistry = viewManager?.Registry;

            if (viewManager == null)
            {
                Debug.LogWarning("[MatchManager] Null ViewManager provided!");
            }
            else
            {
                Debug.Log("[MatchManager] ViewManager set successfully");
            }
        }

        /// <summary>
        /// Initializes the match with default players but doesn't start the game flow.
        /// </summary>
        public void Initialize()
        {
            var player1 = new Player(
                "Player 1",
                LoadDeck(Faction.UnitedStates),
                Faction.UnitedStates
            );
            var player2 = new Player(
                "Player 2",
                LoadDeck(Faction.SovietUnion),
                Faction.SovietUnion
            );

            // Create a new board
            board = new Board(player1, player2);

            // Create the strategy planner for the opponent (player2)
            strategyPlanner = new StrategyPlanner(this, StrategySource.Dummy, logger);

            // Subscribe to strategy planner events
            strategyPlanner.OnStrategyDetermined += (strategy) =>
            {
                OnStrategyDetermined?.Invoke(strategy);
            };

            strategyPlanner.OnDecisionExecuting += (decision) =>
            {
                OnDecisionExecuting?.Invoke(decision);
            };

            strategyPlanner.OnDecisionExecuted += (decision) =>
            {
                OnDecisionExecuted?.Invoke(decision);
            };
        }

        /// <summary>
        /// Starts a new match with the initialized players.
        /// </summary>
        public void StartMatch()
        {
            // Ensure the game is initialized
            if (board == null)
            {
                Initialize();
            }

            // Reset match state
            IsMatchInProgress = true;

            // Notify listeners that the match has started
            OnMatchStarted?.Invoke("Match started");

            // Start first turn
            StartTurn();
        }

        private void StartTurn()
        {
            // Notify listeners that a new turn is starting
            OnTurnStarted?.Invoke(this, board.CurrentTurnPlayer);

            // Reset card attack status
            board.CurrentTurnPlayer.ResetCardAttackStatus();

            // Draw a card as the frist step for each turn
            Card drawnCard = DrawCard(); // Player cards are face up
            if (drawnCard != null)
            {
                logger?.Log($"[{CurrentPlayerId}] Card drawn: {drawnCard.Title}");
                OnCardDrawn?.Invoke(drawnCard);
                TriggerUIUpdateNeeded();
            }

            // If it's the opponent's turn, process it automatically
            if (!board.IsPlayerTurn())
            {
                logger?.Log("Let the opponent run the first turn");

                // Trigger the event for the UI layer to handle AI processing
                OnProcessAITurn?.Invoke(board, strategyPlanner);
            }
        }

        /// <summary>
        /// Advances to the next turn in the game.
        /// </summary>
        public void NextTurn()
        {
            if (board == null)
                throw new InvalidOperationException("Match not started");

            // End the current turn
            var currentPlayer = board.CurrentTurnPlayer;
            logger?.Log($"[MatchManager] Ending turn for player {currentPlayer.Id}");

            // Process end of turn effects
            board.ProcessEndOfTurnEffects();

            // Notify listeners that the turn is ending
            OnTurnEnded?.Invoke(this, currentPlayer);

            // Start the next turn
            // Switch to the next player
            var nextPlayer = board.SwitchCurrentPlayer();
            // Increment turn number only on player turn, so that two player will add the same credits
            if (board.IsPlayerTurn())
            {
                board.IncrementTurnNumber();
            }
            logger?.Log(
                $"[MatchManager] Starting turn {board.TurnNumber} for player {nextPlayer.Id}"
            );

            // Initialize the player for the new turn
            // Add credits based on the turn number
            int creditsToAdd = CREDITS_PER_TURN * board.TurnNumber;
            nextPlayer.AddCredits(creditsToAdd);
            logger?.Log($"[MatchManager] Added {creditsToAdd} credits to player {nextPlayer.Id}");

            // Draw a card for the player
            Card drawnCard = DrawCard();
            if (drawnCard != null)
            {
                logger?.Log($"[{CurrentPlayerId}] Card drawn: {drawnCard.Title}");
                OnCardDrawn?.Invoke(drawnCard);
                TriggerUIUpdateNeeded();
            }

            // Process start of turn effects
            board.ProcessStartOfTurnEffects();

            // Notify listeners that a new turn is starting
            OnTurnStarted?.Invoke(this, nextPlayer);

            // Reset card attack status for both players
            nextPlayer.ResetCardAttackStatus();

            // If it's the opponent's turn, process it automatically
            if (!board.IsPlayerTurn())
            {
                logger?.Log("Processing opponent turn");

                // Trigger the event for the UI layer to handle AI processing
                OnProcessAITurn?.Invoke(board, strategyPlanner);
            }
        }

        private List<Card> LoadDeck(Faction ownerFaction)
        {
            var cardTypes = CardLoader.LoadCardTypes();
            var deck = new List<Card>();

            // Shuffle the card types before adding them to the deck
            var shuffledCardTypes = cardTypes.OrderBy(x => Guid.NewGuid()).ToList();

            var orderCardTypes = shuffledCardTypes.FindLast(x => x.Category == CardCategory.Order);

            foreach (var cardType in shuffledCardTypes)
            {
                // TODO: Add deck building rules here (e.g., card limits, faction restrictions)
                deck.Add(new Card(cardType, ownerFaction));
            }

            // TEST: Let's always make sure the order card is in the deck at position 2
            deck.Insert(0, new Card(orderCardTypes, ownerFaction));

            return deck;
        }

        public void EndMatch()
        {
            if (!IsMatchInProgress)
                return;

            IsMatchInProgress = false;
            OnMatchEnded?.Invoke($"Match ended after {TurnNumber} turns");
            logger?.Log($"Match ended after {TurnNumber} turns");
        }

        public Card DrawCard()
        {
            var player = board.CurrentTurnPlayer;
            var drawnCard = player.DrawCard(!board.IsPlayerTurn());
            if (drawnCard != null)
            {
                logger?.Log($"[{player.Id}] Card drawn: {drawnCard.Title}");
            }
            return drawnCard;
        }

        public bool CanDeployUnitCard(Card card)
        {
            if (!IsMatchInProgress || card == null)
            {
                logger?.LogError("[MatchManager] Cannot deploy card: card is null");
                return false;
            }

            var currentPlayer = board.CurrentTurnPlayer;
            if (currentPlayer == null)
            {
                logger?.LogError("[MatchManager] Cannot deploy card: current player is null");
                return false;
            }

            // Check if it's a unit card
            if (card.CardType.Category != CardCategory.Unit)
            {
                logger?.LogError("[MatchManager] Cannot deploy card: not a unit card");
                return false;
            }

            // Check if there's at least one empty slot on the battlefield
            bool hasEmptySlot = false;
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                if (currentPlayer.Battlefield.IsSlotEmpty(i))
                {
                    hasEmptySlot = true;
                    break;
                }
            }

            return currentPlayer.Hand.Contains(card)
                && currentPlayer.Credits >= card.DeploymentCost
                && hasEmptySlot;
        }

        public bool CanDeployOrderCard(Card card)
        {
            if (!IsMatchInProgress || card == null)
            {
                logger?.LogError("[MatchManager] Cannot deploy order card: card is null");
                return false;
            }

            var currentPlayer = board.CurrentTurnPlayer;
            if (currentPlayer == null)
            {
                logger?.LogError("[MatchManager] Cannot deploy order card: current player is null");
                return false;
            }

            // Check if it's an order card
            if (card.CardType.Category != CardCategory.Order)
            {
                logger?.LogError("[MatchManager] Cannot deploy card: not an order card");
                return false;
            }

            // Order cards just need to be in hand and have enough credits
            return currentPlayer.Hand.Contains(card)
                && currentPlayer.Credits >= card.DeploymentCost;
        }

        // For backward compatibility
        public bool CanDeployCard(Card card)
        {
            if (card == null)
                return false;

            return card.CardType.Category == CardCategory.Unit ? CanDeployUnitCard(card)
                : card.CardType.Category == CardCategory.Order ? CanDeployOrderCard(card)
                : false;
        }

        private bool DeployUnitCard(Card card, int position)
        {
            var currentPlayer = board.CurrentTurnPlayer;

            if (!CanDeployUnitCard(card))
            {
                // Add more detailed logging to help diagnose the issue
                if (card == null)
                {
                    logger?.LogError("[MatchManager] Cannot deploy unit card: card is null");
                }
                else if (!currentPlayer.Hand.Contains(card))
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: not in player's hand"
                    );
                }
                else if (currentPlayer.Credits < card.DeploymentCost)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: insufficient credits (has {currentPlayer.Credits}, needs {card.DeploymentCost})"
                    );
                }
                else if (position < 0 || position >= Battlefield.SLOT_COUNT)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: invalid position {position}"
                    );
                }
                else if (!currentPlayer.Battlefield.IsSlotEmpty(position))
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: position {position} is already occupied"
                    );
                }
                return false;
            }

            bool success = currentPlayer.DeployUnitCard(card, position);
            if (success)
            {
                OnCardDeployed?.Invoke(card, position);
                TriggerUIUpdateNeeded();
            }
            return success;
        }

        private bool DeployOrderCard(Card card)
        {
            if (!CanDeployOrderCard(card))
            {
                // Add more detailed logging to help diagnose the issue
                var player = board.CurrentTurnPlayer;
                if (card == null)
                {
                    logger?.LogError("[MatchManager] Cannot deploy order card: card is null");
                }
                else if (!player.Hand.Contains(card))
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy order card {card.Title}: not in player's hand"
                    );
                }
                else if (player.Credits < card.DeploymentCost)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy order card {card.Title}: insufficient credits (has {player.Credits}, needs {card.DeploymentCost})"
                    );
                }
                return false;
            }

            var currentPlayer = board.CurrentTurnPlayer;
            bool success = currentPlayer.DeployOrderCard(card);
            if (success)
            {
                OnCardDeployed?.Invoke(card, -1);
                TriggerUIUpdateNeeded();
            }
            return success;
        }

        // For backward compatibility
        public bool DeployCard(Card card, int position)
        {
            if (card == null)
                return false;

            if (card.Owner != board.CurrentTurnPlayer)
            {
                logger?.LogError($"[MatchManager] Cannot deploy card {card.Title}: card belongs to {card.Owner.Id} but current player is {board.CurrentTurnPlayer.Id}");
                return false;
            }

            // Set card face-up when deployed to the battlefield
            card.SetFaceDown(false);

            bool success = card.CardType.Category == CardCategory.Unit ? DeployUnitCard(card, position)
                : card.CardType.Category == CardCategory.Order ? DeployOrderCard(card)
                : false;
            return success;
        }

        /// <summary>
        /// Create a card view with the specified prefab
        /// </summary>
        public CardView CreateCardView(Card card, Transform parent, CardView prefab)
        {
            if (card == null || parent == null || prefab == null)
            {
                Debug.LogError("[MatchManager] Cannot create card view - null parameter(s)");
                return null;
            }

            // Use the ViewManager if available
            if (viewManager != null)
            {
                return viewManager.CreateCardView(card, parent, card.Owner == Player);
            }

            // Fallback direct creation if no ViewManager
            CardView cardView = UnityEngine.Object.Instantiate(prefab, parent);
            cardView.Initialize(card);

            // Register the view in our registry
            if (viewRegistry != null)
            {
                viewRegistry.RegisterCard(card, cardView);
            }

            return cardView;
        }

        /// <summary>
        /// Destroy a card view
        /// </summary>
        public void DestroyCardView(Card card)
        {
            if (card == null) return;

            // Use the ViewManager if available
            if (viewManager != null)
            {
                viewManager.DestroyCardView(card);
                return;
            }

            // Fallback if no ViewManager
            if (viewRegistry != null)
            {
                CardView view = viewRegistry.GetCardView(card);
                if (view != null)
                {
                    viewRegistry.UnregisterCard(card);
                    UnityEngine.Object.Destroy(view.gameObject);
                }
            }
        }

        /// <summary>
        /// Initiates an attack between two cards
        /// </summary>
        /// <param name="attackerCard">The attacking card</param>
        /// <param name="defenderCard">The defending card</param>
        /// <returns>True if the attack was successful, false otherwise</returns>
        public bool InitiateAttack(Card attackerCard, Card defenderCard)
        {
            logger?.Log($"Initiating attack from {attackerCard.Title} to {defenderCard.Title}");

            // Basic null checks
            if (attackerCard == null || defenderCard == null || board == null)
            {
                logger?.Log("Attack failed: Null card or board reference");
                return false;
            }

            // Check if the attack is valid
            if (!CanAttack(attackerCard, defenderCard))
            {
                logger?.Log($"Invalid attack from {attackerCard.Title} to {defenderCard.Title}");
                return false;
            }

            // Mark the attacker as having attacked this turn
            attackerCard.HasAttackedThisTurn = true;

            // Calculate damage
            int attackDamage = attackerCard.Attack;
            int counterAttackDamage = defenderCard.CounterAttack;

            // Apply damage to defender
            defenderCard.TakeDamage(attackDamage);

            // Apply counter-attack damage to attacker if defender is still alive
            if (defenderCard.CurrentDefense > 0)
            {
                attackerCard.TakeDamage(counterAttackDamage);
            }

            attackerCard.Owner.SpendCredits(attackerCard.OperationCost);

            // Check if any cards died as a result of the attack
            CheckCardDeath(attackerCard);
            CheckCardDeath(defenderCard);

            // Notify listeners about the attack
            OnAttackCompleted?.Invoke(
                attackerCard,
                defenderCard,
                attackDamage,
                defenderCard.CurrentDefense
            );

            TriggerUIUpdateNeeded();

            return true;
        }

        /// <summary>
        /// Checks if a card has died and handles its death
        /// </summary>
        /// <param name="card">The card to check</param>
        private void CheckCardDeath(Card card)
        {
            if (card.CurrentDefense <= 0)
            {
                // Remove the card from the battlefield
                card.Owner.RemoveFromBattlefield(card);

                logger?.Log($"Card {card.Title} has died and been moved to the discard pile");

                // Fire event to notify UI
                OnCardDied?.Invoke(card);
                TriggerUIUpdateNeeded();
            }
        }

        /// <summary>
        /// Checks if it's currently the human player's turn.
        /// </summary>
        /// <returns>True if it's the human player's turn, false otherwise.</returns>
        public bool IsPlayerTurn()
        {
            if (board == null || CurrentPlayer == null)
                return false;

            return CurrentPlayer == Player;
        }

        /// <summary>
        /// Checks if a card can attack another card
        /// </summary>
        /// <param name="attackerCard">The attacking card</param>
        /// <param name="defenderCard">The defending card</param>
        /// <returns>True if the attack is valid, false otherwise</returns>
        public bool CanAttack(Card attackerCard, Card defenderCard)
        {
            if (attackerCard == null || defenderCard == null)
                return false;

            // Check if the attacker has already attacked this turn
            if (attackerCard.HasAttackedThisTurn)
            {
                logger?.Log("[MatchManager] Attack failed: Attacker has already attacked this turn");
                return false;
            }

            // Check if the cards belong to different players
            if (attackerCard.Owner == defenderCard.Owner)
            {
                logger?.Log("[MatchManager] Attack failed: Cards belong to the same player");
                return false;
            }

            // Check if both cards are on the battlefield
            bool attackerOnBattlefield = attackerCard.Owner.Battlefield.Contains(attackerCard);
            bool defenderOnBattlefield = defenderCard.Owner.Battlefield.Contains(defenderCard);

            if (!attackerOnBattlefield || !defenderOnBattlefield)
            {
                logger?.Log("[MatchManager] Attack failed: One or more cards are not on the battlefield");
                return false;
            }

            // Add any other attack validation rules here
            if (attackerCard.OperationCost > attackerCard.Owner.Credits)
            {
                logger?.Log("[MatchManager] Attack failed: Attacker does not have enough credits");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when a card is drawn by any player
        /// </summary>
        private void TriggerCardDrawn(Card card)
        {
            if (card == null) return;

            Debug.Log($"[MatchManager] Card drawn: {card.Title} by {card.Owner.Id}");

            // Trigger any subscribed events
            OnCardDrawn?.Invoke(card);

            // Always trigger UI update after game state changes
            TriggerUIUpdateNeeded();
        }

        /// <summary>
        /// Called when a card is deployed to the battlefield
        /// </summary>
        private void TriggerCardDeployed(Card card, int slotIndex)
        {
            if (card == null) return;

            Debug.Log($"[MatchManager] Card deployed: {card.Title} to slot {slotIndex}");

            // Trigger any subscribed events
            OnCardDeployed?.Invoke(card, slotIndex);

            // Always trigger UI update after game state changes
            TriggerUIUpdateNeeded();
        }

        /// <summary>
        /// Called when a card is destroyed/removed from the battlefield
        /// </summary>
        private void TriggerCardDied(Card card)
        {
            if (card == null) return;

            Debug.Log($"[MatchManager] Card died: {card.Title}");

            // Trigger any subscribed events
            OnCardDied?.Invoke(card);

            // Always trigger UI update after game state changes
            TriggerUIUpdateNeeded();
        }

        /// <summary>
        /// Called when a card is discarded
        /// </summary>
        private void TriggerCardDiscarded(Card card)
        {
            if (card == null) return;

            Debug.Log($"[MatchManager] Card discarded: {card.Title}");

            // Trigger any subscribed events
            OnCardDiscarded?.Invoke(card);

            // Always trigger UI update after game state changes
            TriggerUIUpdateNeeded();
        }

        /// <summary>
        /// Called when a card is returned to the hand
        /// </summary>
        private void TriggerCardReturned(Card card)
        {
            if (card == null) return;

            Debug.Log($"[MatchManager] Card returned: {card.Title}");

            // Trigger any subscribed events
            OnCardReturned?.Invoke(card);

            // Always trigger UI update after game state changes
            TriggerUIUpdateNeeded();
        }

        /// <summary>
        /// Called when any game state change requires a UI update
        /// </summary>
        public void TriggerUIUpdateNeeded()
        {
            Debug.Log("[MatchManager] UI update triggered");
            OnUIUpdateNeeded?.Invoke();
        }
    }
}
