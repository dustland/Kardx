using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Kardx
{
    /// <summary>
    /// Attach this component to a GameObject to debug all event system events it receives.
    /// </summary>
    public class EventDebugger : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, 
        IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, 
        IDragHandler, IEndDragHandler, IDropHandler, IScrollHandler, 
        IUpdateSelectedHandler, ISelectHandler, IDeselectHandler, 
        IMoveHandler, ISubmitHandler, ICancelHandler
    {
        [SerializeField]
        private bool logPointerEvents = true;
        
        [SerializeField]
        private bool logDragEvents = true;
        
        [SerializeField]
        private bool logDropEvents = true;
        
        [SerializeField]
        private bool logOtherEvents = false;

        private void Awake()
        {
            Debug.Log($"[EventDebugger] Initialized on {gameObject.name}");
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (logPointerEvents)
                Debug.Log($"[EventDebugger] OnPointerEnter on {gameObject.name}");
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (logPointerEvents)
                Debug.Log($"[EventDebugger] OnPointerExit on {gameObject.name}");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (logPointerEvents)
                Debug.Log($"[EventDebugger] OnPointerDown on {gameObject.name}");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (logPointerEvents)
                Debug.Log($"[EventDebugger] OnPointerUp on {gameObject.name}");
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (logPointerEvents)
                Debug.Log($"[EventDebugger] OnPointerClick on {gameObject.name}");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (logDragEvents)
                Debug.Log($"[EventDebugger] OnBeginDrag on {gameObject.name}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (logDragEvents)
                Debug.Log($"[EventDebugger] OnDrag on {gameObject.name}");
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (logDragEvents)
                Debug.Log($"[EventDebugger] OnEndDrag on {gameObject.name}");
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (logDropEvents)
            {
                Debug.Log($"[EventDebugger] OnDrop on {gameObject.name}");
                
                // Log details about the drop event
                Debug.Log($"[EventDebugger] Drop details - pointerDrag: {eventData.pointerDrag?.name ?? "null"}, " +
                          $"pointerEnter: {eventData.pointerEnter?.name ?? "null"}, " +
                          $"position: {eventData.position}");
                
                // Log the raycast results under the pointer
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);
                
                Debug.Log($"[EventDebugger] Objects under pointer: {results.Count}");
                foreach (var result in results)
                {
                    Debug.Log($"[EventDebugger] - {result.gameObject.name} (Layer: {result.gameObject.layer}, " +
                              $"SortingOrder: {result.sortingOrder}, Distance: {result.distance})");
                }
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (logOtherEvents)
                Debug.Log($"[EventDebugger] OnScroll on {gameObject.name}");
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            if (logOtherEvents)
                Debug.Log($"[EventDebugger] OnUpdateSelected on {gameObject.name}");
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (logOtherEvents)
                Debug.Log($"[EventDebugger] OnSelect on {gameObject.name}");
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (logOtherEvents)
                Debug.Log($"[EventDebugger] OnDeselect on {gameObject.name}");
        }

        public void OnMove(AxisEventData eventData)
        {
            if (logOtherEvents)
                Debug.Log($"[EventDebugger] OnMove on {gameObject.name}");
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (logOtherEvents)
                Debug.Log($"[EventDebugger] OnSubmit on {gameObject.name}");
        }

        public void OnCancel(BaseEventData eventData)
        {
            if (logOtherEvents)
                Debug.Log($"[EventDebugger] OnCancel on {gameObject.name}");
        }
    }
}
