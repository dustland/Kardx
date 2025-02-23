using System;
using System.Collections.Generic;
using System.IO;
using Kardx.Core;
using Kardx.Core.Data.Cards;
using Newtonsoft.Json;
using UnityEngine;

namespace Kardx.Core.Data
{
    public static class DeckLoader
    {
        public static List<Card> LoadDeck(string playerId)
        {
            var cards = new List<Card>();
            try
            {
                string filePath = Path.Combine(Application.streamingAssetsPath, $"cards.json");
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    var jsonCards = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                        jsonData
                    );

                    foreach (var jsonCard in jsonCards)
                    {
                        var cardType = new CardType();
                        cardType.Initialize(
                            id: jsonCard["id"].ToString(),
                            name: jsonCard["name"].ToString(),
                            description: jsonCard["description"].ToString(),
                            category: (CardCategory)
                                Enum.Parse(typeof(CardCategory), jsonCard["category"].ToString()),
                            subtype: jsonCard["subtype"].ToString(),
                            deploymentCost: Convert.ToInt32(jsonCard["deploymentCost"]),
                            operationCost: Convert.ToInt32(jsonCard["operationCost"]),
                            baseDefence: Convert.ToInt32(jsonCard["baseDefence"]),
                            baseAttack: Convert.ToInt32(jsonCard["baseAttack"]),
                            baseCounterAttack: Convert.ToInt32(jsonCard["baseCounterAttack"]),
                            rarity: (CardRarity)
                                Enum.Parse(typeof(CardRarity), jsonCard["rarity"].ToString()),
                            setId: jsonCard["setId"].ToString()
                        );

                        if (jsonCard.ContainsKey("imageUrl"))
                        {
                            cardType.SetImageUrl(jsonCard["imageUrl"].ToString());
                        }

                        cards.Add(new Card(cardType, ""));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading deck for {playerId}: {ex.Message}");
                return CreateTestDeck();
            }

            return cards.Count > 0 ? cards : CreateTestDeck();
        }

        private static List<Card> CreateTestDeck()
        {
            var testDeck = new List<Card>();
            var testCard = new CardType();
            testCard.Initialize(
                id: "TEST_01",
                name: "Test Card",
                description: "A test card",
                category: CardCategory.Unit,
                subtype: "Test",
                deploymentCost: 1,
                operationCost: 1,
                baseDefence: 1,
                baseAttack: 1,
                baseCounterAttack: 1,
                rarity: CardRarity.Standard,
                setId: "TEST"
            );

            // Add 30 copies of the test card
            for (int i = 0; i < 30; i++)
            {
                testDeck.Add(new Card(testCard, ""));
            }

            return testDeck;
        }
    }
}
