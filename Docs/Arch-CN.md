# Kardx 架构：设计原则和系统结构

[View English Version](./Arch.md)

_本文档提供 Kardx 游戏的高级架构指导，概述核心设计原则、系统结构和组件交互。有关数据模型的详细信息，请参阅 [Data-CN.md](./Data-CN.md)。_

## 概述

Kardx 架构旨在创建一个可维护、可测试和可扩展的代码库，能够随着游戏需求的发展而演变。其核心是强调关注点分离、单向数据流和组件之间的事件驱动通信。

## 核心架构原则

### 1. 关注点分离

- **数据模型**：负责维护游戏状态（例如 `Card`、`Player`、`MatchManager`）
- **游戏逻辑**：编排游戏流程和规则（例如 `StrategyPlanner`、`Decision`）
- **UI 层**：负责可视化游戏状态（例如 `CardView`、`MatchView`）
- **明确的边界**：数据模型不应关心其如何显示，UI 不应直接修改数据模型

### 2. 单向数据流

- 数据沿一个方向流动：从数据模型到 UI
- UI 组件反映数据模型的状态，而不是相反
- 游戏状态的更改应仅通过数据模型中定义良好的方法进行
- UI 通过事件系统对数据模型的更改做出反应

### 3. 事件驱动架构

- 数据模型通过事件通知 UI 状态变化
- UI 组件订阅这些事件并相应更新
- 这解耦了数据模型和 UI，使两者更易于维护
- 事件有助于在没有紧密耦合的情况下编排复杂的游戏流程

### 4. 类型系统一致性

- 在整个代码库中一致使用类型（例如，卡片标识符使用字符串 ID 而不是 int）
- 公共 API 使用不可变集合接口（例如 `IReadOnlyList<T>`）以防止意外修改
- 类型安全枚举（例如 `DecisionType`、`CardType`）表示明确定义的状态
- 谨慎使用泛型集合，平衡性能和清晰度

## 系统架构

```
┌─────────────────────────────────────────┐
│              用户界面                    │
│  ┌─────────────┐       ┌─────────────┐  │
│  │  MatchView  │       │  CardView   │  │
│  └─────────────┘       └─────────────┘  │
└───────────────┬─────────────┬───────────┘
                │             │
                │ 事件        │ 反映
                │             │
┌───────────────▼─────────────▼───────────┐
│               游戏逻辑                   │
│  ┌─────────────┐       ┌─────────────┐  │
│  │MatchManager │       │StrategyPlanner│ │
│  └─────────────┘       └─────────────┘  │
└───────────────┬─────────────┬───────────┘
                │             │
                │ 管理        │ 操作
                │             │
┌───────────────▼─────────────▼───────────┐
│               数据模型                   │
│  ┌─────────────┐       ┌─────────────┐  │
│  │    Card     │       │   Player    │  │
│  └─────────────┘       └─────────────┘  │
└─────────────────────────────────────────┘
```

## 关键组件

### 数据模型层

1. **Card**：代表游戏中的可玩卡牌

   - 封装静态数据（CardType）和运行时状态
   - 使用 Guid 进行唯一实例标识
   - 维护自身状态（FaceDown、CurrentDefence 等）

2. **Player**：管理玩家状态，包括卡牌和资源

   - 提供只读集合视图（Hand、Battlefield）以防止外部修改
   - 在数据级别强制执行游戏规则（例如，最大手牌数量）
   - 包含所有有效玩家操作的方法（DrawCard、DeployCard 等）

3. **Board**：代表一场比赛的完整游戏状态
   - 跟踪回合信息和当前玩家
   - 作为两名玩家的容器
   - 提供对游戏状态元素的受控访问

### 游戏逻辑层

1. **MatchManager**：编排整体游戏流程

   - 管理回合转换和游戏状态进程
   - 暴露主要游戏状态变化的事件
   - 协调玩家操作和 UI 更新

2. **StrategyPlanner**：处理 AI 和规划系统

   - 通过协程执行策略，与 Unity 的帧系统集成
   - 规划和执行决策序列
   - 为不同的 AI 策略提供扩展点

3. **Decision**：代表游戏中的单个操作
   - 使用一致的基于字符串的 ID 引用卡牌
   - 遵循命令模式方法来封装操作
   - 形成玩家和 AI 决策的基础

### UI 层

1. **CardView**：卡牌的可视化表示

   - 根据底层 Card 模型更新其显示
   - 处理卡牌特定的交互（例如，选择、翻转）
   - 在视觉状态和数据模型之间保持明确的分离

2. **MatchView**：管理整体比赛 UI
   - 订阅 MatchManager 事件以更新显示
   - 编排卡牌 UI 的创建和管理
   - 避免直接数据模型操作

## 实现指南

### 数据模型类（例如 `Card`、`Player`、`MatchManager`）

1. **封装状态**：

   ```csharp
   // 好：状态通过私有 setter 封装
   public bool FaceDown { get; private set; }

   // 好：修改状态的公共方法
   public void SetFaceDown(bool isFaceDown)
   {
       FaceDown = isFaceDown;
   }
   ```

2. **单一事实来源**：

   - 每一块状态应该只由一个组件拥有
   - 示例：`Player.DeployCard()` 是部署时设置卡牌面朝下状态的唯一位置

3. **状态变化通知**：

   ```csharp
   // 好：状态变化时通知监听器
   public bool DeployCard(Card card, int position)
   {
       // 变更状态
       if (!currentPlayer.DeployCard(card, position))
           return false;

       // 通知监听器
       OnCardDeployed?.Invoke(card, position);
       return true;
   }
   ```

4. **使用适当的集合类型**：

   ```csharp
   // 好：私有集合，公共只读访问
   private readonly List<Card> hand = new();
   public IReadOnlyList<Card> Hand => hand;

   // 好：在 UI 中操作 IReadOnlyList 时使用 ToList()
   var position = hand.ToList().IndexOf(card);
   ```

### UI 类（例如 `CardView`、`MatchView`）

1. **反映，不修改**：

   ```csharp
   // 好：UI 反映模型状态
   cardView.SetFaceDown(card.FaceDown);

   // 差：UI 修改模型状态
   card.SetFaceDown(false);
   cardView.SetFaceDown(false);
   ```

2. **订阅事件**：

   ```csharp
   // 好：UI 订阅模型事件
   matchManager.OnCardDeployed += HandleCardDeployed;

   private void HandleCardDeployed(Card card, int position)
   {
       // 基于新状态更新 UI
       // ...
   }
   ```

3. **使用当前状态初始化**：

   ```csharp
   // 好：使用当前模型状态初始化 UI
   public void Initialize(Card card)
   {
       this.card = card;
       UpdateCardView(); // 反映当前状态
   }
   ```

4. **使用协程进行 Unity 集成**：
   ```csharp
   // 好：使用协程进行 Unity 中的异步操作
   public IEnumerator ExecuteNextStrategyCoroutine(Board board)
   {
       // 与 Unity 帧系统配合的实现
       yield return null;
   }
   ```

## 要避免的反模式

### 1. 从 UI 直接修改状态

```csharp
// 差：UI 直接修改数据模型
private void HandleCardDeployed(Card card, int position)
{
    card.SetFaceDown(false); // UI 不应修改数据模型
    cardView.SetFaceDown(false);
}
```

### 2. 冗余状态设置

```csharp
// 差：在多个地方设置状态
public bool DeployCard(Card card, int position)
{
    currentPlayer.DeployCard(card, position); // 已经设置了面朝下状态
    card.SetFaceDown(false); // 冗余，可能导致不一致
    // ...
}
```

### 3. 双向依赖

```csharp
// 差：CardView 修改 Card，创建循环依赖
public void SetFaceDown(bool faceDown)
{
    if (card != null)
    {
        card.SetFaceDown(faceDown); // UI 修改数据模型
    }
    cardBackOverlay.gameObject.SetActive(faceDown);
}
```

### 4. 类型不一致

```csharp
// 差：混合使用 int 和 string ID 造成混淆和错误
public class Decision
{
    public int SourceCardId { get; set; }  // 使用 int
    public string TargetCardId { get; set; }  // 使用字符串
}
```

## 实际案例

### 案例 1：卡牌部署

**正确流程**：

1. 用户将卡牌拖动到战场槽位
2. UI 调用 `matchManager.DeployCard(card, position)`
3. `MatchManager` 调用 `Player.DeployCard(card, position)`
4. `Player` 更新卡牌状态并设置 `card.SetFaceDown(false)`
5. `MatchManager` 触发 `OnCardDeployed` 事件
6. `MatchView` 处理事件并更新 UI 以反映新状态

**好处**：

- 明确的责任链
- 状态变化的单一事实来源
- UI 始终反映数据模型的当前状态

### 案例 2：抽卡

**正确流程**：

1. 游戏逻辑确定应该抽一张卡
2. `MatchManager` 调用 `Player.DrawCard(faceDown)`
3. `Player` 设置卡牌的面朝下状态并将其添加到手牌中
4. `MatchManager` 触发 `OnCardDrawn` 事件
5. `MatchView` 处理事件并为卡牌创建 UI，反映其当前状态

### 案例 3：策略执行

**正确流程**：

1. `MatchManager` 确定现在是 AI 玩家的回合
2. `MatchManager` 通过协程从 `StrategyPlanner` 请求策略
3. `StrategyPlanner` 确定并执行一系列 `Decision` 对象
4. 每个决策通过 `MatchManager` 触发适当的事件
5. `MatchView` 根据这些事件更新 UI

## 未来架构考虑

### 1. 网络和多人游戏

事件驱动架构为未来的网络功能奠定了基础：

- 事件可以序列化并通过网络发送
- 模型和视图之间明确的分离允许客户端-服务器架构
- 状态封装使得玩家操作的验证成为可能

### 2. 模块化卡牌效果

未来的卡牌效果系统可以利用：

- 已有的修饰符系统
- 基于决策的操作系统，用于复杂的卡牌能力
- 用于触发和响应游戏事件的事件架构

### 3. 存档/加载和持久化

该架构支持持久化，通过：

- 明确的状态封装使序列化变得简单直接
- 可以从保存的游戏中重放操作的事件系统
- 关注点分离，允许不同的持久化策略
