# 物理、动画、同步与平台

## 1. 物理系统

| 优先级 | 核心知识 |
|---|---|
| P0 | Collider、Rigidbody、Trigger |
| P0 | Broad Phase / Narrow Phase |
| P1 | AABB、OBB、SAT、GJK |
| P1 | 离散与连续碰撞检测 |
| P1 | 积分、约束、摩擦和反弹 |
| P1 | Character Controller |

一次碰撞检测不会让所有物体两两精确比较：

```text
Broad Phase
用 AABB、网格或 BVH 快速找出可能相交的候选对
        |
        v
Narrow Phase
使用具体形状计算是否接触、接触点和法线
        |
        v
Solver
根据约束、质量、速度、摩擦和恢复系数求解响应
```

典型问题：

- 高速子弹为什么会穿透？
- Trigger 和 Collision 的处理差异是什么？
- Broad Phase 为什么需要空间加速结构？
- 近战攻击用碰撞器还是动画关键帧范围检测？

高速物体可能在两个离散时间点分别位于障碍物两侧，从未“采样到”相交状态。
连续碰撞检测会考虑运动轨迹，但成本也更高，因此通常只给关键高速对象使用。

## 2. 动画系统

| 优先级 | 核心知识 |
|---|---|
| P0 | 骨骼、蒙皮、关键帧 |
| P0 | 动画状态机、Blend Tree |
| P1 | Root Motion、Animation Event |
| P1 | IK、动画分层、Avatar Mask |
| P2 | GPU Skinning、动画压缩 |

动画系统常见数据流：

```text
玩法状态
-> 选择状态与动画片段
-> 混合姿势
-> 计算骨骼局部/全局变换
-> 蒙皮得到最终顶点
-> 提交渲染
```

Root Motion 让动画驱动位移，适合动作与距离强绑定的表现；代码驱动位移更便于
玩法、网络和碰撞控制。工程中常按技能类型混合使用，而不是举办一场非黑即白的
路线之争。

## 3. 游戏网络同步

| 优先级 | 核心知识 |
|---|---|
| P0 | 服务器权威模型 |
| P0 | 状态同步与帧同步 |
| P0 | 快照、插值、外推 |
| P0 | 客户端预测与服务器校正 |
| P1 | 延迟补偿、输入缓冲 |
| P1 | AOI、属性同步、增量同步 |
| P1 | 确定性、随机种子、浮点差异 |
| P1 | 断线重连、状态恢复 |
| P2 | Rollback、Replay、反作弊 |

### 3.1 状态同步

服务器计算权威状态，向客户端发送快照或增量：

```text
客户端输入 -> 服务器仿真 -> 权威状态 -> 客户端插值显示
```

优点是客户端不必完全确定性；代价是状态带宽和显示延迟，需要插值、外推与预测。

### 3.2 帧同步

服务器主要转发按逻辑帧编号组织的输入，各端执行同一套确定性逻辑：

```text
各端输入 -> 服务器排序确认 -> 广播帧输入 -> 各端执行相同仿真
```

优点是传输输入即可，适合大量确定性状态；代价是必须控制浮点、随机数、执行
顺序和版本差异，并设计卡帧、预测或回滚策略。

典型问题：

- 状态同步和帧同步如何选择？
- 客户端预测为什么会产生回拉？
- 如何平滑服务器校正？
- 帧同步为什么要求确定性？
- 攻击判定依赖动画帧时，弱网如何补偿？
- 哪些结果必须由服务端权威计算？

## 4. Unity 专项

Unity 已拆分为独立的 [Unity 引擎基础](../unity-engine/README.md) 子模块，系统覆盖：

- Scene、GameObject、Component、MonoBehaviour 和 Prefab。
- PlayerLoop、生命周期、协程、时间与异步。
- SceneManager、Resources、AssetBundle 与 Addressables。
- Input System、角色移动、摄像机、Collider、Rigidbody 与 Physics Material。
- Built-in、URP、HDRP、Render Queue、Sorting Layer 和 Shader。
- Animator、UGUI、ScriptableObject、序列化、GC、Jobs/Burst 与 IL2CPP。

本页不再维护第二份 Unity 提纲，避免同一知识点在两个目录中各自进化。

## 5. Unreal 专项

Unreal 已拆分为独立的 [UE 引擎基础](../unreal-engine/README.md) 子模块，系统覆盖：

- Editor、World、Level、Actor、Component 和 Spawn。
- UObject、反射、CDO、GC、对象指针与 Actor 生命周期。
- GameMode、GameState、PlayerController、PlayerState、Pawn 和 Character。
- Blueprint Graph、Interface、Event Dispatcher 与 C++ 协作。
- Soft Reference、Asset Manager、World Partition、Data Layer 和 Cook。
- Enhanced Input、CharacterMovement、Animation Blueprint 和 AI。
- Chaos、Collision、Replication、RPC 和客户端预测。
- Material、Render Thread、Nanite、Lumen、UMG、GAS 与 Unreal Insights。

本页不再维护第二份 Unreal 提纲，避免基础模块和速查表对同一概念给出不同边界。

## 6. 平台工程

| 优先级 | 核心知识 |
|---|---|
| P1 | Android/iOS 生命周期 |
| P1 | 前后台切换、权限、输入设备 |
| P1 | 文件系统、包体和热更新限制 |
| P1 | 多分辨率、安全区、本地化 |
| P2 | 主机认证、跨平台抽象 |

跨平台抽象应隔离能力差异，而不是假装所有平台完全相同。常见做法是定义稳定的
上层接口，再由平台实现报告支持能力和限制，让调用方可以选择降级路径。

[上一章：引擎架构与运行时](./01-engine-architecture-and-runtime.md) |
[返回引擎与客户端核心](./README.md)
