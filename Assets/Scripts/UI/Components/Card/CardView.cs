using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kardx.Core;

namespace Kardx.UI.Components.Card
{
  using Card = Kardx.Core.Data.Cards.Card; // Alias for Card
  public class CardView : MonoBehaviour
  {
    [Header("Card Data")]
    [SerializeField] private Card card;

    [Header("UI Elements")]
    [SerializeField] private Image cardImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private GameObject highlightEffect;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private CardDragHandler dragHandler;
    private bool isDraggable = true;

    public Card Card => card;

    private void Awake()
    {
      dragHandler = GetComponent<CardDragHandler>();
    }

    public void Initialize(Card cardData)
    {
      card = cardData;
      UpdateDisplay();

      if (dragHandler != null)
      {
        dragHandler.OnDragStarted += HandleDragStarted;
        dragHandler.OnDragEnded += HandleDragEnded;
      }
    }

    public void UpdateDisplay()
    {
      if (card == null) return;

      var cardType = card.CardType;

      nameText.text = cardType.NameKey;
      descriptionText.text = cardType.DescriptionKey;
      costText.text = cardType.DeploymentCost.ToString();
      attackText.text = card.Attack.ToString();
      healthText.text = $"{card.CurrentHealth}/{card.MaxHealth}";

      UpdateCardFrame();
      UpdateModifierEffects();
    }

    private void UpdateCardFrame()
    {
      switch (card.CardType.Category)
      {
        case CardCategory.Unit:
          break;
        case CardCategory.Order:
          break;
        case CardCategory.Countermeasure:
          break;
        case CardCategory.Headquarters:
          break;
      }
    }

    private void UpdateModifierEffects()
    {
      foreach (var modifier in card.Modifiers)
      {
      }
    }

    public void PlayDeployAnimation()
    {
      if (animator != null)
      {
        animator.SetTrigger("Deploy");
      }
    }

    public void PlayAttackAnimation()
    {
      if (animator != null)
      {
        animator.SetTrigger("Attack");
      }
    }

    public void PlayDamageAnimation()
    {
      if (animator != null)
      {
        animator.SetTrigger("TakeDamage");
      }
    }

    public void SetHighlight(bool isHighlighted)
    {
      if (highlightEffect != null)
      {
        highlightEffect.SetActive(isHighlighted);
      }
    }

    private void HandleDragStarted()
    {
      if (!isDraggable) return;
      transform.SetAsLastSibling();
    }

    private void HandleDragEnded(bool wasSuccessful)
    {
      if (!wasSuccessful)
      {
      }
    }

    public void SetDraggable(bool canDrag)
    {
      isDraggable = canDrag;
      if (dragHandler != null)
      {
        dragHandler.enabled = canDrag;
      }
    }

    private void OnDestroy()
    {
      if (dragHandler != null)
      {
        dragHandler.OnDragStarted -= HandleDragStarted;
        dragHandler.OnDragEnded -= HandleDragEnded;
      }
    }
  }
}