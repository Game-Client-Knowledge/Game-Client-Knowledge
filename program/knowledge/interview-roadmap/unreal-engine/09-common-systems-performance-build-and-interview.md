# UE 系统、性能、构建与面试冲刺

## 常用系统

| 系统 | 面试抓手 |
|---|---|
| UMG/Slate/CommonUI | Widget 生命周期、Invalidation/Retainer、输入路由、平台导航 |
| Gameplay Tags/GAS | 标签语义；ASC、Ability、Effect、Attribute、Prediction 与复制 |
| DataAsset/DataTable/Config | 资产对象、表格数据、ini 配置的版本/引用/热更边界 |
| SaveGame | 版本化 schema、异步 IO、平台用户、校验；不保存运行时裸引用 |
| Audio/MetaSounds | voice、流送、并发、混音与平台预算 |
| Sequencer | 轨道驱动与 Gameplay 状态协调，跳转/联机/结束清理 |
| Online Services | 身份、Session/Lobby、断线、跨平台与异步生命周期 |

## Module、Plugin 与构建链

Module 是编译/加载边界，`Build.cs` 声明依赖；Plugin 可封装多个 Module、内容和配置。Public/Private 依赖影响编译传播，循环依赖需拆接口。

- UHT：反射代码生成；
- UBT：构建图与编译；
- Build：编译代码；Cook：平台化内容；Stage：整理部署目录；Package：生成交付物。

开发编辑器可用而 Shipping/Cook 失败，常见于条件宏、裁剪/未引用 Asset、平台 RHI、配置和插件阶段差异。

## 性能与稳定性

```text
stat unit/taskgraph/rhi -> Unreal Insights 时间线
-> LLM/memreport/Reference Viewer
-> GPU Visualizer/RenderDoc
-> 固定设备与场景建立 P95/P99 基线
```

CPU 热点常见 Tick、Blueprint 高频调用、Task 拖尾、GC、同步加载；内存关注 UObject/资源依赖、纹理/RenderTarget、池与流送；GPU 关注阴影、Lumen、透明、材质、分辨率和带宽。

日志与崩溃需包含符号、Build/CL、平台、Map、网络角色和关键 breadcrumb。Automation/Functional/Gauntlet 等测试覆盖纯逻辑、地图、联机、Cook/包和性能场景。

## 30 秒核心回答

> UE 以 UObject 反射/GC 为运行时底座，World 中 Actor 组合 Component，Gameplay Framework 按服务器、连接、玩家和 Pawn 生命周期分配职责。资源通过硬/软引用与 Asset Manager 管理，网络以服务器权威 Replication/RPC 同步，渲染跨 Game/Render/RHI 线程。工程上要用 Insights、LLM 和 GPU Capture 分账，并把 Build/Cook/Stage/Package、平台和版本纳入验证。

## 高频题

1. UMG Widget 频繁重建/Prepass 如何定位？
2. GAS prediction 与服务器权威如何协作？
3. DataAsset、DataTable、Config 如何选择？
4. Public Module 依赖为何拖慢增量编译？
5. Cook 丢资源怎样从引用和 Asset Manager 规则排查？
6. Insights 中 Game Thread 等待 Task 如何定位根因？
7. 内存上涨如何区分 UObject 可达、资源缓存与 allocator 保留？
8. Shipping 崩溃如何保证符号和版本可追踪？

## 项目证据

准备一个 UE 专项案例：版本/平台/网络模式 → Insights 或 Capture 证据 → 方案对比 → 前后 Game/GPU ms、内存或包体 → 副作用 → 自动测试/监控。

[上一章：渲染](./08-rendering-materials-and-ue5-graphics.md) | [返回模块](./README.md)
