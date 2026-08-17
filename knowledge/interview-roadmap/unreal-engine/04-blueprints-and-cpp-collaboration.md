# 蓝图系统与 C++ 协作

## 1. 蓝图是什么

Blueprint Visual Scripting 是 UE 的节点式编程体系。它可以定义：

- 类和对象。
- 变量、结构、数组、Set、Map。
- 函数和事件。
- 控制流。
- Component 组合。
- 编辑器构造逻辑。
- 网络 RPC 和 Replication 配置。

蓝图不是“不会写代码的人专用的动画连线”。它是运行在引擎对象和反射体系上的
正式脚本层，只是语法长成节点和引脚。

## 2. 常见 Blueprint 类型

| 类型 | 用途 |
|---|---|
| Blueprint Class | 定义 Actor、Object、Component 等类 |
| Level Blueprint | 当前 Level 专属事件和编排 |
| Blueprint Interface | 声明跨类型契约 |
| Blueprint Function Library | 提供静态工具函数 |
| Blueprint Macro Library | 复用节点片段 |
| Animation Blueprint | 计算 Skeletal Mesh 动画 Pose |
| Widget Blueprint | UMG UI |
| Blueprint Async Action | 封装异步节点 |

Material、Niagara、Control Rig 也使用节点图，但它们不是普通 Gameplay Blueprint
Event Graph，执行模型和目标系统不同。

## 3. Blueprint Class 的组成

```text
BP_Door
├── Components
│   ├── SceneRoot
│   ├── DoorMesh
│   └── BoxCollision
├── Variables
│   ├── OpenAngle
│   └── OpenDuration
├── Construction Script
├── Event Graph
└── Functions / Macros
```

Class Defaults 影响新实例默认值，Level 中实例可覆盖标记为 Instance Editable 的
属性。

## 4. 节点和引脚

### 4.1 Execution Pin

白色执行线定义有副作用节点的顺序：

```text
Event BeginPlay
-> Branch
-> Spawn Actor
-> Play Sound
```

### 4.2 Data Pin

彩色数据线传递值或引用：

```text
Get Health
-> Float / MaxHealth
-> Percent
-> Set Progress Bar
```

### 4.3 Pure Node

Pure 节点没有执行引脚，系统会在消费者需要值时求值。它应该：

- 无副作用。
- 成本可控。
- 同输入得到可预期结果。

一个 Pure Getter 被图中三个节点使用，可能执行多次。若 Getter 内部遍历所有
Actor，它虽然是绿色节点，CPU 看到的仍是一片红色。

## 5. 常见控制流

- Branch：if。
- Sequence：按顺序触发多个执行出口。
- For Loop / For Each。
- Switch。
- Gate。
- Do Once。
- FlipFlop。
- Timeline。
- Delay 和其他 Latent Node。

`Sequence` 只保证当前执行流出口顺序，不会让后面的耗时任务并行。

## 6. Event、Function 与 Macro

### 6.1 Event

- Event Graph 入口。
- 可接 Latent Node。
- Custom Event 可配置 RPC。
- 适合响应生命周期、输入、碰撞和消息。

### 6.2 Function

- 有明确输入输出。
- 可覆盖和复用。
- 通常不能包含 Delay 等 Latent 行为。
- 适合可封装、可测试的同步逻辑。

### 6.3 Macro

- 编译时展开节点片段。
- 可拥有多个执行输入/输出。
- 适合小型控制流复用。
- 过度使用会让调试调用边界不清楚。

判断：

```text
要返回值、表达稳定 API -> Function
要网络/事件入口或 Latent 流程 -> Event
要复用一段节点结构 -> Macro
```

## 7. Construction Script 与 Event BeginPlay

```text
Construction Script
-> 编辑器放置、改属性、编译或 Spawn 时构造
-> 适合预览和组件装配

BeginPlay
-> 实例进入游戏时
-> 适合运行时玩法初始化
```

例如程序化围栏：

```text
Length 改变
-> Construction Script 清理旧段
-> 根据长度添加 Instanced Mesh
-> 编辑器立即预览
```

不要在 Construction Script 中：

- 修改其他关卡对象的永久状态。
- 启动 Timer。
- 保存玩家数据。
- 发送在线请求。

## 8. 蓝图通信五种方式

### 8.1 直接引用

持有明确对象引用并调用函数：

```text
DoorButton
-> Door Reference
-> Call Open
```

适合稳定的一对一关系。引用来源可以是 Expose on Spawn、Instance Editable、
Overlap 或系统注册。

### 8.2 Cast

```text
Other Actor
-> Cast To BP_Player
-> 调用 Player 专属能力
```

Cast 是运行时类型检查和引用转换，不是“寻找对象”。输入引用必须先存在。

对具体 Blueprint Class 的 Cast 还可能建立资源依赖。大量 Cast 的主要架构问题
通常是上层依赖具体类，而不只是节点本身的执行成本。

### 8.3 Blueprint Interface

定义契约：

```text
BPI_Interactable
└── Interact(Interactor)
```

Door、Chest、NPC 都可实现。调用者不需要知道具体类型。

适合：

- 多种对象共享同一能力。
- 降低具体类依赖。

接口不保存状态，也不保证目标一定以你期望的方式实现。

### 8.4 Event Dispatcher

发布者广播，多个监听者绑定：

```text
HealthComponent
-> OnHealthChanged Dispatcher
   -> HUD
   -> Hit Effect
   -> Audio
```

适合一对多通知。必须管理 Bind/Unbind 生命周期，避免重复绑定和失效监听。

### 8.5 Component / Subsystem / Gameplay Message

稳定能力可抽成 Component；全局服务可放合适 Subsystem；跨系统事实可用消息或
Gameplay Tag 通道。不要只在“Cast”和“Get All Actors”之间二选一。

## 9. 通信方式选择

| 关系 | 推荐起点 |
|---|---|
| 已知稳定对象 | 直接引用 |
| 确实需要具体子类能力 | Cast |
| 多类型共享能力 | Interface |
| 一对多状态通知 | Event Dispatcher |
| Actor 可组合能力 | ActorComponent |
| 生命周期明确的全局服务 | Subsystem |
| 松耦合跨系统消息 | Message/Gameplay Tag |

## 10. 一个蓝图门示例

### Components

```text
BP_Door
├── Root
├── DoorFrame
├── DoorLeaf
└── Trigger
```

### Variables

```text
OpenAngle = 90
OpenTime = 0.5
IsLocked = false
```

### Event Graph

```text
OnComponentBeginOverlap
-> Does Implement Interface BPI_Interactor?
-> Branch IsLocked
   -> true: Play Locked Sound
   -> false: Play Open Timeline
-> Timeline Alpha
-> Lerp Rotator(Closed, Open)
-> SetRelativeRotation(DoorLeaf)
```

扩展：

- `BPI_Interactable` 支持主动按键交互。
- Dispatcher 通知任务系统“门已打开”。
- C++ DoorBase 实现状态和网络，蓝图配置 Mesh、Sound 和 Timeline。

## 11. C++ 向蓝图暴露类和属性

```cpp
UCLASS(Blueprintable)
class MYGAME_API AWeaponBase : public AActor
{
    GENERATED_BODY()

public:
    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Weapon")
    float Damage = 20.0f;

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category="Weapon")
    TSubclassOf<AActor> ProjectileClass;

    UFUNCTION(BlueprintCallable, Category="Weapon")
    void Fire();
};
```

设计建议：

- 配置用 `EditDefaultsOnly`，避免每个实例随意漂移。
- 只读状态用 `BlueprintReadOnly`。
- 需要 Class 用 `TSubclassOf`，不要用无类型 UObject。
- Category 和 Tooltip 让蓝图 API 可理解。

## 12. C++ 调用蓝图事件

### BlueprintImplementableEvent

```cpp
UFUNCTION(BlueprintImplementableEvent, Category="Weapon")
void PlayFireEffects();
```

C++：

```cpp
void AWeaponBase::Fire()
{
    // 权威玩法逻辑。
    SpawnProjectile();

    // 可由 Blueprint 实现的表现。
    PlayFireEffects();
}
```

### BlueprintNativeEvent

```cpp
UFUNCTION(BlueprintNativeEvent)
bool CanFire() const;

bool AWeaponBase::CanFire_Implementation() const
{
    return Ammo > 0;
}
```

蓝图可以覆盖，C++ 有默认实现。

## 13. 蓝图调用 C++ 与继承策略

推荐分层：

```text
C++ Base
├── 生命周期和所有权
├── 性能敏感逻辑
├── 网络权威与验证
├── 稳定 API
└── 自动化测试

Blueprint Child
├── Mesh / Material / Sound
├── Class Defaults
├── 小型玩法编排
├── Timeline / UI / VFX
└── 设计师快速迭代
```

不要让 C++ 只剩一个空壳，也不要让一张 Blueprint Event Graph 承担所有服务器
权威、存档、资源和 UI 逻辑。

## 14. Blueprint Function Library

```cpp
UCLASS()
class MYGAME_API UCombatBlueprintLibrary
    : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintPure, Category="Combat")
    static float CalculateMitigatedDamage(
        float RawDamage,
        float Armor
    );
};
```

适合无状态工具。若函数依赖 World、网络会话或可变服务，优先使用明确
WorldContext、Subsystem 或对象方法，而不是用静态函数偷偷访问全局。

## 15. Latent 与异步节点

Delay、Move Component To、Async Load Asset 等节点会暂停当前流程，稍后恢复：

```text
执行节点
-> 注册 Latent/Async 操作
-> 当前调用返回
-> 后续帧完成
-> 触发 Completed 输出
```

异步完成时原对象可能已销毁或 World 已切换。自定义 Async Action 应：

- 保存必要弱引用。
- 提供完成/失败/取消。
- 明确对象生命周期。
- 防止多次完成。

## 16. 蓝图性能

Blueprint VM 按节点执行有额外开销，但“蓝图都很慢”过于粗糙。

适合蓝图：

- 低频玩法编排。
- UI、音频和 VFX。
- 设计师配置和快速迭代。
- 调用粒度较大的 C++ 函数。

适合下沉 C++：

- 每帧大循环。
- 大量实体数值计算。
- 复杂网络权威逻辑。
- 重复调用的细碎节点链。
- 需要严格内存/线程控制的系统。

优化先看 Blueprint Profiler/Unreal Insights。把十个便宜节点改成 C++，却保留
每帧加载资源的同步节点，并不会获得有意义的胜利。

## 17. 蓝图可维护性

建议：

- Event Graph 只做入口和高层编排。
- 把重复逻辑提取为 Function。
- 控制 Macro 大小。
- 使用 Comment、Category 和统一命名。
- 减少交叉长线，必要时使用 Reroute Node。
- 避免 Tick 中 GetAllActorsOfClass。
- 通过 Interface/Component 表达能力。
- 拆分责任，不创建“BP_GameManager_All_Final”。

## 18. 蓝图调试

- 选择 Debug Object。
- 节点断点。
- Watch Pin/Variable。
- Execution Trace。
- Print String，仅用于临时诊断。
- Blueprint Profiler。
- Output Log 和 Visual Logger。
- 网络 PIE 中确认当前窗口是 Server 还是 Client。

蓝图断点命中了“正确节点”仍不等于命中了“正确机器”。多人模式先确认
Authority、Owning Client 和实例。

## 19. 高频误区

| 误区 | 更准确的理解 |
|---|---|
| Cast 会自动找到对象 | Cast 只转换已有引用 |
| Blueprint Interface 自动广播 | 它是契约调用，不是一对多事件 |
| Event Dispatcher 不用解绑 | 监听生命周期仍需管理 |
| Pure Node 只算一次 | 可能被多个消费者重复求值 |
| Construction Script 只运行一次 | 编辑器和生成期间可能多次运行 |
| Blueprint 一定不能做正式项目 | 应按迭代、性能和维护边界选择 |
| C++ 比蓝图天然更架构化 | 糟糕边界可以用任何语言写出来 |

## 20. 本章检查

1. Blueprint Class 与 Level Blueprint 有何区别？
2. Execution Pin 和 Data Pin 分别表达什么？
3. Pure Node 为什么可能重复执行？
4. Event、Function、Macro 如何选择？
5. Cast 为什么不是对象查找？
6. Interface 与 Event Dispatcher 分别适合什么关系？
7. `BlueprintImplementableEvent` 与 `BlueprintNativeEvent` 有何区别？
8. C++ Base + Blueprint Child 应如何分工？
9. Latent Node 完成时为什么要重新检查对象？
10. 哪些 Blueprint 热点适合迁移到 C++？

参考：
[UE 5.6 Introduction to Blueprints](https://dev.epicgames.com/documentation/en-us/unreal-engine/introduction-to-blueprints-visual-scripting-in-unreal-engine?application_version=5.6)

[上一章：Gameplay Framework 与主循环](./03-gameplay-framework-and-game-loop.md) |
[返回 UE 引擎基础](./README.md) |
[下一章：资源、异步加载与大世界](./05-assets-async-loading-and-world-partition.md)
