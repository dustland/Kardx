using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kardx.Core;
using Kardx.Core.Planning;
using Kardx.UI.Components;
using Kardx.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI.Scenes
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
        private GameObject playerCardSlotPrefab;

        [SerializeField]
        private GameObject opponentCardSlotPrefab;

        [SerializeField]
        private GameObject cardPrefab;

        [SerializeField]
        private TextMeshProUGUI turnText;

        [SerializeField]
        private TextMeshProUGUI playerCreditsText;

        [SerializeField]
        private TextMeshProUGUI opponentCreditsText;

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
                    playerBattlefieldView.InitializeSlots(playerCardSlotPrefab);
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
                    opponentBattlefieldView.InitializeSlots(opponentCardSlotPrefab);
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
            
            // For player cards deployed on the battlefield, attempt to place any detached card view
            if (card.Owner == matchManager.Player && playerBattlefieldView != null)
            {
                bool handled = playerBattlefieldView.DeployCard(card, slotIndex);
                Debug.Log($"[MatchView] Card deployment {(handled ? "succeeded" : "not needed")}");
                
                // Update player's hand to reflect the card being removed
                if (playerHandView != null)
                {
                    playerHandView.UpdateHand(matchManager.Player.Hand);
                }
            }
            else if (card.Owner == matchManager.Opponent && opponentBattlefieldView != null)
            {
                // Update the opponent's battlefield
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
                
                // Update opponent's hand to reflect the card being removed
                if (opponentHandView != null)
                {
                    opponentHandView.UpdateHand(matchManager.Opponent.Hand);
                }
            }
            
            // No need for UpdateUI() as we've made the specific updates needed
        }

        private void HandleAttackCompleted(Card attackerCard, Card targetCard, int damageDealt, int remainingHealth)
        {
            Debug.Log($"[MatchView] Attack completed: {attackerCard.Title} -> {targetCard.Title} - Damage: {damageDealt} - Remaining Health: {remainingHealth}");
            
            // Update only the battlefields that were affected
            if (attackerCard.Owner == matchManager.Player && playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield(matchManager.Player.Battlefield);
            }
            else if (attackerCard.Owner == matchManager.Opponent && opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
            }
            
            if (targetCard.Owner == matchManager.Player && playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield(matchManager.Player.Battlefield);
            }
            else if (targetCard.Owner == matchManager.Opponent && opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
            }
            
            // No need for UpdateUI() as we've made the specific updates needed
        }

        private void HandleProcessAITurn(Board board, StrategyPlanner planner, Action callback)
        {
            Debug.Log("[MatchView] Processing AI turn");
            
            // AI turn is handled by the MatchManager
            // After AI processing is complete, invoke the callback
            callback?.Invoke();
            
            // Update specific UI components after AI turn
            
            // Update opponent battlefield
            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
            }
            
            // Update player battlefield (in case AI affected it)
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield(matchManager.Player.Battlefield);
            }
            
            // Update opponent hand
            if (opponentHandView != null)
            {
                opponentHandView.UpdateHand(matchManager.Opponent.Hand);
            }
            
            // Update turn and credits display
            UpdateTurnDisplay();
            UpdateCreditsDisplay();
            
            // No need for UpdateUI() as we've made the specific updates needed
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
                playerBattlefieldView.UpdateBattlefield(matchManager.Player.Battlefield);
            }

            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
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
                playerBattlefieldView.ClearHighlights();
            }

            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.ClearHighlights();
            }
        }

        // Method to end the player's turn
        public void EndTurn()
        {
            if (matchManager == null)
                return;

            matchManager.NextTurn();
            
            // Update specific UI components that change on turn end
            
            // Update each battlefield
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield(matchManager.Player.Battlefield);
            }
            
            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
            }
            
            // Update turn and credits display
            UpdateTurnDisplay();
            UpdateCreditsDisplay();
            
            // No need to call UpdateUI() as we've made specific updates
        }

        private void HandleMatchStarted(string message)
        {
            Debug.Log($"[MatchView] Match started: {message}");
            
            // Update specific UI components based on their initial state
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.UpdateBattlefield(matchManager.Player.Battlefield);
            }
            
            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
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
            
            // Update only the specific battlefield that contained the card
            if (card.Owner == matchManager.Player && playerBattlefieldView != null)
            {
                // Update just the player's battlefield
                playerBattlefieldView.UpdateBattlefield(matchManager.Player.Battlefield);
                Debug.Log($"[MatchView] Updated player battlefield after card death: {card.Title}");
            }
            else if (card.Owner == matchManager.Opponent && opponentBattlefieldView != null)
            {
                // Update just the opponent's battlefield
                opponentBattlefieldView.UpdateBattlefield(matchManager.Opponent.Battlefield);
                Debug.Log($"[MatchView] Updated opponent battlefield after card death: {card.Title}");
            }
            
            // No need for UpdateUI() as we've made the specific updates needed
        }
    }
}
