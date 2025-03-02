using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kardx.Core;
using Kardx.UI.Components;
using Kardx.Utils;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Kardx.UI.Scenes
{
    [System.Serializable]
    public class CardListWrapper
    {
        public List<CardType> cards;
    }

    [System.Serializable]
    public class AbilityListWrapper
    {
        public List<AbilityType> abilities;
    }

    public class CardCollectionView : MonoBehaviour
    {
        [SerializeField]
        private Transform cardContainer; // Parent object for card UI elements

        [SerializeField]
        private GameObject cardPrefab; // Prefab for displaying a single card

        [SerializeField]
        private CardDetailView cardDetailView; // Reference to the CardDetailView

        private List<CardType> cards = new();

        private void Awake()
        {
            // First try to use the serialized reference
            if (cardDetailView == null)
            {
                // Try to find CardPanel in the scene
                var cardPanel = GameObject.Find("CardPanel");
                if (cardPanel != null)
                {
                    cardDetailView = cardPanel.GetComponent<CardDetailView>();
                    Debug.Log("[CardCollectionView] Found CardDetailView on CardPanel");
                }

                // If still not found, try finding it anywhere in the scene
                if (cardDetailView == null)
                {
                    cardDetailView = FindAnyObjectByType<CardDetailView>(FindObjectsInactive.Include);
                    if (cardDetailView == null)
                    {
                        Debug.LogError(
                            "[CardCollectionView] CardDetailView not found in scene. Please ensure CardPanel has CardDetailView component."
                        );
                        return;
                    }
                }
            }

            Debug.Log(
                $"[CardCollectionView] Found CardDetailView on: {cardDetailView.gameObject.name}"
            );
            CardView.InitializeSharedDetailView(cardDetailView);

            // Make sure the CardPanel is initially inactive
            cardDetailView.gameObject.SetActive(false);
        }

        private void Start()
        {
            cards = CardLoader.LoadCardTypes();
            DisplayCards();
        }

        private void DisplayCards()
        {
            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogError("[CardCollectionView] Missing required components");
                return;
            }

            try
            {
                foreach (var cardType in cards ?? Enumerable.Empty<CardType>())
                {
                    if (cardType == null)
                        continue;

                    GameObject cardInstance = Instantiate(cardPrefab, cardContainer);
                    var cardComponent = cardInstance.GetComponent<CardView>();

                    if (cardComponent != null)
                    {
                        cardComponent.Initialize(cardType);
                    }
                    else
                    {
                        Debug.LogError(
                            $"[CardCollectionView] CardView component missing on prefab"
                        );
                        Destroy(cardInstance);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CardCollectionView] Error displaying cards: {ex.Message}");
            }
        }
    }
}
