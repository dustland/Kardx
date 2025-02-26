using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kardx.Core;
using Kardx.Core.Strategy;

namespace Kardx.Examples
{
  /// <summary>
  /// Example of how to use the strategy system.
  /// </summary>
  public class StrategyExample : MonoBehaviour
  {
    [SerializeField]
    private StrategySource strategySource = StrategySource.Dummy;

    [SerializeField]
    private StrategyType dummyStrategyType = StrategyType.Random;

    [SerializeField]
    private string aiPersonality = "balanced";

    [SerializeField]
    private string player1Id = "Player";

    [SerializeField]
    private string player2Id = "Opponent";

    [SerializeField]
    private Faction player1Faction = Faction.SovietUnion;

    [SerializeField]
    private Faction player2Faction = Faction.UnitedStates;

    private MatchManager matchManager;

    // Start is called before the first frame update
    void Start()
    {
      // Create a match manager
      matchManager = new MatchManager(new UnityLogger());

      // Set the strategy source and configuration
      matchManager.SetStrategySource(strategySource);
      matchManager.SetDummyStrategyType(dummyStrategyType);
      matchManager.SetAIPersonality(aiPersonality);

      // Subscribe to events
      SubscribeToEvents();

      // Create players
      var player1 = new Player(player1Id, CreateDeck(player1Faction), player1Faction);
      var player2 = new Player(player2Id, CreateDeck(player2Faction), player2Faction);

      // Start the match
      matchManager.StartMatch(player1, player2);

      Debug.Log($"Match started with {strategySource} strategy");
    }

    private void SubscribeToEvents()
    {
      // Subscribe to strategy events
      matchManager.OnStrategyDetermined += OnStrategyDetermined;
      matchManager.OnStrategyActionExecuting += OnStrategyActionExecuting;
      matchManager.OnStrategyActionExecuted += OnStrategyActionExecuted;

      // Subscribe to turn events
      matchManager.OnTurnStarted += (sender, player) =>
          Debug.Log($"Turn started for {player.Id}");

      matchManager.OnTurnEnded += (sender, player) =>
          Debug.Log($"Turn ended for {player.Id}");
    }

    private void OnStrategyDetermined(Kardx.Core.Strategy.Strategy strategy)
    {
      Debug.Log($"Strategy determined: {strategy.Reasoning}");
      Debug.Log($"Actions: {strategy.Actions.Count}");

      foreach (var action in strategy.Actions)
      {
        string actionDetails = action.Type.ToString();

        // Add more details based on action type
        if (action.Type == StrategyActionType.DeployCard)
        {
          actionDetails += $": Card ID {action.TargetCardId}";
        }
        else if (action.Type == StrategyActionType.AttackWithCard ||
                action.Type == StrategyActionType.UseCardAbility ||
                action.Type == StrategyActionType.MoveCard ||
                action.Type == StrategyActionType.BuffCard ||
                action.Type == StrategyActionType.DebuffCard)
        {
          actionDetails += $": {action.SourceCardId} -> {action.TargetCardId}";

          if (action.Type == StrategyActionType.UseCardAbility)
          {
            actionDetails += $" (Ability {action.AbilityIndex})";
          }
        }
        else if (action.Type == StrategyActionType.DiscardCard ||
                action.Type == StrategyActionType.ReturnCardToHand)
        {
          actionDetails += $": Card ID {action.TargetCardId}";
        }

        Debug.Log($"- {actionDetails}");
      }
    }

    private void OnStrategyActionExecuting(StrategyAction action)
    {
      Debug.Log($"Executing action: {action.Type}");
    }

    private void OnStrategyActionExecuted(StrategyAction action)
    {
      Debug.Log($"Action executed: {action.Type}");
    }

    private List<Card> CreateDeck(Faction faction)
    {
      // Create a simple deck for testing
      var deck = new List<Card>();

      // Add some cards to the deck
      for (int i = 0; i < 30; i++)
      {
        deck.Add(new Card
        {
          Id = i + 1,
          Name = $"Card {i + 1}",
          Cost = Random.Range(1, 6),
          Attack = Random.Range(1, 5),
          Health = Random.Range(1, 5),
          Faction = faction
        });
      }

      return deck;
    }

    // Update is called once per frame
    void Update()
    {
      // Example of ending the player's turn when the space key is pressed
      if (Input.GetKeyDown(KeyCode.Space))
      {
        var board = matchManager.GetBoard();
        if (board != null && board.CurrentPlayer.Id == player1Id)
        {
          Debug.Log("Player ending turn");
          matchManager.EndTurn();
        }
      }

      // Example of changing the strategy source when the 1, 2, or 3 key is pressed
      if (Input.GetKeyDown(KeyCode.Alpha1))
      {
        matchManager.SetStrategySource(StrategySource.Dummy);
        Debug.Log("Switched to Dummy strategy");
      }
      else if (Input.GetKeyDown(KeyCode.Alpha2))
      {
        matchManager.SetStrategySource(StrategySource.AI);
        Debug.Log("Switched to AI strategy");
      }
      else if (Input.GetKeyDown(KeyCode.Alpha3))
      {
        matchManager.SetStrategySource(StrategySource.Remote);
        Debug.Log("Switched to Remote strategy");
      }

      // Example of changing the dummy strategy type when the Q, W, E, or R key is pressed
      if (Input.GetKeyDown(KeyCode.Q))
      {
        matchManager.SetDummyStrategyType(StrategyType.Aggressive);
        Debug.Log("Switched to Aggressive strategy type");
      }
      else if (Input.GetKeyDown(KeyCode.W))
      {
        matchManager.SetDummyStrategyType(StrategyType.Defensive);
        Debug.Log("Switched to Defensive strategy type");
      }
      else if (Input.GetKeyDown(KeyCode.E))
      {
        matchManager.SetDummyStrategyType(StrategyType.Balanced);
        Debug.Log("Switched to Balanced strategy type");
      }
      else if (Input.GetKeyDown(KeyCode.R))
      {
        matchManager.SetDummyStrategyType(StrategyType.Random);
        Debug.Log("Switched to Random strategy type");
      }
    }
  }
}