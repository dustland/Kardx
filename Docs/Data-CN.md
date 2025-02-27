# Kardx 游戏数据架构

[View English Version](./Data.md)

## 介绍

本文档介绍了 Kardx 的数据架构，这是一款受 [Kards](https://kards.com) 启发的卡牌游戏。该架构强调模块化、数据驱动的能力和可扩展性，结构分为三个主要层次：

- **数据层**：管理静态定义、区域和基线属性。
- **逻辑层**：实现游戏规则、回合阶段以及能力和效果的动态应用。
- **UI 层**：负责数据可视化，支持动画、延迟加载图像（WebP）和响应式设计。

数据层和逻辑层共同构成核心系统，设计为独立于 UnityEngine 运行。

## 核心数据模型

### 战局状态

战局封装了核心游戏状态，强制执行严格的双人模式，具有集中式回合管理和效果跟踪。它维护玩家引用、活动的全局效果和整体游戏进程。

```csharp
public class Board
{
    private readonly Player[] players = new Player[2];
    private readonly List<GameEffect> activeEffects = new();
    private int turnNumber = 1;
    private string currentPlayerId;

    public Player CurrentPlayer => GetPlayerById(currentPlayerId);
    public Player Player => players[0];
    public Player Opponent => players[1];
}
```

### 卡牌数据模型

卡牌使用双层设计实现，区分静态数据（由 CardType 定义）和运行时状态。这种分离使内存使用效率更高，并在卡牌定义和实例特定修改之间提供清晰的界限。每张卡牌包括：

- **静态数据**：基础统计、能力和费用
- **运行时状态**：当前值、活动修饰符和可见性状态
- **实例数据**：唯一标识符、所有权详细信息和动态效果

```csharp
public class Card
{
    private Guid instanceId;
    private CardType cardType;
    private bool isFaceDown;
    private Faction ownerFaction;
    private List<Modifier> modifiers = new();
    private int currentDefence;
    private string currentAbilityId;

    public int Defense => cardType.BaseDefence + GetAttributeModifier("defense");
    public int Attack => cardType.BaseAttack + GetAttributeModifier("attack");
    public int DeploymentCost => cardType.DeploymentCost;
}
```

### 玩家状态

玩家模型管理所有玩家特定的资源和卡牌集合。将这些集合公开为只读，确保状态修改仅通过验证的方法进行。关键属性包括：

- **战场容量**：固定大小为 5 个槽位
- **手牌限制**：最多 10 张牌
- **信用额度**：每位玩家 9 个信用
- **阵营对齐**
- **总部卡牌**

```csharp
public class Player
{
    public const int BATTLEFIELD_SLOT_COUNT = 5;
    private const int MAX_HAND_SIZE = 10;
    private const int MAX_CREDITS = 9;

    private string playerId;
    private int credits;
    private Faction faction;

    private readonly List<Card> hand = new();
    private readonly Card[] battlefield = new Card[BATTLEFIELD_SLOT_COUNT];
    private readonly Stack<Card> deck = new();
    private readonly Queue<Card> discardPile = new();
    private Card headquartersCard;

    public IReadOnlyList<Card> Hand => hand;
    public IReadOnlyList<Card> Battlefield => Array.AsReadOnly(battlefield);
    public IReadOnlyList<Card> Deck => deck.ToList();
    public IReadOnlyList<Card> DiscardPile => discardPile.ToList();
}
```

## 数据类型

### 枚举

游戏使用多个枚举来定义分类数据，确保系统的一致性和清晰性：

```csharp
public enum CardCategory
{
    Unit,           // 可部署单位
    Order,          // 一次性效果
    Countermeasure, // 旨在抵消对手的行动
    Headquarter,    // 部署在战场上的特殊卡牌
}

public enum Faction
{
    UnitedStates,    // 美国部队
    SovietUnion,     // 苏联部队
    BritishEmpire,   // 英国部队
    ThirdReich,      // 德国部队
    Empire,          // 日本部队
    Neutral          // 非对齐卡牌或特殊情况
}

public enum CardRarity
{
    Standard = 1,
    Limited = 2,
    Special = 3,
    Elite = 4,
}

public enum ZoneType
{
    Deck,
    Hand,
    Battlefield,
    DiscardPile,
    Exile,
    Limbo,
}

public enum ModifierType
{
    Buff,
    Debuff,
    Status,
}

public enum AbilityCategory
{
    Tactic,
    Passive,
}

public enum EffectCategory
{
    Damage,
    Heal,
    Buff,
    Debuff,
    Draw,
    Discard,
}

public enum TriggerType
{
    OnDeployment,
    OnDeath,
    OnTurnStart,
    OnTurnEnd,
    OnDamageDealt,
    OnDamageTaken,
}
```

## 核心系统

### 游戏状态管理

游戏状态按层次结构组织，从比赛级别到单个卡牌属性。此结构确保清晰的所有权、有效的状态更新和效果的适当范围。

```csharp
战局
├── Players[2]                    // 恰好 2 名玩家
│   ├── Deck                     // 可供抽取的卡牌
│   ├── Hand                     // 手中持有的卡牌（最多 10 张）
│   ├── Battlefield[5]           // 固定部署槽位
│   │   └── Card                 // 部署的卡牌
│   │       ├── CardType         // 静态数据定义
│   │       ├── RuntimeState     // 动态值（例如，防御，攻击）
│   │       │   └── Modifiers    // 临时效果
│   │       └── InstanceData     // 唯一属性（ID，所有权，面朝下）
│   └── DiscardPile             // 被丢弃或移出游戏的卡牌
└── ActiveEffects               // 影响游戏规则的全局效果
```

此结构保证：

1. 通过区域放置实现明确的卡牌所有权
2. 静态定义与动态游戏状态之间的分离
3. 本地化更新以提高效率
4. 为全局和卡牌特定效果提供适当的范围
5. 在游戏组件之间保持一致的状态跟踪

### 状态转换

#### 卡牌移动

卡牌移动机制强制执行严格的验证层次结构。`Player` 类管理基本的区域转换和资源验证，而 `MatchManager` 强制执行整体游戏规则和回合顺序。移动是原子的并经过严格验证。

关键验证包括：

- 最大手牌数量（10 张牌）
- 足够的资源（可用于部署的信用）
- 有效的战场槽位可用性（5 个位置）
- 卡牌所有权和游戏状态的一致性

```csharp
public class Player
{
    public Card DrawCard(bool faceDown = false);
    public bool DeployCard(Card card, int position);
    public bool DiscardFromHand(Card card);
    public bool RemoveFromBattlefield(Card card);
}

public class MatchManager
{
    public bool CanDeployCard(Card card);
    public bool DeployCard(Card card, int position);
}
```

这些措施确保：

1. 卡牌仅在合法区域之间转换
2. 所有转换在生效前经过验证
3. 资源成本得到适当应用
4. 状态修改以原子方式执行

#### 回合阶段

回合进程遵循结构化的顺序，由以下阶段定义：

```csharp
public enum TurnPhase
{
    StartTurn,    // 维护和抽牌
    MainPhase,    // 部署和激活
    CombatPhase,  // 攻击声明
    ResponsePhase,// 反制行动
    EndPhase      // 清理和效果解析
}
```

### 资源管理

资源管理遵循严格的层次模型。各个玩家管理他们的信用和卡牌槽位，而 `MatchManager` 设置全局约束，例如回合限制和起始条件。

**游戏常量：**

- 战场槽位：每位玩家 5 个
- 最大手牌数量：10 张牌
- 信用额度：每位玩家 9 个
- 起始手牌：4 张牌
- 最大回合数：50

```csharp
public class Player
{
    public const int BATTLEFIELD_SLOT_COUNT = 5;
    private const int MAX_HAND_SIZE = 10;
    private const int MAX_CREDITS = 9;

    private int credits;
    private readonly List<Card> hand = new();
    private readonly Card[] battlefield = new Card[BATTLEFIELD_SLOT_COUNT];
    private readonly Stack<Card> deck = new();
    private readonly Queue<Card> discardPile = new();

    public bool SpendCredits(int amount);
    public void AddCredits(int amount);
    public void StartTurn(int turnNumber);
}

public class MatchManager
{
    private readonly int startingHandSize = 4;
    private readonly int maxTurns = 50;

    public void StartMatch(string player1Id, string player2Id,
                         Faction player1Faction, Faction player2Faction);
}
```

关键特性：

1. 强制资源限制
2. 基于回合的信用生成
3. 资源消耗的严格验证
4. 原子资源交易
5. 范围资源状态跟踪

### 数据持久性

卡牌定义以结构化格式存储，确保一致性并促进序列化。例如：

```json
{
  "id": "INF-001",
  "title": "Veteran Infantry",
  "category": "Unit",
  "rarity": "Standard",
  "stats": {
    "attack": 2,
    "defense": 3,
    "deploymentCost": 2
  },
  "abilities": [
    {
      "id": "FirstStrike",
      "trigger": "OnAttack",
      "effect": "DamageFirst"
    }
  ]
}
```

未来，持久化游戏历史将是必要的，以增强玩家体验和分析。这将允许跟踪玩家进度、成就，并为游戏平衡和改进提供有价值的见解。

## 最佳实践

1. **数据不可变性：**

   - 将卡牌定义加载为只读
   - 保持不可变的实例 ID
   - 以追加方式记录历史操作

2. **状态验证**

   - 强制执行区域转换的定义路径
   - 在所有边界验证资源限制
   - 在应用更改之前验证卡牌状态

3. **数据一致性**

   - 确保所有状态更改是原子的
   - 验证所有引用
   - 明确区分派生数据

4. **性能考虑**
   - 在可行的情况下批量状态更新
   - 缓存频繁访问的数据
   - 适当预设集合大小

## 数据驱动的战斗系统

数据驱动的战斗系统利用外部 JSON 配置文件定义游戏规则、卡牌属性和能力，使游戏在不更改引擎代码的情况下高度可调。

例如，JSON 配置文件可能如下所示：

```json
{
  "cards": [
    {
      "id": "INF-001",
      "title": "Veteran Infantry",
      "category": "Unit",
      "rarity": "Standard",
      "stats": {
        "attack": 2,
        "defense": 3,
        "deploymentCost": 2
      },
      "abilities": [
        {
          "id": "FirstStrike",
          "trigger": "OnAttack",
          "effect": "DamageFirst",
          "parameters": {
            "multiplier": 1.5
          }
        }
      ]
    },
    {
      "id": "ORD-001",
      "title": "Order: Swift Maneuver",
      "category": "Order",
      "rarity": "Limited",
      "stats": {
        "deploymentCost": 1
      },
      "abilities": [
        {
          "id": "SpeedBoost",
          "trigger": "OnDeployment",
          "effect": "IncreaseSpeed",
          "parameters": {
            "duration": 2
          }
        }
      ]
    }
  ]
}
```

此 JSON 文件定义了每张卡牌的规则：

- **卡牌定义**：每张卡牌由 ID、标题、类别和稀有度标识，将其置于游戏生态系统中。
- **统计**：基础属性（攻击、防御、部署成本）决定战斗表现。
- **能力**：每个能力都通过触发器（例如 OnAttack 或 OnDeployment）、效果和参数来微调其行为。

1. **解析和验证：**

   - 在游戏启动时，规则引擎读取并反序列化 JSON 文件为结构化数据对象。
   - 这些对象经过验证，以确保所有必需属性都存在且格式良好。

2. **规则编译：**

   - 对于每张卡牌，引擎编译基础统计并注册能力。例如，具有触发器 "OnAttack" 的能力与卡牌的攻击动作相关联。

3. **动态执行：**

   - 在游戏过程中，当事件发生时（例如，卡牌攻击或部署），引擎从编译数据中查找适当的规则。
   - 然后，它应用 JSON 中定义的任何修饰符或额外效果，计算伤害或触发所需的额外效果。

4. **平衡调整：**
   - 游戏设计师可以更新 JSON 以调整统计和能力，从而在不修改引擎代码的情况下快速迭代游戏机制。

这种方法将游戏逻辑与数据定义分离，促进灵活和可维护的战斗系统设计。

## UI 组件

UI 层负责可视化数据和处理导致状态变更的用户交互。尽管其实现不是本文档的重点，但下面提供了简要概述。

### CardView 组件

CardView 组件渲染卡牌的状态，处理：

- 可见性（面朝上/面朝下）
- 内容更新
- 交互事件
- 视觉效果（例如，拖放，动画）

显著特性包括基于卡牌状态的自动刷新和用于显示卡牌详细信息的专用视图。

```csharp
public class CardView : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private Sprite cardBackSprite;

    private void ShowCardBack()
    {
        contentRoot.gameObject.SetActive(false);
        backgroundImage.sprite = cardBackSprite;
    }

    public void UpdateCardView()
    {
        if (card != null && card.FaceDown)
        {
            ShowCardBack();
            return;
        }
        // 显示标准卡牌内容...
    }
}
```

## 结论

本文档提供了 Kardx 数据架构的全面概述，这是一款受 Kards 启发的卡牌游戏。通过将架构划分为不同的层次——数据、逻辑和 UI 层——我们确保了模块化、可扩展和可维护的代码库。静态定义与运行时状态之间的分离使内存使用效率更高，并支持动态游戏功能，同时在游戏元素之间保持一致性。

此设计的关键优势包括：

- **清晰性和模块化**：层次结构清晰地定义了棋盘、玩家和卡牌级别的职责，允许直接的状态管理和更新。
- **强大的验证**：严格的验证机制集成到卡牌移动和资源管理系统中，确保所有状态更改以原子方式发生并符合游戏规则。
- **可扩展性**：关注点的分离允许未来的增强和可扩展性，适应更复杂的游戏动态而不影响可维护性。
- **性能和一致性**：通过战略缓存、集合的预设大小和精确的状态转换，设计在游戏数据中平衡性能和一致性。

展望未来，潜在的未来发展领域包括自适应难度机制的整合、更深入的全局效果管理集成以及数据持久性策略的改进。这些改进旨在进一步提升性能并增强整体玩家体验。

此设计为构建稳健和可扩展的游戏系统奠定了坚实的基础，提供了可扩展到更广泛的数据驱动交互应用程序的见解。
