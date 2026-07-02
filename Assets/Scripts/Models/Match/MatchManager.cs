using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Utils;
using Kardx.UI;
using UnityEngine;
using Kardx.Managers;
using Kardx.Planning;
using Kardx.Acting;
using Kardx.Models;
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
        private AbilitySystem abilitySystem;
        private List<CardType> cardTypeCatalog;
        private Card pendingEnemyOrder;
        private bool awaitingOrderResolution;

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
        public event Action<string, string> OnMatchWon;

        public event Action<Card> OnEnemyOrderPending;
        public event Action<Card> OnOrderCountered;
        public event Action OnPendingOrderResolved;

        // Ability-related events
        public event Action<Card, Card, int, int> OnAttackCompleted;

        // Generic event to trigger UI updates when needed
        public event System.Action OnUIUpdateNeeded;

        // Event for signaling when AI needs to process its turn
        public event Action<Board, StrategyPlanner> OnProcessAITurn;

        // Reference to ViewManager instead of ViewRegistry directly
        private ViewManager viewManager;

        // Reference to the CardRegistry for tracking card views
        private ViewRegistry viewRegistry;

        public ViewManager ViewManager => viewManager;
        public ViewRegistry ViewRegistry => viewRegistry;
        public AbilitySystem AbilitySystem => abilitySystem;
        public bool HasPendingEnemyOrder => pendingEnemyOrder != null;
        public Card PendingEnemyOrder => pendingEnemyOrder;
        public bool AwaitingOrderResolution => awaitingOrderResolution;

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
            cardTypeCatalog = CardLoader.LoadCardTypes();

            var player = new Player(
                "Player",
                LoadDeck(Faction.UnitedStates),
                Faction.UnitedStates
            );
            var opponent = new Player(
                "Opponent",
                LoadDeck(Faction.SovietUnion),
                Faction.SovietUnion
            );

            board = new Board(player, opponent);

            abilitySystem = new AbilitySystem(this);

            SetupHeadquarters(player, Faction.UnitedStates);
            SetupHeadquarters(opponent, Faction.SovietUnion);

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
            if (board == null)
            {
                Initialize();
            }

            IsMatchInProgress = true;

            DealOpeningHands();

            OnMatchStarted?.Invoke("Match started");

            StartTurn();
        }

        private void DealOpeningHands()
        {
            for (int i = 0; i < GameConstants.StartingHandSize; i++)
            {
                var playerCard = DrawCard(board.Player);
                if (playerCard != null)
                    OnCardDrawn?.Invoke(playerCard);

                var opponentCard = DrawCard(board.Opponent);
                if (opponentCard != null)
                    OnCardDrawn?.Invoke(opponentCard);
            }
        }

        private void SetupHeadquarters(Player player, Faction faction)
        {
            var hqCard = DeckBuilder.CreateHeadquarters(faction, cardTypeCatalog);
            player.SetHeadquarters(hqCard);
            abilitySystem.RegisterCardAbilities(hqCard);
        }

        /// <summary>
        /// Starts a turn for the current player
        /// </summary>
        private void StartTurn()
        {
            board.StartTurn();

            abilitySystem.ProcessTurnStart(board.CurrentTurnPlayer);

            Card drawnCard = DrawCard(board.CurrentTurnPlayer);
            if (drawnCard != null)
            {
                logger?.Log($"[{CurrentPlayerId}] Card drawn: {drawnCard.Title}");
                OnCardDrawn?.Invoke(drawnCard);
                TriggerUIUpdateNeeded();
            }

            OnTurnStarted?.Invoke(this, board.CurrentTurnPlayer);

            if (board.TurnNumber >= GameConstants.MaxTurns)
            {
                EndMatch("Maximum turns reached");
                return;
            }

            if (board.IsOpponentTurn())
            {
                logger?.Log("[MatchManager] Let the opponent run the turn");

                // Execute AI strategy
                strategyPlanner?.ExecuteNextStrategy(board);

                OnProcessAITurn?.Invoke(board, strategyPlanner);

                if (!awaitingOrderResolution)
                {
                    logger?.Log("[MatchManager] AI turn complete, ending turn");
                    NextTurn();
                }
                else
                {
                    logger?.Log("[MatchManager] AI turn paused - awaiting order resolution");
                }
            }
        }

        /// <summary>
        /// Ends the current turn
        /// </summary>
        private void EndTurn()
        {
            if (board == null)
                return;

            var currentPlayer = board.CurrentTurnPlayer;
            logger?.Log($"[MatchManager] Ending turn for player {currentPlayer.Id}");

            abilitySystem.ProcessTurnEnd(currentPlayer);

            board.ProcessEndOfTurnEffects();

            // Notify listeners that the turn is ending
            OnTurnEnded?.Invoke(this, currentPlayer);

            // End the current turn in the board
            board.EndTurn();

            // Process start of turn effects for the next turn
            board.ProcessStartOfTurnEffects();
        }

        /// <summary>
        /// Advances to the next turn in the game.
        /// </summary>
        public void NextTurn()
        {
            if (board == null)
                return;

            // End the current turn
            EndTurn();

            // Get the new current player after the board has switched
            var nextPlayer = board.CurrentTurnPlayer;

            logger?.Log(
                $"[MatchManager] Starting turn {board.TurnNumber} for player {nextPlayer.Id}"
            );

            // Start the turn logic
            StartTurn();

            TriggerUIUpdateNeeded();
        }

        private List<Card> LoadDeck(Faction ownerFaction)
        {
            if (cardTypeCatalog == null)
                cardTypeCatalog = CardLoader.LoadCardTypes();

            return DeckBuilder.BuildDeck(ownerFaction, cardTypeCatalog);
        }

        public void EndMatch(string reason = null)
        {
            if (!IsMatchInProgress)
                return;

            IsMatchInProgress = false;
            string message = reason ?? $"Match ended after {TurnNumber} turns";
            OnMatchEnded?.Invoke(message);
            logger?.Log(message);
        }

        public void DeclareWinner(string winnerId, string reason)
        {
            if (!IsMatchInProgress)
                return;

            IsMatchInProgress = false;
            OnMatchWon?.Invoke(winnerId, reason);
            OnMatchEnded?.Invoke($"{winnerId} wins: {reason}");
            logger?.Log($"{winnerId} wins: {reason}");
        }

        /// <summary>
        /// Draws a card for the specified player
        /// </summary>
        public Card DrawCard(Player player)
        {
            if (player == null) return null;

            // Only opponent cards are face down
            bool faceDown = player == board.Opponent;
            var drawnCard = player.DrawCard(faceDown);

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
                abilitySystem.RegisterCardAbilities(card);
                abilitySystem.ProcessCardDeployed(card);
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
                if (currentPlayer == Opponent)
                {
                    pendingEnemyOrder = card;
                    awaitingOrderResolution = true;
                    OnEnemyOrderPending?.Invoke(card);
                    OnCardDeployed?.Invoke(card, -1);
                    OnCardDiscarded?.Invoke(card);
                }
                else
                {
                    abilitySystem.ProcessCardDeployed(card);
                    OnCardDeployed?.Invoke(card, -1);
                    OnCardDiscarded?.Invoke(card);
                }
                TriggerUIUpdateNeeded();
            }
            return success;
        }

        public bool ResolvePendingEnemyOrder()
        {
            if (pendingEnemyOrder == null)
                return false;

            var order = pendingEnemyOrder;
            pendingEnemyOrder = null;
            awaitingOrderResolution = false;

            abilitySystem.ProcessCardDeployed(order);
            OnPendingOrderResolved?.Invoke();
            TriggerUIUpdateNeeded();

            if (board.IsOpponentTurn() && IsMatchInProgress)
            {
                NextTurn();
            }

            return true;
        }

        public bool CanPlayCountermeasure(Card card)
        {
            if (!IsMatchInProgress || card == null || pendingEnemyOrder == null)
                return false;

            if (!Player.Hand.Contains(card))
                return false;

            if (card.CardType.Category != CardCategory.Countermeasure)
                return false;

            return Player.Credits >= card.DeploymentCost;
        }

        public bool PlayCountermeasure(Card card)
        {
            if (!CanPlayCountermeasure(card))
                return false;

            if (!Player.PlayCountermeasureCard(card))
                return false;

            pendingEnemyOrder = null;
            awaitingOrderResolution = false;

            OnOrderCountered?.Invoke(card);
            OnCardDiscarded?.Invoke(card);
            OnPendingOrderResolved?.Invoke();
            TriggerUIUpdateNeeded();

            if (board.IsOpponentTurn() && IsMatchInProgress)
            {
                NextTurn();
            }

            return true;
        }

        public bool CanMoveUnit(Card card, int toSlotIndex)
        {
            if (!IsMatchInProgress || card == null)
                return false;

            if (!IsPlayerTurn())
                return false;

            if (card.Owner != Player)
                return false;

            if (!Player.Battlefield.Contains(card))
                return false;

            if (toSlotIndex < 0 || toSlotIndex >= Battlefield.SLOT_COUNT)
                return false;

            return Player.Battlefield.IsSlotEmpty(toSlotIndex);
        }

        public bool MoveUnit(Card card, int toSlotIndex)
        {
            if (!CanMoveUnit(card, toSlotIndex))
                return false;

            bool success = Player.MoveUnitOnBattlefield(card, toSlotIndex);
            if (success)
            {
                TriggerUIUpdateNeeded();
            }
            return success;
        }

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
            if (!IsMatchInProgress || attackerCard == null || defenderCard == null || board == null)
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

            abilitySystem.ProcessAttack(attackerCard, defenderCard);

            int attackDamage = attackerCard.Attack;
            int counterAttackDamage = defenderCard.CounterAttack;

            defenderCard.TakeDamage(attackDamage);
            abilitySystem.ProcessCardDamaged(defenderCard, attackDamage, attackerCard);

            if (defenderCard.CurrentDefense > 0)
            {
                attackerCard.TakeDamage(counterAttackDamage);
                abilitySystem.ProcessCardDamaged(attackerCard, counterAttackDamage, defenderCard);
                abilitySystem.ProcessDefend(defenderCard, attackerCard);
            }

            attackerCard.Owner.SpendCredits(attackerCard.OperationCost);

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

        public bool InitiateAttackOnHQ(Card attackerCard, Player defendingPlayer)
        {
            if (attackerCard == null || defendingPlayer == null || board == null)
                return false;

            var hq = defendingPlayer.Headquarter;
            if (hq == null || !hq.IsAlive)
                return false;

            if (!CanAttackHQ(attackerCard, defendingPlayer))
                return false;

            attackerCard.HasAttackedThisTurn = true;
            abilitySystem.ProcessAttack(attackerCard, hq);

            int attackDamage = attackerCard.Attack;
            hq.TakeDamage(attackDamage);
            abilitySystem.ProcessCardDamaged(hq, attackDamage, attackerCard);

            attackerCard.Owner.SpendCredits(attackerCard.OperationCost);

            CheckHQDeath(defendingPlayer);

            OnAttackCompleted?.Invoke(attackerCard, hq, attackDamage, hq.CurrentDefense);
            TriggerUIUpdateNeeded();
            return true;
        }

        public bool CanAttack(Card attackerCard, Card defenderCard)
        {
            if (!IsMatchInProgress || attackerCard == null || defenderCard == null)
                return false;

            if (attackerCard.OperationCost > attackerCard.Owner.Credits)
                return false;

            var defendingPlayer = defenderCard.Owner;
            return CombatRules.IsValidAttackTarget(attackerCard, defenderCard, defendingPlayer);
        }

        public bool CanAttackHQ(Card attackerCard, Player defendingPlayer)
        {
            if (!IsMatchInProgress || attackerCard == null || defendingPlayer == null)
                return false;

            if (attackerCard.OperationCost > attackerCard.Owner.Credits)
                return false;

            return CombatRules.CanAttackHQ(attackerCard, defendingPlayer);
        }

        private void CheckHQDeath(Player defendingPlayer)
        {
            var hq = defendingPlayer.Headquarter;
            if (hq == null || hq.IsAlive)
                return;

            var winner = defendingPlayer == board.Player ? board.Opponent : board.Player;
            DeclareWinner(winner.Id, $"Destroyed {defendingPlayer.Id}'s Headquarters");
        }
        private void CheckCardDeath(Card card)
        {
            HandleCardDeath(card);
        }

        public void HandleCardDeath(Card card)
        {
            if (card == null || card.CurrentDefense > 0)
                return;

            if (card.IsHeadquarters)
            {
                CheckHQDeath(card.Owner);
                return;
            }

            if (card.Owner == null || !card.Owner.Battlefield.Contains(card))
                return;

            abilitySystem.ProcessCardDestroyed(card);
            card.Owner.RemoveFromBattlefield(card);

            logger?.Log($"Card {card.Title} has died and been moved to the discard pile");

            OnCardDied?.Invoke(card);
            GameStateValidator.LogValidationResults(board);
            TriggerUIUpdateNeeded();
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
            GameStateValidator.LogValidationResults(board);
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
