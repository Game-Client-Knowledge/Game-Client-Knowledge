# 时间、延迟与体验补偿速记

## 三条时间线

```text
Server Time：权威服务器时间轴
Simulation Time：固定 Tick 推进玩法
Render Time：客户端用于平滑显示的时间轴
```

本地预测可能领先服务器确认；远端插值通常故意落后服务器。两者同时存在并不矛盾。

## Tick 与帧率

- Simulation Tick 决定规则推进；
- Render Frame 决定画面刷新；
- Network Send Rate 决定数据发送频率；
- 三者不要求相等。

示例：

```text
服务器模拟 60 Tick/s
输入发送 30~60 Hz
状态快照 20 Hz
客户端渲染 120 FPS
```

客户端依靠预测和插值填补频率差。

## 时钟同步

不要直接相信客户端墙钟。常见估计：

1. 客户端记录请求发送单调时间；
2. 服务器返回接收/发送时间；
3. 客户端测得 RTT；
4. 多次采样，过滤高抖动值；
5. 估计 Server Offset；
6. 缓慢修正，避免时间轴跳变。

单次 RTT/2 只是近似，上下行路径可能不对称。

## 延迟构成

```text
输入采样
+ 客户端排队/帧边界
+ 上行网络
+ 服务器排队和 Tick
+ 下行网络
+ 客户端缓冲
+ 渲染与显示
```

只看 Ping 无法解释全部输入延迟。还要看服务器 Tick、发送队列、插值延迟和显示链。

## 五种体验技术

| 技术 | 作用 |
|---|---|
| 本地预测 | 自己立即响应，不等服务器确认 |
| Reconciliation | 用权威状态修正本地预测 |
| 快照插值 | 在两个远端历史状态之间平滑 |
| 有限外推 | 快照缺失时短时估计未来 |
| Rollback | 真实输入晚到后恢复历史并重模拟 |

服务器 Lag Compensation 则是在受限历史窗口内按玩家开火时的视图重建命中查询。

## 输入延迟

帧同步/Rollback 可将本地输入安排到未来 `D` 个 Tick：

- D 大：迟到和回滚更少，操作更慢；
- D 小：响应更快，回滚更频繁；
- 可按 RTT/Jitter 动态选择，但变化要平滑；
- 竞技模式需避免一方通过异常网络让所有人承担过高延迟。

## 插值缓冲

```text
renderTime = estimatedServerTime - interpolationDelay
```

Delay 应覆盖大部分 Jitter。过小会缓冲欠载、频繁外推；过大会增加远端显示延迟。

自适应策略关注：

- Snapshot 到达间隔分位数；
- 连续欠载/积压；
- 丢包突发；
- 服务器发送频率变化；
- 调整速度上限。

## 校正与视觉平滑

逻辑状态必须服从服务器；画面可平滑：

| 误差 | 处理 |
|---|---|
| 小 | 忽略或缓慢吸收 |
| 中 | 逻辑立即纠正，Render Transform 平滑 |
| 大 | Snap/Teleport 并清空历史 |
| 持续 | 上报规则版本、网络或作弊诊断 |

逻辑碰撞位置和显示位置要分离，否则平滑过程会产生新的判定错误。

## Lag Compensation 公平性

- 服务器保存短期历史 Hitbox；
- 将客户端开火 Tick 映射到可信服务器时间；
- 限制最大回溯窗口；
- 校验当时武器、弹药、射速和角色状态；
- 回溯只用于查询，结果应用到当前权威世界；
- 高延迟玩家不能获得无限补偿。

体验目标是在射手感受和被击者公平之间取舍。

## 暂停、后台与加速

- 在线权威模拟通常不能被单个客户端暂停；
- 客户端后台恢复后要重新同步时间和状态；
- 本地 `timeScale` 只影响表现，不改变服务器 Tick；
- 长时间卡帧不能一次执行无限追帧；
- 服务器过载时应监控 Tick Drift，而不是伪装成网络延迟。

## 指标

- RTT、Jitter、Loss、Reorder；
- Input-to-Server 和 Input-to-Display；
- Server Tick Time/Drift；
- Snapshot Buffer 欠载；
- 外推时长；
- 校正次数和距离；
- Rollback 次数、深度和重模拟时间；
- Lag Compensation 回溯分布。

## 高频追问

1. 为什么本地玩家领先，远端玩家却落后？
2. Tick、快照频率和 FPS 为什么可以不同？
3. Ping 很低但操作仍慢，可能卡在哪里？
4. 插值延迟如何选择？
5. 校正为什么分逻辑位置和显示位置？
6. Rollback 为何仍可能设置输入延迟？
7. Lag Compensation 怎样限制高延迟优势？

[上一篇：传输与协议](./03-transport-and-protocol-quick-notes.md) | [返回多人游戏模块](./README.md) | [深入：游戏同步模型](./game-synchronization/README.md) | [下一篇：复制与带宽](./05-replication-interest-and-bandwidth-quick-notes.md)
