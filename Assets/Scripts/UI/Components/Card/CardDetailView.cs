using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kardx.UI.Components.Card
{
    using Card = Kardx.Core.Data.Cards.Card;
    using CardType = Kardx.Core.Data.Cards.CardType;

    public class CardDetailView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private CardView cardView;

        private void Awake()
        {
            if (cardView == null)
            {
                cardView = GetComponentInChildren<CardView>();
            }
        }

        public void Show(CardType cardType)
        {
            if (cardView != null)
            {
                cardView.Initialize(cardType);
            }
            gameObject.SetActive(true);
        }

        public void Show(Card card)
        {
            if (cardView != null)
            {
                cardView.Initialize(card);
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Hide();
        }
    }
}
