# 游戏客户端面试知识地图

## 掌握标准

- **P0**：目标岗位高频，必须能展开追问并给项目证据。
- **P1**：常见专项或项目相关追问，能讲清机制与取舍。
- **P2**：岗位定向内容，了解边界并避免错误表述。

“会”至少意味着：定义、流程、对比、失效条件、验证方法都能回答。

## 知识领域索引

| 领域 | 高频主题 | 入口 |
|---|---|---|
| 语言/运行时 | C++ 生命周期、C# GC、Lua VM | [语言与数据结构](./foundations/01-language-runtime-and-data-structures.md) |
| 数据结构/算法 | 容器、图、寻路、空间索引 | [语言与数据结构](./foundations/01-language-runtime-and-data-structures.md) |
| OS/内存/并发 | 虚拟内存、Cache、锁、任务系统 | [系统、网络与数学](./foundations/02-systems-network-and-math.md) |
| 通用网络 | TCP/UDP、协议、弱网 | [系统、网络与数学](./foundations/02-systems-network-and-math.md) |
| 多人游戏架构 | 拓扑、权威、会话、匹配、传输、复制与观测 | [多人游戏](./multiplayer-game/README.md) |
| 游戏数学 | 矩阵、四元数、几何检测 | [系统、网络与数学](./foundations/02-systems-network-and-math.md) |
| 引擎架构 | 主循环、对象、资源、场景 | [引擎运行时](./engine-client/01-engine-architecture-and-runtime.md) |
| 图形渲染 | 坐标、Draw Call、GPU 管线 | [渲染专项](./rendering/README.md) |
| 物理/动画 | 碰撞、刚体、骨骼、状态机 | [物理、动画与平台](./engine-client/02-physics-animation-network-and-platforms.md) |
| 游戏同步 | 状态同步、帧同步、Rollback、预测、校正与重连 | [多人游戏：同步模型](./multiplayer-game/game-synchronization/README.md) |
| 游戏防作弊 | 威胁模型、权威校验、客户端完整性、行为检测与运营处置 | [多人游戏：防作弊](./multiplayer-game/game-anti-cheat/README.md) |
| 性能/工程 | CPU/GPU、内存、资源、稳定性 | [性能工程](./engineering/01-performance-and-production.md) |
| 引擎专项 | Unity/UE 对象与工程链 | [Unity](./unity-engine/README.md) / [UE](./unreal-engine/README.md) |
| 项目/设计 | 项目复盘、系统设计、表达 | [项目表达](./engineering/02-project-design-and-study-strategy.md) |

## 岗位优先级

| 领域 | Unity 玩法 | UE 玩法 | 引擎 | 渲染 |
|---|---:|---:|---:|---:|
| 主语言/生命周期 | P0 | P0 | P0 | P0 |
| 数据结构与算法 | P0 | P0 | P0 | P0 |
| 内存与并发 | P1 | P0 | P0 | P0 |
| 通用网络/同步 | P0 | P0 | P1 | P2 |
| 游戏数学 | P0 | P0 | P0 | P0 |
| 引擎架构 | P0 | P0 | P0 | P1 |
| 图形渲染 | P1 | P1 | P1 | P0 |
| 安全与防作弊 | P1 | P1 | P1 | P2 |
| 性能与项目 | P0 | P0 | P0 | P0 |

岗位 JD 与个人项目优先于通用表：项目写了资源热更、帧同步或渲染优化，就默认升级为 P0。

## 依赖主线

```text
语言/生命周期 + 数据结构 + 内存并发
                 -> 引擎主循环/资源
数学 -> 渲染        -> 性能
网络 -> 多人游戏 -> 同步/防作弊 -> 系统设计
引擎专项 + 项目证据  -> 最终表达
```

## 查漏方法

对每个 P0 主题完成：

1. 30 秒下定义；
2. 2 分钟画流程；
3. 比较至少两个方案；
4. 说明一个失效/故障场景；
5. 报出项目工具、指标与结果。

任一项只能说“差不多”，就把它加入追问清单，而不是继续扩大阅读范围。

[返回路线](./README.md) | [进入综合模拟题](./02-os-memory-concurrency-mock-interview.md)
