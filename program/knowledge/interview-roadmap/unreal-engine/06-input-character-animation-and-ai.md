# UE 输入、移动、动画与 AI 速记

## 输入与移动

Enhanced Input 用 Input Action 表达意图，Mapping Context 按模式/优先级映射设备输入，Modifier/Trigger 处理值与触发。Context 通常加到 LocalPlayer Subsystem，绑定随 Pawn/Controller 生命周期管理。

Pawn 是可控制实体；Character 组合 Capsule、SkeletalMesh 与 CharacterMovementComponent（CMC）。`AddMovementInput` 提供期望方向，CMC 在移动更新中消费；直接 `SetActorLocation` 可能绕过移动模式、碰撞和网络预测。

CMC 联机主线：客户端预测并保存 move → Server 校验/模拟 → 必要时 correction → 客户端重放未确认 move → Simulated Proxy 插值。自定义移动要扩展可序列化 move 状态，而非只在本地 Tick 改位置。

## 相机

Spring Arm 处理距离/碰撞，CameraComponent 提供视图，PlayerCameraManager 管理 ViewTarget、blend、shake 与最终 POV。相机属于本地表现，不应把服务器权威逻辑耦合进 Camera Actor。

## 动画

```text
Gameplay/CMC 状态 -> Animation Blueprint
-> State Machine/Blend Space/Montage
-> Pose + IK/Control Rig/Motion Warping
-> Skeletal Mesh
```

Anim Instance 更新可能涉及并行评估；线程安全路径不能随意访问 Game Thread UObject。Notify 适合表现/窗口通知，权威伤害不只依赖动画帧事件。Root Motion 要与 CMC、网络和碰撞统一控制权。

## AI

AIController 控制 Pawn；Blackboard 保存决策数据；Behavior Tree/StateTree 组织逻辑；EQS 选择环境位置；Perception 输入感知；NavMesh/PathFollowing 执行导航。

Behavior Tree Task 必须正确结束或处理 latent/abort；频繁 Service/EQS 查询要预算。大量 AI 通过感知频率、LOD、群体/批处理和导航查询限流优化。

## 高频追问

1. Mapping Context 和 Input Action 分别负责什么？
2. CMC 客户端预测与校正如何工作？
3. 为什么不能用 SetActorLocation 替代网络移动？
4. Montage、State Machine、Blend Space 如何分工？
5. Root Motion 如何参与联机移动？
6. Anim Blueprint 并行更新有哪些线程限制？
7. BT Task Abort/latent 不处理会怎样？

[上一章：资源与大世界](./05-assets-async-loading-and-world-partition.md) | [下一章：物理与网络](./07-physics-collision-and-networking.md)
