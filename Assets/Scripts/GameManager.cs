using UnityEngine;

public class GameManager : MonoBehaviour
{
  public CardManager cardManager;  // 引用 CardManager

  void Start()
  {
    // 初始化玩家手牌（假设起始手牌 5 张）
    for (int i = 0; i < 5; i++)
    {
      cardManager.AddCardToHand();
    }
  }
}