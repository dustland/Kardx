using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  private CanvasGroup canvasGroup;
  private RectTransform rectTransform;
  private Vector2 originalPosition;
  private Transform originalParent;
  private Image battlefieldBackground;
  public bool IsDragging { get; private set; }

  // Add reference to drop zone
  private BattleFieldDropZone dropZone;

  void Awake()
  {
    canvasGroup = GetComponent<CanvasGroup>();
    if (canvasGroup == null)
    {
      canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }
    rectTransform = GetComponent<RectTransform>();
    battlefieldBackground = GameObject.Find("BattleField").GetComponent<Image>();
    dropZone = FindObjectOfType<BattleFieldDropZone>();
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    IsDragging = true;
    canvasGroup.blocksRaycasts = false;
    originalPosition = rectTransform.anchoredPosition;
    originalParent = transform.parent;

    // Show battlefield background
    if (battlefieldBackground != null)
    {
      battlefieldBackground.color = new Color(1, 1, 1, 0.2f); // Semi-transparent white
    }
  }

  public void OnDrag(PointerEventData eventData)
  {
    rectTransform.position = eventData.position;
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    IsDragging = false;
    canvasGroup.blocksRaycasts = true;

    // Hide battlefield background
    if (battlefieldBackground != null)
    {
      battlefieldBackground.color = new Color(1, 1, 1, 0f);
    }

    // Check if we're over a valid drop zone
    if (dropZone == null || !dropZone.CanAcceptCard(gameObject))
    {
      ReturnToOriginalPosition();
    }

    // Force layout refresh
    LayoutRebuilder.ForceRebuildLayoutImmediate(transform.parent as RectTransform);
  }

  public void ReturnToOriginalPosition()
  {
    rectTransform.anchoredPosition = originalPosition;
    transform.SetParent(originalParent);
  }
}