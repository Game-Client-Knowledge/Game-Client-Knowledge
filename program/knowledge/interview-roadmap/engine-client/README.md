# 引擎与客户端核心复习

## 目标

把基础知识放回一帧：数据何时产生、在哪个线程更新、由谁消费，什么时候同步和销毁。

```text
输入/网络 -> 游戏逻辑 -> 物理 -> 动画
-> 渲染数据提取 -> Render/RHI -> Present
```

实际引擎会跨 Game、Worker、Render、RHI 线程流水执行；回答重点是依赖与同步，而不是背固定顺序。

## 内容

1. [引擎架构与运行时](./01-engine-architecture-and-runtime.md)：时间、对象、模块、事件、资源与场景。
2. [物理、动画与平台](./02-physics-animation-network-and-platforms.md)：物理动画协作和平台边界。
3. [多人游戏](../multiplayer-game/README.md)：拓扑、会话匹配、传输、复制、同步、防作弊和观测。
4. 引擎专项：[Unity](../unity-engine/README.md) / [Unreal](../unreal-engine/README.md)。
5. 深入专题：[ECS](../../ecs/README.md) / [图形渲染](../rendering/README.md)。

## 自检框架

对任意系统都能画出：

```text
输入 -> 更新时机/线程 -> 状态所有权
-> 下游消费者 -> 同步点 -> 销毁/回收
```

能调用组件 API 不等于理解运行模型；项目追问通常从线程、资源失效或峰值性能继续深入。

[返回总路线](../README.md)
