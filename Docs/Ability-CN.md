# Kardx 技能系统设计文档

[English Version](./Ability.md)

## 概述

Kardx 技能系统采用**数据驱动**设计，为游戏卡牌的技能定义和执行提供灵活的框架。通过借鉴 [Kards](https://kards.com) 的游戏机制，该系统既能支持基础技能类型，也能轻松扩展到复杂的战略性卡牌效果。

### 核心设计理念

Kardx 技能系统建立在四个核心设计理念上：

- **数据与逻辑分离**：技能定义（静态数据）与技能实例（运行时状态）清晰分离
- **声明式配置**：通过 JSON 定义技能，无需代码即可创建新技能
- **可组合性**：复杂技能可由基本效果组合构建
- **事件驱动触发**：基于游戏状态变化自动触发相应技能

## 数据结构

### AbilityType（技能类型）

`AbilityType` 定义了可以附加到卡牌的技能的静态属性。这是技能的模板或蓝图。

```csharp
public class AbilityType
{
    // 基本信息
    public string Id { get; private set; }           // 唯一标识符
    public string Name { get; private set; }         // 显示名称
    public string Description { get; private set; }  // 文本描述
    public string IconPath { get; private set; }     // 技能图标路径

    // 激活参数
    public TriggerType Trigger { get; private set; } // 触发类型
    public int CooldownTurns { get; private set; }   // 冷却回合数
    public int UsesPerTurn { get; private set; }     // 每回合使用次数
    public int UsesPerMatch { get; private set; }    // 每场比赛使用次数
    public bool RequiresFaceUp { get; private set; } // 是否需要正面朝上
    public int OperationCost { get; private set; }   // 使用成本

    // 目标参数
    public TargetingType Targeting { get; private set; } // 目标类型
    public RangeType Range { get; private set; }         // 范围类型
    public bool CanTargetFaceDown { get; private set; }  // 是否可目标面朝下卡牌

    // 效果参数
    public EffectType Effect { get; private set; }      // 效果类型
    public int EffectValue { get; private set; }        // 效果值
    public int EffectDuration { get; private set; }     // 效果持续回合数

    // 特殊参数
    public string SpecialEffectId { get; private set; }              // 特殊效果ID
    public Dictionary<string, object> CustomParameters { get; }      // 自定义参数

    // 方法
    public AbilityType Clone();                         // 创建此技能类型的副本
    public void SetCustomParameter(string key, object value);  // 设置自定义参数
    public T GetCustomParameter<T>(string key, T defaultValue = default);  // 获取带类型转换的自定义参数

    // 序列化
    public static AbilityType FromJson(string json);    // 从JSON创建技能类型
    public string ToJson();                             // 转换为JSON
}
```

在游戏中，技能类型通常通过 JSON 配置定义，例如：

```json
{
  "id": "ability_strategic_bombing",
  "name": "战略轰炸",
  "description": "对所有敌方单位造成 {value} 点伤害，并对其总部造成额外 2 点伤害",
  "iconPath": "icons/abilities/strategic_bombing.png",
  "trigger": "Manual",
  "cooldownTurns": 2,
  "usesPerTurn": 1,
  "usesPerMatch": 3,
  "requiresFaceUp": true,
  "operationCost": 3,
  "targeting": "AllEnemies",
  "range": "Any",
  "canTargetFaceDown": false,
  "effect": "Damage",
  "effectValue": 1,
  "effectDuration": 0,
  "specialEffectId": "",
  "customParameters": {
    "additionalHQDamage": 2,
    "requiresAircraft": true
  }
}
```

### Ability（技能实例）

```csharp
public class Ability
{
    // 引用技能类型定义
    public AbilityType AbilityType { get; }

    // 运行时状态
    public Card OwnerCard { get; }
    public int UsesThisTurn { get; private set; }
    public int TotalUses { get; private set; }
    public int TurnsSinceLastUse { get; private set; }
    public bool IsActive { get; private set; }

    // 技能验证与执行方法
    public bool CanUse(Card target = null);
    public void Execute(List<Card> targets);
    public List<Card> GetValidTargets();
}
```

## 技能系统组件

### AbilitySystem（技能系统）

中枢管理系统，负责技能注册与查询、技能触发检测、技能执行与效果应用以及自定义效果处理器管理。

```csharp
public class AbilitySystem
{
    // 注册自定义效果处理器
    public void RegisterSpecialEffectHandler(string effectId, ISpecialEffectHandler handler);

    // 处理各类触发点
    public void ProcessTurnStart(Player player);
    public void ProcessTurnEnd(Player player);
    public void ProcessCardDeployed(Card card);
    public void ProcessCardDamaged(Card card, int damage, Card source);

    // 执行技能
    public bool ExecuteAbility(Ability ability, List<Card> targets = null);
}
```

### ISpecialEffectHandler（特殊效果处理器）

```csharp
public interface ISpecialEffectHandler
{
    // 执行自定义效果
    bool ExecuteEffect(Ability ability, Card source, List<Card> targets, Dictionary<string, object> parameters);
}
```

## 触发与效果类型

### 触发类型（TriggerType）

| 类型                | 描述           | Kards 对应 |
| ------------------- | -------------- | ---------- |
| `Manual`            | 由玩家手动激活 | 指令卡     |
| `OnDeploy`          | 卡牌部署时触发 | 入场效果   |
| `OnTurnStart`       | 回合开始时触发 | 回合开始   |
| `OnTurnEnd`         | 回合结束时触发 | 回合结束   |
| `OnDamaged`         | 受到伤害时触发 | 受伤触发   |
| `OnDestroyed`       | 被摧毁时触发   | 阵亡效果   |
| `OnAttack`          | 攻击时触发     | 攻击触发   |
| `OnDefend`          | 被攻击时触发   | 防御触发   |
| `OnDraw`            | 抽到时触发     | 抽牌触发   |
| `OnDiscard`         | 弃牌时触发     | 弃牌触发   |
| `OnCombatDamage`    | 造成战斗伤害时 | 伤害触发   |
| `OnOrderPlay`       | 使用指令牌时   | 命令触发   |
| `OnFrontlineChange` | 前线变化时     | 前线变更   |

### 目标类型（TargetingType）

| 类型            | 描述         | 使用场景   |
| --------------- | ------------ | ---------- |
| `None`          | 无需目标     | 全局性效果 |
| `SingleAlly`    | 单个友方单位 | 增益、治疗 |
| `SingleEnemy`   | 单个敌方单位 | 点杀、弱化 |
| `AllAllies`     | 所有友方单位 | 群体增益   |
| `AllEnemies`    | 所有敌方单位 | 群体伤害   |
| `Row`           | 整行单位     | 区域效果   |
| `Column`        | 整列单位     | 区域效果   |
| `Self`          | 自身         | 自我强化   |
| `RandomEnemy`   | 随机敌方目标 | 随机效果   |
| `FrontlineUnit` | 前线单位     | 前线战术   |
| `HQ`            | 总部目标     | 战略打击   |
| `SameNation`    | 同阵营单位   | 阵营协同   |

### 效果类型（EffectType）

| 类型            | 描述       | 参数示例                                           |
| --------------- | ---------- | -------------------------------------------------- |
| `Damage`        | 造成伤害   | `{"value": 2, "damageType": "physical"}`           |
| `Heal`          | 治疗单位   | `{"value": 2, "overHeal": false}`                  |
| `Buff`          | 增益效果   | `{"attack": 1, "defense": 2, "duration": 2}`       |
| `Debuff`        | 减益效果   | `{"attack": -1, "defense": -1, "duration": 2}`     |
| `Draw`          | 抽牌       | `{"count": 1, "specific": "order"}`                |
| `Discard`       | 弃牌       | `{"count": 1, "random": true}`                     |
| `Move`          | 移动卡牌   | `{"destination": "battlefield", "position": 2}`    |
| `Summon`        | 召唤单位   | `{"cardTypeId": "INF-001", "position": 0}`         |
| `Transform`     | 变形       | `{"cardTypeId": "INF-002", "keepModifiers": true}` |
| `Modifier`      | 属性修正   | `{"attack": 2, "defense": -1}`                     |
| `Counter`       | 添加计数器 | `{"counterType": "charge", "value": 1}`            |
| `Destroy`       | 直接摧毁   | `{"ignoreEffects": false}`                         |
| `ReturnToHand`  | 返回手牌   | `{"position": "top"}`                              |
| `CopyCard`      | 复制卡牌   | `{"destination": "hand"}`                          |
| `GainOperation` | 获得资源   | `{"amount": 2, "resourceType": "credits"}`         |
| `Special`       | 自定义效果 | _取决于特殊效果处理器_                             |

## 数据驱动示例

### 基础单位技能

```json
{
  "id": "bombardier_strike",
  "name": "轰炸机打击",
  "description": "对一个敌方单位造成 2 点伤害",
  "trigger": "Manual",
  "targeting": "SingleEnemy",
  "effect": "Damage",
  "effectValue": 2,
  "usesPerTurn": 1
}
```

### 高级战略技能

```json
{
  "id": "total_mobilization",
  "name": "全面动员",
  "description": "为你的总部提供 +2 防御，并使你每回合多获得 1 个信用点数，持续 3 回合",
  "trigger": "OnDeploy",
  "targeting": "None",
  "effect": "Special",
  "specialEffectId": "strategicDecision",
  "customParameters": {
    "hqDefenseBonus": 2,
    "creditIncrement": 1,
    "duration": 3,
    "victoryPointCost": 2
  }
}
```

## 技能执行流程

技能执行流程包括以下步骤：

1. **触发检测**：AbilitySystem 监听游戏事件并识别哪些技能应被触发
2. **目标验证**：检查目标是否合法并收集有效目标
3. **条件验证**：验证技能使用条件（冷却、可用次数等）
4. **成本支付**：扣除操作成本
5. **效果应用**：执行技能效果
6. **状态更新**：更新技能状态（使用次数、冷却等）
7. **事件通知**：触发技能执行相关事件

## 扩展：战略决策系统

战略决策系统是 Kards 风格的高级技能机制，允许玩家做出影响整体战局的重大决策。

```json
{
  "strategicDecision": {
    "name": "闪电战",
    "description": "部署后，你的单位攻击+1，但每回合末损失 1 点生命",
    "intelCost": 2,
    "victoryPoints": 1,
    "escalationEffects": {
      "blitzkrieg": {
        "unitAttackBonus": 1,
        "endOfTurnDamage": 1,
        "duration": "permanent"
      }
    }
  }
}
```

## 设计最佳实践

创建技能时应遵循以下设计最佳实践：

- **一致性优先**：保持技能触发和效果的一致语义
- **模块化设计**：复杂技能分解为多个简单效果
- **平衡考量**：限制强力技能的使用频率和条件
- **性能优化**：延迟评估目标和验证条件，避免不必要的计算
- **可扩展定义**：通过自定义参数扩展基础技能类型

---

_文档更新日期: 2025-02-26_
