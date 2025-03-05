using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core;
using Kardx.Core.Planning;
using Kardx.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI
{
    public class MatchView : MonoBehaviour
    {
        [Header("References")]
        private MatchManager matchManager;

        [Header("Layout Areas")]
        [SerializeField]
        private Transform playerHandArea;

        [SerializeField]
        private Transform playerBattlefieldArea;

        [SerializeField]
        private Transform opponentHandArea;

        [SerializeField]
        private Transform opponentBattlefieldArea;

        [SerializeField]
        private Transform playerHeadquarter;

        [SerializeField]
        private Transform opponentHeadquarter;

        [SerializeField]
        private Transform orderArea;

        [Header("UI Elements")]

        [SerializeField]
        private TextMeshProUGUI turnText;

        [SerializeField]
        private TextMeshProUGUI playerCreditsText;

        [SerializeField]
        private TextMeshProUGUI opponentCreditsText;

        [SerializeField]
        private TextMeshProUGUI logText;

        [Header("View Components")]
        // References to the actual components - set during initialization
        private PlayerBattlefieldView playerBattlefieldView;
        private OpponentBattlefieldView opponentBattlefieldView;
        private HandView playerHandView;
        private HandView opponentHandView;

        [SerializeField]
        private CardDetailView cardDetailView;

        // Public accessor for MatchManager
        public MatchManager MatchManager => matchManager;

        private void Awake()
        {
            // Create MatchManager instance first
            matchManager = new MatchManager(new SimpleLogger("[MatchManager]"));

            // Initialize the match right away - this creates the board and players
            matchManager.Initialize();

            // Ensure the CardPanel is initially active so it can run coroutines
            // but immediately hide it
            if (!cardDetailView.gameObject.activeSelf)
            {
                cardDetailView.gameObject.SetActive(true);
                cardDetailView.Hide();
            }

            // Initialize the shared reference
            CardView.InitializeSharedDetailView(cardDetailView);

            // Make sure the CardPanel is initially inactive
            cardDetailView.gameObject.SetActive(false);

            // Now we can initialize all view components
            InitializeViewComponents();
        }

        private void InitializeViewComponents()
        {
            // Initialize battlefield views
            if (playerBattlefieldArea != null)
            {
                playerBattlefieldView = playerBattlefieldArea.GetComponent<PlayerBattlefieldView>();
                if (playerBattlefieldView != null)
                {
                    playerBattlefieldView.Initialize(matchManager);
                }
                else
                {
                    Debug.LogError("PlayerBattlefieldView component not found on playerBattlefieldArea. Please add this component in the Unity Editor.");
                }
            }

            if (opponentBattlefieldArea != null)
            {
                opponentBattlefieldView = opponentBattlefieldArea.GetComponent<OpponentBattlefieldView>();
                if (opponentBattlefieldView != null)
                {
                    opponentBattlefieldView.Initialize(matchManager);
                }
                else
                {
                    Debug.LogError("OpponentBattlefieldView component not found on opponentBattlefieldArea. Please add this component in the Unity Editor.");
                }
            }

            // Initialize hand views - should be set up in Unity Editor
            if (playerHandArea != null)
            {
                playerHandView = playerHandArea.GetComponent<HandView>();
                if (playerHandView != null)
                {
                    playerHandView.Initialize(matchManager);
                }
                else
                {
                    Debug.LogError("HandView component not found on playerHandArea. Please add this component in the Unity Editor.");
                }
            }

            if (opponentHandArea != null)
            {
                opponentHandView = opponentHandArea.GetComponent<HandView>();
                if (opponentHandView != null)
                {
                    opponentHandView.Initialize(matchManager);
                }
                else
                {
                    Debug.LogError("HandView component not found on opponentHandArea. Please add this component in the Unity Editor.");
                }
            }
        }

        private void Start()
        {
            // Set up the match
            SetupMatch();
        }

        private void SetupMatch()
        {
            // Subscribe to MatchManager events
            SetupEventHandlers();

            // Start the match
            matchManager.StartMatch();

            // Initialize the UI
            UpdateUI();
        }

        private void SetupEventHandlers()
        {
            if (matchManager != null)
            {
                // Set up event subscriptions
                matchManager.OnCardDeployed += HandleCardDeployed;
                matchManager.OnTurnStarted += HandleTurnStarted;
                matchManager.OnTurnEnded += HandleTurnEnded;
                matchManager.OnAttackCompleted += HandleAttackCompleted;
                matchManager.OnProcessAITurn += HandleProcessAITurn;
                matchManager.OnCardDrawn += HandleCardDrawn;
                matchManager.OnCardDied += HandleCardDied;
                matchManager.OnMatchStarted += HandleMatchStarted;
                matchManager.OnMatchEnded += HandleMatchEnded;
            }
        }

        private void HandleTurnStarted(object sender, Player player)
        {
            Debug.Log($"[MatchView] Turn started for {(player == matchManager.Player ? "Player" : "Opponent")}");
            UpdateTurnDisplay();
            UpdateCreditsDisplay();
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield();
            }
            else if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield();
            }
        }

        private void HandleTurnEnded(object sender, Player player)
        {
            Debug.Log($"[MatchView] Turn ended for {(player == matchManager.Player ? "Player" : "Opponent")}");
        }

        private void UpdateTurnDisplay()
        {
            if (turnText == null || matchManager == null)
                return;

            var currentPlayer = matchManager.CurrentPlayer;
            bool isPlayerTurn = currentPlayer == matchManager.Player;

            turnText.text = isPlayerTurn ? "Your Turn" : "Opponent's Turn";
        }

        private void UpdateCreditsDisplay()
        {
            if (playerCreditsText == null || opponentCreditsText == null || matchManager == null)
                return;

            var player = matchManager.Player;
            var opponent = matchManager.Opponent;

            if (player != null)
                playerCreditsText.text = $"Credits: {player.Credits}";

            if (opponent != null)
                opponentCreditsText.text = $"Credits: {opponent.Credits}";
        }

        private void HandleCardDeployed(Card card, int slotIndex)
        {
            Debug.Log($"[MatchView] Card deployed: {card.Title} at slot {slotIndex}");

            if (playerBattlefieldView != null && card.Owner == matchManager.Player)
            {
                playerBattlefieldView.OnCardDeployed(card, slotIndex);
            }

            if (opponentBattlefieldView != null && card.Owner == matchManager.Opponent)
            {
                opponentBattlefieldView.OnCardDeployed(card, slotIndex);
            }
            UpdateCreditsDisplay();
        }

        private void HandleAttackCompleted(Card attackerCard, Card targetCard, int damageDealt, int remainingHealth)
        {
            Debug.Log($"[MatchView] Attack completed: {attackerCard.Title} -> {targetCard.Title} - Damage: {damageDealt} - Remaining Health: {remainingHealth}");

            // Update only the battlefields that were affected
            if (attackerCard.Owner == matchManager.Player && playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield();
            }
            else if (attackerCard.Owner == matchManager.Opponent && opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield();
            }

            if (targetCard.Owner == matchManager.Player && playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield();
            }
            else if (targetCard.Owner == matchManager.Opponent && opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield();
            }

            UpdateCreditsDisplay(); // Attack spends player credits so should update
        }

        private void HandleProcessAITurn(Board board, StrategyPlanner planner)
        {
            Debug.Log("[MatchView] Processing AI turn");

            // Execute the AI strategy directly without coroutine
            ExecuteAIStrategy(board, planner);
        }

        private void ExecuteAIStrategy(Board board, StrategyPlanner planner)
        {
            Debug.Log("[MatchView] Executing AI strategy");

            // Execute the AI's strategy synchronously
            planner.ExecuteNextStrategy(board);

            Debug.Log("[MatchView] AI strategy execution completed");

            // Update turn and credits display
            UpdateTurnDisplay();
            UpdateCreditsDisplay();
        }

        // Method to update the entire UI - this is now thinner because each component handles its own updates
        public void UpdateUI()
        {
            if (matchManager == null)
                return;

            // Update hand views
            if (playerHandView != null)
            {
                playerHandView.UpdateHand();
            }

            if (opponentHandView != null)
            {
                opponentHandView.UpdateHand();
            }

            // Update battlefield views
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield();
            }

            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield();
            }

            // Update credits and turn display
            UpdateCreditsDisplay();
            UpdateTurnDisplay();
        }

        // Method to clear all highlights - delegates to battlefield views
        public void ClearAllHighlights()
        {
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.ClearCardHighlights();
            }

            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.ClearCardHighlights();
            }
        }

        // Method to end the player's turn
        public void EndTurn()
        {
            if (matchManager == null)
                return;

            matchManager.NextTurn();

            // Update turn and credits display
            UpdateTurnDisplay();
            UpdateCreditsDisplay();
        }

        private void HandleMatchStarted(string message)
        {
            Debug.Log($"[MatchView] Match started: {message}");

            // Update specific UI components based on their initial state
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield();
            }

            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield();
            }

            if (playerHandView != null)
            {
                playerHandView.UpdateHand(matchManager.Player.Hand);
            }

            if (opponentHandView != null)
            {
                opponentHandView.UpdateHand(matchManager.Opponent.Hand);
            }

            // Update turn display
            UpdateTurnDisplay();
        }

        private void HandleMatchEnded(string message)
        {
            Debug.Log($"[MatchView] Match ended: {message}");
        }

        private void HandleCardDrawn(Card card)
        {
            Debug.Log($"[MatchView] Card drawn: {card.Title}");

            // For cards drawn by the player, update the player's hand
            if (card.Owner == matchManager.Player && playerHandView != null)
            {
                // Add just the new card to the hand visualization
                playerHandView.AddCardToHand(card);
                Debug.Log($"[MatchView] Added card to player hand: {card.Title}");
            }
            else if (card.Owner == matchManager.Opponent && opponentHandView != null)
            {
                // Add the card to the opponent's hand visualization
                opponentHandView.AddCardToHand(card);
                Debug.Log($"[MatchView] Added card to opponent hand: {card.Title}");
            }

            // We don't need to call UpdateUI() since we've made the specific updates needed
        }

        private void HandleCardDied(Card card)
        {
            Debug.Log($"[MatchView] Card died: {card.Title}");

            // Remove the card from the battlefield view
            if (card.Owner == matchManager.Player && playerBattlefieldView != null)
            {
                // Remove the card from the player's battlefield view
                playerBattlefieldView.RemoveCard(card);
                Debug.Log($"[MatchView] Removed card from player battlefield: {card.Title}");
            }
            else if (card.Owner == matchManager.Opponent && opponentBattlefieldView != null)
            {
                // Remove the card from the opponent's battlefield view
                opponentBattlefieldView.RemoveCard(card);
                Debug.Log($"[MatchView] Removed card from opponent battlefield: {card.Title}");
            }

            // Update UI elements that depend on the battlefield state
            UpdateCreditsDisplay();
        }

        private void OnDestroy()
        {
            // Unsubscribe from MatchManager events to prevent memory leaks
            if (matchManager != null)
            {
                matchManager.OnCardDeployed -= HandleCardDeployed;
                matchManager.OnTurnStarted -= HandleTurnStarted;
                matchManager.OnTurnEnded -= HandleTurnEnded;
                matchManager.OnAttackCompleted -= HandleAttackCompleted;
                matchManager.OnProcessAITurn -= HandleProcessAITurn;
                matchManager.OnCardDrawn -= HandleCardDrawn;
                matchManager.OnCardDied -= HandleCardDied;
                matchManager.OnMatchStarted -= HandleMatchStarted;
                matchManager.OnMatchEnded -= HandleMatchEnded;
            }
        }

        public void SetLogText(string message)
        {
            if (logText != null)
            {
                logText.text = message;
            }
        }
    }
}
