using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Kardx.UI.Components.Card;
using Newtonsoft.Json;
using Kardx.Core;

namespace Kardx.UI.Scenes.Card
{
    using Card = Kardx.Core.Data.Cards.Card; // Alias for Card
    using CardType = Kardx.Core.Data.Cards.CardType;
    using AbilityType = Kardx.Core.Data.Abilities.AbilityType;
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
                try
                {
                    string jsonData = File.ReadAllText(filePath);
                    var jsonCards = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(jsonData);
                    
                    cards = new List<CardType>();
                    foreach (var jsonCard in jsonCards)
                    {
                        var cardType = ScriptableObject.CreateInstance<CardType>();
                        cardType.Initialize(
                            id: jsonCard["id"].ToString(),
                            name: jsonCard["name"].ToString(),
                            description: jsonCard["description"].ToString(),
                            category: (CardCategory)Enum.Parse(typeof(CardCategory), jsonCard["category"].ToString()),
                            subtype: jsonCard["subtype"].ToString(),
                            deploymentCost: Convert.ToInt32(jsonCard["deploymentCost"]),
                            operationCost: Convert.ToInt32(jsonCard["operationCost"]),
                            baseDefence: Convert.ToInt32(jsonCard["baseDefence"]),
                            baseAttack: Convert.ToInt32(jsonCard["baseAttack"]),
                            baseCounterAttack: Convert.ToInt32(jsonCard["baseCounterAttack"]),
                            rarity: (CardRarity)Enum.Parse(typeof(CardRarity), jsonCard["rarity"].ToString()),
                            setId: jsonCard["setId"].ToString()
                        );
                        cards.Add(cardType);
                    }

                    Debug.Log($"Successfully loaded {cards.Count} cards");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading cards: {ex.Message}\n{ex.StackTrace}");
                    cards = new List<CardType>();
                }
            }
            else
            {
                Debug.LogError($"Cards JSON file not found at path: {filePath}");
                cards = new List<CardType>();
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
            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogError("[CardCollectionView] Missing required components");
                return;
            }

            try
            {
                foreach (var cardType in cards ?? Enumerable.Empty<CardType>())
                {
                    if (cardType == null) continue;

                    GameObject cardInstance = Instantiate(cardPrefab, cardContainer);
                    var cardComponent = cardInstance.GetComponent<CardView>();
                    
                    if (cardComponent != null)
                    {
                        cardComponent.Initialize(cardType);
                    }
                    else
                    {
                        Debug.LogError($"[CardCollectionView] CardView component missing on prefab");
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