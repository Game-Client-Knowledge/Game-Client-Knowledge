# Unity 系统、性能与面试冲刺

## 系统速查

| 系统 | 面试抓手 |
|---|---|
| Animator | 参数/状态/Blend Tree、层、Root Motion、Culling；避免每帧字符串查找 |
| UGUI | RectTransform、Canvas rebuild、布局/Graphic dirty、Raycast；拆分变化频率不同 Canvas |
| ScriptableObject | Asset 数据与共享配置，不是运行时状态万能容器；注意编辑器污染与引用 |
| 序列化 | 字段规则、`SerializeReference`、版本迁移；不等于 C# 任意对象序列化 |
| Audio/VFX | voice/粒子预算、池化、可见性和平台限制 |
| Assembly Definition | 编译边界与依赖，不应形成循环 |

## Mono、IL2CPP、Jobs/Burst

Mono 便于开发/JIT（平台允许时）；IL2CPP AOT 转 C++，受反射、泛型和裁剪影响。真机需保留配置与 AOT 用例。

Jobs 将可并行数据工作调度到 worker，Burst 优化受支持的高性能 C#；Native Collections 需要显式生命周期和安全依赖。普通 Unity Object 多数仍要求主线程，Jobs 不是把任意 MonoBehaviour 搬后台。

## 性能回答

```text
目标机 Development/Release Profiler
-> CPU Timeline/GC Alloc/Job/Rendering
-> Memory Profiler / Frame Debugger / GPU Capture
-> 建基线 -> 单假设改动 -> P95/P99/峰值回归
```

常见热点：空 Update、Instantiate/Destroy、布局/Canvas 重建、字符串/闭包/LINQ 分配、同步资源加载、Shader variant、透明 Overdraw、过量物理查询。

## 30 秒核心模型

> Unity 用 Scene 组织 GameObject，GameObject 组合 Component，PlayerLoop 驱动生命周期、物理、动画和渲染。资源引用依赖 GUID/.meta，异步加载仍需管理激活尖峰和 handle 生命周期。MonoBehaviour 适合引擎适配，领域逻辑可放普通 C#；高频批量计算再考虑 Jobs/Burst。性能必须在目标机发布路径中用 Profiler 与 GPU/内存工具分账。

## 高频题

1. Unity Object fake-null 如何产生？
2. Awake/OnEnable/Start 的职责边界？
3. coroutine、Task、Job 的选择？
4. Addressables 资源为什么释放不掉或被提前释放？
5. Canvas rebuild 如何定位？
6. Mono 与 IL2CPP 的主要工程差异？
7. CPU/GPU bound 如何判断？
8. 如何设计没有数千 Update 的客户端架构？

## 项目证据

准备一个完整案例：设备/构建 → Profiler 证据 → 根因 → 方案对比 → 前后 ms/内存/包体 → 画质或开发成本 → 自动回归。

[上一章：渲染](./07-rendering-order-pipelines-and-shaders.md) | [返回模块](./README.md)
