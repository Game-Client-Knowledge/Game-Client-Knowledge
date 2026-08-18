# UObject、反射、GC 与生命周期速记

## 反射体系

UHT 解析 `UCLASS/USTRUCT/UENUM/UPROPERTY/UFUNCTION` 元数据并生成胶水，支持序列化、编辑器、Blueprint、GC 引用追踪和网络复制。它不是标准 C++ RTTI 的简单别名。

`UPROPERTY` 的 specifier 决定编辑、序列化、Blueprint 和复制行为；是否被 GC 追踪取决于可识别引用与容器。`UFUNCTION` 可暴露调用、RPC、事件等。

## 身份、CDO 与创建

UObject 身份由 Class、Name、Outer 等组成。Outer 表示命名/层级上下文，不天然等于所有权或 GC 强引用。

CDO 保存类默认值并参与实例初始化；构造函数会为 CDO 执行，不能假设存在 World/玩家。UObject 用 `NewObject`，Actor 用 `SpawnActor`，默认子对象用 `CreateDefaultSubobject`。

## GC 与指针

UE GC 从 Root Set 和被反射系统识别的强引用追踪可达 UObject。C++ 栈上的普通裸指针不会自动成为 GC 根。

| 类型 | 用途 |
|---|---|
| `TObjectPtr` / 反射强引用 | 拥有可追踪引用（具体版本/场景声明） |
| `TWeakObjectPtr` | 不延长生命周期，使用前验证 |
| `TSoftObjectPtr` | 资产路径/延迟加载，避免硬依赖 |
| `TSharedPtr` | 非 UObject C++ 对象；不要替代 UObject GC |

`IsValid` 处理 null 与 pending kill 等状态，但异步回调后仍需重新验证。

## 生命周期与调度

Actor/Component 生命周期包含构造、注册、初始化、BeginPlay、Tick、EndPlay/Unregister/销毁等阶段；编辑器、网络 Spawn 和关卡加载路径会影响细节。

Tick 有 TickGroup 和 prerequisite；顺序依赖应显式声明。低频工作用 Timer/事件/Subsystem，异步回调捕获弱对象并在 Game Thread 验证。

Delegate 选择单播/多播、动态/原生要考虑 Blueprint/序列化与开销；绑定必须在 EndPlay/销毁路径解除，避免回调已失效对象。

## 高频追问

1. UHT 生成代码解决什么？
2. Outer 是否拥有 UObject？
3. CDO 何时创建，构造函数为何不能访问 World？
4. UObject 为何不能普通 new/delete？
5. `TWeakObjectPtr` 与 `TSoftObjectPtr` 的差异？
6. TickGroup/prerequisite 如何解决阶段依赖？
7. GC 与 `TSharedPtr` 分别管理哪些对象？

[上一章：Actor](./01-editor-level-actor-and-components.md) | [下一章：Gameplay Framework](./03-gameplay-framework-and-game-loop.md)
