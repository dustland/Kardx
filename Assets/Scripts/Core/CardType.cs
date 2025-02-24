using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Kardx.Core
{
    public class CardType
    {
        // Identity
        [JsonProperty("id")]
        private string id; // Unique identifier (GUID or slug)
        [JsonProperty("title")]
        private string title; // Localization key for the card name
        [JsonProperty("description")]
        private string description; // Localization key for the card description
        [JsonProperty("category")]
        private CardCategory category; // e.g., Unit, Order, Countermeasure
        [JsonProperty("subtype")]
        private string subtype; // Archetype (e.g., Warrior, Mage)

        // Costs
        [JsonProperty("deploymentCost")]
        private int deploymentCost; // Resource cost to play the card
        [JsonProperty("operationCost")]
        private int operationCost; // Resource cost to use abilities

        // Stats
        [JsonProperty("baseDefence")]
        private int baseDefence; // Base defense of the card
        [JsonProperty("baseAttack")]
        private int baseAttack; // Attack power of the card
        [JsonProperty("baseCounterAttack")]
        private int baseCounterAttack; // Power used for counterattacks

        // Metadata
        [JsonProperty("rarity")]
        private CardRarity rarity; // Card rarity level
        [JsonProperty("setId")]
        private string setId; // Card edition or set identifier

        // Visuals
        [JsonProperty("imageUrl")]
        private string imageUrl; // Optimized WebP image URL

        // Attributes & Abilities
        [JsonProperty("attributes")]
        private SerializableDictionary<string, int> attributes = new();
        [JsonProperty("abilities")]
        private List<AbilityType> abilities = new();

        // Public properties with validation
        public string Id => id;
        public string Title => title;
        public string Description => description;
        public CardCategory Category => category;
        public string Subtype => subtype;
        public int DeploymentCost => deploymentCost;
        public int OperationCost => operationCost;
        public int BaseDefence => baseDefence;
        public int BaseAttack => baseAttack;
        public int BaseCounterAttack => baseCounterAttack;
        public CardRarity Rarity => rarity;
        public string SetId => setId;
        public string ImageUrl => imageUrl;
        public IReadOnlyDictionary<string, int> Attributes => attributes;
        public IReadOnlyList<AbilityType> Abilities => abilities;

        // Clone this card type
        public CardType Clone()
        {
            return new CardType
            {
                id = Guid.NewGuid().ToString(),
                title = $"{title} (Clone)",
                description = description,
                category = category,
                subtype = subtype,
                deploymentCost = deploymentCost,
                operationCost = operationCost,
                baseDefence = baseDefence,
                baseAttack = baseAttack,
                baseCounterAttack = baseCounterAttack,
                rarity = rarity,
                setId = setId,
                imageUrl = imageUrl,
                attributes = new SerializableDictionary<string, int>(attributes),
                abilities = new List<AbilityType>(abilities)
            };
        }

        // Factory method to create from JSON
        public static CardType FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CardType>(json);
        }

        // Method to convert to JSON
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public void Initialize(
            string id,
            string title,
            string description,
            CardCategory category,
            string subtype,
            int deploymentCost,
            int operationCost,
            int baseDefence,
            int baseAttack,
            int baseCounterAttack,
            CardRarity rarity,
            string setId
        )
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.category = category;
            this.subtype = subtype;
            this.deploymentCost = Math.Max(0, deploymentCost);
            this.operationCost = Math.Max(0, operationCost);
            this.baseDefence = Math.Max(1, baseDefence);
            this.baseAttack = Math.Max(0, baseAttack);
            this.baseCounterAttack = Math.Max(0, baseCounterAttack);
            this.rarity = rarity;
            this.setId = setId;

            if (string.IsNullOrEmpty(this.id))
            {
                this.id = Guid.NewGuid().ToString();
            }
        }

        // Method to set the image URL
        public void SetImageUrl(string url)
        {
            this.imageUrl = url;
        }
    }

    // Helper class for serializing dictionaries
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public SerializableDictionary() : base() { }

        public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary) { }
    }
}
