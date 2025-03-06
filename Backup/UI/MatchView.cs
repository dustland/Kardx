using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kardx.Core;
using Kardx.Cards;
using System;
using System.Linq;
using TMPro;

namespace Kardx.UI
{
    /// <summary>
    /// Main view component for the match, coordinating all UI elements
    /// </summary>
    public class MatchView : MonoBehaviour
    {
        // Match components
        [Header("Match Components")]
        private MatchManager matchManager;
        private ViewManager viewManager;
        
        // Hand View references
        [Header("Hand View References")]
        [SerializeField] 
        private HandView playerHandView;
        [SerializeField] 
        private HandView opponentHandView;
        
        // Battlefield View references
        [Header("Battlefield View References")]
        [SerializeField] 
        private PlayerBattlefieldView playerBattlefieldView;
        [SerializeField] 
        private OpponentBattlefieldView opponentBattlefieldView;
        
        // UI Elements
        [Header("UI Elements")]
        [SerializeField] 
        private Button endTurnButton;
        [SerializeField] 
        private TextMeshProUGUI turnText;
        [SerializeField] 
        private TextMeshProUGUI phaseText;
        [SerializeField] 
        private TextMeshProUGUI playerResourceText;
        [SerializeField] 
        private TextMeshProUGUI opponentResourceText;
        [SerializeField] 
        private TextMeshProUGUI messageText;
        
        // State tracking
        private bool isInitialized = false;
        
        /// <summary>
        /// Initialize the match view with the match manager
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[MatchView] Initializing match view");
            
            // Find the MatchManager if it's not already set
            if (matchManager == null)
            {
                matchManager = FindObjectOfType<MatchManager>();
                if (matchManager == null)
                {
                    Debug.LogError("[MatchView] Failed to find MatchManager");
                    return;
                }
            }
            
            // Create the ViewManager if needed
            if (viewManager == null)
            {
                viewManager = gameObject.AddComponent<ViewManager>();
                if (viewManager == null)
                {
                    Debug.LogError("[MatchView] Failed to create ViewManager");
                    return;
                }
                
                // Initialize the ViewManager
                viewManager.Initialize(matchManager);
                
                // Tell the match manager about our ViewManager
                matchManager.SetViewManager(viewManager);
            }
            
            // Initialize hand views
            if (playerHandView != null)
            {
                playerHandView.Initialize(matchManager);
            }
            
            if (opponentHandView != null)
            {
                opponentHandView.Initialize(matchManager);
            }
            
            // Initialize battlefield views
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.Initialize(matchManager);
            }
            
            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.Initialize(matchManager);
            }
            
            // Set up event listeners
            SetupEventListeners();
            
            // Update the UI with current state
            UpdateUI();
            
            // Enable end turn button if it's the player's turn
            UpdateEndTurnButton();
            
            isInitialized = true;
            Debug.Log("[MatchView] Match view initialized");
        }
        
        /// <summary>
        /// Set up event listeners for the match manager events
        /// </summary>
        private void SetupEventListeners()
        {
            if (matchManager == null) return;
            
            // Subscribe to game events (only the essential ones)
            matchManager.OnCardDrawn += (card) => UpdateUI();
            matchManager.OnCardDeployed += (card, position) => UpdateUI();
            matchManager.OnCardDied += (card) => UpdateUI();
            matchManager.OnUIUpdateNeeded += UpdateUI;
            
            // Add click handler for end turn button
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }
        }
        
        /// <summary>
        /// Called when the end turn button is clicked
        /// </summary>
        private void OnEndTurnClicked()
        {
            if (matchManager == null) return;
            
            // Advance to the next turn
            matchManager.NextTurn();
            
            // Update the UI
            UpdateUI();
            
            // Enable/disable the end turn button based on current turn
            UpdateEndTurnButton();
        }
        
        /// <summary>
        /// Enable/disable the end turn button based on whose turn it is
        /// </summary>
        private void UpdateEndTurnButton()
        {
            if (endTurnButton == null || matchManager == null) return;
            
            bool isPlayerTurn = matchManager.IsPlayerTurn();
            endTurnButton.interactable = isPlayerTurn;
        }
        
        /// <summary>
        /// Update the UI based on the current match state
        /// </summary>
        public void UpdateUI()
        {
            if (matchManager == null) return;
            
            // Update resource displays
            UpdateResourceDisplay();
            
            // Update turn and phase indicators
            UpdatePhaseDisplay();
            
            // Synchronize all card views with model state
            viewManager.SynchronizeAllCardViews();
            
            // Update battlefield highlights
            playerBattlefieldView?.UpdateHighlights();
            opponentBattlefieldView?.UpdateHighlights();
            
            // Update layout groups to ensure proper positioning
            RefreshLayoutGroups();
        }
        
        /// <summary>
        /// Updates the resource display for both players
        /// </summary>
        private void UpdateResourceDisplay()
        {
            if (matchManager == null) return;
            
            // Update player resource text
            if (playerResourceText != null)
            {
                int playerResources = matchManager.Player.CurrentResources;
                int playerMaxResources = matchManager.Player.MaxResources;
                playerResourceText.text = $"Resources: {playerResources}/{playerMaxResources}";
            }
            
            // Update opponent resource text
            if (opponentResourceText != null)
            {
                int opponentResources = matchManager.Opponent.CurrentResources;
                int opponentMaxResources = matchManager.Opponent.MaxResources;
                opponentResourceText.text = $"Resources: {opponentResources}/{opponentMaxResources}";
            }
        }
        
        /// <summary>
        /// Updates the turn and phase displays
        /// </summary>
        private void UpdatePhaseDisplay()
        {
            if (matchManager == null) return;
            
            // Update turn text
            if (turnText != null)
            {
                string turnOwner = matchManager.IsPlayerTurn() ? "Your" : "Opponent's";
                int turnNumber = matchManager.Board.TurnNumber;
                turnText.text = $"{turnOwner} Turn (#{turnNumber})";
            }
            
            // Update phase text
            if (phaseText != null)
            {
                Phase currentPhase = matchManager.Board.CurrentPhase;
                phaseText.text = $"Phase: {currentPhase}";
            }
        }
        
        /// <summary>
        /// Refreshes all layout groups in the UI hierarchy to ensure proper positioning
        /// </summary>
        private void RefreshLayoutGroups()
        {
            // Refresh player hand layout
            if (playerHandView != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(playerHandView.GetComponent<RectTransform>());
            }
            
            // Refresh opponent hand layout
            if (opponentHandView != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(opponentHandView.GetComponent<RectTransform>());
            }
            
            // Refresh player battlefield layout
            if (playerBattlefieldView != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(playerBattlefieldView.GetComponent<RectTransform>());
            }
            
            // Refresh opponent battlefield layout
            if (opponentBattlefieldView != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(opponentBattlefieldView.GetComponent<RectTransform>());
            }
        }
        
        /// <summary>
        /// Validates that the game state is consistent
        /// </summary>
        private void ValidateGameState()
        {
            if (matchManager == null || viewManager == null) return;
            
            Debug.Log("[MatchView] Validating game state...");
            
            // Check player hand
            if (playerHandView != null)
            {
                IReadOnlyCollection<Card> playerHandCards = matchManager.Player.Hand.Cards;
                int cardViewCount = playerHandView.GetCardViews().Length;
                
                if (playerHandCards.Count != cardViewCount)
                {
                    Debug.LogWarning($"[MatchView] Player hand inconsistency: Model has {playerHandCards.Count} cards, but view has {cardViewCount} cards");
                    
                    // Log details of the cards in the model
                    Debug.Log("[MatchView] Cards in player hand model:");
                    foreach (var card in playerHandCards)
                    {
                        Debug.Log($"  - {card.Title} (ID: {card.InstanceId})");
                    }
                    
                    // Log details of the cards in the view
                    Debug.Log("[MatchView] Cards in player hand view:");
                    foreach (var cardView in playerHandView.GetCardViews())
                    {
                        Debug.Log($"  - {cardView.Card?.Title ?? "Null"} (ID: {cardView.Card?.InstanceId ?? -1})");
                    }
                }
            }
            
            // Check opponent hand
            if (opponentHandView != null)
            {
                IReadOnlyCollection<Card> opponentHandCards = matchManager.Opponent.Hand.Cards;
                int cardViewCount = opponentHandView.GetCardViews().Length;
                
                if (opponentHandCards.Count != cardViewCount)
                {
                    Debug.LogWarning($"[MatchView] Opponent hand inconsistency: Model has {opponentHandCards.Count} cards, but view has {cardViewCount} cards");
                }
            }
            
            Debug.Log("[MatchView] Game state validation complete");
        }
    }
}
