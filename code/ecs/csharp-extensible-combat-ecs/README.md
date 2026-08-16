# C# 高扩展战斗 ECS 示例

## 1. 示例目标

本工程通过一个小型战斗逻辑层演示：

- Entity 是 `index + generation`。
- Component 只保存 unmanaged 数据。
- Archetype、Chunk 和 SoA 组件列支持批处理。
- Player 输入和多类 AI 统一生成 Intent。
- 移动、Buff、攻击、抗性、伤害和死亡分阶段执行。
- 新角色类型通过组件组合扩展，而不是继承层级。

完整设计参见：

[C# 高扩展战斗 ECS 示例架构](../../../knowledge/ecs/10-csharp-extensible-demo-architecture.md)。

## 2. 运行环境

- .NET 8 SDK
- C# 12
- 无第三方 NuGet 依赖

本机使用 .NET SDK 8.0.424 验证。若 `dotnet` 不在 `PATH`，使用 `$HOME/.dotnet/dotnet`。

## 3. 运行命令

```bash
dotnet build
dotnet run --project src/ExtensibleCombatEcs
dotnet run --project src/ExtensibleCombatEcs -- --self-test
```

验证结果：

```text
dotnet format：通过
Release Build：0 warning，0 error
Self Tests：全部通过
```

## 4. 推荐阅读顺序

1. `Game/Components.cs`：理解角色只是组件组合。
2. `Game/DemoBootstrap.cs`：查看 Player、两类 Enemy、Teammate、Object 和 Buff。
3. `Systems/ControlSystems.cs`：查看输入和 AI 如何只生成 Intent。
4. `Systems/BuffAndMovementSystems.cs`：查看 Buff、世界上下文和移动。
5. `Systems/CombatSystems.cs`：查看攻击、抗性、事件和死亡。
6. `Ecs/World.cs`：理解迁移、Location Table 和 Query Cache。
7. `Ecs/Chunk.cs`：理解组件列和 swap-remove。

## 5. 项目结构

```text
src/ExtensibleCombatEcs/
├── Program.cs
├── Ecs/
│   ├── Entity.cs
│   ├── ComponentMask.cs
│   ├── ComponentRegistry.cs
│   ├── ComponentColumn.cs
│   ├── Chunk.cs
│   ├── Archetype.cs
│   ├── Query.cs
│   ├── World.cs
│   ├── CommandBuffer.cs
│   └── FrameEventBuffer.cs
├── Game/
│   ├── Components.cs
│   ├── Definitions.cs
│   ├── SimulationContext.cs
│   ├── DemoBootstrap.cs
│   └── DemoSelfTests.cs
└── Systems/
    ├── ControlSystems.cs
    ├── BuffAndMovementSystems.cs
    ├── CombatSystems.cs
    └── SimulationPipeline.cs
```

## 6. 核心数据流

```text
Player Input / AI
-> MoveIntent / AttackIntent
-> Buff Aggregation
-> Resolve Movement Stats
-> Ground Movement
-> Attack Validation
-> DamageEvent
-> Resistance + Health
-> EffectEvent
-> DeadTag Command
```

## 7. 实体组合

```text
Player
= PlayerControlled + 通用移动/战斗组件

Chaser Enemy
= ChaseBehavior + 通用移动/战斗组件

Guard Enemy
= GuardBehavior + 通用移动/战斗组件

Teammate
= FollowBehavior + AssistAttackBehavior + 通用移动/战斗组件

Object
= WorldObjectTag + Position + Obstacle
```

## 8. 扩展方式

### 新增飞行移动

新增 `FlyingMover` 和 `FlyingMovementSystem`，继续消费已有 `MoveIntent` 与 `ResolvedMoveSpeed`，无需修改地面移动。

### 新增远程风筝敌人

新增 `KiteBehavior` 和 `KiteAiSystem`，输出相同 Intent，复用移动与攻击执行层。

### 新增仇恨系统

将仇恨关系建模为 `ThreatEntry`，由 `ThreatAggregationSystem` 生成 `AggroTarget`，各类 AI 读取结果。

### 新增特殊 Buff

普通数值 Buff 进入聚合层；护盾、周期治疗、反伤等独特机制使用独立组件和 System。

### 新增投射物

```text
AttackSystem
-> SpawnProjectileEvent
-> ProjectileMovementSystem
-> CollisionEvent
-> DamageEvent
```

## 9. 性能边界

当前实现保留：

- unmanaged 热组件。
- Archetype 级 Query。
- 固定容量 Chunk。
- 组件列连续遍历。
- 帧事件值类型连续缓冲。
- 延迟结构变更。

教学性简化：

- 各组件列是独立 C# 数组，不是同一块 Native Memory。
- Command Buffer 会装箱命令。
- Buff 聚合按 Owner 随机访问。
- 组件 ID 限制为 64 个。
- Scheduler 是显式单线程顺序。
- Spawn 时逐个 Add 会产生多次迁移。

[在代码阅读器中打开工程](/code/workspace/?project=csharp-extensible-combat-ecs)
