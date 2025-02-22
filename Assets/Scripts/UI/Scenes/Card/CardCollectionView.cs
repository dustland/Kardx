using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Kardx.Core.Data.Cards;
using Kardx.Core.Data.Abilities;
using UnityEngine.UI;
using Kardx.UI.Components.Card;

namespace Kardx.UI.Scenes.Card
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
        [SerializeField] private Transform cardContainer; // Parent object for card UI elements
        [SerializeField] private GameObject cardPrefab; // Prefab for displaying a single card

        private List<CardType> cards = new();
        private List<AbilityType> abilities = new();

        private void Start()
        {
            LoadCards();
            LoadAbilities();
            DisplayCards();
        }

        private void LoadCards()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "cards.json");
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                CardListWrapper wrapper = JsonUtility.FromJson<CardListWrapper>("{\"cards\":" + jsonData + "}");
                cards = wrapper.cards;
                Debug.Log("Loaded " + cards.Count + " cards");
            }
            else
            {
                Debug.LogError("Cards JSON file not found!");
            }
        }

        private void LoadAbilities()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "abilities.json");
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                AbilityListWrapper wrapper = JsonUtility.FromJson<AbilityListWrapper>("{\"abilities\":" + jsonData + "}");

                abilities = wrapper.abilities;
                Debug.Log("Loaded " + abilities.Count + " abilities");
            }
            else
            {
                Debug.LogError("Abilities JSON file not found!");
            }
        }

        private void DisplayCards()
        {
            foreach (var card in cards)
            {
                GameObject cardInstance = Instantiate(cardPrefab, cardContainer);
                // var cardComponent = cardInstance.GetComponent<CardView>();
                // if (cardComponent != null)
                // {
                //     cardComponent.Initialize(card);
                // }
            }
        }
    }
}