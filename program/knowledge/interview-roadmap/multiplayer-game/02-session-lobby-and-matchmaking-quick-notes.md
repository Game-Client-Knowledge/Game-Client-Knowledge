# 会话、大厅、组队与匹配速记

## 端到端状态机

```text
Offline -> Authenticated -> Online
-> Party -> Matchmaking
-> Reserved -> Connecting -> Ready
-> InMatch -> Reconnecting / Spectating
-> Settling -> Online
```

每个状态都要定义允许的请求、超时、取消和幂等。不要让客户端通过跳过 UI 直接调用后续接口。

## 身份与会话

- Access Token 用于短期访问，Refresh Token 用于续期；
- Game Join Token 应绑定账号、Match、服务器、角色和过期时间；
- 退出、封禁或重复登录后支持撤销；
- Session ID 不等于永久 Player ID；
- 重连使用新凭据恢复原 Match 身份；
- 日志避免记录原始令牌。

游戏服务器应验证由可信后端签发的 Join Token，不信任客户端自报账号。

## Presence 与组队

Presence 表达在线、忙碌、队伍和游戏状态。组队系统需处理：

- Leader、Member 与权限；
- 邀请过期、重复接受和撤销；
- 队长转移和离线；
- 队伍版本与并发修改；
- 跨平台好友与隐私；
- 队伍整体进入/退出匹配；
- 部分成员连接失败。

通过 `partyVersion` 或条件更新避免两个并发操作互相覆盖。

## Lobby 与 Room

Lobby 常用于：

- 地图/模式和规则选择；
- Ready 状态；
- 队伍与席位；
- 房主设置；
- 聊天和观战；
- 服务器信息与开始倒计时。

Lobby 状态由服务器维护。客户端发送“Ready 请求”，不能直接把全房间状态改为开始。

## 匹配输入

常见维度：

| 维度 | 作用 |
|---|---|
| Skill/MMR | 对局公平 |
| Region/RTT | 网络体验 |
| Party Size | 组队公平和填充 |
| Platform/Input | 跨平台和输入设备平衡 |
| Mode/Map | 内容偏好 |
| Role | 阵容和职责 |
| Trust/Risk | 风险池和反作弊 |
| Wait Time | 逐步放宽条件 |

匹配是多目标优化，不存在同时让公平、延迟、等待时间和组队自由全部最优的方案。

## 搜索范围扩张

```text
初始：窄 MMR + 低 RTT + 同输入设备
等待增加：逐步扩大 MMR / Region / 平台范围
超过上限：提示、机器人填充或取消
```

扩张策略应按地区、时段、模式和人口动态调整，并记录每次放宽对质量的影响。

## Match Reservation

匹配成功不应立即视为开局成功：

1. 生成唯一 Match ID；
2. 预留游戏服务器容量；
3. 为成员生成短期 Join Token；
4. 客户端接受并连接；
5. 服务器确认成员 Ready；
6. 达到开始条件后锁定名单；
7. 超时则 Backfill、重排或取消。

Reservation 和 Join 请求必须幂等，防止重复分配两个房间。

## Backfill 与中途加入

适合休闲或长局游戏，不一定适合严格竞技。要定义：

- 哪个阶段允许加入；
- 新玩家获得什么装备、分数和保护；
- 队伍/角色平衡；
- 连接和资源加载时间；
- 是否影响排名；
- 原玩家离开后的惩罚；
- 观战转参战规则。

## 断线重连

服务器保留玩家槽位和权威实体一段 Grace Period：

```text
检测断线 -> AI/冻结/继续移动
-> 客户端重新认证
-> 获取 Match Join Token
-> 恢复快照或检查点
-> 补齐事件/输入 -> 重获控制
```

重连成功不应重新发放出生、任务或结算奖励。

## 匹配与安全

- 风险账号可进入隔离池，但需防止正常队友被连带；
- 队伍成员风险不同，要定义整队策略；
- 防止反复取消匹配探测对手；
- Join Token 隐藏内部服务器信息；
- 匹配接口限流，防止队列操纵；
- 演员、Boosting 和胜负交易需要关系图与回放。

## 关键指标

- Queue Time P50/P95/P99；
- 预测胜率与实际胜率分布；
- RTT、跨区比例；
- Ready/Connect 失败率；
- 匹配取消和 Dodge；
- Backfill 成功率；
- 重连成功时间；
- Party 拆散率；
- 风险池误伤与对局质量。

## 高频追问

1. Party、Lobby、Match Session 有什么区别？
2. 为什么匹配成功后还需要 Reservation？
3. 如何权衡 MMR、公平、延迟和排队时间？
4. 断线重连如何避免重复 Spawn 和发奖？
5. Backfill 为什么不适合所有竞技模式？
6. Join Token 应绑定哪些上下文？
7. 风险账号组队时如何处理正常队友？

[上一篇：架构与拓扑](./01-architecture-and-topology-quick-notes.md) | [返回多人游戏模块](./README.md) | [下一篇：传输与协议](./03-transport-and-protocol-quick-notes.md)
