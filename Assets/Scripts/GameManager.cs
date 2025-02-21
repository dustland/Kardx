using UnityEngine;

public class GameManager : MonoBehaviour
{
  public CardManager cardManager;  // 引用 CardManager
  private CardInfoView cardInfoView;

  void Start()
  {
    // Initialize player's hand
    for (int i = 0; i < 5; i++)
    {
      cardManager.AddCardToHand();
    }

    // Find CardInfoView
    cardInfoView = GameObject.FindObjectOfType<CardInfoView>(includeInactive: true);
    if (cardInfoView == null)
    {
      Debug.LogError("CardInfoView not found in scene!");
    }
  }

  public CardInfoView GetCardInfoView()
  {
    return cardInfoView;
  }
}