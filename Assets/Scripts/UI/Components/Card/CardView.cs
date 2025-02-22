using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Kardx.Core;
using Kardx.Core.Data.Cards;

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
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI deploymentCostText;
    [SerializeField] private TextMeshProUGUI operationCostText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI defenceText;
    [SerializeField] private TextMeshProUGUI abilityText;
    [SerializeField] private TextMeshProUGUI abilityDescriptionText;
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

    private void Start()
    {
      UpdateCardView();
    }

    public void UpdateCardView()
    {
      if (card == null) return;

      nameText.text = card.Name;
      descriptionText.text = card.Description;
      deploymentCostText.text = card.DeploymentCost.ToString();
      operationCostText.text = card.OperationCost.ToString();
      attackText.text = card.Attack.ToString();
      defenceText.text = card.CurrentDefence.ToString();
      abilityText.text = card.CurrentAbility.Name;
      abilityDescriptionText.text = card.CurrentAbility.Description;

      UpdateCardFrame();
      UpdateModifierEffects();

      // Load and set the card image
      // Assuming you have a method to load images from URLs
      LoadCardImage(card.ImageUrl);
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
        case CardCategory.Headquarter:
          break;
      }
    }

    private void UpdateModifierEffects()
    {
      foreach (var modifier in card.Modifiers)
      {
      }
    }

    private void LoadCardImage(string url)
    {
      // Implement image loading logic here
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

    public void Initialize(Card card)
    {
      this.card = card;
      UpdateCardView();
    }
  }
}