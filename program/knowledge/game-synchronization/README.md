# 状态同步与帧同步

## 1. 文档范围

本专题讨论游戏联机中两种核心同步架构：**状态同步（State Synchronization）**
与**帧同步（Frame Synchronization / Deterministic Lockstep）**。

内容覆盖基本概念与对比、反作弊机制设计、权威与架构边界，以及同步状态的
具体设计，适合作为游戏客户端网络同步方向的知识入门与复习材料。

## 2. 一句话理解

> 帧同步 = 同步操作，各自演算；状态同步 = 同步结果，服务器演算。

帧同步把"计算"交给每台客户端，网络上传的是**输入**；
状态同步把"计算"交给服务器，网络上传的是**状态**。

## 3. 文档导航

建议按以下顺序阅读：

1. [基本概念与对比](./01-concepts-and-comparison.md)
   理解两种同步的工作原理、关键概念、优缺点与典型应用。
2. [反作弊机制设计](./02-anti-cheat-design.md)
   基于"客户端永远不可信"原则，了解两套架构各自的防线。
3. [闭环条件与架构边界](./03-authority-and-boundary.md)
   理解帧同步的确定性闭环条件，以及"谁算逻辑"与"网络传什么"两个正交维度。
4. [同步状态设计](./04-synced-state-design.md)
   深入两种架构中指令流与世界状态的具体设计。

## 4. 相关模块

- [多人游戏面试复习](../interview-roadmap/multiplayer-game/README.md)
  面试视角的快速复习，其中包含
  [游戏同步模型](../interview-roadmap/multiplayer-game/game-synchronization/README.md)
  与
  [游戏防作弊](../interview-roadmap/multiplayer-game/game-anti-cheat/README.md)
  两个子模块，可与本专题交叉阅读。
