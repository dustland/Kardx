using System;
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
    [SerializeField] private CardType cardType;

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
    public CardType CardType => cardType;

    private void Awake()
    {
      dragHandler = GetComponent<CardDragHandler>();
    }

    private void Start()
    {
      // UpdateCardView();
    }

    public void UpdateCardView()
    {
      try
      {
        if (card == null && cardType == null)
        {
          Debug.LogWarning("[CardView] No card data available for update");
          return;
        }

        // Get the active card data source (either Card or CardType)
        var name = card != null ? card.Name : cardType?.Name ?? "";
        var description = card != null ? card.Description : cardType?.Description ?? "";
        var deploymentCost = card != null ? card.DeploymentCost : cardType?.DeploymentCost ?? 0;
        var operationCost = card != null ? card.OperationCost : cardType?.OperationCost ?? 0;
        var attack = card != null ? card.Attack : cardType?.BaseAttack ?? 0;
        var defence = card != null ? card.CurrentDefence : cardType?.BaseDefence ?? 0;
        var imageUrl = card != null ? card.ImageUrl : cardType?.ImageUrl;
        var abilities = card != null ? card.CardType.Abilities : cardType?.Abilities;

        // Update UI elements safely
        if (nameText != null) nameText.text = name;
        if (descriptionText != null) descriptionText.text = description;
        if (deploymentCostText != null) deploymentCostText.text = deploymentCost.ToString();
        if (operationCostText != null) operationCostText.text = operationCost.ToString();
        if (attackText != null) attackText.text = attack.ToString();
        if (defenceText != null) defenceText.text = defence.ToString();

        // Handle abilities if available
        if (abilities != null && abilities.Count > 0 && abilities[0] != null)
        {
          if (abilityText != null) abilityText.text = abilities[0].Name ?? "";
          if (abilityDescriptionText != null) abilityDescriptionText.text = abilities[0].Description ?? "";
        }
        else
        {
          if (abilityText != null) abilityText.text = "";
          if (abilityDescriptionText != null) abilityDescriptionText.text = "";
        }

        UpdateCardFrame();
        if (card != null)
        {
          UpdateModifierEffects();
        }

        LoadCardImage(imageUrl);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[CardView] Error updating card view: {ex.Message}");
      }
    }

    private void UpdateCardFrame()
    {
      var category = card != null ? card.CardType.Category : cardType?.Category ?? CardCategory.Unit;
      
      switch (category)
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

    public void SetDraggable(bool canDrag)
    {
      isDraggable = canDrag;
      if (dragHandler != null)
      {
        dragHandler.enabled = canDrag;
      }
    }

    public void Initialize(Card card)
    {
      this.card = card;
      this.cardType = null;
      SetDraggable(true);
      UpdateCardView();
    }

    public void Initialize(CardType cardType)
    {
      this.card = null;
      this.cardType = cardType;
      SetDraggable(false); // CardType view should not be draggable
      UpdateCardView();
    }

    private void OnDestroy()
    {
      if (dragHandler != null)
      {
        dragHandler.OnDragStarted -= HandleDragStarted;
        dragHandler.OnDragEnded -= HandleDragEnded;
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
  }
}