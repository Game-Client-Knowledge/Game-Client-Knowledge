# 物理引擎与物理材质

## 1. Unity 物理系统负责什么

Unity 内置 3D 物理系统基于 PhysX，2D 物理使用独立的 2D 模拟体系。两套组件
不要混用：

```text
3D: Rigidbody + Collider + Physics
2D: Rigidbody2D + Collider2D + Physics2D
```

物理系统主要负责：

- 碰撞查询。
- 刚体运动。
- 接触生成与碰撞响应。
- 重力、力、冲量和阻尼。
- 关节约束。
- Trigger 重叠事件。

它不是“真实世界复制器”。实时物理追求稳定、可控和足够可信，偶尔也会为了
不让一摞箱子飞向月球而牺牲一点教科书式浪漫。

## 2. Collider：物理形状

Collider 定义用于碰撞的形状，不必与渲染 Mesh 完全相同。

### 2.1 Primitive Collider

- BoxCollider。
- SphereCollider。
- CapsuleCollider。

优点：

- 计算便宜。
- 稳定。
- 适合角色、箱体和大多数道具。

### 2.2 Compound Collider

在带 Rigidbody 的根节点下放多个子 Collider：

```text
Car (Rigidbody)
├── BodyBox (BoxCollider)
├── FrontShape (BoxCollider)
└── CabinShape (BoxCollider)
```

它用多个简单形状近似复杂物体，通常比一个高精度 MeshCollider 更高效。

### 2.3 MeshCollider

使用 Mesh 作为碰撞形状，适合复杂静态环境。动态物体通常需要 Convex 或改用
复合 Collider，具体限制以当前版本为准。

渲染模型有五万面，不代表碰撞也应该五万面。玩家通常不会因为楼梯扶手的碰撞体
少了三颗螺丝而退款，但会因为物理帧超时而明显感觉卡顿。

## 3. Rigidbody：运动状态

Collider 说明“形状是什么”，Rigidbody 说明“它是否参与刚体仿真以及怎么动”。

### 3.1 Static Collider

```text
Collider
无 Rigidbody
```

用于地面、墙壁和不移动环境。频繁移动 Static Collider 会迫使物理世界更新空间
结构，应改为 Kinematic Rigidbody 等合适方案。

### 3.2 Dynamic Rigidbody

```text
Collider + Rigidbody
isKinematic = false
```

由物理系统根据力、速度、重力和碰撞推进。

### 3.3 Kinematic Rigidbody

```text
Collider + Rigidbody
isKinematic = true
```

不由普通力推动，通常由脚本或动画控制，但仍可参与查询和与动态刚体交互。
适合移动平台、机关和需要完全控制轨迹的对象。

## 4. 一次物理步做什么

```mermaid
flowchart LR
    Integrate[积分速度与预测位置] --> Broad[Broad Phase 粗检测]
    Broad --> Narrow[Narrow Phase 精检测]
    Narrow --> Contact[生成接触点]
    Contact --> Solve[约束求解]
    Solve --> Write[写回位置与速度]
    Write --> Events[Collision / Trigger 回调]
```

### 4.1 Broad Phase

使用空间结构快速排除不可能相交的对象对。

### 4.2 Narrow Phase

对候选形状进行精确检测，计算接触点、法线和穿透。

### 4.3 Solver

迭代求解接触、摩擦和 Joint 约束。迭代次数越高通常更稳定，也更耗 CPU。

## 5. Fixed Timestep

物理通常按 `Time.fixedDeltaTime` 推进：

```text
累积帧时间
-> 足够一个 fixed step?
-> FixedUpdate
-> Physics simulation
-> 可能重复多次
```

固定步过大：

- 运动不够平滑。
- 碰撞更容易遗漏。
- 控制响应粗糙。

固定步过小：

- 每秒物理次数增加。
- CPU 成本上升。
- 卡顿时可能补算更多步骤。

应结合目标设备、对象数量和玩法精度测试，而不是因为 `0.01` 看起来比 `0.02`
更认真就直接翻倍物理预算。

## 6. Collision 与 Trigger

### 6.1 Collision

普通 Collider 发生接触并产生物理响应：

```csharp
private void OnCollisionEnter(Collision collision)
{
    Debug.Log($"Hit {collision.gameObject.name}");
}
```

`Collision` 可提供接触点、相对速度和碰撞对象等信息。

### 6.2 Trigger

启用 `isTrigger` 后，不产生普通阻挡响应，只报告重叠：

```csharp
private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        OpenDoor();
    }
}
```

用于：

- 任务区域。
- 拾取物。
- 伤害范围。
- 检查点。
- AI 感知区域。

常规 3D Trigger 事件要正常产生，碰撞对中通常至少一方需要 Rigidbody。若两个
对象都只是静态 Collider，物理系统不会把它们当成需要动态报告的组合。

## 7. 物理材质是什么

物理材质描述 Collider 表面的摩擦与弹性，不负责视觉颜色。

```text
Visual Material
-> Shader、纹理、颜色、渲染状态

Physics Material
-> 静摩擦、动摩擦、弹性、合并规则
```

给冰面设置蓝色 Material 不会自动变滑；给 Collider 设置低摩擦 Physics
Material 才影响物理。美术材质负责“看起来像冰”，物理材质负责“走起来像冰”。

### 7.1 版本命名

- Unity 2021/2022 等版本的 3D API 常见 `PhysicMaterial`。
- Unity 6.3 的 3D API 使用 `PhysicsMaterial`。
- 2D 使用独立的 Physics Material 2D 类型。

概念相同，代码应以项目 Editor 版本的 API 为准。

## 8. Physics Material 参数

| 参数 | 含义 |
|---|---|
| Static Friction | 接触面尚未滑动时阻止启动的摩擦 |
| Dynamic Friction | 已经滑动时阻碍相对运动的摩擦 |
| Bounciness | 碰撞后保留多少法向速度趋势 |
| Friction Combine | 两个表面摩擦如何组合 |
| Bounce Combine | 两个表面弹性如何组合 |

常见 Combine：

- Average。
- Minimum。
- Maximum。
- Multiply。

两个 Collider 各自有材质时，物理系统根据组合模式得到最终值。不同版本对冲突
模式的优先规则可能有细节差异，应查当前版本文档。

示例配置：

```text
Ice
Static Friction  = 0.02
Dynamic Friction = 0.01
Bounciness       = 0

Rubber Ball
Static Friction  = 0.6
Dynamic Friction = 0.5
Bounciness       = 0.85
```

这些值不是现实材料的绝对复刻。接触点数量、求解器、速度和时间步都会影响结果。

## 9. 运行时分配物理材质

Unity 6 风格示意：

```csharp
public sealed class SurfaceSwitcher : MonoBehaviour
{
    [SerializeField] private PhysicsMaterial ice;
    [SerializeField] private PhysicsMaterial rubber;

    private Collider targetCollider;

    private void Awake()
    {
        targetCollider = GetComponent<Collider>();
    }

    public void UseIce()
    {
        targetCollider.sharedMaterial = ice;
    }

    public void UseRubber()
    {
        targetCollider.sharedMaterial = rubber;
    }
}
```

旧版本将类型名替换为 `PhysicMaterial`。修改共享资源会影响所有引用它的对象；
只想影响当前 Collider 时，应创建独立实例或使用版本对应的实例接口，并负责其
生命周期。

## 10. Layer Collision Matrix

Layer 不只用于摄像机剔除，也用于物理过滤。Project Settings 中可以配置：

```text
Player      vs Enemy       = collide
Player      vs PlayerGhost = ignore
Projectile  vs Owner       = ignore
UIRaycast   vs World       = ignore
```

先用 Layer Matrix 排除不需要的组合，比在每次 `OnCollisionEnter` 里收到事件后
再说“抱歉找错人”更高效。

运行时：

```csharp
Physics.IgnoreLayerCollision(playerLayer, ghostLayer, true);
Physics.IgnoreCollision(colliderA, colliderB, true);
```

## 11. 物理查询

### 11.1 Raycast

```csharp
if (Physics.Raycast(
    origin,
    direction,
    out RaycastHit hit,
    maxDistance,
    targetMask,
    QueryTriggerInteraction.Ignore))
{
    Debug.Log(hit.collider.name);
}
```

适合射击、视线和地面检测。

### 11.2 SphereCast / CapsuleCast

带体积地扫过空间，适合：

- 摄像机避障。
- 角色前方检测。
- 近战攻击。
- 比单线 Raycast 更稳定的地面查询。

### 11.3 Overlap

```csharp
int count = Physics.OverlapSphereNonAlloc(
    center,
    radius,
    results,
    enemyMask
);
```

适合范围技能和邻域查询。NonAlloc 版本避免每次返回新数组，但需要：

- 预分配 Buffer。
- 处理 Buffer 装满。
- 清楚有效元素范围。

如果场上敌人超过数组容量，物理系统不会替你把数组变大并附上一封道歉信。

## 12. 离散与连续碰撞检测

### Discrete

只比较离散物理步位置，成本较低。高速小物体可能从墙一侧跳到另一侧，发生穿透。

### Continuous 系列

考虑运动轨迹或更保守的检测，适合高速关键对象，但成本更高。具体模式如
Continuous、Continuous Dynamic、Continuous Speculative 的支持和行为取决于
对象类型与 Unity 版本。

常见策略：

- 普通物体用 Discrete。
- 高速子弹用 Raycast 或连续检测。
- 重要角色使用合适的连续模式。
- 不要把全场所有碎片都设成最高成本模式。

## 13. Joint

Joint 用约束连接 Rigidbody：

- FixedJoint。
- HingeJoint。
- SpringJoint。
- ConfigurableJoint。

使用场景：

- 门轴。
- 绳索或链条。
- 车辆悬挂。
- 布娃娃。

约束链过长、质量比悬殊或迭代不足会抖动。可以调整质量、求解迭代、投影和结构，
而不是只把所有参数同时调大并等待物理引擎理解诚意。

## 14. 物理性能

重点观察：

- 活跃 Rigidbody 数量。
- Collider 对数量和形状复杂度。
- Layer Matrix 是否过滤无关组合。
- MeshCollider 和 Compound Collider 使用。
- Fixed Timestep 与补算次数。
- Solver Iterations。
- Raycast/Overlap 数量和分配。
- Transform 与 Physics 同步。
- 睡眠对象是否被无意义唤醒。

使用 Physics Profiler、CPU Timeline 和目标场景回放定位，不要只通过“刚体很多”
猜测。

## 15. 高频误区

| 误区 | 更准确的理解 |
|---|---|
| 有 Collider 就会掉落 | 还需要 Dynamic Rigidbody 才受重力仿真 |
| Trigger 完全不参与物理系统 | 它不阻挡，但仍参与 Broad Phase 和重叠事件 |
| 视觉 Material 决定摩擦 | 摩擦来自 Physics Material |
| 摩擦 1 就完全不会滑 | 求解是近似，受力和接触配置仍会影响 |
| Rigidbody 应在 Update 里随便改 Transform | 应通过合适的物理接口和固定步更新 |
| MeshCollider 最精确所以最好 | 精度、动态限制和成本需要权衡 |

## 16. 本章检查

1. Collider 与 Rigidbody 的职责有何区别？
2. Static、Dynamic、Kinematic Collider 如何区分？
3. Broad Phase 和 Narrow Phase 分别做什么？
4. Trigger 为什么通常要求碰撞对中至少有一个 Rigidbody？
5. Physics Material 和视觉 Material 有何区别？
6. 静摩擦、动摩擦和弹性分别控制什么？
7. Layer Collision Matrix 为什么比回调内过滤更早？
8. 高速子弹穿墙有哪些解决方法？

参考版本说明：
[Unity 6.3 PhysicsMaterial API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PhysicsMaterial.html) |
[Unity 6.3 Collider 概览](https://docs.unity3d.com/6000.3/Documentation/Manual/CollidersOverview.html)

[上一章：输入、角色移动与摄像机](./05-input-character-movement-and-camera.md) |
[返回 Unity 引擎基础](./README.md) |
[下一章：渲染管线、层级与 Shader](./07-rendering-order-pipelines-and-shaders.md)
