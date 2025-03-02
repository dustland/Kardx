using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core.Planning;
using Kardx.Utils;

namespace Kardx.Core
{
    /// <summary>
    /// Manages the game match, handling turns and player actions.
    /// </summary>
    public class MatchManager
    {
        private Kardx.Utils.ILogger logger;
        private Board board;
        private StrategyPlanner strategyPlanner;
        private int startingHandSize = 4;
        private int maxTurns = 50;
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
        public event Action<Card> OnCardDiscarded;
        public event Action<Strategy> OnStrategyDetermined;
        public event Action<Decision> OnDecisionExecuting;
        public event Action<Decision> OnDecisionExecuted;
        public event EventHandler<Player> OnTurnStarted;
        public event EventHandler<Player> OnTurnEnded;
        public event Action<string> OnMatchStarted;
        public event Action<string> OnMatchEnded;

        // Ability-related events
        public event Action<Card, Card, int, int> OnAttackCompleted;
        public event Action<Card> OnCardDied;

        // Event for signaling when AI needs to process its turn
        public event Action<Board, StrategyPlanner, Action> OnProcessAITurn;

        /// <summary>
        /// Creates a new instance of the MatchManager class.
        /// </summary>
        /// <param name="logger">Optional logger for debugging.</param>
        public MatchManager(Kardx.Utils.ILogger logger = null)
        {
            // Use the SimpleLogger if no logger is provided
            this.logger = logger ?? new SimpleLogger("MatchManager");
        }

        /// <summary>
        /// Starts a new match with default players.
        /// </summary>
        public void StartMatch()
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
            strategyPlanner = new StrategyPlanner(StrategySource.Dummy, player2, this, logger);

            // Subscribe to strategy planner events
            if (strategyPlanner != null)
            {
                strategyPlanner.OnStrategyDetermined += (strategy) =>
                    OnStrategyDetermined?.Invoke(strategy);

                strategyPlanner.OnDecisionExecuting += (decision) =>
                    OnDecisionExecuting?.Invoke(decision);

                strategyPlanner.OnDecisionExecuted += (decision) =>
                    OnDecisionExecuted?.Invoke(decision);

                // Subscribe to additional events for opponent actions
                SubscribeToStrategyPlannerEvents(strategyPlanner);
            }

            logger?.Log($"[MatchManager] Match started between {player1.Id} and {player2.Id}");
            IsMatchInProgress = true;
            OnMatchStarted?.Invoke($"Match started between {player1.Id} and {player2.Id}");

            // Start the first turn
            if (board == null)
                throw new InvalidOperationException("Match not started");

            var currentPlayer = board.CurrentTurnPlayer;
            logger?.Log($"[MatchManager] Starting first turn for player {currentPlayer.Id}");

            // Initialize the player for the first turn
            // Add credits based on the turn number
            int creditsToAdd = CREDITS_PER_TURN * board.TurnNumber;
            currentPlayer.AddCredits(creditsToAdd);
            logger?.Log(
                $"[MatchManager] Added {creditsToAdd} credits to player {currentPlayer.Id}"
            );

            // Draw a card for the player
            Card drawnCard = currentPlayer.DrawCard();
            if (drawnCard != null)
            {
                OnCardDrawn?.Invoke(drawnCard);
                logger?.Log($"[{CurrentPlayerId}] Card drawn: {drawnCard.Title}");
            }

            // Notify listeners that a new turn is starting
            OnTurnStarted?.Invoke(this, currentPlayer);

            // Reset card attack status
            currentPlayer.ResetCardAttackStatus();

            // If it's the opponent's turn, process it automatically
            if (currentPlayer.Id == board.Opponent.Id)
            {
                ProcessOpponentTurn();
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
            board.SwitchCurrentPlayer();
            board.IncrementTurnNumber();

            var nextPlayer = board.CurrentTurnPlayer;
            logger?.Log(
                $"[MatchManager] Starting turn {board.TurnNumber} for player {nextPlayer.Id}"
            );

            // Initialize the player for the new turn
            // Add credits based on the turn number
            int creditsToAdd = CREDITS_PER_TURN * board.TurnNumber;
            nextPlayer.AddCredits(creditsToAdd);
            logger?.Log($"[MatchManager] Added {creditsToAdd} credits to player {nextPlayer.Id}");

            // Draw a card for the player
            Card drawnCard = nextPlayer.DrawCard(nextPlayer.Id == board.Opponent.Id);
            if (drawnCard != null)
            {
                OnCardDrawn?.Invoke(drawnCard);
                logger?.Log($"[{CurrentPlayerId}] Card drawn: {drawnCard.Title}");
            }

            // Process start of turn effects
            board.ProcessStartOfTurnEffects();

            // Notify listeners that a new turn is starting
            OnTurnStarted?.Invoke(this, nextPlayer);

            // Reset card attack status for both players
            board.Player.ResetCardAttackStatus();
            board.Opponent.ResetCardAttackStatus();

            // If it's the opponent's turn, process it automatically
            if (nextPlayer.Id == board.Opponent.Id)
            {
                ProcessOpponentTurn();
            }
        }

        /// <summary>
        /// Processes the opponent's turn automatically.
        /// </summary>
        private void ProcessOpponentTurn()
        {
            if (board == null)
                throw new InvalidOperationException("Match not started");

            logger?.Log("Processing opponent turn");

            // Trigger the event for the UI layer to handle AI processing
            OnProcessAITurn?.Invoke(board, strategyPlanner, NextTurn);
        }

        private List<Card> LoadDeck(Faction ownerFaction)
        {
            var cardTypes = CardLoader.LoadCardTypes();
            var deck = new List<Card>();

            // Shuffle the card types before adding them to the deck
            var shuffledCardTypes = cardTypes.OrderBy(x => Guid.NewGuid()).ToList();

            foreach (var cardType in shuffledCardTypes)
            {
                // TODO: Add deck building rules here (e.g., card limits, faction restrictions)
                deck.Add(new Card(cardType, ownerFaction));
            }

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

            int occupiedSlots = 0;
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                if (!currentPlayer.Battlefield.IsSlotEmpty(i))
                {
                    occupiedSlots++;
                }
            }

            return currentPlayer.Hand.Contains(card)
                && currentPlayer.Credits >= card.DeploymentCost
                && occupiedSlots < Battlefield.SLOT_COUNT;
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

        public bool DeployUnitCard(Card card, int position)
        {
            if (!CanDeployUnitCard(card))
            {
                // Add more detailed logging to help diagnose the issue
                var player = board.CurrentTurnPlayer;
                if (card == null)
                {
                    logger?.LogError("[MatchManager] Cannot deploy unit card: card is null");
                }
                else if (!player.Hand.Contains(card))
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: not in player's hand"
                    );
                }
                else if (player.Credits < card.DeploymentCost)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: insufficient credits (has {player.Credits}, needs {card.DeploymentCost})"
                    );
                }
                else if (position < 0 || position >= Battlefield.SLOT_COUNT)
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: invalid position {position}"
                    );
                }
                else if (!player.Battlefield.IsSlotEmpty(position))
                {
                    logger?.LogError(
                        $"[MatchManager] Cannot deploy unit card {card.Title}: position {position} is already occupied"
                    );
                }
                return false;
            }

            var currentPlayer = board.CurrentTurnPlayer;
            return currentPlayer.DeployUnitCard(card, position);
        }

        public bool DeployOrderCard(Card card)
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
            // Use a special position value (-1) for order cards
            return currentPlayer.DeployOrderCard(card);
        }

        // For backward compatibility
        public bool DeployCard(Card card, int position)
        {
            if (card == null)
                return false;

            return card.CardType.Category == CardCategory.Unit ? DeployUnitCard(card, position)
                : card.CardType.Category == CardCategory.Order ? DeployOrderCard(card)
                : false;
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
                return false;

            // Check if the cards belong to different players
            if (attackerCard.Owner == defenderCard.Owner)
                return false;

            // Check if both cards are on the battlefield
            bool attackerOnBattlefield = attackerCard.Owner.Battlefield.Contains(attackerCard);
            bool defenderOnBattlefield = defenderCard.Owner.Battlefield.Contains(defenderCard);

            if (!attackerOnBattlefield || !defenderOnBattlefield)
                return false;

            // Add any other attack validation rules here

            return true;
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

            // Check if the attack is valid
            if (!CanAttack(attackerCard, defenderCard))
            {
                logger?.Log($"Invalid attack from {attackerCard.Title} to {defenderCard.Title}");
                return false;
            }

            // Process the attack
            return ProcessAttack(attackerCard, defenderCard);
        }

        /// <summary>
        /// Processes an attack between two cards
        /// </summary>
        /// <param name="attackerCard">The attacking card</param>
        /// <param name="defenderCard">The defending card</param>
        /// <returns>True if the attack was processed successfully, false otherwise</returns>
        public bool ProcessAttack(Card attackerCard, Card defenderCard)
        {
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
            if (defenderCard.CurrentDefence > 0)
            {
                attackerCard.TakeDamage(counterAttackDamage);
            }

            // Notify listeners about the attack
            OnAttackCompleted?.Invoke(
                attackerCard,
                defenderCard,
                attackDamage,
                counterAttackDamage
            );

            // Check if any cards died as a result of the attack
            CheckCardDeath(attackerCard);
            CheckCardDeath(defenderCard);

            return true;
        }

        /// <summary>
        /// Checks if a card has died and handles its death
        /// </summary>
        /// <param name="card">The card to check</param>
        private void CheckCardDeath(Card card)
        {
            if (card.CurrentDefence <= 0)
            {
                // Remove the card from the battlefield using the proper method
                card.Owner.RemoveFromBattlefield(card);

                logger?.Log($"Card {card.Title} has died and been moved to the discard pile");

                // Fire event to notify UI
                OnCardDied?.Invoke(card);
            }
        }

        private void SubscribeToStrategyPlannerEvents(StrategyPlanner planner)
        {
            if (planner == null)
                return;

            // Since we're now using matchManager.DeployCard directly in the StrategyPlanner,
            // we don't need to handle card deployments here anymore as the OnCardDeployed event
            // will be triggered automatically by the DeployCard method.

            // We can still handle other decision types here if needed
            planner.OnDecisionExecuted += (decision) => {
                // Handle other decision types if needed
                // For example, card discards, attacks, etc.
            };
        }
    }
}
