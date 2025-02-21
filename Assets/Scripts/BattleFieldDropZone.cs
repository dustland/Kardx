using UnityEngine;
using UnityEngine.EventSystems;

public class BattleFieldDropZone : MonoBehaviour, IDropHandler
{
  private CardManager cardManager;

  void Awake()
  {
    cardManager = FindObjectOfType<CardManager>();
  }

  public void OnDrop(PointerEventData eventData)
  {
    GameObject droppedCard = eventData.pointerDrag;
    if (droppedCard != null && CanAcceptCard(droppedCard))
    {
      // Set the card's new parent to the drop zone
      droppedCard.transform.SetParent(transform);

      // Notify the card manager
      cardManager.PlayCard(droppedCard);
    }
  }

  public bool CanAcceptCard(GameObject card)
  {
    // Basic validation - ensure we have a card and a card manager
    if (card == null || cardManager == null) return false;

    // Add your game-specific validation logic here
    return true;
  }
}