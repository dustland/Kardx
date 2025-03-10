using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kardx.Models.Cards;
using Kardx.Views.Cards;
using Kardx.Views.Match;
using Kardx.Models.Match;
using Kardx.Utils;
using UnityEngine.SceneManagement;
using Kardx.Models;
using Kardx.Views.Hand;
using DG.Tweening;

namespace Kardx.Managers
{
    /// <summary>
    /// Manages the synchronization between model objects and their UI representations.
    /// Acts as a bridge between the Core and UI layers.
    /// </summary>
    public class ViewManager : MonoBehaviour
    {
        [SerializeField] private CardView playerCardPrefab;
        [SerializeField] private CardView opponentCardPrefab;

        private MatchManager matchManager;
        private ViewRegistry viewRegistry = new ViewRegistry();
        private CardDetailView cardDetailView;

        // Components that manage different areas of the UI
        private PlayerHandView playerHandView;
        private OpponentHandView opponentHandView;
        private PlayerBattlefieldView playerBattlefieldView;
        private OpponentBattlefieldView opponentBattlefieldView;

        // Events
        public event Action<Card, CardView> OnCardViewCreated;
        public event Action<Card> OnCardViewDestroyed;
        public event Action<Card, GameArea, GameArea> OnCardAreaChanged;

        /// <summary>
        /// Initialize with the match manager
        /// </summary>
        public void Initialize(MatchManager matchManager)
        {
            this.matchManager = matchManager;

            // Store references to UI views
            playerHandView = FindAnyObjectByType<PlayerHandView>();
            opponentHandView = FindAnyObjectByType<OpponentHandView>();
            playerBattlefieldView = FindAnyObjectByType<PlayerBattlefieldView>();
            opponentBattlefieldView = FindAnyObjectByType<OpponentBattlefieldView>();

            // Find the CardDetailView in the scene
            cardDetailView = FindAnyObjectByType<CardDetailView>(FindObjectsInactive.Include);
            if (cardDetailView == null)
            {
                Debug.LogWarning("[ViewManager] CardDetailView not found in scene");
            }
            else
            {
                Debug.Log("[ViewManager] Found CardDetailView in scene");
            }

            // Subscribe to relevant events
            if (matchManager != null)
            {
                matchManager.OnTurnStarted += HandleTurnStarted;
                matchManager.OnTurnEnded += HandleTurnEnded;
                matchManager.OnCardDeployed += HandleCardDeployed;
                matchManager.OnCardDied += HandleCardDied;
                matchManager.OnCardDrawn += HandleCardDrawn;
                matchManager.OnCardDiscarded += HandleCardDiscarded;
                matchManager.OnCardReturned += HandleCardReturned;
                matchManager.OnAttackCompleted += HandleAttackCompleted;
            }

            // Subscribe to registry events
            viewRegistry.OnCardViewRegistered += (card, view) => OnCardViewCreated?.Invoke(card, view);
            viewRegistry.OnCardViewUnregistered += (card) => OnCardViewDestroyed?.Invoke(card);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (matchManager != null)
            {
                matchManager.OnTurnStarted -= HandleTurnStarted;
                matchManager.OnTurnEnded -= HandleTurnEnded;
                matchManager.OnCardDeployed -= HandleCardDeployed;
                matchManager.OnCardDied -= HandleCardDied;
                matchManager.OnCardDrawn -= HandleCardDrawn;
                matchManager.OnCardDiscarded -= HandleCardDiscarded;
                matchManager.OnCardReturned -= HandleCardReturned;
                matchManager.OnAttackCompleted -= HandleAttackCompleted;
            }

            // Clear registry
            viewRegistry.Clear();
        }

        /// <summary>
        /// Get the view registry
        /// </summary>
        public ViewRegistry Registry => viewRegistry;

        /// <summary>
        /// Handle turn started event
        /// </summary>
        private void HandleTurnStarted(object sender, Player player)
        {
            // Update UI for the new turn
            // ...
        }

        /// <summary>
        /// Handle turn ended event
        /// </summary>
        private void HandleTurnEnded(object sender, Player player)
        {
            // Update UI for the end of the turn
            // ...
        }

        /// <summary>
        /// Handle card drawn event
        /// </summary>
        private void HandleCardDrawn(Card card)
        {
            // Card moved from deck to hand
            // Determine the correct hand area based on the card's owner
            GameArea targetArea = card.Owner == matchManager.Player ?
                GameArea.PlayerHand : GameArea.OpponentHand;
                
            // Trigger the card area changed event (from Deck to Hand)
            OnCardAreaChanged?.Invoke(card, GameArea.Deck, targetArea);

            // The PlayerHandView and OpponentHandView will handle adding the card to the appropriate hand
            // based on their own HandleCardDrawn implementations
            
            UpdateView();
        }

        /// <summary>
        /// Handle card deployed event
        /// </summary>
        private void HandleCardDeployed(Card card, int slotIndex)
        {
            // Card moved from hand to battlefield
            // Trigger the card area changed event (from Hand to Battlefield)
            OnCardAreaChanged?.Invoke(card, GameArea.Hand, GameArea.Battlefield);

            UpdateView();
        }

        /// <summary>
        /// Handle card died event
        /// </summary>
        private void HandleCardDied(Card card)
        {
            // Get the card view for the dying card
            CardView cardView = viewRegistry.GetCardView(card);
            if (cardView != null)
            {
                // Get or add a CanvasGroup component for fade animation
                CanvasGroup canvasGroup = cardView.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = cardView.gameObject.AddComponent<CanvasGroup>();
                }
                
                // Ensure the card is visible above other elements during animation
                cardView.transform.SetAsLastSibling();
                
                // First slightly scale up the card for dramatic effect
                cardView.transform.DOScale(cardView.transform.localScale * 1.2f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        // Then use the utility class for the main death animation
                        Sequence deathSequence = DOTweenAnimationUtility.AnimateCardDeath(
                            cardView.transform,
                            canvasGroup,
                            floatDistance: 50f,
                            duration: 0.8f,
                            onComplete: () =>
                            {
                                UnityEngine.Object.Destroy(cardView.gameObject);
                            }
                        );
                        
                        // Play the sequence
                        deathSequence.Play();
                    });
            }
            
            // Card moved from battlefield to graveyard (destroyed)
            UpdateView();
        }

        /// <summary>
        /// Handle card discarded event
        /// </summary>
        private void HandleCardDiscarded(Card card)
        {
            // Card moved from hand to graveyard
            UpdateView();
        }

        /// <summary>
        /// Handle card returned event (from battlefield to hand)
        /// </summary>
        private void HandleCardReturned(Card card)
        {
            // Card moved from battlefield to hand
            UpdateView();
        }

        /// <summary>
        /// Handle attack completed event
        /// </summary>
        private void HandleAttackCompleted(Card attackerCard, Card targetCard, int damage, int resultingHealth)
        {
            // Get the card views for the attacker and target
            CardView attackerView = viewRegistry.GetCardView(attackerCard);
            CardView targetView = viewRegistry.GetCardView(targetCard);
            
            if (attackerView != null && targetView != null)
            {
                // Play attack animation using DOTweenAnimationUtility directly
                Sequence attackSequence = DOTweenAnimationUtility.AnimateCardAttack(
                    attackerView.transform,
                    targetPosition: targetView.transform.position,
                    lungeDistance: 30f,
                    duration: 0.4f,
                    flashColor: new Color(1f, 0.5f, 0f, 0.8f), // Orange flash
                    onImpactCallback: () => 
                    {
                        // Simple scale pulse effect for the target card
                        Vector3 originalScale = targetView.transform.localScale;
                        targetView.transform.DOScale(originalScale * 1.2f, 0.1f)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() => 
                            {
                                targetView.transform.DOScale(originalScale, 0.2f)
                                    .SetEase(Ease.OutBack);
                            });
                    }
                );
                
                // Play the sequence
                attackSequence.Play();
            }
            
            // Update the view after the attack
            UpdateView();
        }

        /// <summary>
        /// Updates all UI elements to match the current game state
        /// This is the primary method for ensuring model-view consistency
        /// </summary>
        public void UpdateView()
        {
            if (matchManager == null)
            {
                Debug.LogError("[ViewManager] Cannot update view - MatchManager is null");
                return;
            }

            // Track which cards we've already processed to avoid duplicates
            HashSet<Card> processedCards = new HashSet<Card>();

            // Update player hand
            UpdatePlayerHand(processedCards);

            // Update player battlefield
            UpdatePlayerBattlefield(processedCards);

            // Update opponent hand
            UpdateOpponentHand(processedCards);

            // Update opponent battlefield
            UpdateOpponentBattlefield(processedCards);

            // Clean up any views that are no longer needed
            CleanupUnusedViews(processedCards);

            Debug.Log("[ViewManager] View update complete");
        }

        /// <summary>
        /// Updates the player's hand view to match the model
        /// </summary>
        private void UpdatePlayerHand(HashSet<Card> processedCards)
        {
            if (matchManager.Player == null || matchManager.Player.Hand == null || playerHandView == null)
                return;

            // Get all cards in the player's hand
            var handCards = matchManager.Player.Hand.GetCards();

            // Create a set of cards that should be in the hand
            HashSet<Card> cardsInHand = new HashSet<Card>(handCards);

            // Get all card views currently in the hand
            List<CardView> handViews = new List<CardView>();
            foreach (Transform child in playerHandView.GetHandTransform())
            {
                CardView view = child.GetComponent<CardView>();
                if (view != null)
                {
                    handViews.Add(view);
                }
            }

            // Remove cards that shouldn't be in the hand
            foreach (CardView view in handViews)
            {
                if (view.Card == null || !cardsInHand.Contains(view.Card))
                {
                    // This card shouldn't be in the hand, remove it
                    viewRegistry.UnregisterCard(view.Card);
                    Destroy(view.gameObject);
                }
            }

            // Add cards that should be in the hand but aren't
            foreach (Card card in handCards)
            {
                if (!processedCards.Contains(card))
                {
                    // Check if this card already has a view in the hand
                    CardView existingView = viewRegistry.GetCardView(card);
                    Transform handTransform = playerHandView.GetHandTransform();

                    if (existingView != null)
                    {
                        // If the view exists but isn't in the hand, move it
                        if (existingView.transform.parent != handTransform)
                        {
                            existingView.transform.SetParent(handTransform);
                            existingView.transform.localPosition = Vector3.zero;
                            existingView.transform.localRotation = Quaternion.identity;
                            existingView.transform.localScale = Vector3.one;
                        }

                        // Update the view to ensure it's in the correct state
                        existingView.UpdateUI();
                    }
                    else
                    {
                        // Create a new view for this card
                        playerHandView.AddCardToHand(card);
                    }

                    processedCards.Add(card);
                }
            }
        }

        /// <summary>
        /// Updates the player's battlefield view to match the model
        /// </summary>
        private void UpdatePlayerBattlefield(HashSet<Card> processedCards)
        {
            if (matchManager.Player == null || matchManager.Player.Battlefield == null || playerBattlefieldView == null)
                return;

            var battlefield = matchManager.Player.Battlefield;

            // Update each slot in the battlefield
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                Card card = battlefield.GetCardAt(i);
                PlayerCardSlot slot = playerBattlefieldView.GetSlots()[i];

                // Get the current card view in this slot (if any)
                CardView existingView = null;
                if (slot.CardContainer.childCount > 0)
                {
                    existingView = slot.CardContainer.GetComponentInChildren<CardView>();
                }

                if (card != null)
                {
                    // This slot should have a card
                    if (existingView != null && existingView.Card == card)
                    {
                        // The correct card is already in this slot, just update it
                        existingView.UpdateUI();
                    }
                    else
                    {
                        // Either wrong card or no card in this slot
                        if (existingView != null)
                        {
                            // Remove the incorrect card
                            viewRegistry.UnregisterCard(existingView.Card);
                            Destroy(existingView.gameObject);
                        }

                        // Create the correct card
                        playerBattlefieldView.CreateCardUI(card, i);
                    }

                    processedCards.Add(card);
                }
                else if (existingView != null)
                {
                    // This slot should be empty but has a card
                    viewRegistry.UnregisterCard(existingView.Card);
                    Destroy(existingView.gameObject);
                }
            }
        }

        /// <summary>
        /// Updates the opponent's hand view to match the model
        /// </summary>
        private void UpdateOpponentHand(HashSet<Card> processedCards)
        {
            if (matchManager.Opponent == null || matchManager.Opponent.Hand == null || opponentHandView == null)
                return;

            // Get all cards in the opponent's hand
            var handCards = matchManager.Opponent.Hand.GetCards();

            // Create a set of cards that should be in the hand
            HashSet<Card> cardsInHand = new HashSet<Card>(handCards);

            // Get all card views currently in the hand
            List<CardView> handViews = new List<CardView>();
            foreach (Transform child in opponentHandView.GetHandTransform())
            {
                CardView view = child.GetComponent<CardView>();
                if (view != null)
                {
                    handViews.Add(view);
                }
            }

            // Remove cards that shouldn't be in the hand
            foreach (CardView view in handViews)
            {
                if (view.Card == null || !cardsInHand.Contains(view.Card))
                {
                    // This card shouldn't be in the hand, remove it
                    viewRegistry.UnregisterCard(view.Card);
                    Destroy(view.gameObject);
                }
            }

            // Add cards that should be in the hand but aren't
            foreach (Card card in handCards)
            {
                if (!processedCards.Contains(card))
                {
                    // Check if this card already has a view in the hand
                    CardView existingView = viewRegistry.GetCardView(card);
                    Transform handTransform = opponentHandView.GetHandTransform();

                    if (existingView != null)
                    {
                        // If the view exists but isn't in the hand, move it
                        if (existingView.transform.parent != handTransform)
                        {
                            existingView.transform.SetParent(handTransform);
                            existingView.transform.localPosition = Vector3.zero;
                            existingView.transform.localRotation = Quaternion.identity;
                            existingView.transform.localScale = Vector3.one;
                        }

                        // Update the view to ensure it's in the correct state
                        existingView.UpdateUI();
                    }
                    else
                    {
                        // Create a new view for this card
                        opponentHandView.AddCardToHand(card);
                    }

                    processedCards.Add(card);
                }
            }
        }

        /// <summary>
        /// Updates the opponent's battlefield view to match the model
        /// </summary>
        private void UpdateOpponentBattlefield(HashSet<Card> processedCards)
        {
            if (matchManager.Opponent == null || matchManager.Opponent.Battlefield == null || opponentBattlefieldView == null)
                return;

            var battlefield = matchManager.Opponent.Battlefield;

            // Update each slot in the battlefield
            for (int i = 0; i < Battlefield.SLOT_COUNT; i++)
            {
                Card card = battlefield.GetCardAt(i);
                OpponentCardSlot slot = opponentBattlefieldView.GetSlots()[i];

                // Get the current card view in this slot (if any)
                CardView existingView = null;
                if (slot.CardContainer.childCount > 0)
                {
                    existingView = slot.CardContainer.GetComponentInChildren<CardView>();
                }

                if (card != null)
                {
                    // This slot should have a card
                    if (existingView != null && existingView.Card == card)
                    {
                        // The correct card is already in this slot, just update it
                        existingView.UpdateUI();
                    }
                    else
                    {
                        // Either wrong card or no card in this slot
                        if (existingView != null)
                        {
                            // Remove the incorrect card
                            viewRegistry.UnregisterCard(existingView.Card);
                            Destroy(existingView.gameObject);
                        }

                        // Create the correct card
                        opponentBattlefieldView.CreateCardUI(card, i);
                    }

                    processedCards.Add(card);
                }
                else if (existingView != null)
                {
                    // This slot should be empty but has a card
                    viewRegistry.UnregisterCard(existingView.Card);
                    Destroy(existingView.gameObject);
                }
            }
        }

        /// <summary>
        /// Removes any card views that don't correspond to cards in the current game state
        /// </summary>
        private void CleanupUnusedViews(HashSet<Card> validCards)
        {
            // Get all card views currently registered
            List<Card> cardsToRemove = new List<Card>();

            foreach (KeyValuePair<Card, CardView> entry in viewRegistry.CardViews)
            {
                // If the card isn't in our processed set, it's not in any valid game area
                if (!validCards.Contains(entry.Key))
                {
                    // Mark for removal from registry
                    cardsToRemove.Add(entry.Key);

                    // Destroy the unused view if it still exists
                    if (entry.Value != null)
                    {
                        Debug.Log($"[ViewManager] Destroying unused view for card {entry.Key.Title}");
                        GameObject.Destroy(entry.Value.gameObject);
                    }
                }
            }

            // Remove all unused cards from the registry
            foreach (Card card in cardsToRemove)
            {
                viewRegistry.UnregisterCard(card);
            }
        }

        /// <summary>
        /// Creates a card view for a card model
        /// </summary>
        public CardView CreateCardView(Card card, Transform parent, bool isPlayerCard = true)
        {
            if (card == null)
            {
                Debug.LogError("[ViewManager] Cannot create card view: Card is null");
                return null;
            }

            // Check if a view already exists for this card
            CardView existingView = viewRegistry.GetCardView(card);
            if (existingView != null)
            {
                Debug.LogWarning($"[ViewManager] View already exists for card {card.Title} ({card.InstanceId})");
                return existingView;
            }

            // Determine which prefab to use based on the card's owner
            // This ensures we always use the correct prefab regardless of where the card is being displayed
            bool cardBelongsToPlayer = card.Owner != null && !card.Owner.IsOpponent;
            CardView prefab = cardBelongsToPlayer ? playerCardPrefab : opponentCardPrefab;
            
            // Log the decision for debugging
            Debug.Log($"[ViewManager] Creating view for card {card.Title}, Owner: {(cardBelongsToPlayer ? "Player" : "Opponent")}, Using prefab: {(cardBelongsToPlayer ? "Player" : "Opponent")}");
            
            if (prefab == null)
            {
                Debug.LogError("[ViewManager] Card prefab is null");
                return null;
            }

            // Instantiate the card view
            CardView view = Instantiate(prefab, parent);
            view.Initialize(card);

            // Set the ViewManager reference
            view.SetViewManager(this);

            // Register in the registry
            viewRegistry.RegisterCard(card, view);

            return view;
        }

        /// <summary>
        /// Destroys a card view
        /// </summary>
        public void DestroyCardView(Card card)
        {
            CardView view = viewRegistry.GetCardView(card);
            if (view != null)
            {
                // Unregister first
                viewRegistry.UnregisterCard(card);

                // Then destroy the GameObject
                Destroy(view.gameObject);

                Debug.Log($"[ViewManager] Destroyed view for card {card.Title}");
            }
        }

        /// <summary>
        /// Creates card views for cards in a list
        /// </summary>
        public List<CardView> CreateCardViews(IEnumerable<Card> cards, Transform parent, bool isPlayerCards = true)
        {
            List<CardView> views = new List<CardView>();
            foreach (var card in cards)
            {
                CardView view = CreateCardView(card, parent, isPlayerCards);
                if (view != null)
                {
                    views.Add(view);
                }
            }
            return views;
        }

        /// <summary>
        /// Gets the CardDetailView for the current scene
        /// </summary>
        public CardDetailView GetCardDetailView()
        {
            return cardDetailView;
        }
    }
}
