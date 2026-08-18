# Gameplay Framework 与主循环速记

## 职责与网络位置

| 类型 | 核心职责 | 网络边界 |
|---|---|---|
| GameInstance | 进程/旅行间持久服务入口 | 每个进程本地存在 |
| GameMode | 规则、加入/出生/胜负 | 仅服务器 |
| GameState | 全局可复制比赛状态 | 服务器权威，客户端副本 |
| PlayerController | 玩家连接、输入与控制 | 服务端 + owning client |
| PlayerState | 可复制玩家公共状态 | 各端可见（按 relevancy） |
| Pawn/Character | 被控制世界实体 | Character 自带移动能力 |
| Controller | 控制意图，Possess Pawn | AI/Player 分支 |

把数据放错类型会造成旅行丢失、客户端取不到 GameMode、复制范围错误或 Pawn 重生后状态丢失。

## Possess 与出生

玩家加入通常由 GameMode 选择 PlayerStart、Spawn Pawn、Controller Possess，并把可复制状态放在 GameState/PlayerState。Pawn 是可替换载体，长期玩家数据不应全部挂在 Pawn。

无缝旅行、重连、观战和重生会继续追问状态迁移与 ownership。

## Subsystem 与 LocalPlayer

Engine/GameInstance/World/LocalPlayer Subsystem 提供对应生命周期的服务边界。选择依据是数据需要存活多久、属于哪个世界/本地玩家。不要把所有管理器塞进 GameInstance。

LocalPlayer 表示本地玩家上下文；分屏时一个进程可有多个 LocalPlayer/PlayerController。

## 更新与线程

Gameplay 主要在 Game Thread；Render/RHI/Task Graph 并行处理其他工作。Tick、Timer、事件、Async Task 的选择：持续逐帧才 Tick，低频用 Timer，事实通知用事件，CPU 数据工作用任务并在主线程提交 UObject 结果。

一次输入到画面：Enhanced Input → PlayerController/Pawn → CharacterMovement/Gameplay → 动画 → Render state 提取 → Render/RHI → Present。

## 高频追问

1. GameMode 与 GameState 为何分开？
2. PlayerController 与 PlayerState 分别保存什么？
3. Pawn 重生时哪些状态应保留？
4. GameInstance 万能化有什么问题？
5. 如何选择不同 Subsystem 生命周期？
6. 分屏下 LocalPlayer 如何影响 UI/输入？
7. Tick、Timer、事件和 Task 如何选择？

[上一章：UObject](./02-uobject-reflection-gc-and-lifecycle.md) | [下一章：Blueprint/C++](./04-blueprints-and-cpp-collaboration.md)
