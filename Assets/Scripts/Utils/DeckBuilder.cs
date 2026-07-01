using System;
using System.Collections.Generic;
using System.Linq;
using Kardx.Models;
using Kardx.Models.Cards;

namespace Kardx.Utils
{
    /// <summary>
    /// Builds playable decks from card definitions with copy limits and faction filtering.
    /// </summary>
    public static class DeckBuilder
    {
        public static List<Card> BuildDeck(Faction faction, IReadOnlyList<CardType> allCardTypes)
        {
            var deck = new List<Card>();
            var rng = new Random();

            var playableTypes = allCardTypes
                .Where(t => t.Category == CardCategory.Unit
                    || t.Category == CardCategory.Order
                    || t.Category == CardCategory.Countermeasure)
                .ToList();

            foreach (var cardType in playableTypes)
            {
                int maxCopies = GetMaxCopies(cardType.Rarity);
                for (int i = 0; i < maxCopies; i++)
                {
                    deck.Add(new Card(cardType, faction));
                }
            }

            while (deck.Count < GameConstants.DeckSize && playableTypes.Count > 0)
            {
                var cardType = playableTypes[rng.Next(playableTypes.Count)];
                deck.Add(new Card(cardType, faction));
            }

            if (deck.Count > GameConstants.DeckSize)
            {
                deck = deck.OrderBy(_ => rng.Next()).Take(GameConstants.DeckSize).ToList();
            }

            return deck;
        }

        public static Card CreateHeadquarters(Faction faction, IReadOnlyList<CardType> allCardTypes)
        {
            var hqType = allCardTypes.FirstOrDefault(t =>
                t.Category == CardCategory.Headquarters
                && t.Id.Contains(faction == Faction.UnitedStates ? "us" : "soviet")
            );

            if (hqType == null)
            {
                hqType = allCardTypes.FirstOrDefault(t => t.Category == CardCategory.Headquarters);
            }

            if (hqType == null)
            {
                hqType = CreateDefaultHeadquartersType(faction);
            }

            return new Card(hqType, faction);
        }

        private static int GetMaxCopies(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Elite => GameConstants.MaxCopiesElite,
                CardRarity.Limited or CardRarity.Special => GameConstants.MaxCopiesLimited,
                _ => GameConstants.MaxCopiesStandard,
            };
        }

        private static CardType CreateDefaultHeadquartersType(Faction faction)
        {
            var hq = new CardType();
            hq.Initialize(
                id: $"hq-{faction.ToString().ToLower()}",
                title: $"{faction} Headquarters",
                description: "Your command center. Destroy the enemy HQ to win.",
                category: CardCategory.Headquarters,
                subtype: "HQ",
                deploymentCost: 0,
                operationCost: 0,
                baseDefense: GameConstants.HqBaseDefense,
                baseAttack: 0,
                baseCounterAttack: 0,
                rarity: CardRarity.Standard,
                setId: "base_set"
            );
            return hq;
        }
    }
}
