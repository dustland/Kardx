using Kardx.UI.Components.Card;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kardx.UI.Scenes.Battle
{
    using Card = Kardx.Core.Data.Cards.Card;

    public class BattlefieldDropZone
        : MonoBehaviour,
            IDropHandler,
            IPointerEnterHandler,
            IPointerExitHandler
    {
        [Header("References")]
        [SerializeField]
        private BattleView battleView;

        [Header("Visual Feedback")]
        [SerializeField]
        private GameObject highlightEffect;

        private void Start()
        {
            // Ensure we have a reference to BattleView
            if (battleView == null)
            {
                battleView = GetComponentInParent<BattleView>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView != null && IsValidDropTarget(cardView.Card))
            {
                SetHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        public void OnDrop(PointerEventData eventData)
        {
            CardView cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView == null)
                return;

            if (IsValidDropTarget(cardView.Card))
            {
                HandleCardDrop(cardView);
            }
        }

        private void HandleCardDrop(CardView cardView)
        {
            if (battleView == null)
                return;

            // Try to deploy the card through BattleManager
            if (battleView.DeployCard(cardView.Card))
            {
                // Move the card to this drop zone and position it properly
                cardView.transform.SetParent(transform);
                cardView.transform.localPosition = Vector3.zero;
                cardView.transform.localScale = Vector3.one;
                
                SetHighlight(false);
            }
        }

        private void SetHighlight(bool active)
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(active);
            }
        }

        public bool IsValidDropTarget(Card card)
        {
            if (card == null || battleView == null)
                return false;

            // Check if this is a player's battlefield and if the card can be deployed
            return transform.parent.CompareTag("PlayerBattlefield")
                && battleView.CanDeployCard(card);
        }
    }
}
