# Unreal Engine 客户端面试复习

## 定位

面向已有 UE 项目经验的复习者。回答应围绕 UObject 运行时、Gameplay Framework 网络职责、资源依赖、线程/渲染和构建链，不停留在编辑器操作。

## 主线

```text
UObject/反射/GC -> World/Actor/Component
-> Gameplay Framework/输入/移动
-> Asset Manager/异步/World Partition
-> Replication/渲染 -> Build/Cook/Insights
```

## 章节

1. [Actor 与 Component](./01-editor-level-actor-and-components.md)
2. [UObject、反射、GC 与生命周期](./02-uobject-reflection-gc-and-lifecycle.md)
3. [Gameplay Framework](./03-gameplay-framework-and-game-loop.md)
4. [Blueprint 与 C++ 边界](./04-blueprints-and-cpp-collaboration.md)
5. [资源与大世界](./05-assets-async-loading-and-world-partition.md)
6. [输入、移动、动画与 AI](./06-input-character-animation-and-ai.md)
7. [物理、碰撞与网络](./07-physics-collision-and-networking.md)
8. [渲染与 UE5 图形](./08-rendering-materials-and-ue5-graphics.md)
9. [系统、性能、构建与面试](./09-common-systems-performance-build-and-interview.md)

## 版本边界

声明 UE 版本、平台、网络模式、是否使用 Enhanced Input、World Partition、Nanite/Lumen、GAS 和具体渲染路径。UE5.x 小版本 API 与能力边界会变化。

## 项目证据

准备一次 Unreal Insights/LLM/RenderDoc 或网络调试案例，给 trace、根因、前后指标、失败处理和防回归。

[返回总路线](../README.md)
