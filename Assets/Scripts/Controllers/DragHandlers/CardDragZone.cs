using UnityEngine;
using UnityEngine.UI;

namespace Kardx.Controllers.DragHandlers
{
    /// <summary>
    /// Marks a screen region as a valid drop zone. Contains no drop logic.
    /// </summary>
    public class CardDragZone : MonoBehaviour
    {
        [SerializeField] private CardDragTargetKind zoneKind;

        public CardDragTargetKind ZoneKind => zoneKind;

        public void Configure(CardDragTargetKind kind)
        {
            zoneKind = kind;
        }

        private void Awake()
        {
            var graphic = GetComponent<Graphic>();
            if (graphic == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
                image.raycastTarget = true;
            }
        }
    }
}
