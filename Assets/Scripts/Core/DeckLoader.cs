using System;
using System.Collections.Generic;
using System.IO;
using Kardx.Core;
using Newtonsoft.Json;

namespace Kardx.Core
{
    public static class DeckLoader
    {
        private static readonly string DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Kardx",
            "Data"
        );

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public static List<Card> LoadDeck(string playerId)
        {
            var cards = new List<Card>();
            try
            {
                string filePath = Path.Combine(DataDirectory, "cards.json");
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    var cardTypes = JsonConvert.DeserializeObject<List<CardType>>(jsonData, JsonSettings);

                    foreach (var cardType in cardTypes)
                    {
                        cards.Add(new Card(cardType));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error loading deck for {playerId}: {ex.Message}");
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
                title: "Test Card",
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
                testDeck.Add(new Card(testCard));
            }

            return testDeck;
        }
    }
}
