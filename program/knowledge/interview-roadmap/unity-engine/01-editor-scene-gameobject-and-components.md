# 编辑器、Scene 与对象模型

## 1. Unity Project 里有什么

一个 Unity Project 不只是 `Assets/` 目录：

```text
MyGame/
├── Assets/          自己创建或导入的资源与代码
├── Packages/        包清单和包依赖
├── ProjectSettings/ 项目级配置
├── Library/         导入缓存和派生数据，可重新生成
├── Temp/            临时文件
├── Logs/            编辑器日志
└── UserSettings/    本机用户设置
```

通常应提交：

- `Assets/` 及其 `.meta` 文件。
- `Packages/manifest.json` 和锁定文件。
- `ProjectSettings/`。

通常不提交 `Library/`、`Temp/` 和构建输出。`Library/` 很大，但它更像加工仓库，
不是原材料；删掉后 Unity 会重新导入，代价是你可以去泡一杯比较完整的茶。

### 1.1 `.meta` 和 GUID

Unity 为资源配套生成 `.meta` 文件，其中保存稳定 GUID 和导入设置。Scene、
Prefab、Material 等引用资源时，主要依赖 GUID，而不是只记文件名。

```text
Assets/Characters/Hero.prefab
Assets/Characters/Hero.prefab.meta
                         |
                         v
                    stable GUID
```

如果只移动资源却丢掉 `.meta`，Unity 可能把它当成一个新资源，原引用就会失效。
因此移动和重命名最好在 Unity Editor 内完成，并让版本控制同时记录资源与
`.meta`。

## 2. 编辑器常用窗口

| 窗口 | 作用 |
|---|---|
| Project | 浏览项目资源，不等于运行中的场景对象 |
| Hierarchy | 查看当前已加载 Scene 中的 GameObject 层级 |
| Scene | 编辑世界，选择、移动和摆放对象 |
| Game | 查看 Camera 最终输出 |
| Inspector | 查看并修改对象、组件或资源的序列化字段 |
| Console | 查看日志、警告和异常 |
| Profiler | 分析 CPU、GPU、内存、渲染、物理等性能 |

Project 中的 Prefab 是资源模板，Hierarchy 中的是场景实例。两者图标可能相似，
身份却不同：一个是设计稿，一个是已经站在舞台上的演员。

## 3. Scene 是什么

Scene 是一种可序列化资源，保存一组场景对象及其关系，例如：

- 根 GameObject 和 Transform 层级。
- 组件的序列化字段。
- 对其他 Prefab、Material、Texture 等资源的引用。
- 光照、导航、烘焙数据和场景设置。

Scene 不等于一个 C# 类，也不等于整个应用。运行时可以同时加载多个 Scene：

```text
Bootstrap Scene
├── GameManager
├── AudioManager
└── NetworkManager

Level_01 Scene
├── Terrain
├── Enemies
└── Props

UI Scene
└── Canvas
```

其中一个 Scene 被标记为 Active Scene。新建且没有指定父节点的 GameObject，
通常会进入 Active Scene。多场景加载将在
[场景、资源与异步加载](./04-scenes-assets-and-async-loading.md)详细说明。

## 4. GameObject 是什么

GameObject 是 Unity 场景对象的容器和身份节点，主要提供：

- 名称。
- Tag。
- Layer。
- 激活状态。
- Scene 归属。
- Component 列表。
- Transform 层级。

它本身不负责渲染、碰撞、播放声音或执行玩法。能力来自 Component：

```text
Player (GameObject)
├── Transform
├── CharacterController
├── Animator
├── AudioSource
└── PlayerController (MonoBehaviour)
```

这种模式叫“组合优于继承”。与其设计：

```text
GameObject
└── MovingGameObject
    └── AnimatedMovingGameObject
        └── NetworkedAnimatedMovingGameObject
```

不如给对象组合 Movement、Animator、Network 等组件。继承树像一棵一旦长歪就
很难扶正的树，组件更像工具腰带，需要什么就挂什么。

## 5. Transform 为什么特殊

每个 GameObject 都有且只有一个 Transform，无法正常移除。它保存：

- 局部位置 `localPosition`。
- 局部旋转 `localRotation`。
- 局部缩放 `localScale`。
- 父子层级。
- 推导出的世界位置和旋转。

```text
Player
└── WeaponSocket
    └── Sword
```

Sword 的世界变换由自己的局部变换和父级链共同决定。修改父级会影响所有后代，
层级太深也会增加变换传播和理解成本。

常见区别：

```csharp
transform.position       // 世界空间位置
transform.localPosition  // 相对父节点的位置
```

`SetParent(parent, worldPositionStays)` 决定换父节点时是否尽量保持世界变换。

## 6. Component 是什么

Component 是挂在 GameObject 上的功能单元。常见组件包括：

| 组件 | 职责 |
|---|---|
| Transform | 空间位置与层级 |
| MeshFilter | 提供 Mesh 数据 |
| MeshRenderer | 使用 Material 渲染 Mesh |
| Collider | 提供碰撞形状 |
| Rigidbody | 让对象参与刚体仿真 |
| Camera | 定义观察和渲染输出 |
| AudioSource | 播放音频 |
| Animator | 驱动动画状态机 |
| MonoBehaviour 脚本 | 自定义玩法与系统逻辑 |

自定义脚本通常继承 `MonoBehaviour`，而不是直接继承 `Component`：

```csharp
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    public int Current { get; private set; }

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        Current = Mathf.Max(0, Current - amount);
    }
}
```

挂载后：

```text
Player GameObject
└── Health component instance
```

同一个脚本资源可以挂到多个 GameObject，每次挂载都是独立组件实例，各自保存
序列化字段和运行状态。

## 7. 组件如何查找和依赖

```csharp
private Rigidbody body;

private void Awake()
{
    body = GetComponent<Rigidbody>();
}
```

常见 API：

- `GetComponent<T>()`：当前 GameObject。
- `GetComponentInChildren<T>()`：当前对象及后代。
- `GetComponentInParent<T>()`：当前对象及祖先。
- `TryGetComponent<T>(out value)`：查询并返回是否存在。
- `AddComponent<T>()`：运行时添加组件。

高频路径中应缓存稳定引用，不要每帧无意义地在层级中查找。更重要的是，依赖应
清楚表达：

```csharp
[RequireComponent(typeof(Rigidbody))]
public sealed class PhysicsMover : MonoBehaviour
{
    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
}
```

也可以用 `[SerializeField]` 让依赖在 Inspector 中显式配置：

```csharp
[SerializeField] private Animator animator;
```

公开字段不是唯一序列化方式。通常使用私有字段加 `[SerializeField]`，避免为了
Inspector 方便而把所有状态都开放给任意代码修改。

## 8. 托管对象与原生对象

许多 `UnityEngine.Object` 类型同时涉及：

```text
C# 托管包装对象
        |
        v
Unity 原生引擎对象
```

调用 `Destroy(component)` 或 `Destroy(gameObject)` 后，原生对象会按 Unity
规则销毁，但 C# 引用可能暂时仍在，直到 GC 回收托管包装。Unity 为
`UnityEngine.Object` 重载了相等比较，因此对象可能表现为“像 null”：

```csharp
Destroy(target);

// 稍后检查时，Unity 的 == 可能认为它等于 null。
if (target == null)
{
    Debug.Log("Native object is gone.");
}
```

这就是常说的 fake null。不要把 Unity 对象销毁和普通 C# 引用设为 `null` 当成
同一件事。

## 9. 组件在引擎里如何组织

下面是帮助理解的概念模型，不承诺等同于某个 Unity 版本的私有内存布局：

```text
GameObject 原生对象
├── Instance ID
├── Active / Tag / Layer / Scene
└── Component handles
    ├── Transform 原生组件
    ├── Renderer 原生组件
    ├── Collider 原生组件
    └── MonoBehaviour 原生桥接
              |
              v
        C# script instance
```

### 9.1 内置组件

Transform、Renderer、Collider、Rigidbody 等核心能力主要由引擎原生层实现，
C# API 提供托管访问入口：

```csharp
Renderer renderer = GetComponent<Renderer>();
renderer.enabled = false;
```

这次属性设置最终会进入引擎对象。频繁跨越托管/原生边界是否昂贵取决于 API 和
版本，不能把所有 Unity API 调用一概判成慢，但高频热路径仍应使用 Profiler
验证。

### 9.2 MonoBehaviour 脚本组件

把脚本挂到 GameObject 时，Scene/Prefab 会保存对 MonoScript 资源的引用和该
组件的序列化字段。加载时，Unity 根据脚本类型创建对应托管实例，并把它与组件
身份关联：

```text
Prefab 中的 script GUID
-> 定位 MonoScript / C# 类型
-> 创建组件实例
-> 反序列化字段
-> 接入生命周期消息
```

因此重命名类、移动程序集、删除脚本或改变序列化字段时，可能出现 Missing Script
或数据迁移问题。文件名、类名和程序集边界应遵循稳定约定。

### 9.3 `AddComponent` 概念过程

```csharp
Health health = gameObject.AddComponent<Health>();
```

概念上会经历：

```text
检查组件类型和约束
-> 在 GameObject 上创建组件
-> 建立原生/托管关联
-> 初始化序列化默认值
-> 根据对象激活状态进入 Awake / OnEnable 等生命周期
```

`[RequireComponent]` 可在编辑器添加或 `AddComponent` 时补齐依赖，
`[DisallowMultipleComponent]` 可限制同类脚本重复挂载。但特性只能保证结构
底线，不能替代运行时依赖验证和清楚的所有权设计。

### 9.4 生命周期函数为何不用 `override`

常见写法：

```csharp
private void Update()
{
}
```

它不是重写 `MonoBehaviour.Update` 虚函数。Unity 识别特定名称和签名，并在
PlayerLoop 对应阶段调用存在这些消息的脚本。拼成 `Updata` 不会编译报错，因为
那只是一个普通私有方法，但 Unity 也不会调用它。

可以用 IDE 模板、代码分析器和最小运行日志减少此类错误。

## 10. Prefab 是什么

Prefab 是可复用的 GameObject 层级模板：

```text
Enemy.prefab
├── Transform
├── MeshRenderer
├── Collider
├── Animator
└── EnemyController
```

把 Prefab 放入 Scene 会产生 Prefab Instance。实例可以：

- 保持与 Prefab 资源的连接。
- 对字段做 Override。
- Apply 修改回资源。
- Revert 回模板值。
- Unpack 断开 Prefab 关系。

Prefab Variant 可在基础 Prefab 上保存差异，例如：

```text
EnemyBase
├── EnemyMelee Variant
└── EnemyRanged Variant
```

不要把 Variant 当成没有代价的继承系统。层级太深时，Override 来源会变得像
多人同时批注过的合同，需要点开几层才知道最终值来自哪里。

## 11. 如何实例化 GameObject 到 Scene

### 11.1 从 Prefab 实例化

```csharp
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform spawnRoot;

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject enemy = Instantiate(
            enemyPrefab,
            position,
            rotation,
            spawnRoot
        );

        enemy.name = "Enemy_Runtime";
        return enemy;
    }
}
```

过程可以理解为：

```text
Prefab 资源
-> 克隆 GameObject 层级和组件
-> 恢复序列化字段与资源引用
-> 设置父节点/位置/旋转
-> 进入父节点所在 Scene，或当前 Active Scene
-> 触发生命周期回调
```

如果没有父节点，实例通常进入 Active Scene。需要指定其他已加载 Scene 时：

```csharp
GameObject enemy = Instantiate(enemyPrefab);
SceneManager.MoveGameObjectToScene(enemy, targetScene);
```

使用该 API 时需引入 `UnityEngine.SceneManagement`，并确保传入的是根
GameObject；有父节点的对象应先调整层级或通过父节点所在 Scene 决定归属。

### 11.2 从空对象开始

```csharp
GameObject marker = new GameObject("RuntimeMarker");
marker.transform.position = Vector3.zero;
marker.AddComponent<MarkerBehaviour>();
```

这适合简单运行时对象。复杂对象更适合 Prefab，否则代码会逐项搭组件，慢慢长成
一份只能由作者本人翻译的装配说明书。

## 12. 销毁与对象池

```csharp
Destroy(enemy);          // 通常延迟到当前 Update 循环之后处理
Destroy(enemy, 2.0f);    // 延迟销毁
```

`DestroyImmediate` 主要用于谨慎的编辑器工具，不应作为普通运行时代码的默认
选择。

频繁创建和销毁子弹、特效或飘字会产生：

- 原生对象创建/销毁成本。
- 托管分配与 GC 压力。
- 组件初始化与事件注册成本。

对象池会预先创建并重复启停实例：

```text
Get -> 激活并重置 -> 使用 -> Release -> 停用并归还
```

归还时必须重置速度、计时器、事件订阅和临时状态。对象池如果只负责“藏起来”
而不负责“洗干净”，下一位使用者会收到上一局留下的惊喜。

## 13. 本章检查

1. Project 窗口中的 Prefab 和 Hierarchy 中的实例有什么区别？
2. GameObject 与 Component 各自负责什么？
3. 为什么 Transform 不能像普通组件一样移除？
4. 自定义组件为什么通常继承 MonoBehaviour？
5. MonoBehaviour 为什么能接收 `Update`，却不需要写 `override`？
6. `.meta` 丢失为什么可能让引用断开？
7. `Destroy` 后 Unity 对象为何可能表现为 null？
8. `Instantiate` 的对象默认进入哪个 Scene，如何改变？

[返回 Unity 引擎基础](./README.md) |
[下一章：MonoBehaviour 生命周期与主循环](./02-monobehaviour-lifecycle-and-playerloop.md)
