using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Card : MonoBehaviour, IPointerClickHandler
{
  private Image cardImage;
  private GameManager gameManager;

  void Awake()
  {
    // Get components on initialization
    cardImage = GetComponent<Image>();
    gameManager = FindObjectOfType<GameManager>();
  }

  public void OnPointerClick(PointerEventData eventData)
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