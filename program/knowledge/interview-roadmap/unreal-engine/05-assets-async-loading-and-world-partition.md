# 资源、异步加载与大世界

## 1. UE Asset 与 Package

Content Browser 中常见资源：

- Blueprint Class。
- Static/Skeletal Mesh。
- Texture、Material、Material Instance。
- Animation、Sound、Niagara System。
- DataAsset、DataTable。
- World/Map。

磁盘上的 `.uasset` 或 `.umap` 是 Package 文件，内部可包含一个或多个 UObject
导出。Content Browser 展示的是资产视图，不应把它简单当作普通文件浏览器。

```text
/Game/Characters/Hero/BP_Hero
        |
        v
Package path + Object name
```

资源移动或重命名会生成 Redirector 维持旧引用。完成批量迁移后应检查并 Fix Up
Redirectors，而不是让重定向链承担项目族谱工作。

## 2. 硬引用

```cpp
UPROPERTY(EditDefaultsOnly)
TObjectPtr<UStaticMesh> WeaponMesh;
```

拥有此属性的对象加载时，目标资源通常作为依赖一起加载。硬引用优点：

- 使用简单。
- 目标已在内存。
- Cook 依赖明确。

风险：

```text
Player Blueprint
-> Weapon Blueprint
-> Hero Mesh
-> 20 个 Material
-> 100 张 Texture
-> Niagara / Sound
```

引用图会级联。启动界面只想显示角色名字，却因为硬引用角色 Class 把整套战斗
资源请进内存，这种情况并不少见。

适合硬引用：

- 一定会使用的小资源。
- 组件默认资源。
- 生命周期完全一致的依赖。

## 3. 软引用

```cpp
UPROPERTY(EditDefaultsOnly)
TSoftObjectPtr<UStaticMesh> WeaponMesh;

UPROPERTY(EditDefaultsOnly)
TSoftClassPtr<AEnemy> EnemyClass;
```

软引用主要保存资产路径，目标可以尚未加载。

| 类型 | 指向 |
|---|---|
| `TSoftObjectPtr<T>` | Asset/Object |
| `TSoftClassPtr<T>` | Class，常用于 Blueprint Generated Class |
| `FSoftObjectPath` | 无强类型对象路径 |

```text
Soft Reference
-> 知道资源住址
-> 不保证资源已在家
-> 使用前显式加载
```

`SoftPtr.Get()` 只在资源已加载时返回对象，不会自动帮你同步加载。

## 4. 同步加载为何危险

```cpp
UStaticMesh* Mesh = WeaponMesh.LoadSynchronous();
```

同步加载可能让 Game Thread 等待 IO、解压、反序列化和依赖，造成卡顿。它适合：

- 工具。
- 明确的加载界面阶段。
- 小型必需资源且已确认成本。

不适合在战斗 Tick、碰撞回调或玩家点击后无预算地加载大型资源。

“只同步加载一个 Blueprint”可能顺着硬引用加载一整个家族，单数语法并不保证
单数成本。

## 5. C++ 异步加载

```cpp
UCLASS()
class MYGAME_API AAsyncMeshActor : public AActor
{
    GENERATED_BODY()

public:
    void BeginLoad();

protected:
    UPROPERTY(EditDefaultsOnly)
    TSoftObjectPtr<UStaticMesh> MeshAsset;

    UPROPERTY(VisibleAnywhere)
    TObjectPtr<UStaticMeshComponent> MeshComponent;

    TSharedPtr<FStreamableHandle> LoadHandle;

    void OnMeshLoaded();
};
```

```cpp
void AAsyncMeshActor::BeginLoad()
{
    if (MeshAsset.IsNull())
    {
        return;
    }

    LoadHandle =
        UAssetManager::GetStreamableManager().RequestAsyncLoad(
            MeshAsset.ToSoftObjectPath(),
            FStreamableDelegate::CreateWeakLambda(this, [this]()
            {
                OnMeshLoaded();
            })
        );
}

void AAsyncMeshActor::OnMeshLoaded()
{
    if (!IsValid(this))
    {
        return;
    }

    if (UStaticMesh* LoadedMesh = MeshAsset.Get())
    {
        MeshComponent->SetStaticMesh(LoadedMesh);
    }

    LoadHandle.Reset();
}
```

示例重点：

- 保存 Handle 以便观察、取消或管理请求。
- 回调使用弱生命周期语义。
- 完成后重新验证 Actor 和资源。
- Component 的硬引用可继续保持已使用 Mesh。

具体 Handle 管理策略应根据是否需要持续保活、取消和批量资源决定。

## 6. Blueprint 异步加载

常见节点：

```text
Soft Object Reference
-> Async Load Asset
-> Completed(Object)
-> Cast / Set Asset
```

Soft Class：

```text
Soft Class Reference
-> Async Load Class Asset
-> Loaded Class
-> Spawn Actor from Class
```

异步完成前：

- 显示 Placeholder。
- 防止重复请求。
- 允许页面关闭/关卡切换。
- 检查 Completed 时调用方是否仍有效。

## 7. Asset Manager

Asset Manager 在软引用和 StreamableManager 之上提供：

- Primary Asset Type/ID。
- 扫描目录和规则。
- Asset Bundle。
- Cook Rule。
- 批量加载和卸载。
- 资源审计。

### 7.1 Primary 与 Secondary Asset

Primary Asset 有稳定 `FPrimaryAssetId`，可被 Asset Manager 直接管理：

```text
Weapon:Sword_Fire
Monster:Goblin_Boss
Map:Forest_01
```

被 Primary Asset 引用的 Mesh、Material 等通常作为 Secondary Asset 依赖。

### 7.2 PrimaryDataAsset

```cpp
UCLASS(BlueprintType)
class MYGAME_API UWeaponData : public UPrimaryDataAsset
{
    GENERATED_BODY()

public:
    UPROPERTY(EditDefaultsOnly)
    float Damage = 10.0f;

    UPROPERTY(EditDefaultsOnly)
    TSoftObjectPtr<UStaticMesh> Mesh;

    UPROPERTY(EditDefaultsOnly)
    TSoftClassPtr<AActor> ProjectileClass;
};
```

用 DataAsset 保存定义，用 Actor/Component 保存运行状态：

```text
UWeaponData: Damage、Mesh、Projectile Class
Weapon Runtime: Ammo、Cooldown、Owner
```

## 8. Asset Bundle

同一 Primary Asset 可定义不同资源集合：

```text
Character Hero
├── UI Bundle: Portrait、Nameplate
├── Gameplay Bundle: Class、Abilities
└── Full Bundle: Mesh、Animation、VFX、Audio
```

角色选择界面只加载 UI Bundle，不必提前加载战斗全部资源。Bundle 是逻辑加载组，
不是直接等同于最终容器文件。

## 9. Asset Registry

Asset Registry 保存 Asset 元数据，可在不完整加载对象的情况下查询：

- Class。
- Package Path。
- Tags。
- Primary Asset 信息。

适合编辑器浏览、运行时资源发现和 Asset Manager 扫描。查询到 `FAssetData`
不代表资源 UObject 已经加载。

```text
Asset Registry = 图书馆目录卡
Loaded UObject = 已经从书库取到手里的书
```

## 10. Reference Viewer 与 Size Map

### Reference Viewer

查看：

- 谁引用我。
- 我引用谁。
- 硬/软依赖关系。

### Size Map

估计资源及依赖占用。适合发现：

- 一个 UI Widget 硬引用整套角色。
- 一个公共 DataTable 拖入全部怪物 Class。
- Blueprint Cast 导致不期望的类依赖。

工具给出证据，不应只凭 Content 文件夹大小判断运行时内存。

## 11. Level Streaming

传统结构：

```text
Persistent Level
├── Streaming Level: Town
├── Streaming Level: Dungeon
└── Streaming Level: Lighting
```

可通过 Volume、Blueprint 或 C++ 控制加载和可见：

```text
Load Stream Level
-> Loaded
-> Make Visible
-> Gameplay Activate
```

卸载时要处理：

- Actor EndPlay。
- 引用失效。
- 异步任务取消。
- 网络 Actor 和 Authority。
- 共享资源是否仍被其他 Level 使用。

## 12. World Partition

World Partition 将大 World 自动划分为可流送 Cell：

```text
World
├── Cell A: near player, loaded
├── Cell B: near player, loaded
├── Cell C: far, unloaded
└── HLOD proxy for distant region
```

关键概念：

- Streaming Source：玩家、摄像机或自定义来源。
- Cell：运行时加载单位。
- Data Layer：按玩法/主题组织可激活内容。
- HLOD：远处用合并代理表现。
- One File Per Actor：降低多人编辑同一 Map 文件冲突。

World Partition 不等于“勾选后大世界自然优化”。Cell 尺寸、Actor 引用、HLOD、
导航、物理、内存预算和网络相关性仍需设计。

## 13. Data Layer

Data Layer 可组织：

- 白天/夜晚内容。
- 任务前后状态。
- 不同玩法模式。
- 编辑器工作集。

运行时 Data Layer 激活与资源流送要考虑网络权威。客户端自行切换任务层而服务器
不知情，会得到一场只有自己相信存在的世界变化。

## 14. HLOD

Hierarchical LOD 将远处多个 Actor/组件简化成代理：

```text
近处：独立高精度 Mesh
中远：普通 LOD / Nanite 策略
远处：HLOD Proxy
更远：卸载
```

目标：

- 减少 Actor/Component 数量。
- 减少 Draw Call。
- 保持远景轮廓。

构建 HLOD 会增加 Cook 数据和生成流程，需要验证材质、光照、阴影和破坏状态。

## 15. 跨 Level 引用

硬引用一个可能未加载 Cell/Level 的 Actor 会制造生命周期耦合。常见策略：

- Soft Object Path。
- Gameplay Tag/稳定 ID 后运行时解析。
- Subsystem 注册。
- Interface/消息。
- World Partition 支持的引用规则。

Actor 卸载后，不要让 Timer、Delegate 或异步回调继续拿旧指针敲门。

## 16. Cook 与 Package

概念流水线：

```text
Source Assets
-> Cook 为目标平台生成数据
-> Stage 组织运行文件
-> Package 生成发布包
-> Pak/IoStore 等容器
```

资源是否进入 Cook 通常取决于：

- Map/Asset 硬引用。
- Asset Manager 扫描和 Cook Rule。
- Additional Asset Directories。
- 插件和平台配置。

软引用资源若没有正确 Cook 规则，编辑器里能加载，Shipping 包可能找不到。

## 17. 加载性能标准流程

```text
确定加载场景和目标设备
-> Unreal Insights 查看 IO/Async Loading/Game Thread
-> Reference Viewer 找依赖链
-> Size Map 看资源规模
-> 改硬引用为受管理软引用
-> 分 Bundle/Cell/阶段加载
-> 记录前后加载时间和峰值内存
```

不要只把 `LoadSynchronous` 换成 `RequestAsyncLoad` 就宣布完成。异步回调后的 Actor
Spawn、组件注册、Shader/PSO 和资源上传仍可能产生尖峰。

## 18. 本章检查

1. Asset、Package 和 UObject 有何关系？
2. 硬引用为什么会形成级联加载？
3. `TSoftObjectPtr.Get` 与 `LoadSynchronous` 有何区别？
4. 异步加载回调为什么应使用弱生命周期？
5. Asset Manager 的 Primary Asset 解决什么问题？
6. Asset Bundle 与最终打包容器为何不是一回事？
7. Asset Registry 查询为何不等于加载资源？
8. Level Streaming 与 World Partition 如何选择？
9. Data Layer 和 HLOD 分别解决什么问题？
10. 软引用资源为何仍可能在 Shipping 包中缺失？

参考：
[UE 5.6 Asynchronous Asset Loading](https://dev.epicgames.com/documentation/en-us/unreal-engine/asynchronous-asset-loading-in-unreal-engine?application_version=5.6)

[上一章：蓝图系统与 C++ 协作](./04-blueprints-and-cpp-collaboration.md) |
[返回 UE 引擎基础](./README.md) |
[下一章：输入、角色移动、动画与 AI](./06-input-character-animation-and-ai.md)
