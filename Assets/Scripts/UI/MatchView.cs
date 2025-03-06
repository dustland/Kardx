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

        [Header("Debug Tools")]
        [SerializeField]
        private Button validateStateButton;

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

            // Set up the validate state button
            if (validateStateButton != null)
            {
                validateStateButton.onClick.AddListener(ValidateGameState);
            }
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

            turnText.text = isPlayerTurn ? $"Your Turn: {matchManager.TurnNumber}" : $"Opponent's Turn: {matchManager.TurnNumber}";
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

            // For order cards, we don't need to update the battlefield
            if (card.CardType.Category == CardCategory.Order)
            {
                Debug.Log($"[MatchView] Deployed an order card: {card.Title}");

                // Find the card GameObject that was dragged
                CardView detachedCardView = null;
                foreach (CardView cardView in FindObjectsByType<CardView>(FindObjectsSortMode.None))
                {
                    // Check if this card view represents our card and is likely detached (at the root)
                    if (cardView.Card == card && cardView.transform.parent == cardView.transform.root)
                    {
                        detachedCardView = cardView;
                        break;
                    }
                }

                if (detachedCardView != null && orderArea != null)
                {
                    // Move the card to the order zone
                    detachedCardView.transform.SetParent(orderArea, false);
                    detachedCardView.transform.localPosition = Vector3.zero;

                    // Start a coroutine to handle the order card animation
                    StartCoroutine(ProcessOrderCard(detachedCardView.gameObject));
                }
                else
                {
                    Debug.LogWarning($"[MatchView] Could not find detached card view for order card {card.Title} or order area is null");
                }

                // Update credits display
                UpdateCreditsDisplay();

                // Update the player's hand
                if (card.Owner == matchManager.Player && playerHandView != null)
                {
                    playerHandView.UpdateHand();
                }
                else if (card.Owner == matchManager.Opponent && opponentHandView != null)
                {
                    opponentHandView.UpdateHand();
                }
                return;
            }

            // Handle unit cards deployment to battlefield
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

        private IEnumerator ProcessOrderCard(GameObject orderCardObject)
        {
            if (orderCardObject == null)
                yield break;

            // Wait for 2 seconds to simulate the order effect
            yield return new WaitForSeconds(2f);

            // Destroy the card object after the delay
            Destroy(orderCardObject);

            Debug.Log("[MatchView] Order card effect completed and card removed");
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
            ValidateGameState();

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

        /// <summary>
        /// Validates the consistency between UI representation and the data model
        /// </summary>
        public void ValidateGameState()
        {
            if (matchManager == null)
            {
                SetLogText("Cannot validate: MatchManager is null");
                return;
            }

            List<string> inconsistencies = new List<string>();

            // Validate player hand
            ValidatePlayerHand(inconsistencies);

            // Validate player battlefield
            ValidatePlayerBattlefield(inconsistencies);

            // Validate opponent hand
            ValidateOpponentHand(inconsistencies);

            // Validate opponent battlefield
            ValidateOpponentBattlefield(inconsistencies);

            // Validate order area
            ValidateOrderArea(inconsistencies);

            // Validate player resources
            ValidatePlayerResources(inconsistencies);

            // Display results
            if (inconsistencies.Count == 0)
            {
                SetLogText("VALIDATION PASSED: UI is consistent with data model");
            }
            else
            {
                string result = $"VALIDATION FAILED: Found {inconsistencies.Count} inconsistencies:\n";
                foreach (var issue in inconsistencies.Take(10)) // Limit to 10 issues to avoid overflow
                {
                    result += $"• {issue}\n";
                }

                if (inconsistencies.Count > 10)
                {
                    result += $"...and {inconsistencies.Count - 10} more issues.";
                }

                SetLogText(result);
                Debug.LogError(result);
            }
        }

        private void ValidatePlayerHand(List<string> inconsistencies)
        {
            if (playerHandView == null) return;

            var modelCards = matchManager.Player.Hand.GetCards();
            var uiCards = playerHandView.GetCardViews().Select(cv => cv.Card).ToList();

            // Check for cards in model but missing from UI
            foreach (var card in modelCards)
            {
                if (!uiCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Player hand: Card '{card.Title}' (ID: {card.InstanceId}) exists in model but not in UI");
                }
            }

            // Check for cards in UI but missing from model
            foreach (var card in uiCards)
            {
                if (!modelCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Player hand: Card '{card.Title}' (ID: {card.InstanceId}) exists in UI but not in model");
                }
            }

            // Check if counts match
            if (modelCards.Count != uiCards.Count)
            {
                inconsistencies.Add($"Player hand: Count mismatch - Model: {modelCards.Count}, UI: {uiCards.Count}");
            }
        }

        private void ValidatePlayerBattlefield(List<string> inconsistencies)
        {
            if (playerBattlefieldView == null) return;

            var modelCards = matchManager.Player.Battlefield.Cards.ToList();
            var uiCards = playerBattlefieldView.GetCardViews().Select(cv => cv.Card).ToList();

            // Check for cards in model but missing from UI
            foreach (var card in modelCards)
            {
                if (!uiCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Player battlefield: Card '{card.Title}' (ID: {card.InstanceId}) exists in model but not in UI");
                }
            }

            // Check for cards in UI but missing from model
            foreach (var card in uiCards)
            {
                if (card != null && !modelCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Player battlefield: Card '{card.Title}' (ID: {card.InstanceId}) exists in UI but not in model");
                }
            }

            // Check if cards are in the correct positions
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                var modelCard = matchManager.Player.Battlefield.GetCardAt(i);
                var uiCardView = playerBattlefieldView.GetCardViewAt(i);
                var uiCard = uiCardView?.Card;

                if (modelCard != null && uiCard == null)
                {
                    inconsistencies.Add($"Player battlefield: Slot {i} has card '{modelCard.Title}' in model but empty in UI");
                }
                else if (modelCard == null && uiCard != null)
                {
                    inconsistencies.Add($"Player battlefield: Slot {i} is empty in model but has card '{uiCard.Title}' in UI");
                }
                else if (modelCard != null && uiCard != null && !modelCard.InstanceId.Equals(uiCard.InstanceId))
                {
                    inconsistencies.Add($"Player battlefield: Slot {i} has card '{modelCard.Title}' in model but '{uiCard.Title}' in UI");
                }
            }
        }

        private void ValidateOpponentHand(List<string> inconsistencies)
        {
            if (opponentHandView == null) return;

            var modelCards = matchManager.Opponent.Hand.GetCards();
            var uiCards = opponentHandView.GetCardViews().Select(cv => cv.Card).ToList();

            // Check card counts only, not individual cards (since opponent cards are face-down)
            if (modelCards.Count != uiCards.Count)
            {
                inconsistencies.Add($"Opponent hand: Count mismatch - Model: {modelCards.Count}, UI: {uiCards.Count}");
            }
        }

        private void ValidateOpponentBattlefield(List<string> inconsistencies)
        {
            if (opponentBattlefieldView == null) return;

            var modelCards = matchManager.Opponent.Battlefield.Cards.ToList();
            var uiCards = opponentBattlefieldView.GetCardViews().Select(cv => cv.Card).ToList();

            // Check for cards in model but missing from UI
            foreach (var card in modelCards)
            {
                if (!uiCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Opponent battlefield: Card '{card.Title}' (ID: {card.InstanceId}) exists in model but not in UI");
                }
            }

            // Check for cards in UI but missing from model
            foreach (var card in uiCards)
            {
                if (card != null && !modelCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Opponent battlefield: Card '{card.Title}' (ID: {card.InstanceId}) exists in UI but not in model");
                }
            }

            // Check if cards are in the correct positions
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                var modelCard = matchManager.Opponent.Battlefield.GetCardAt(i);
                var uiCardView = opponentBattlefieldView.GetCardViewAt(i);
                var uiCard = uiCardView?.Card;

                if (modelCard != null && uiCard == null)
                {
                    inconsistencies.Add($"Opponent battlefield: Slot {i} has card '{modelCard.Title}' in model but empty in UI");
                }
                else if (modelCard == null && uiCard != null)
                {
                    inconsistencies.Add($"Opponent battlefield: Slot {i} is empty in model but has card '{uiCard.Title}' in UI");
                }
                else if (modelCard != null && uiCard != null && !modelCard.InstanceId.Equals(uiCard.InstanceId))
                {
                    inconsistencies.Add($"Opponent battlefield: Slot {i} has card '{modelCard.Title}' in model but '{uiCard.Title}' in UI");
                }
            }
        }

        private void ValidateOrderArea(List<string> inconsistencies)
        {
            if (orderArea == null) return;

            // Get order cards from model
            // Since we don't have a direct OrderArea property, we'll check played order cards
            var playerOrderCards = matchManager.Player.GetCardsInPlay().Where(c => c.IsOrderCard).ToList();
            var opponentOrderCards = matchManager.Opponent.GetCardsInPlay().Where(c => c.IsOrderCard).ToList();

            // Get card views from order area
            var orderCardViews = orderArea.GetComponentsInChildren<CardView>(includeInactive: false);
            var uiOrderCards = orderCardViews.Select(cv => cv.Card).ToList();

            // Combine model order cards
            var allModelOrderCards = new List<Card>();
            allModelOrderCards.AddRange(playerOrderCards);
            allModelOrderCards.AddRange(opponentOrderCards);

            // Check for cards in model but missing from UI
            foreach (var card in allModelOrderCards)
            {
                if (!uiOrderCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Order area: Card '{card.Title}' (ID: {card.InstanceId}) exists in model but not in UI");
                }
            }

            // Check for cards in UI but missing from model
            foreach (var card in uiOrderCards)
            {
                if (!allModelOrderCards.Any(c => c.InstanceId.Equals(card.InstanceId)))
                {
                    inconsistencies.Add($"Order area: Card '{card.Title}' (ID: {card.InstanceId}) exists in UI but not in model");
                }
            }

            // Check total count
            if (allModelOrderCards.Count != uiOrderCards.Count)
            {
                inconsistencies.Add($"Order area: Count mismatch - Model: {allModelOrderCards.Count}, UI: {uiOrderCards.Count}");
            }
        }

        private void ValidatePlayerResources(List<string> inconsistencies)
        {
            // Check Credits display
            if (playerCreditsText != null)
            {
                int displayedCredits = 0;
                if (int.TryParse(playerCreditsText.text.Replace("Credits: ", ""), out displayedCredits))
                {
                    if (displayedCredits != matchManager.Player.Credits)
                    {
                        inconsistencies.Add($"Player credits: UI displays {displayedCredits} but model has {matchManager.Player.Credits}");
                    }
                }
            }

            if (opponentCreditsText != null)
            {
                int displayedCredits = 0;
                if (int.TryParse(opponentCreditsText.text.Replace("Credits: ", ""), out displayedCredits))
                {
                    if (displayedCredits != matchManager.Opponent.Credits)
                    {
                        inconsistencies.Add($"Opponent credits: UI displays {displayedCredits} but model has {matchManager.Opponent.Credits}");
                    }
                }
            }

            // Check turn display
            if (turnText != null)
            {
                string turnDisplay = turnText.text;
                if (turnDisplay.Contains(matchManager.TurnNumber.ToString()))
                {
                    bool isPlayerTurn = matchManager.CurrentPlayer == matchManager.Player;
                    if ((isPlayerTurn && !turnDisplay.Contains("Your Turn")) ||
                        (!isPlayerTurn && !turnDisplay.Contains("Opponent's Turn")))
                    {
                        inconsistencies.Add($"Turn indicator: Wrong player shown. Model indicates {(isPlayerTurn ? "player" : "opponent")}'s turn");
                    }
                }
                else
                {
                    inconsistencies.Add($"Turn indicator: Wrong turn number. UI shows {turnDisplay} but model has turn {matchManager.TurnNumber}");
                }
            }
        }
    }
}
