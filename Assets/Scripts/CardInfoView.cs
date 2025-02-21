using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // Add DOTween namespace

public class CardInfoView : MonoBehaviour
{
  public Image cardDisplayImage;  // Reference to the Image component that will show the card
  private RectTransform cardRectTransform; // Reference to card image's RectTransform

  void Awake()
  {
    cardRectTransform = cardDisplayImage.GetComponent<RectTransform>();
    Debug.Log($"CardInfoView initialized with display image: {cardDisplayImage}", this);
  }

  public void ShowCard(Sprite cardSprite)
  {
    if (cardDisplayImage == null)
    {
      Debug.LogError("cardDisplayImage not assigned!", this);
      return;
    }

    // Reset rotation and scale of the card image
    cardRectTransform.localRotation = Quaternion.Euler(0, 90, 0);
    cardRectTransform.localScale = Vector3.one * 0.8f;

    cardDisplayImage.sprite = cardSprite;
    gameObject.SetActive(true);    // Show InfoView

    // Animate only the card image
    Sequence sequence = DOTween.Sequence();
    sequence.Append(cardRectTransform.DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack))
            .Join(cardRectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));
  }

  public void HideCard()
  {
    gameObject.SetActive(false);   // Hide InfoView
  }

  // Detect clicks outside the card to close the view
  void Update()
  {
    if (Input.GetMouseButtonDown(0))
    {
      HideCard();
    }
  }
}