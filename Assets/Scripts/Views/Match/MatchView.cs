using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Managers;
using Kardx.Views.Cards;
using Kardx.Views.Hand;
using Kardx.Models.Match;
using Kardx.Controllers.DragHandlers;

namespace Kardx.Views.Match
{
    /// <summary>
    /// Main view component for the match, coordinating all UI elements
    /// </summary>
    public class MatchView : MonoBehaviour
    {
        // Match components
        [Header("Match Components")]
        [SerializeField]
        private MatchManager matchManager;
        [SerializeField]
        private ViewManager viewManager;

        // Public accessor for matchManager
        public MatchManager MatchManager => matchManager;

        // Hand View references
        [Header("Hand View References")]
        [SerializeField]
        private PlayerHandView playerHandView;
        [SerializeField]
        private OpponentHandView opponentHandView;

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

        [SerializeField]
        private Button resolveOrderButton;

        private HeadquarterView playerHqView;
        private HeadquarterView opponentHqView;

        /// <summary>
        /// Called when the GameObject is initialized
        /// </summary>
        private void Awake()
        {
            Debug.Log("[MatchView] Awake called");

            // Try to get the MatchManager component from the same GameObject
            if (matchManager == null)
            {
                Debug.LogError("[MatchView] MatchManager is null");
                return;
            }

            // Create the ViewManager if needed
            if (viewManager == null)
            {
                Debug.LogError("[MatchView] ViewManager is null");
                return;
            }
            // Initialize the ViewManager
            viewManager.Initialize(matchManager);

            // Tell the match manager about our ViewManager
            matchManager.SetViewManager(viewManager);
        }

        /// <summary>
        /// Called when the GameObject is enabled and active
        /// </summary>
        private void Start()
        {
            Debug.Log("[MatchView] Start called");

            // Start the match
            if (matchManager != null)
            {
                // Initialize the match if needed
                matchManager.Initialize();

                Debug.Log("[MatchView] Match started");
            }
            else
            {
                Debug.LogError("[MatchView] Cannot start match: MatchManager is null");
            }

            // Initialize the match view
            Initialize();

            // Start the match
            matchManager.StartMatch();
        }

        /// <summary>
        /// Initialize the match view with the match manager
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[MatchView] Initializing match view");

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
            SetupHeadquarterViews();
            EnsureDragZones();

            UpdateUI();

            // Enable end turn button if it's the player's turn
            UpdateEndTurnButton();

            Debug.Log("[MatchView] Match view initialized");
        }

        /// <summary>
        /// Set up event listeners for the match manager events
        /// </summary>
        private void SetupEventListeners()
        {
            if (matchManager == null) return;

            // Subscribe to game events (only the essential ones)
            matchManager.OnTurnStarted += (sender, player) => UpdateUI();
            matchManager.OnTurnEnded += (sender, player) => UpdateUI();
            matchManager.OnCardDrawn += (card) => UpdateUI();
            matchManager.OnCardDeployed += (card, position) => UpdateUI();
            matchManager.OnCardDied += (card) => UpdateUI();
            matchManager.OnUIUpdateNeeded += UpdateUI;
            matchManager.OnEnemyOrderPending += HandleEnemyOrderPending;
            matchManager.OnPendingOrderResolved += HandlePendingOrderResolved;
            matchManager.OnOrderCountered += (_) => HandlePendingOrderResolved();
            matchManager.OnAttackCompleted += (_, _, _, _) => UpdateHeadquarterDisplays();
            matchManager.OnMatchWon += (_, reason) => ShowMessage($"Victory! {reason}");

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            if (resolveOrderButton != null)
            {
                resolveOrderButton.onClick.AddListener(OnResolveOrderClicked);
                resolveOrderButton.gameObject.SetActive(false);
            }
        }

        private void SetupHeadquarterViews()
        {
            if (matchManager == null) return;

            var opponentGo = GameObject.Find("OpponentHeadquarter");
            if (opponentGo != null)
            {
                opponentHqView = opponentGo.GetComponent<HeadquarterView>();
                if (opponentHqView == null)
                    opponentHqView = opponentGo.AddComponent<HeadquarterView>();
                opponentHqView.Initialize(matchManager, opponentHq: true);
            }

            var playerGo = GameObject.Find("PlayerHeadquarter");
            if (playerGo != null)
            {
                playerHqView = playerGo.GetComponent<HeadquarterView>();
                if (playerHqView == null)
                    playerHqView = playerGo.AddComponent<HeadquarterView>();
                playerHqView.Initialize(matchManager, opponentHq: false);
            }
        }

        private void EnsureDragZones()
        {
            var zoneRoot = GameObject.Find("OrderDropArea");
            if (zoneRoot == null)
            {
                foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (behaviour.GetType().Name == "OrderDropHandler")
                    {
                        zoneRoot = behaviour.gameObject;
                        break;
                    }
                }
            }

            if (zoneRoot == null)
            {
                zoneRoot = new GameObject("OrderDropArea");
                zoneRoot.transform.SetParent(transform, false);
                var rect = zoneRoot.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            RemoveLegacyDragHandlers(zoneRoot);
            EnsureDragZone(zoneRoot, CardDragTargetKind.OrderZone);
            EnsureDragZone(zoneRoot, CardDragTargetKind.CountermeasureZone);
        }

        private static void RemoveLegacyDragHandlers(GameObject zoneRoot)
        {
            foreach (var behaviour in zoneRoot.GetComponents<MonoBehaviour>())
            {
                var name = behaviour.GetType().Name;
                if (name == "OrderDropHandler" || name == "CountermeasureDropHandler")
                    Destroy(behaviour);
            }
        }

        private static void EnsureDragZone(GameObject host, CardDragTargetKind kind)
        {
            foreach (var zone in host.GetComponents<CardDragZone>())
            {
                if (zone.ZoneKind == kind)
                    return;
            }

            var dragZone = host.AddComponent<CardDragZone>();
            dragZone.Configure(kind);
        }

        private void HandleEnemyOrderPending(Card order)
        {
            ShowMessage($"Enemy played {order.Title}! Drag a countermeasure to cancel, or click End Turn to resolve.");
            if (resolveOrderButton != null)
                resolveOrderButton.gameObject.SetActive(true);
            UpdateUI();
        }

        private void HandlePendingOrderResolved()
        {
            if (resolveOrderButton != null)
                resolveOrderButton.gameObject.SetActive(false);
            ShowMessage("");
            UpdateUI();
        }

        private void OnResolveOrderClicked()
        {
            matchManager?.ResolvePendingEnemyOrder();
        }

        private void ShowMessage(string text)
        {
            if (messageText != null)
                messageText.text = text;
        }

        private void UpdateHeadquarterDisplays()
        {
            playerHqView?.UpdateDisplay();
            opponentHqView?.UpdateDisplay();
        }

        /// <summary>
        /// Called when the end turn button is clicked
        /// </summary>
        private void OnEndTurnClicked()
        {
            if (matchManager == null) return;

            if (matchManager.HasPendingEnemyOrder)
            {
                matchManager.ResolvePendingEnemyOrder();
                return;
            }

            matchManager.NextTurn();
            UpdateUI();
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

            // Update all card views with model state
            viewManager.UpdateView();

            // Update battlefield highlights
            playerBattlefieldView?.UpdateHighlights();
            opponentBattlefieldView?.UpdateHighlights();

            // Update end turn button
            UpdateEndTurnButton();
            UpdateHeadquarterDisplays();

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
                int playerResources = matchManager.Player.Credits;
                int playerMaxResources = GameConstants.MaxCredits;
                playerResourceText.text = $"Resources: {playerResources}/{playerMaxResources}";
            }

            // Update opponent resource text
            if (opponentResourceText != null)
            {
                int opponentResources = matchManager.Opponent.Credits;
                int opponentMaxResources = GameConstants.MaxCredits;
                opponentResourceText.text = $"Resources: {opponentResources}/{opponentMaxResources}";
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
        public void ValidateGameState()
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
                        Debug.Log($"  - {cardView.Card?.Title ?? "Null"} (ID: {cardView.Card?.InstanceId ?? "None"})");
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
