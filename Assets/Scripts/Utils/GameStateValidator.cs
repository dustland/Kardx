using System.Collections.Generic;
using System.Linq;
using Kardx.Models;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using UnityEngine;

namespace Kardx.Utils
{
    /// <summary>
    /// Validates game state integrity across zones and player collections.
    /// </summary>
    public static class GameStateValidator
    {
        public static List<string> ValidateMatch(Board board)
        {
            var errors = new List<string>();
            if (board == null)
            {
                errors.Add("Board is null");
                return errors;
            }

            errors.AddRange(ValidatePlayer(board.Player));
            errors.AddRange(ValidatePlayer(board.Opponent));
            return errors;
        }

        public static List<string> ValidatePlayer(Player player)
        {
            var errors = new List<string>();
            if (player == null)
            {
                errors.Add("Player is null");
                return errors;
            }

            var allCards = new HashSet<string>();
            void TrackCard(Card card, string location)
            {
                if (card == null)
                    return;

                if (!allCards.Add(card.InstanceId))
                {
                    errors.Add($"[{player.Id}] Card {card.Title} ({card.InstanceId}) appears in multiple zones (also in {location})");
                }
            }

            foreach (var card in player.Deck.Cards)
                TrackCard(card, "deck");
            foreach (var card in player.Hand.Cards)
                TrackCard(card, "hand");
            foreach (var card in player.Battlefield.Cards)
                TrackCard(card, "battlefield");
            foreach (var card in player.DiscardPile.Cards)
                TrackCard(card, "discard");

            if (player.Headquarter != null)
                TrackCard(player.Headquarter, "headquarters");

            if (player.Hand.Count > GameConstants.MaxHandSize)
            {
                errors.Add($"[{player.Id}] Hand exceeds max size: {player.Hand.Count}/{GameConstants.MaxHandSize}");
            }

            if (player.Credits > GameConstants.MaxCredits)
            {
                errors.Add($"[{player.Id}] Credits exceed max: {player.Credits}/{GameConstants.MaxCredits}");
            }

            if (player.Headquarter == null)
            {
                errors.Add($"[{player.Id}] Missing headquarters card");
            }

            return errors;
        }

        public static void LogValidationResults(Board board)
        {
            var errors = ValidateMatch(board);
            if (errors.Count == 0)
            {
                Debug.Log("[GameStateValidator] State is valid");
                return;
            }

            foreach (var error in errors)
            {
                Debug.LogWarning($"[GameStateValidator] {error}");
            }
        }
    }
}
