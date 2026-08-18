# Chaos 物理、碰撞与网络同步

## 1. Chaos 物理体系

UE5 的物理系统以 Chaos 为核心，常见能力：

- 刚体。
- 碰撞查询和响应。
- Physical Material。
- Constraint。
- Cloth、Destruction、Vehicles 等扩展。

实时物理的目标是稳定、可控和足够可信，不是逐分子复刻现实。把 Restitution
调到 1 也不代表球会获得永动机许可证。

## 2. Collision Shape

常见 Shape：

- Box。
- Sphere。
- Capsule。
- Convex。
- Complex Triangle Mesh。

角色通常使用 Capsule，场景使用简单碰撞或按需求使用 Complex Collision。

```text
Visible Mesh: 很复杂
Collision: 若干 Box/Capsule/Convex 近似
```

简单形状更稳定、便宜。复杂三角形碰撞应根据静态/动态、查询和平台限制选择。

## 3. Query 与 Simulation

Collision Enabled 常见概念：

| 模式 | Query | Physics Simulation |
|---|---|---|
| No Collision | 否 | 否 |
| Query Only | 是 | 否 |
| Physics Only | 否/受限 | 是 |
| Query and Physics | 是 | 是 |

Query 包括 Trace、Sweep、Overlap；Simulation 处理刚体接触和响应。

一个只用于射线命中的检测体不一定需要物理模拟，一个模拟碎片也不一定需要参与
所有 Gameplay Query。

## 4. Object Channel、Trace Channel 与 Response

### Object Channel

说明对象是什么：

```text
Pawn
WorldStatic
WorldDynamic
PhysicsBody
Vehicle
自定义 Projectile
```

### Trace Channel

说明查询想找什么：

```text
Visibility
Camera
自定义 WeaponTrace
自定义 Interaction
```

### Response

对每个 Channel：

- Block。
- Overlap。
- Ignore。

```text
Projectile vs Enemy: Block
Projectile vs Owner: Ignore
Pickup vs Pawn: Overlap
CameraTrace vs Foliage: Ignore/自定义
```

Collision Profile 把这些配置命名复用，例如 `Pawn`、`Trigger`、`Ragdoll`。

## 5. Hit 与 Overlap

### Hit

Block 接触可产生 Hit 事件，常包含：

- Impact Point。
- Impact Normal。
- Actor/Component。
- Physical Material。
- Bone Name。

### Overlap

Overlap 不阻挡，适合：

- 拾取物。
- 伤害范围。
- 任务区域。
- 感知。

需要启用 Generate Overlap Events，并确保双方配置组合会产生 Overlap。

回调没有触发时按顺序检查：

```text
Collision Enabled
-> Object Type
-> Response
-> Generate Events
-> 实际 Shape 是否相交
-> Actor 是否在正确 World/Authority
```

## 6. Trace、Sweep 与 Overlap

### Line Trace

```cpp
FHitResult Hit;
FCollisionQueryParams Params(
    SCENE_QUERY_STAT(WeaponTrace),
    false,
    this
);

const bool bHit = GetWorld()->LineTraceSingleByChannel(
    Hit,
    Start,
    End,
    ECC_Visibility,
    Params
);
```

### Sweep

让 Sphere/Capsule/Box 沿路径移动，适合：

- 近战。
- 摄像机。
- 角色空间检测。

### Overlap

查询一个体积当前重叠对象，适合范围技能。

选择：

```text
一条射线 -> Line Trace
有体积的运动检测 -> Sweep
当前位置范围查询 -> Overlap
```

## 7. Physical Material

Physical Material 控制：

- Friction。
- Restitution。
- Density 等物理参数（依具体系统）。
- Surface Type。

Surface Type 可驱动：

```text
Trace Hit Physical Material
-> SurfaceType_Grass
-> 草地脚步声、粒子、弹孔
```

视觉 Material 负责“看起来像金属”，Physical Material 负责摩擦、弹性和表面
Gameplay 标签。二者可关联，但不是同一个资源。

## 8. Rigid Body 与 Constraint

PrimitiveComponent 可启用 Simulate Physics：

```text
Mass / Inertia
Gravity
Linear / Angular Damping
Velocity
Collision Shape
Constraints
```

常见问题：

- 质量比过大导致 Constraint 不稳定。
- 高速物体穿透，需要 CCD 或 Trace。
- 每帧 SetActorTransform 与物理争夺状态。
- 大量休眠刚体被反复唤醒。
- 复杂碰撞导致 Broad/Narrow Phase 成本。

## 9. 网络模式

| 模式 | 说明 |
|---|---|
| Standalone | 单机，无远端连接 |
| Listen Server | 服务器同时有本地玩家 |
| Dedicated Server | 无渲染本地玩家的专用服务器 |
| Client | 连接服务器的客户端 |

UE 多人游戏使用服务器权威模型：

```text
Client 输入/请求
-> Server 验证并修改权威状态
-> Replication 发送结果
-> Client 表现与预测
```

客户端不应直接宣布“我击中了，所以加 100 分”。服务器可以听取证词，但需要查验
证据。

## 10. Actor Replication

```cpp
AMyActor::AMyActor()
{
    bReplicates = true;
    SetReplicateMovement(true);
}
```

关键规则：

- 服务器生成的 Replicated Actor 可复制到相关客户端。
- 客户端自己生成的 Actor 默认只是本地对象。
- Actor 必须 Relevant 才会发送。
- Component 也要满足复制设置和所有权规则。
- Replication 发送状态变化，不是复制整个 C++ 内存。

## 11. 属性复制

```cpp
UPROPERTY(ReplicatedUsing=OnRep_Health)
float Health = 100.0f;

UFUNCTION()
void OnRep_Health(float OldHealth);
```

注册：

```cpp
void AMyCharacter::GetLifetimeReplicatedProps(
    TArray<FLifetimeProperty>& OutLifetimeProps) const
{
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);

    DOREPLIFETIME(AMyCharacter, Health);
}
```

服务器修改 `Health`，复制系统在网络更新中发送，客户端收到后调用 RepNotify。

注意：

- 不保证每个中间值都到客户端。
- 多个 OnRep 之间不要依赖未声明的固定顺序。
- OnRep 主要在接收复制的一端触发；服务器若需要相同表现，应明确调用共享逻辑。
- 持久状态用属性复制，瞬时请求/通知可考虑 RPC。

## 12. RPC

### Server RPC

所属客户端请求服务器：

```cpp
UFUNCTION(Server, Reliable)
void ServerFire(FVector_NetQuantize AimOrigin, FVector_NetQuantizeNormal AimDir);

void AMyCharacter::ServerFire_Implementation(
    FVector_NetQuantize AimOrigin,
    FVector_NetQuantizeNormal AimDir)
{
    if (!CanFire())
    {
        return;
    }

    PerformAuthoritativeFire(AimOrigin, AimDir);
}
```

### Client RPC

服务器发送给拥有该 Actor 的客户端：

```cpp
UFUNCTION(Client, Reliable)
void ClientShowError(const FText& Message);
```

### NetMulticast RPC

服务器调用，在服务器和相关客户端执行，常用于瞬时表现：

```cpp
UFUNCTION(NetMulticast, Unreliable)
void MulticastPlayImpactFX(FVector_NetQuantize Location);
```

Multicast 不是持久状态存储。后来加入的客户端不会因为过去发过 RPC 就自动看到
正确当前状态。

## 13. Reliable 与 Unreliable

### Reliable

- 保证尽力按可靠语义送达。
- 丢包会重传。
- 过多会阻塞后续可靠流并增加压力。

适合：

- 低频关键命令。
- 必须执行的状态转换请求。

### Unreliable

- 可能丢失。
- 适合高频、可被新数据替代的事件。

适合：

- 高频瞄准/表现更新。
- 可丢失特效。

不要把每帧输入都设为 Reliable，只因为“可靠听起来更专业”。可靠队列堵塞时，
专业地卡住仍然是卡住。

## 14. Ownership

RPC 能否执行取决于 Actor 的 Owning Connection：

```text
PlayerController
-> Possessed Pawn
-> Owned Weapon / Components
```

常见失败：

- 客户端在不拥有的 World Actor 上调用 Server RPC。
- Server 调 Client RPC，但 Actor 没有对应 owning client。
- Widget 直接试图复制；Widget 不是网络 Actor。

UI 请求通常通过本地 PlayerController/Pawn/Component 的拥有链发送到服务器。

## 15. Authority、Autonomous 与 Simulated

概念角色：

- Authority：服务器权威 Actor。
- Autonomous Proxy：本地拥有并可预测的 Pawn。
- Simulated Proxy：客户端看到的其他复制 Actor。

同一 C++ 类在不同机器上扮演不同角色。日志必须带 NetMode、Role/Authority 和
Actor 名称，否则三个窗口同时打印“BeginPlay”，看起来像引擎复读。

## 16. Relevancy、Frequency 与 Dormancy

### Relevancy

只给需要知道的连接发送 Actor：

- 距离。
- Owner Only。
- Always Relevant。
- 自定义规则。

### Net Update Frequency

控制考虑发送更新的频率，不等于最终网络包固定频率。

### Dormancy

长期不变 Actor 可休眠，变化时唤醒/Flush。

优化目标：

```text
少发无关 Actor
-> 少发未变化属性
-> 降低不重要对象频率
-> 对静止对象 Dormancy
```

大量玩家可考虑 Replication Graph 或版本对应的 Iris 能力。

## 17. CharacterMovement 网络同步

CharacterMovement 已实现复杂预测和校正：

```text
Client Saved Move
-> ServerMove RPC/网络移动数据
-> Server 模拟与确认
-> Client Correction
-> Re-simulate unacknowledged moves
```

自定义移动若绕开组件：

- 输入不进 Saved Move。
- 服务器不知道自定义状态。
- 校正持续发生。
- 模拟代理动画不一致。

应扩展 Movement Mode、网络序列化和预测，而不是在 Client Tick 私改位置。

## 18. 一个开火流程

```text
Client 按下 Fire
-> 本地播放可预测枪口反馈
-> ServerFire RPC
-> Server 检查冷却、弹药、位置和瞄准
-> Server Trace / Spawn Projectile
-> Server 修改 Health/Ammo
-> 属性 Replication + OnRep
-> Multicast/Gameplay Cue 播放非持久表现
```

需要处理：

- 延迟补偿。
- 射线起点合法性。
- 射速作弊。
- 可靠性。
- 命中历史。
- 观战者和后来加入者。

## 19. 物理复制

Simulated Rigidbody/Chaos 状态可复制，但高频物理同步很昂贵：

- 服务器权威。
- 客户端插值/平滑。
- 关键对象提高频率。
- 非关键碎片本地表现。
- 休眠与 Relevancy。

完全确定的多人物理解算很难。很多项目只同步关键权威结果，让碎片和布料在客户端
各自表演，不要求每块石头跨机器保持哲学一致。

## 20. 网络调试

- PIE 多玩家和 Dedicated Server 模式。
- `HasAuthority` / NetMode 日志。
- Network Profiler。
- Unreal Insights Networking。
- Packet Simulation：延迟、丢包、抖动。
- Replication Graph Debug。
- Visual Logger。

必须在弱网和独立进程测试。单窗口 PIE 中正常，只能证明单窗口 PIE 中正常。

## 21. 本章检查

1. Object Channel、Trace Channel 和 Response 有何区别？
2. Hit 与 Overlap 分别要求什么配置？
3. Line Trace、Sweep、Overlap 如何选择？
4. Physical Material 与视觉 Material 有何区别？
5. 服务器生成和客户端生成的 Replicated Actor 有何区别？
6. 属性复制与 RPC 如何选择？
7. RepNotify 为什么不能保证每个中间值？
8. RPC 为什么依赖 Ownership？
9. Reliable RPC 为什么不能无限使用？
10. CharacterMovement 自定义移动为何需要扩展预测数据？
11. Relevancy、Frequency、Dormancy 分别优化什么？

参考：
[UE Networking Overview](https://dev.epicgames.com/documentation/en-us/unreal-engine/networking-overview-for-unreal-engine)

[上一章：输入、角色移动、动画与 AI](./06-input-character-animation-and-ai.md) |
[返回 UE 引擎基础](./README.md) |
[下一章：渲染管线、材质与 UE5 图形能力](./08-rendering-materials-and-ue5-graphics.md)
