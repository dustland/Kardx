using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IPointerClickHandler
{
  private Image cardImage;
  private GameManager gameManager;
  private CardDragHandler dragHandler;

  void Awake()
  {
    // Get components on initialization
    cardImage = GetComponent<Image>();
    gameManager = FindObjectOfType<GameManager>();
    dragHandler = GetComponent<CardDragHandler>();
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    // Only handle click if we're not dragging
    if (!dragHandler.IsDragging)
    {
      var cardInfoView = gameManager.GetCardInfoView();
      if (cardInfoView != null)
      {
        // First activate the GameObject to ensure Awake() is called
        cardInfoView.gameObject.SetActive(true);
        cardInfoView.ShowCard(cardImage.sprite);
      }
    }
  }
}