# Chaos 物理、碰撞与网络速记

## 碰撞

Collision Preset 由 Object Type、Trace Channel 和对各 Channel 的 Ignore/Overlap/Block Response 组成。Query 用于 Trace/Overlap，Simulation 用于物理响应；组件可分别启用。

- Hit 表示阻挡接触/扫掠结果，Overlap 表示重叠通知；事件还依赖生成开关与双方配置。
- Line Trace 是射线，Sweep 带形状，Overlap 查询某区域当前对象。
- 简单形状/Compound 通常优于复杂动态三角网格；Physical Material 提供摩擦、弹性和表面类型。
- Chaos 刚体/Constraint 的稳定性受时间步、质量比、solver 和形状影响。

## 网络权威

UE 使用服务器权威复制。Actor 开启 replication 只是入口；属性、组件、RPC、Relevancy、Dormancy、Frequency 和 Ownership 共同决定流量和可见性。

| 机制 | 用途 | 关键边界 |
|---|---|---|
| Replicated Property/RepNotify | 持久状态最终同步 | 不保证每次中间变化都触达 |
| Server RPC | owning client 请求服务器 | 校验 ownership 与输入，服务端验证 |
| Client RPC | 服务器通知 owning client | 只到目标连接 |
| NetMulticast | 服务器通知相关副本 | 不替代持久状态；受 relevancy |
| Reliable | 必须到达且有序 | 堆积会阻塞，不能滥用于高频状态 |
| Unreliable | 高频可丢事件/状态 | 需容忍丢失和重排语义 |

Authority 表示状态权威；Autonomous Proxy 是拥有者本地预测 Pawn；Simulated Proxy 是其他客户端代理。Ownership 决定 RPC 路由，不等于 Attach/Outer。

## 带宽与移动

Relevancy/Replication Graph 控制对谁复制，NetUpdateFrequency 控制更新机会，Dormancy 让稳定 Actor 停止发送。量化、条件复制、Fast Array/增量、聚合事件减少带宽。

开火主线：客户端本地表现/请求 → Server 验证节流、弹药、命中 → 更新复制状态/Multicast 表现 → 客户端校正。回溯判定需保存历史并防作弊。

物理复制更难保持完全一致；关键 Gameplay 由服务器状态驱动，客户端做插值/预测，不依赖各端 Chaos 自然得到同结果。

## 高频追问

1. Object Channel 与 Trace Channel 如何配合？
2. Hit/Overlap/Trace/Sweep 的区别？
3. RepNotify 会不会看到每次赋值？
4. RPC ownership 错误时为什么不执行？
5. Reliable 为什么不能发送每帧位置？
6. Relevancy、Dormancy、Frequency 各优化什么？
7. CMC 与普通 Actor Replication 的差异？

[上一章：输入、动画与 AI](./06-input-character-animation-and-ai.md) | [下一章：渲染](./08-rendering-materials-and-ue5-graphics.md)
