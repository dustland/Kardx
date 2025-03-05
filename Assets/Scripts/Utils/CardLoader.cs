using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kardx.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Kardx.Utils
{
    public static class CardLoader
    {
        private static readonly string DataDirectory = Application.streamingAssetsPath;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        private class CardDataJson
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public CardCategory Category { get; set; }
            public string Subtype { get; set; }
            public int DeploymentCost { get; set; }
            public int OperationCost { get; set; }
            public int BaseDefense { get; set; }
            public int BaseAttack { get; set; }
            public int BaseCounterAttack { get; set; }
            public CardRarity Rarity { get; set; }
            public string SetId { get; set; }
            public string ImageUrl { get; set; }
            public Dictionary<string, int> Attributes { get; set; }

            [JsonProperty("abilities")]
            public List<string> AbilityIds { get; set; } // Store ability IDs instead of full objects
        }

        public static List<CardType> LoadCardTypes()
        {
            var cardTypes = new List<CardType>();
            try
            {
                // Load abilities first
                var abilities = LoadAbilities();
                if (abilities == null)
                {
                    Debug.LogError("Failed to load abilities");
                    return CreateTestCardTypes();
                }

                // Load card data
                string cardFilePath = Path.Combine(DataDirectory, "cards.json");
                if (!File.Exists(cardFilePath))
                {
                    Debug.LogError($"Cards file not found at {cardFilePath}");
                    return CreateTestCardTypes();
                }

                string cardJsonData = File.ReadAllText(cardFilePath);
                var cardDataList = JsonConvert.DeserializeObject<List<CardDataJson>>(
                    cardJsonData,
                    JsonSettings
                );

                // Convert CardDataJson to CardType and link abilities
                foreach (var cardData in cardDataList)
                {
                    var cardType = new CardType();
                    cardType.Initialize(
                        id: cardData.Id,
                        title: cardData.Title,
                        description: cardData.Description,
                        category: cardData.Category,
                        subtype: cardData.Subtype,
                        deploymentCost: cardData.DeploymentCost,
                        operationCost: cardData.OperationCost,
                        baseDefense: cardData.BaseDefense,
                        baseAttack: cardData.BaseAttack,
                        baseCounterAttack: cardData.BaseCounterAttack,
                        rarity: cardData.Rarity,
                        setId: cardData.SetId
                    );

                    // Set image URL if available
                    if (!string.IsNullOrEmpty(cardData.ImageUrl))
                    {
                        cardType.SetImageUrl(cardData.ImageUrl);
                    }

                    // Link abilities
                    if (cardData.AbilityIds != null)
                    {
                        foreach (var abilityId in cardData.AbilityIds)
                        {
                            var ability = abilities.FirstOrDefault(a => a.Id == abilityId);
                            if (ability != null)
                            {
                                cardType.AddAbility(ability);
                            }
                            else
                            {
                                Debug.LogWarning(
                                    $"Ability {abilityId} not found for card {cardData.Id}"
                                );
                            }
                        }
                    }

                    cardTypes.Add(cardType);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading card types: {ex.Message}");
                return CreateTestCardTypes();
            }

            return cardTypes.Count > 0 ? cardTypes : CreateTestCardTypes();
        }

        private static List<AbilityType> LoadAbilities()
        {
            try
            {
                string abilityFilePath = Path.Combine(DataDirectory, "abilities.json");
                if (!File.Exists(abilityFilePath))
                {
                    Debug.LogError($"Abilities file not found at {abilityFilePath}");
                    return null;
                }

                string abilityJsonData = File.ReadAllText(abilityFilePath);
                return JsonConvert.DeserializeObject<List<AbilityType>>(
                    abilityJsonData,
                    JsonSettings
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading abilities: {ex.Message}");
                return null;
            }
        }

        private static List<CardType> CreateTestCardTypes()
        {
            var cardTypes = new List<CardType>();
            var cardType1 = new CardType();
            cardType1.Initialize(
                id: "test_unit_1",
                title: "Test Unit 1",
                description: "A test unit card",
                category: CardCategory.Unit,
                subtype: "Warrior",
                deploymentCost: 2,
                operationCost: 1,
                baseDefense: 2,
                baseAttack: 1,
                baseCounterAttack: 1,
                rarity: CardRarity.Standard,
                setId: "TEST"
            );
            cardTypes.Add(cardType1);

            var cardType2 = new CardType();
            cardType2.Initialize(
                id: "test_order_1",
                title: "Test Order 1",
                description: "A test order card",
                category: CardCategory.Order,
                subtype: "Spell",
                deploymentCost: 2,
                operationCost: 1,
                baseDefense: 0,
                baseAttack: 0,
                baseCounterAttack: 0,
                rarity: CardRarity.Standard,
                setId: "TEST"
            );
            cardTypes.Add(cardType2);
            return cardTypes;
        }

        private static List<Card> CreateTestDeck()
        {
            var cards = new List<Card>();
            var testTypes = CreateTestCardTypes();
            foreach (var cardType in testTypes)
            {
                cards.Add(new Card(cardType));
            }
            return cards;
        }
    }
}
