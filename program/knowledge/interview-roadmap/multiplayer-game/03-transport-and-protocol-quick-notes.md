# 传输、可靠性与协议速记

## 先选语义，再选协议

| 数据 | 需要的语义 |
|---|---|
| 登录、结算、背包 | 可靠、幂等、可审计 |
| Spawn/Despawn | 可靠、有生命周期顺序 |
| 高频位置/视角 | 新状态覆盖旧状态，旧包通常无需重传 |
| 输入帧 | 低延迟，可冗余最近输入 |
| 聊天 | 可靠、限流、内容审核 |
| 语音 | 低延迟，允许少量丢失 |

“全部可靠有序”会让一个丢包阻塞所有后续数据；“全部不可靠”又会丢失关键业务事实。

## TCP、UDP、QUIC 与 WebSocket

| 方案 | 特点 | 常见用途 |
|---|---|---|
| TCP | 可靠有序字节流，存在队头阻塞 | 登录、聊天、低频业务 |
| UDP | 报文、无交付保证，应用自定义语义 | 实时输入、状态、语音 |
| QUIC | 基于 UDP，内建安全和多 Stream | 业务连接、下载、部分实时场景 |
| WebSocket | 浏览器友好，通常基于 TCP | Web 游戏、后台和低频实时 |

同步模型与传输正交。状态同步、帧同步都可以使用 UDP 上的选择性可靠层。

## 可靠通道设计

可按业务拆 Channel：

```text
Channel 0：可靠有序，生命周期/结算
Channel 1：不可靠有序，位置快照
Channel 2：半可靠，输入帧/短期事件
Channel 3：可靠无序，大块资源元数据
```

不同 Channel 避免互相队头阻塞。实现需要 Sequence、ACK、ACK Bits、重传、超时和拥塞控制。

## 包头与消息头

示意：

```text
Packet {
  protocolVersion
  connectionId
  packetSequence
  ack / ackBits
  flags
  messages[]
}

Message {
  channel
  type
  simulationTick?
  payloadLength
  payload
}
```

Packet Sequence、Simulation Tick 和业务 Request ID 解决不同问题，不应混用。

## 序列化

要求：

- 明确字节序、整数宽度和浮点规则；
- 长度前置并设置上限；
- 枚举和字段范围验证；
- 协议版本和能力协商；
- 可选字段有 Presence/Mask；
- 不直接序列化 C++ 对象内存；
- 新旧版本对未知字段有稳定策略；
- Fuzz 测试解析器。

协议 Schema 应与服务端、客户端和分析工具同源生成，减少手工漂移。

## MTU 与分片

实时 UDP 包应低于保守路径 MTU。网络层分片的任一片丢失会使整包失效。

大数据使用应用层分块：

- Chunk ID、Index、Count；
- 总大小和 Hash；
- 超时与重传；
- 流量优先级；
- 限制并发重组和内存；
- 不能让重连快照阻塞实时输入。

## NAT、Relay 与连接建立

P2P 可能需要：

- 公网地址发现；
- NAT 穿透；
- 对称 NAT 失败后的 Relay；
- 路径切换；
- 隐藏玩家 IP；
- 会话密钥和身份验证。

穿透成功不代表连接可信。双方仍需认证、限流和协议校验。

## 心跳、超时与重连

心跳用于：

- 估计 RTT 和 Jitter；
- 检测连接空闲；
- 刷新 NAT 映射；
- 估计服务器时间；
- 判断重连时机。

超时不能只用一个固定值。移动网络和后台切换可能暂时失联，应区分：

```text
Suspected -> Disconnected -> Reconnecting -> Expired
```

游戏模拟卡死但网络线程仍发心跳时，连接“活着”不代表房间健康。

## 拥塞与背压

- 控制发送速率，不以为 UDP 可以无限发；
- 记录发送队列时延，而不只看字节数；
- 丢包升高时降低低优先级状态频率；
- 关键输入优先于遥测和大快照；
- 服务端按连接设置字节与消息预算；
- 慢客户端不能无限积压可靠消息；
- 超预算时合并、丢弃或断开。

## 安全

- 使用成熟的认证加密方案；
- Session/Match/Connection 绑定；
- Nonce、Sequence 与重放窗口；
- 关键业务请求幂等；
- 解析前验证大小和复杂度；
- 不在客户端保存永久服务端秘密；
- 日志不记录原始令牌和敏感载荷。

玩法合法性仍由服务器校验，详见[游戏防作弊](./game-anti-cheat/README.md)。

## 高频追问

1. TCP 粘包的本质是什么？
2. 为什么位置快照通常不可靠重传？
3. 可靠有序 Channel 为什么可能影响技能响应？
4. Packet Sequence 与 Tick 的区别？
5. 大快照为何使用应用层分块？
6. UDP 为什么仍需要拥塞控制？
7. P2P 穿透和 Relay 分别解决什么？

[上一篇：会话与匹配](./02-session-lobby-and-matchmaking-quick-notes.md) | [返回多人游戏模块](./README.md) | [下一篇：时间与延迟](./04-time-latency-and-compensation-quick-notes.md)
