# 复制、兴趣管理与带宽速记

## 复制什么

Replication 是将权威世界中**需要远端知道的语义状态**传播出去，不是序列化整个运行时对象。

常见内容：

- Entity Spawn/Despawn；
- 位置、旋转、速度和移动模式；
- HP、Buff、技能阶段和队伍；
- 门、机关、载具等交互状态；
- 可靠 Gameplay Event；
- 所有权和可见性变化。

通常不复制摄像机、纯本地 IK、粒子细节、UI 状态和完整骨骼姿态。

## 网络实体身份

```text
NetworkEntityId + Generation/SpawnVersion
```

生命周期：

1. Spawn 建立实体和初始 Baseline；
2. Delta 只应用到匹配 Generation；
3. Despawn 清理实体、历史和预测映射；
4. 晚到旧包因 Generation 不匹配被丢弃。

不能使用进程指针或容易复用的数组下标作为跨网络永久身份。

## Ownership、Authority 与 Relevancy

- **Authority**：谁决定最终状态；
- **Ownership**：哪个连接控制或接收私有数据；
- **Relevancy**：某实体当前是否应发送给该连接；
- **Priority**：带宽不足时谁先发送。

四者不同。客户端拥有 Pawn 不代表拥有权威；相关实体也不一定属于该客户端。

## Snapshot 与 Delta

```text
Snapshot {
  serverTick
  snapshotSequence
  baselineSequence
  spawned[]
  changed[]
  despawned[]
}
```

Delta 只发送相对已确认 Baseline 的变化。Baseline 不可用时：

- 使用更早的已确认版本；
- 发送 Keyframe；
- 重置客户端状态；
- 不能继续发送无法解码的差异链。

状态可以被后续快照覆盖，离散事件则要独立保证交付或幂等。

## 兴趣管理

AOI/Relevancy 可依据：

- 空间距离、网格、Zone；
- 视锥、遮挡和战争迷雾；
- 队伍、频道、任务和所有权；
- 声音/战斗影响范围；
- 观战视角；
- 隐私和防作弊规则。

兴趣集合变化时要正确发送 Spawn/Despawn 或 BecomeRelevant/Irrelevant，避免对象幽灵或信息泄露。

## 空间结构

| 结构 | 适用 |
|---|---|
| Uniform Grid | 分布较均匀、动态对象多 |
| Quadtree/Octree | 空间层级和稀疏区域 |
| BVH | 查询与可见性，更新成本需评估 |
| Zone/Cell | 大世界分区与服务器边界 |
| Replication Graph | 将业务规则和空间节点组合 |

结构选择取决于对象分布、移动频率和查询方式，不只看理论复杂度。

## 优先级和频率

高优先级：

- 本地玩家和直接威胁；
- 近距离、高速和正在交互对象；
- 新 Spawn、重要状态变化；
- 可靠比赛事件。

低优先级：

- 远距离静止对象；
- 装饰和低价值 AI；
- 可由客户端派生的表现；
- 已休眠实体。

更新频率可按距离、速度和重要性分层，并设置最大静默时间，避免长期不刷新。

## Dormancy

稳定对象停止周期复制，仅在唤醒或变化时发送：

- 门关闭后休眠；
- 掉落物静止后降频；
- 远端 NPC 降低更新；
- 重新相关时发送当前完整状态。

Dormancy 状态切换本身需要可靠管理，否则客户端可能永远停留在旧状态。

## 量化与压缩

- 位置相对区域原点编码；
- 旋转使用有限精度；
- 速度按业务范围量化；
- Bool/Enum 使用位打包；
- Changed Mask 指示字段；
- 同类型实体批量共享元数据；
- 大快照再考虑通用压缩。

量化误差要在命中、移动、回放和跨 Zone 边界下测试。

## 带宽预算

粗略估算：

```text
每连接下行
≈ 相关实体数
× 平均变化字节
× 平均更新频率
+ 生命周期/事件/协议开销
```

服务器总出口还乘在线连接数。预算要看 P95/P99 场景，而不是空地图平均值。

超预算策略：

1. 合并状态；
2. 降低低优先级频率；
3. 缩小兴趣范围；
4. 提高量化；
5. 丢弃可覆盖旧状态；
6. 保证输入和关键事件；
7. 慢连接必要时断开。

## 大世界跨区

- 全球稳定 Entity ID；
- 旧 Zone 冻结并输出迁移快照；
- 新 Zone 接管权威并产生新 Epoch；
- 客户端处理双连接或无缝切换；
- 附近实体兴趣集合重建；
- 旧 Zone 晚包因 Epoch/Generation 被丢弃。

## 高频追问

1. Replication 为什么不直接序列化对象内存？
2. Authority、Ownership 和 Relevancy 的区别？
3. Delta Snapshot 为什么需要确认 Baseline？
4. AOI 如何兼顾性能和透视防护？
5. Dormancy 唤醒时为什么要发送完整状态？
6. 带宽不足时如何决定丢谁？
7. 大世界迁移为什么需要 Epoch/Generation？

[上一篇：时间与延迟](./04-time-latency-and-compensation-quick-notes.md) | [返回多人游戏模块](./README.md) | [下一篇：调试与测试](./06-debugging-testing-and-observability-quick-notes.md)
