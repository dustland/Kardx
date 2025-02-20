using UnityEngine;
using UnityEngine.EventSystems;

public class BattleFieldDropZone : MonoBehaviour, IDropHandler
{
  public void OnDrop(PointerEventData eventData)
  {
    GameObject droppedCard = eventData.pointerDrag;
    if (droppedCard != null)
    {
      droppedCard.transform.SetParent(transform); // 让卡牌成为战场子物体
      droppedCard.transform.localPosition = Vector3.zero; // 让卡牌居中
    }
  }
}