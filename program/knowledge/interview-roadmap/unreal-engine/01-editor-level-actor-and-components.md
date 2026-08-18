# World、Actor 与 Component 速记

## 对象关系

World 是运行世界与系统上下文；Level 是 World 中可加载/流送的 Actor 集合；Map 是持久化关卡 Asset。Actor 是可进入 World、可复制/拥有生命周期的实体容器。

组件层级：`UActorComponent` 提供能力与生命周期；`USceneComponent` 增加 Transform/附着；`UPrimitiveComponent` 增加可渲染/可碰撞几何。Actor 的 `RootComponent` 决定根 Transform，子 SceneComponent 使用相对变换。

## 创建与生命周期

- UObject 用 `NewObject`，Actor 用 World `SpawnActor`，不要普通 `new`。
- 构造函数/CDO 阶段设置默认值与默认子对象，不访问依赖运行 World 的逻辑。
- Construction Script/`OnConstruction` 可在编辑器频繁执行，不能承载不可重复副作用。
- Deferred Spawn 允许在完成构造前设置暴露参数，再 `FinishSpawning`。
- 运行时主线常涉及 PostInitializeComponents → BeginPlay → Tick → EndPlay/Destroyed。

`Destroy` 通常标记销毁并在安全时机处理；外部引用必须验证有效性，异步/Timer/Delegate 要解绑。

## Blueprint Class 与实例

Blueprint Generated Class 也有 CDO；实例由类默认值、组件模板和实例 override 构成。Native 构造、Blueprint Construction 与 BeginPlay 的职责必须分开。

## 工程边界

Actor/Component 适合世界对象与可组合能力；长期全局/世界服务优先 Subsystem。避免 Level Blueprint 承载核心玩法，也避免每个 Actor 无意义 Tick。

Content 目录与命名影响资源引用和 Cook。移动 Asset 应通过编辑器并处理 redirector，避免硬路径字符串。

## 高频追问

1. World、Level、Map 的差异？
2. 三层 Component 的能力关系？
3. 构造函数、OnConstruction、BeginPlay 分别做什么？
4. `SpawnActorDeferred` 解决什么？
5. Actor 销毁后 C++/Blueprint 引用如何失效？
6. 什么逻辑不应放 Actor Tick？

[返回模块](./README.md) | [下一章：UObject](./02-uobject-reflection-gc-and-lifecycle.md)
