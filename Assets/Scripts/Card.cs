using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerClickHandler
{
  private CardManager cardManager;

  void Start()
  {
    // 在场景中查找 CardManager
    cardManager = FindObjectOfType<CardManager>();
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    // 当卡牌被点击时，移动到战场
    Debug.Log("Card clicked", gameObject);
    cardManager.PlayCard(gameObject);
  }
}