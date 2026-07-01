using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Kardx.Models.Cards;
using Kardx.Models.Match;
using Kardx.Views.Cards;

namespace Kardx.Views.Match
{
    /// <summary>
    /// Displays a player's headquarters. Attack targeting is handled by CardDragController.
    /// </summary>
    public class HeadquarterView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private bool isOpponentHq = true;
        [SerializeField] private Color highlightColor = new Color(1f, 0.2f, 0f, 0.7f);

        private MatchManager matchManager;
        private Image highlightImage;
        private TextMeshProUGUI healthText;
        private Image backgroundImage;

        public bool IsOpponentHq => isOpponentHq;

        public void Initialize(MatchManager matchManager, bool opponentHq)
        {
            this.matchManager = matchManager;
            this.isOpponentHq = opponentHq;
            EnsureComponents();
            UpdateDisplay();
        }

        private void EnsureComponents()
        {
            backgroundImage = GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
                backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            }
            backgroundImage.raycastTarget = true;

            var healthObj = transform.Find("HealthText");
            if (healthObj == null)
            {
                var go = new GameObject("HealthText");
                go.transform.SetParent(transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                healthText = go.AddComponent<TextMeshProUGUI>();
                healthText.alignment = TextAlignmentOptions.Center;
                healthText.fontSize = 14;
                healthText.color = Color.white;
            }
            else
            {
                healthText = healthObj.GetComponent<TextMeshProUGUI>();
            }

            var highlightObj = transform.Find("Highlight");
            if (highlightObj == null)
            {
                var go = new GameObject("Highlight");
                go.transform.SetParent(transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                highlightImage = go.AddComponent<Image>();
                highlightImage.color = highlightColor;
                highlightImage.raycastTarget = false;
                highlightImage.enabled = false;
            }
            else
            {
                highlightImage = highlightObj.GetComponent<Image>();
            }
        }

        public void UpdateDisplay()
        {
            if (matchManager == null)
                return;

            var owner = isOpponentHq ? matchManager.Opponent : matchManager.Player;
            var hq = owner?.Headquarter;
            if (hq == null || healthText == null)
                return;

            healthText.text = $"HQ\n{hq.CurrentDefense}/{hq.Defense}";
        }

        public void SetHighlight(bool active)
        {
            if (highlightImage != null)
                highlightImage.enabled = active;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanAcceptAttackFromDrag(eventData))
                return;
            SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        private bool CanAcceptAttackFromDrag(PointerEventData eventData)
        {
            if (matchManager == null || !isOpponentHq || !matchManager.IsPlayerTurn())
                return false;

            var cardView = eventData.pointerDrag?.GetComponent<CardView>();
            if (cardView?.Card == null)
                return false;

            return matchManager.CanAttackHQ(cardView.Card, matchManager.Opponent);
        }
    }
}
