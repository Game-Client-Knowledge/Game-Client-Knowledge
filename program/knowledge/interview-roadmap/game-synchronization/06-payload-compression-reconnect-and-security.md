# 协议数据、压缩、重连与安全

## 消息分层

不要把所有同步内容塞进一种“游戏包”。可以分为：

| 消息 | 典型语义 | 可靠性 |
|---|---|---|
| Input Command | 高频输入，可冗余最近几帧 | 通常不可靠或半可靠 |
| State Snapshot | 最新状态覆盖旧状态 | 通常不可靠、有序性由序列号判断 |
| Spawn/Despawn | 实体生命周期 | 可靠、去重 |
| Gameplay Event | 技能确认、命中、交互结果 | 按业务选择可靠性 |
| Config/Inventory | 低频强一致数据 | 可靠、版本化 |
| Checkpoint | 重连、观战、分歧修复 | 可靠、可分块 |
| Ping/Clock Sync | RTT 与时钟估计 | 可丢，持续更新 |

“可靠”不是协议名，而是业务语义。基于 UDP 也可以做选择性确认和重传。

## 通用包头

示意字段：

```text
PacketHeader {
  protocolVersion
  connectionId
  packetSequence
  ack
  ackBits
  channelId
  flags
  payloadBytes
}
```

消息内部再携带 `simulationTick`、`inputSequence`、`snapshotSequence`。协议版本和 Feature Capability 用于灰度与兼容，不能只靠客户端版本字符串猜测格式。

## 三类核心负载

### 输入命令

```text
InputBatch {
  newestInputSequence
  commands[tick N, N-1, N-2]
}
```

冗余最近输入可减少等待重传。连续轴量化为定点整数；按钮用 bitset；离散动作带 Prediction Key。

### 状态快照

```text
Snapshot {
  serverTick
  snapshotSequence
  baselineSequence
  ackedInputSequence
  entityDeltas[]
  events[]
}
```

### 帧输入

```text
LockstepFrame {
  simulationTick
  orderedPlayerCommands[]
  previousStateHash
}
```

接收端必须验证长度、数量、枚举和实体 ID，不能直接按网络数据创建任意大小容器。

## 带宽优化

优先顺序通常是：

1. **少发送**：AOI、休眠、优先级、变化时发送；
2. **低频发送**：按对象重要性分层频率；
3. **发送差异**：相对已确认 Baseline 做 Delta；
4. **量化**：位置、角度、速度按可接受误差编码；
5. **位打包**：Changed Mask、布尔、枚举；
6. **批处理**：共享 Tick、区域原点和类型信息；
7. **通用压缩**：更适合较大 Checkpoint，不一定适合每个小包。

位置可相对区域原点量化，而不是始终发送三个 32 位浮点。量化精度必须结合地图尺寸、命中精度和累计误差测试。

## Delta Baseline

服务器只能相对客户端确实拥有的状态编码：

```text
Server sends Snapshot 20 based on 18
Client ACKs 20
Server may use 20 as future baseline
```

若基线未确认：

- 改用更早的已确认基线；
- 发送独立 Keyframe；
- 请求客户端重置 Baseline；
- 不要继续发送无法解码的 Delta 链。

Delta 适合状态，离散事件不能只依赖“与上一帧不同”，否则丢包会永久漏掉事件。

## MTU、分片与拥塞

大 UDP 包在网络层分片后，任一分片丢失会使整包失效。同步协议应：

- 控制包大小低于保守路径 MTU；
- 对大 Checkpoint 做应用层分块；
- 分块有 ID、总数、校验和和超时；
- 使用发送预算、拥塞控制和背压；
- 不让重连快照挤占实时输入；
- 统计队列延迟，而不只统计出口带宽。

## 断线重连

状态同步：

```text
认证恢复 -> 最新全量/关键快照
-> 可靠状态与实体生命周期
-> 恢复增量流 -> 客户端重建表现
```

帧同步/Rollback：

```text
Checkpoint at Tick K
+ K+1..Current 的输入日志
-> 后台高速追帧
-> 校验 Hash
-> 切回实时 Tick
```

重连还要恢复连接级序列号、Prediction Key 映射、已确认业务请求和场景资源。不要重复发奖、扣款或生成实体。

## 中途加入与观战

观战端通常不需要成为权威模拟节点：

- 状态同步可下发延迟快照流；
- 帧同步可从检查点追帧，或由服务器专门生成观战状态；
- 观战延迟可增加，换取完整时间线和防止窥屏；
- Replay 数据与实时协议可共享语义，但应版本化。

## 安全

服务器验证：

- 输入频率、范围和顺序；
- 角色当前状态是否允许该动作；
- 移动速度、碰撞、资源消耗；
- 射速、弹药、技能冷却；
- 时间戳和 Lag Compensation 窗口；
- 实体可见性和操作权限；
- 消息大小、压缩炸弹和重放攻击。

帧同步还需考虑客户端拥有全量世界状态导致的信息作弊。可由服务器隐藏敏感数据、只下发可见命令，或运行权威模拟后裁决。

加密和签名保护传输与来源，不会让客户端计算天然可信。

## 可观测性

至少记录：

| 类别 | 指标 |
|---|---|
| 网络 | RTT、Jitter、Loss、Reorder、Duplicate、吞吐 |
| 队列 | 发送队列时长、重传队列、拥塞窗口 |
| 同步 | Snapshot/Input Hz、相关实体数、Delta 命中率 |
| 体验 | 预测误差、校正次数/距离、插值缓冲欠载 |
| Rollback | 回滚次数、平均/最大深度、重模拟耗时 |
| 一致性 | Hash 分歧率、首次分歧 Tick、重连成功率 |

只看平均 Ping 无法解释卡顿。应将一次卡顿关联到对应 Tick、网络包、校正和客户端帧耗时。

## 高频追问

1. 为什么位置快照不一定可靠重传？
2. 输入包为什么常冗余最近几帧？
3. Delta Snapshot 为什么需要客户端确认 Baseline？
4. 大 Checkpoint 为什么不能直接塞进一个 UDP 包？
5. 量化如何兼顾大地图和近距离精度？
6. 状态同步与帧同步的重连流程有什么不同？
7. 加密为什么不能代替服务器权威验证？

[上一章：预测与延迟补偿](./05-prediction-interpolation-and-lag-compensation.md) | [返回专题](./README.md) | [下一章：混合架构](./07-hybrid-architecture-and-interview.md)
