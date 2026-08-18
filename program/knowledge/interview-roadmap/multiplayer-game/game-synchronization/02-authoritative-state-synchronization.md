# 权威状态同步

## 核心结论

状态同步复制的是**模拟后的权威结果**。客户端发送输入或行为请求，服务器验证并运行核心模拟，再向相关客户端发送世界快照、状态增量和事件。

```text
客户端输入 -> 服务器验证/模拟 -> 权威状态
-> AOI 筛选/优先级/压缩 -> 客户端
-> 自己：校正预测
-> 他人：快照插值
```

服务器会下发位置，但通常不是每帧发送每个对象的完整 Transform。

## 同步什么

按游戏语义选字段：

| 系统 | 常见权威数据 |
|---|---|
| 移动 | 位置、旋转、线速度、移动模式、地面/载具 ID |
| 战斗 | HP、护盾、Buff、技能阶段、冷却结束 Tick、目标 ID |
| 实体生命周期 | Spawn/Despawn、Prefab/配置 ID、generation |
| 交互 | 门/机关状态、拾取归属、任务进度 |
| 投射物 | 生成 Tick、初始位置/速度、命中或销毁事件 |
| 动画表现 | 语义状态、Montage/动作 ID、开始 Tick；通常不复制完整骨骼姿态 |

纯本地摄像机、粒子随机细节、UI 动画和 IK 通常不进入权威状态。

## 客户端上行数据

移动输入示例：

```text
InputCommand {
  inputSequence
  clientTick
  moveX, moveY
  viewYaw, viewPitch
  buttons
  predictedStateHash?
}
```

客户端应该发送“我按了什么”，而不是直接声明“我已经移动到 `(100, 0, 50)`”。某些低安全业务可以发送目标点或操作结果，但服务器仍需校验速度、碰撞、资源和权限。

输入可合并、冗余携带最近几帧，降低单包丢失的影响。持续按键不必可靠重传到很久以后，离散技能释放则需要去重和明确处理结果。

## 服务器职责

1. 校验连接身份、输入序列、频率和参数范围；
2. 按服务器 Tick 消费输入，缺失时沿用、置空或使用游戏定义策略；
3. 运行移动、物理、战斗和生命周期逻辑；
4. 为每个连接计算 AOI/Relevancy；
5. 按优先级和带宽预算生成快照；
6. 相对客户端已确认基线做 Delta；
7. 下发权威状态、事件和已处理输入号；
8. 保存必要历史，用于重连、回放或延迟补偿。

服务器不应信任客户端命中、冷却结束、背包数量或最终伤害。

## 服务器下行数据

```text
StateSnapshot {
  serverTick
  snapshotSequence
  baselineSequence
  ackedInputSequence
  spawnedEntities[]
  changedEntities[]
  despawnedEntities[]
  reliableEvents[]
}
```

一个实体 Delta 可能包含：

```text
entityId + generation
changedMask
quantizedPosition
quantizedRotation
velocity
movementMode
gameplayFields...
```

`changedMask` 表明哪些字段存在。位置/角度可量化，低频属性只在变化时发送。快照必须说明相对哪个已确认基线编码；否则基线丢失会把 Delta 解成错误状态。

## 拥有者客户端：预测与校正

本地玩家不能等待一个 RTT 才移动：

1. 采样输入并分配 `inputSequence`；
2. 立即在本地预测模拟；
3. 保存未确认输入；
4. 把输入发给服务器；
5. 收到服务器状态和 `ackedInputSequence`；
6. 恢复到服务器权威状态；
7. 删除已确认输入；
8. 重放剩余输入到当前预测 Tick；
9. 逻辑误差立即修正，画面可平滑追赶。

这就是 Client-side Prediction + Server Reconciliation。预测代码与服务器移动规则越一致，校正越少；服务器仍拥有最终决定权。

## 非拥有者客户端：快照插值

远端实体通常不做完整本地预测，而是存入 Snapshot Buffer：

```text
Snapshot 100: position A
Snapshot 101: position B
Render Time 位于两者之间 -> interpolate(A, B)
```

客户端故意落后服务器一段插值延迟，换取两个已知端点。短时缺包可有限外推，超过阈值应冻结、降速或等待新快照，不能无限沿速度飞走。

## 带宽与规模

状态同步下行带宽大致随以下因素增长：

```text
相关实体数 × 每实体变化字段 × 更新频率
```

主要优化：

- AOI、距离和视野裁剪；
- 更新优先级与频率分层；
- Delta、量化、位打包；
- 静止/休眠实体停止发送；
- 事件与连续状态分开；
- 客户端确认基线；
- 预算耗尽时优先玩家、威胁和近距离对象。

## 适用场景

适合：

- MMO 和开放世界；
- FPS/TPS、动作和合作 PvE；
- 支持中途加入、动态实体和非确定性物理的游戏；
- 服务器需要隐藏信息并做强权威判断的场景。

代价：

- 服务器 CPU 和带宽高；
- 客户端需要复杂预测、插值与校正；
- 快照频率、视觉延迟和流量之间要权衡；
- 大误差校正可能突跳。

## 高频追问

1. 客户端为什么发送输入而不是位置？
2. 服务器下发位置时为何还要下发速度和 Tick？
3. 本地玩家与远端玩家为什么使用不同平滑策略？
4. Delta Snapshot 的基线丢了怎么办？
5. 20 Hz 快照如何表现高速投射物？
6. 如何给一千个相关实体分配更新预算？
7. 客户端预测碰撞与服务器不一致时如何处理？

[上一章：权威与时间轴](./01-authority-time-and-network-semantics.md) | [返回专题](./README.md) | [下一章：确定性帧同步](./03-deterministic-lockstep.md)
