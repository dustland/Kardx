using UnityEngine;

public class CardManager : MonoBehaviour
{
  public GameObject cardPrefab;  // 预制件
  public Transform cardArea;     // 手牌区域
  public Transform battleField;  // 战场区域

  // 添加卡牌到手牌
  public GameObject AddCardToHand()
  {
    GameObject newCard = Instantiate(cardPrefab, cardArea);  // 生成卡牌并放入手牌区域
    return newCard;
  }

  // 移动卡牌到战场
  public void PlayCard(GameObject card)
  {
    card.transform.SetParent(battleField, false);  // Move the full instance
  }
}