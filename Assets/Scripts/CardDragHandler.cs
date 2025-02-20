using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
  private RectTransform rectTransform;
  private Vector3 originalPosition;
  private CanvasGroup canvasGroup;

  void Start()
  {
    rectTransform = GetComponent<RectTransform>();
    canvasGroup = GetComponent<CanvasGroup>();
    originalPosition = transform.position;
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    canvasGroup.alpha = 0.6f;  // 透明度变暗
    canvasGroup.blocksRaycasts = false; // 允许拖动时穿透 UI
  }

  public void OnDrag(PointerEventData eventData)
  {
    rectTransform.position = eventData.position; // 让卡牌跟随鼠标
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    canvasGroup.alpha = 1.0f;
    canvasGroup.blocksRaycasts = true;

    // 如果没有放到合法区域，回到原位置
    transform.position = originalPosition;
  }
}