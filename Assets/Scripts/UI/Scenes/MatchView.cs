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
        private Transform playerBattlefieldArea; // Area for player's battlefield with HorizontalLayoutGroup

        [SerializeField]
        private Transform opponentHandArea;

        [SerializeField]
        private Transform opponentBattlefieldArea; // Area for opponent's battlefield with HorizontalLayoutGroup

        [SerializeField]
        private Transform playerHeadquarter;

        [SerializeField]
        private Transform opponentHeadquarter;

        [SerializeField]
        private Transform orderArea;

        [Header("UI Elements")]
        [SerializeField]
        private GameObject playerCardSlotPrefab; // Prefab for player card slot

        [SerializeField]
        private GameObject opponentCardSlotPrefab; // Prefab for opponent card slot

        [SerializeField]
        private GameObject cardPrefab;

        [SerializeField]
        private TextMeshProUGUI turnText;

        [SerializeField]
        private TextMeshProUGUI playerCreditsText;

        [SerializeField]
        private TextMeshProUGUI opponentCreditsText;

        [Header("Battlefield Views")]
        // References to the actual components - set during initialization
        private PlayerBattlefieldView playerBattlefieldView;
        private OpponentBattlefieldView opponentBattlefieldView;

        // Non-serialized fields
        private Dictionary<Card, GameObject> cardUIElements = new();

        [SerializeField]
        private CardDetailView cardDetailView; // Reference to the CardDetailView

        private List<CardType> cards = new();

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
                cardDetailView.Hide(); // This will properly hide it after initialization
            }

            // Initialize the shared reference
            CardView.InitializeSharedDetailView(cardDetailView);

            // Make sure the CardPanel is initially inactive
            cardDetailView.gameObject.SetActive(false);
            
            // Now we can initialize battlefield views since players exist
            InitializeBattlefieldViews();
        }

        private void InitializeBattlefieldViews()
        {
            // Get the PlayerBattlefieldView component from the player battlefield area
            if (playerBattlefieldArea != null)
            {
                playerBattlefieldView = playerBattlefieldArea.GetComponent<PlayerBattlefieldView>();
            }

            // Get the OpponentBattlefieldView component from the opponent battlefield area
            if (opponentBattlefieldArea != null)
            {
                opponentBattlefieldView = opponentBattlefieldArea.GetComponent<OpponentBattlefieldView>();
            }

            // Initialize the battlefield views if they exist
            if (playerBattlefieldView != null)
            {
                playerBattlefieldView.Initialize(MatchManager);
                playerBattlefieldView.InitializeSlots(playerCardSlotPrefab);
            }
            else
            {
                Debug.LogError("PlayerBattlefieldView component not found on playerBattlefieldArea");
            }

            if (opponentBattlefieldView != null)
            {
                opponentBattlefieldView.Initialize(MatchManager);
                opponentBattlefieldView.InitializeSlots(opponentCardSlotPrefab);
            }
            else
            {
                Debug.LogError("OpponentBattlefieldView component not found on opponentBattlefieldArea");
            }
        }

        private void Start()
        {
            // Set up the match
            SetupMatch();
        }

        private void SetupMatch()
        {
            // MatchManager is already created and initialized in Awake()
            // Battlefield views are also already initialized in Awake()
            
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
                matchManager.OnCardDeployed += (card, slotIndex) => HandleCardDeployed(card, slotIndex);
                matchManager.OnTurnStarted += (sender, player) => HandleTurnStarted(player);
                matchManager.OnTurnEnded += (sender, player) => HandleTurnEnded(player);
                matchManager.OnAttackCompleted += (attackerCard, targetCard, damageDealt, remainingHealth) => 
                    HandleAttackCompleted(attackerCard, targetCard, damageDealt, remainingHealth);
                matchManager.OnProcessAITurn += (board, planner, callback) => 
                    HandleProcessAITurn(board, planner, callback);
                matchManager.OnCardDrawn += HandleCardDrawn;
                matchManager.OnCardDied += HandleCardDied;
                matchManager.OnMatchStarted += HandleMatchStarted;
                matchManager.OnMatchEnded += HandleMatchEnded;
            }
        }

        private void HandleTurnStarted(Player player)
        {
            Debug.Log($"[MatchView] Turn started for {(player == matchManager.Player ? "Player" : "Opponent")}");
            UpdateTurnDisplay();
            UpdateCreditsDisplay();
        }

        private void HandleTurnEnded(Player player)
        {
            Debug.Log($"[MatchView] Turn ended for {(player == matchManager.Player ? "Player" : "Opponent")}");
            // Any cleanup needed at the end of a turn
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
            UpdateUI();
        }

        private void HandleAttackCompleted(Card attackerCard, Card targetCard, int damageDealt, int remainingHealth)
        {
            Debug.Log($"[MatchView] Attack completed: {attackerCard.Title} -> {targetCard.Title} - Damage: {damageDealt} - Remaining Health: {remainingHealth}");
            UpdateUI();
        }

        // Method to handle AI turn processing
        private void HandleProcessAITurn(Board board, StrategyPlanner planner, Action callback)
        {
            Debug.Log("[MatchView] Processing AI turn");
            
            // AI turn is handled by the MatchManager
            // After AI processing is complete, invoke the callback
            callback?.Invoke();
            
            // Update the UI to reflect changes
            UpdateUI();
        }

        // Method to update the entire UI
        public void UpdateUI()
        {
            if (matchManager == null)
                return;

            // Update player's hand
            UpdateHand(matchManager.Player, playerHandArea);

            // Update opponent's hand (face down)
            UpdateHand(matchManager.Opponent, opponentHandArea, true);

            // Update player's battlefield
            if (playerBattlefieldView != null)
            {
                UpdateBattlefield(matchManager.Player, playerBattlefieldView);
            }

            // Update opponent's battlefield
            if (opponentBattlefieldView != null)
            {
                UpdateBattlefield(matchManager.Opponent, opponentBattlefieldView);
            }

            // Update credits display
            UpdateCreditsDisplay();

            // Update turn display
            UpdateTurnDisplay();
        }

        // Method to update a player's hand
        private void UpdateHand(Player player, Transform handTransform, bool faceDown = false)
        {
            if (player == null || handTransform == null)
                return;

            // Clear existing cards
            foreach (Transform child in handTransform)
            {
                Destroy(child.gameObject);
            }

            // Add cards from player's hand
            foreach (var card in player.Hand.GetCards())
            {
                var cardGO = CreateCardUI(card, handTransform, faceDown);
                
                // Add appropriate drag handlers based on card type
                var cardView = cardGO.GetComponent<CardView>();
                if (cardView != null && !faceDown && player == matchManager.Player)
                {
                    if (card.IsUnitCard)
                    {
                        if (cardGO.GetComponent<UnitDeployDragHandler>() == null)
                        {
                            cardGO.AddComponent<UnitDeployDragHandler>();
                        }
                    }
                    else if (card.IsOrderCard)
                    {
                        if (cardGO.GetComponent<OrderDeployDragHandler>() == null)
                        {
                            cardGO.AddComponent<OrderDeployDragHandler>();
                        }
                    }
                }
            }
        }

        // Method to update a player's battlefield
        private void UpdateBattlefield(Player player, BaseBattlefieldView battlefieldView)
        {
            if (player == null || battlefieldView == null)
                return;

            battlefieldView.UpdateBattlefield(player.Battlefield);
        }

        // Method to create a card UI element
        public GameObject CreateCardUI(Card card, Transform parent, bool faceDown = false)
        {
            if (card == null || parent == null || cardPrefab == null)
                return null;

            var cardGO = Instantiate(cardPrefab, parent);
            var cardView = cardGO.GetComponent<CardView>();
            
            if (cardView != null)
            {
                cardView.SetCard(card, faceDown);
                cardView.SetInteractable(!faceDown);
            }

            return cardGO;
        }

        // Method to deploy a unit card
        public bool DeployUnitCard(Card card, int slotIndex)
        {
            if (matchManager == null || card == null)
                return false;

            // Check if this is a valid deployment
            if (!CanDeployUnitCard(card, slotIndex))
                return false;

            // Deploy the card
            bool success = matchManager.DeployCard(card, slotIndex);
            
            // Clear all highlights
            ClearAllHighlights();
            
            // Update the UI
            if (success)
            {
                UpdateUI();
            }
            
            return success;
        }

        // Method to deploy an order card
        public bool DeployOrderCard(Card card)
        {
            if (matchManager == null || card == null)
                return false;

            // Check if this is a valid deployment
            if (!CanDeployOrderCard(card))
                return false;

            // Deploy the card
            bool success = matchManager.DeployOrderCard(card);
            
            // Clear all highlights
            ClearAllHighlights();
            
            // Update the UI
            if (success)
            {
                UpdateUI();
            }
            
            return success;
        }

        // Method to deploy a card to a position
        public bool DeployCard(Card card, int position)
        {
            if (matchManager == null || card == null)
                return false;
                
            // Check if it's a unit card
            if (card.CardType.Category == CardCategory.Unit)
            {
                return matchManager.DeployUnitCard(card, position);
            }
            // Check if it's an order card
            else if (card.CardType.Category == CardCategory.Order)
            {
                return matchManager.DeployOrderCard(card);
            }
            
            return false;
        }
        
        // Method to check if a card can be deployed
        public bool CanDeployCard(Card card)
        {
            if (matchManager == null || card == null)
                return false;
                
            // Check if it's a unit card
            if (card.CardType.Category == CardCategory.Unit)
            {
                return matchManager.CanDeployUnitCard(card);
            }
            // Check if it's an order card
            else if (card.CardType.Category == CardCategory.Order)
            {
                return matchManager.CanDeployOrderCard(card);
            }
            
            return false;
        }

        // Method to check if a unit card can be deployed
        public bool CanDeployUnitCard(Card card, int slotIndex)
        {
            if (matchManager == null || card == null)
                return false;

            // Check if it's a unit card
            if (!card.IsUnitCard)
                return false;

            // Check if it's the player's turn
            if (!IsPlayerTurn())
                return false;

            // Check if the card is in the player's hand
            var player = matchManager.Player;
            if (player == null || !player.Hand.Contains(card))
                return false;

            // Check if the slot is valid
            if (slotIndex < 0 || slotIndex >= Player.BATTLEFIELD_SLOT_COUNT)
                return false;

            // Check if the slot is empty
            if (player.Battlefield.GetCardAt(slotIndex) != null)
                return false;

            // Check if the player has enough credits
            if (player.Credits < card.Cost)
                return false;

            return true;
        }

        // Method to check if an order card can be deployed
        public bool CanDeployOrderCard(Card card)
        {
            if (matchManager == null || card == null)
                return false;

            // Check if it's an order card
            if (!card.IsOrderCard)
                return false;

            // Check if it's the player's turn
            if (!IsPlayerTurn())
                return false;

            // Check if the card is in the player's hand
            var player = matchManager.Player;
            if (player == null || !player.Hand.Contains(card))
                return false;

            // Check if the player has enough credits
            if (player.Credits < card.Cost)
                return false;

            return true;
        }

        // Method to check if a card can target another card
        public bool CanTargetCard(Card attackerCard, Card targetCard)
        {
            if (matchManager == null || attackerCard == null || targetCard == null)
                return false;

            // Check if it's the player's turn
            if (!IsPlayerTurn())
                return false;

            // Check if the attacker is on the player's battlefield
            var player = matchManager.Player;
            if (player == null || !player.Battlefield.Contains(attackerCard))
                return false;

            // Check if the target is on the opponent's battlefield
            var opponent = matchManager.Opponent;
            if (opponent == null || !opponent.Battlefield.Contains(targetCard))
                return false;

            // Check if the attacker has already attacked this turn
            if (attackerCard.HasAttackedThisTurn)
                return false;

            return true;
        }

        // Method to target a card with another card
        public bool TargetCard(Card sourceCard, Card targetCard)
        {
            if (matchManager == null || sourceCard == null || targetCard == null)
                return false;

            // Check if this is a valid target
            if (!CanTargetCard(sourceCard, targetCard))
                return false;

            // Execute the attack
            bool success = matchManager.InitiateAttack(sourceCard, targetCard);
            
            // Clear all highlights
            ClearAllHighlights();
            
            // Update UI
            if (success)
            {
                UpdateUI();
            }
            
            return success;
        }

        // Method to check if there are valid targets for a card
        public bool HasValidTargets(Card card)
        {
            if (matchManager == null || card == null)
                return false;

            // Check if it's the player's turn
            if (!IsPlayerTurn())
                return false;

            // Check if the card is on the player's battlefield
            var player = matchManager.Player;
            if (player == null || !player.Battlefield.Contains(card))
                return false;

            // Check if the card has already attacked this turn
            if (card.HasAttackedThisTurn)
                return false;

            // Check if there are any cards on the opponent's battlefield
            var opponent = matchManager.Opponent;
            if (opponent == null || opponent.Battlefield.Count == 0)
                return false;

            return true;
        }

        // Method to highlight valid targets for a card
        public void HighlightValidTargets(Card card)
        {
            if (matchManager == null || card == null || opponentBattlefieldView == null)
                return;

            // Check if it's the player's turn
            if (!IsPlayerTurn())
                return;

            // Check if the card is on the player's battlefield
            var player = matchManager.Player;
            if (player == null || !player.Battlefield.Contains(card))
                return;

            // Check if the card has already attacked this turn
            if (card.HasAttackedThisTurn)
                return;

            // Highlight all cards on the opponent's battlefield
            var opponent = matchManager.Opponent;
            if (opponent != null)
            {
                opponentBattlefieldView.HighlightCards(opponent.Battlefield);
            }
        }

        // Method to highlight valid unit drop slots
        public void HighlightValidUnitDropSlots(Card card)
        {
            if (matchManager == null || card == null || playerBattlefieldView == null)
                return;

            // Check if it's a unit card
            if (!card.IsUnitCard)
                return;

            // Check if it's the player's turn
            if (!IsPlayerTurn())
                return;

            // Check if the card is in the player's hand
            var player = matchManager.Player;
            if (player == null || !player.Hand.Contains(card))
                return;

            // Check if the player has enough credits
            if (player.Credits < card.Cost)
                return;

            // Highlight empty slots on the player's battlefield
            playerBattlefieldView.HighlightEmptySlots(player.Battlefield);
        }

        // Method to clear all highlights
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

        // Method to check if it's the player's turn
        public bool IsPlayerTurn()
        {
            if (matchManager == null)
                return false;

            return matchManager.CurrentPlayer == matchManager.Player;
        }

        // Method to get the current player
        public Player GetCurrentPlayer()
        {
            if (matchManager == null)
                return null;

            return matchManager.CurrentPlayer;
        }

        // Method to get the player
        public Player GetPlayer()
        {
            if (matchManager == null)
                return null;

            return matchManager.Player;
        }

        // Method to get the opponent
        public Player GetOpponent()
        {
            if (matchManager == null)
                return null;

            return matchManager.Opponent;
        }

        // Method to end the player's turn
        public void EndTurn()
        {
            if (matchManager == null)
                return;

            matchManager.NextTurn();
            UpdateUI();
        }

        private List<Card> GetValidTargets(Card sourceCard, Player targetPlayer)
        {
            List<Card> validTargets = new List<Card>();

            // Check if this card can attack
            if (sourceCard == null ||
                !sourceCard.IsUnitCard ||
                sourceCard.HasAttackedThisTurn)
                return validTargets;

            // Add valid targets from the target player's battlefield
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                var targetCard = targetPlayer.Battlefield.GetCardAt(i);
                if (targetCard != null)
                {
                    validTargets.Add(targetCard);
                }
            }

            return validTargets;
        }

        private void HandleMatchStarted(string message)
        {
            Debug.Log($"[MatchView] Match started: {message}");

            // Do a full UI refresh at the start of the match
            UpdateUI();
        }

        private void HandleMatchEnded(string message)
        {
            Debug.Log($"[MatchView] Match ended: {message}");

            // Handle match end UI updates if needed
        }

        private void HandleCardDrawn(Card card)
        {
            Debug.Log($"[MatchView] Card drawn: {card.Title}");
            UpdateUI();
        }

        private void HandleCardDied(Card card)
        {
            Debug.Log($"[MatchView] Card died: {card.Title}");
            UpdateUI();
        }
    }
}
