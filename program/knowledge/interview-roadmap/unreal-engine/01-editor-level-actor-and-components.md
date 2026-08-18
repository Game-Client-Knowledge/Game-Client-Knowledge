# 编辑器、Level、Actor 与 Component

## 1. UE Project 里有什么

一个 C++ UE Project 常见结构：

```text
MyGame/
├── MyGame.uproject       项目描述与插件声明
├── Config/               DefaultEngine.ini 等配置
├── Content/              .uasset、.umap 等内容资源
├── Source/               C++ Module、Target 与 Build.cs
├── Plugins/              项目插件
├── Binaries/             编译产物
├── Intermediate/         中间生成文件
├── Saved/                日志、自动保存、Cook 临时数据
└── DerivedDataCache/     Shader、纹理等派生缓存
```

通常提交：

- `.uproject`。
- `Config/`。
- `Content/`。
- `Source/`。
- 自研 `Plugins/`。

通常不提交 `Binaries/`、`Intermediate/`、`Saved/` 和本地 DDC。它们像厨房里的
半成品和洗碗水：构建时需要，但不适合当成菜谱永久保存。

## 2. 编辑器常用区域

| 区域 | 作用 |
|---|---|
| Viewport | 编辑和预览 World |
| Outliner | 查看当前 World 中 Actor 层级 |
| Details | 编辑 Actor、Component 和 Asset 属性 |
| Content Browser/Drawer | 浏览内容资源 |
| World Settings | GameMode、World、物理等关卡级设置 |
| Output Log | 日志和命令 |
| Blueprint Debugger | 断点、Watch、执行流 |
| Message Log | Map Check、编译和资源问题 |

Content Browser 中的 Blueprint Class 是类资源，Outliner 中的是 Actor 实例。
一个是“演员模板”，一个是“今天已经到片场的演员”。

## 3. World、Level 与 Map

### 3.1 World

`UWorld` 表示一个运行或编辑中的世界上下文，包含：

- 已加载 Level。
- Actor。
- Tick、Timer、物理和网络状态。
- GameMode/GameState 等 Gameplay Framework 实例。

编辑器、PIE、预览和游戏可能同时存在多个 World。使用全局对象或静态缓存时，
不要默认宇宙只有一个。

### 3.2 Level

Level 是 World 中承载 Actor 的一部分。一个 World 可以包含 Persistent Level
和多个 Streaming Level。

### 3.3 Map Asset

`.umap` 是保存 World/Level 数据的内容包。日常交流中“Map”“Level”“关卡”经常
混用，但讨论运行时加载时应说明是整个 World、Streaming Level，还是 World
Partition Cell。

## 4. Actor 是什么

Actor 是可以进入 World 的 UObject，具有：

- World 归属。
- Transform，通常由 RootComponent 提供。
- Component 集合。
- Spawn/Destroy 生命周期。
- Tick、Replication 和网络相关能力。

```text
AActor
├── APawn
│   └── ACharacter
├── AController
│   ├── APlayerController
│   └── AAIController
├── AGameModeBase
├── AGameStateBase
└── 各种自定义 Actor
```

Actor 不等于所有 UObject。Texture、Material、DataAsset 等 UObject 资源不会因为
继承 UObject 就自动站进关卡里。

## 5. Component 三层理解

### 5.1 UActorComponent

不一定有 Transform，适合逻辑能力：

- HealthComponent。
- InventoryComponent。
- AbilitySystemComponent。
- 自定义交互或状态组件。

### 5.2 USceneComponent

带相对 Transform，可组成附着层级：

```text
Root SceneComponent
├── StaticMeshComponent
├── CameraComponent
└── SpringArmComponent
    └── CameraComponent
```

### 5.3 UPrimitiveComponent

可提供渲染、碰撞或场景代理能力的 SceneComponent 基类，StaticMeshComponent、
SkeletalMeshComponent、ShapeComponent 等位于这条体系中。

类比：

```text
Actor             = 设备机箱
ActorComponent    = 没有空间位置的功能板
SceneComponent    = 能安装到空间插槽上的部件
PrimitiveComponent= 可以参与渲染/碰撞的部件
```

## 6. RootComponent 与附着

Actor 的世界 Transform 通常来自 RootComponent。其他 SceneComponent 相对它附着：

```cpp
AMyPickup::AMyPickup()
{
    Root = CreateDefaultSubobject<USceneComponent>(TEXT("Root"));
    SetRootComponent(Root);

    Mesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Mesh"));
    Mesh->SetupAttachment(Root);
}
```

构造函数中的 `CreateDefaultSubobject` 用于创建类默认组件。不要在这里使用
`NewObject` 随意代替，也不要访问尚未准备好的 World 游戏状态。

## 7. C++ Actor 最小示例

```cpp
UCLASS()
class MYGAME_API ASpinningPickup : public AActor
{
    GENERATED_BODY()

public:
    ASpinningPickup();

protected:
    virtual void BeginPlay() override;
    virtual void Tick(float DeltaSeconds) override;

    UPROPERTY(VisibleAnywhere, BlueprintReadOnly)
    TObjectPtr<UStaticMeshComponent> Mesh;

    UPROPERTY(EditAnywhere, BlueprintReadWrite, Category="Pickup")
    float RotationSpeed = 90.0f;
};
```

```cpp
ASpinningPickup::ASpinningPickup()
{
    PrimaryActorTick.bCanEverTick = true;

    Mesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("Mesh"));
    SetRootComponent(Mesh);
}

void ASpinningPickup::BeginPlay()
{
    Super::BeginPlay();
}

void ASpinningPickup::Tick(float DeltaSeconds)
{
    Super::Tick(DeltaSeconds);
    AddActorLocalRotation(
        FRotator(0.0f, RotationSpeed * DeltaSeconds, 0.0f)
    );
}
```

`MYGAME_API` 负责模块导出，宏和属性细节在下一章展开。

## 8. 如何把 Actor 生成到 World

### 8.1 编辑器放置

把 Blueprint Class 或可放置 C++ Actor 拖进 Viewport，关卡保存实例属性。

### 8.2 C++ Spawn

```cpp
FActorSpawnParameters Params;
Params.SpawnCollisionHandlingOverride =
    ESpawnActorCollisionHandlingMethod::AdjustIfPossibleButAlwaysSpawn;

ASpinningPickup* Pickup = GetWorld()->SpawnActor<ASpinningPickup>(
    PickupClass,
    SpawnTransform,
    Params
);
```

需要保存引用时：

```cpp
UPROPERTY()
TObjectPtr<ASpinningPickup> SpawnedPickup;
```

### 8.3 Blueprint Spawn Actor from Class

蓝图常用 `Spawn Actor from Class`：

```text
Class + Transform + Collision Handling
-> Spawn
-> 返回 Actor Reference
```

如果 Class 使用 Soft Class Reference，必须先异步加载类，再 Spawn。

### 8.4 Deferred Spawn

需要在 Construction/初始化完成前写入参数时，可使用 Deferred Spawn：

```text
BeginDeferredActorSpawn
-> 设置 ExposeOnSpawn 参数或调用初始化
-> FinishSpawningActor
```

普通 Spawn 后才补关键参数，可能让 Construction Script 或 BeginPlay 先读取到
错误值。那就像演员开拍后才收到角色设定。

## 9. Blueprint Class 与实例

Blueprint Class 可以：

- 继承 C++ 类或 Blueprint 类。
- 添加 Component。
- 设置默认属性。
- 编写 Construction Script 和 Event Graph。
- 在 Level 中产生多个实例。

```text
ASpinningPickup C++
        |
        v
BP_Coin Blueprint Class
├── Mesh = CoinMesh
├── RotationSpeed = 120
└── OnPickedUp 表现逻辑
        |
        v
Level 中多个 BP_Coin 实例
```

类默认值保存在 Blueprint Generated Class/CDO 体系中，实例可以覆盖可编辑属性。

## 10. Construction Script 是什么

Blueprint Construction Script 或 C++ `OnConstruction` 用于根据属性构造实例：

- 调整组件。
- 生成预览结构。
- 根据长度参数摆放围栏段。
- 更新材质参数。

它可能在编辑器移动、改属性、编译蓝图或生成 Actor 时多次执行。不要在里面：

- 执行不可逆外部操作。
- 写存档或发网络请求。
- 生成无法清理的全局对象。
- 假设只运行一次。

Construction Script 更像“编辑器和生成阶段的自动装配台”，不是 BeginPlay 的
时髦别名。

## 11. Destroy 与 EndPlay

```cpp
Pickup->Destroy();
```

Destroy 会标记 Actor 离开 World，触发相应 EndPlay/销毁流程。不要在调用后继续
假设指针可安全使用：

```cpp
if (IsValid(Pickup))
{
    // UObject 仍有效。
}
```

Actor 被销毁、Level Transition、End PIE、World Cleanup 都可能进入 EndPlay，
清理代码应根据 `EEndPlayReason` 处理必要差异。

## 12. Content Browser 工程习惯

推荐按稳定职责组织：

```text
/Game
├── Characters
├── Gameplay
├── UI
├── Maps
├── Art
├── Audio
└── Developer
```

命名示例：

```text
BP_EnemyGoblin
WBP_Inventory
IA_Jump
IMC_Gameplay
M_Character
MI_Character_Red
SM_Crate
SK_Hero
ABP_Hero
```

移动资源后及时 Fix Up Redirectors，并使用 Reference Viewer 检查依赖。内容目录
如果只能靠“最终版2_真的最终版”导航，Asset Manager 也很难保持尊严。

## 13. 本章检查

1. World、Level 和 Map Asset 有何区别？
2. UObject 与 Actor 的边界是什么？
3. ActorComponent、SceneComponent、PrimitiveComponent 如何区分？
4. RootComponent 为什么决定 Actor Transform？
5. `CreateDefaultSubobject` 为什么常放在构造函数？
6. 编辑器放置和 `SpawnActor` 生成实例有什么不同？
7. Deferred Spawn 解决什么初始化顺序问题？
8. Construction Script 为什么不能假设只执行一次？

[返回 UE 引擎基础](./README.md) |
[下一章：UObject、反射、GC 与生命周期](./02-uobject-reflection-gc-and-lifecycle.md)
