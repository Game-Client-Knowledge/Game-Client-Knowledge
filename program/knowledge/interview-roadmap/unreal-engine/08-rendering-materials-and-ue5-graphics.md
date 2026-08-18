# UE 渲染、材质与 UE5 图形速记

## 线程与管线

Game Thread 更新 UObject/场景，Render Thread 消费渲染代理并构建命令，RHI Thread/队列接近图形 API。跨线程通过 Scene Proxy/命令与资源生命周期同步；强制 flush 和同步创建会破坏流水。

桌面 Deferred 常见主线：Depth/Prepass → Base Pass/GBuffer → Lighting → Translucency → Post/TSR → UI。Forward 路径减少 GBuffer、适合 MSAA/VR 等场景，但灯光/材质和带宽取舍不同。

## Mesh 与材质

Static/Skeletal Mesh Component 产生渲染代理；ISM/HISM 通过实例化降低提交，代价是单实例差异和剔除粒度。

Material 定义 Domain、Blend Mode、Shading Model 和图；Material Instance 复用父材质 Shader。Scalar/Vector/Texture 参数通常不产生新 permutation，Static Switch/Keyword 会产生编译变体。

Material Parameter Collection 是全局参数；Per Instance Custom Data 为实例数据。错误选择会造成全局污染、Draw 拆分或 permutation 爆炸。

## UE5 能力

| 能力 | 核心 | 主要边界 |
|---|---|---|
| Nanite | 虚拟化微多边形与集群剔除 | 材质/变形/平台特性与像素成本仍存在 |
| Lumen | 动态 GI/反射多路径 | 质量、场景更新、硬件/软件路径成本 |
| VSM | 虚拟化高分辨率阴影页 | 页缓存失效、动态物体与内存 |
| TSR | 时域超分辨率 | 运动矢量、历史、ghosting/锐化 |
| RDG | Render Dependency Graph | 资源生命周期、Pass 依赖与别名 |
| Niagara | 模块化 VFX，CPU/GPU 模拟 | 粒子/Overdraw、数据接口与预算 |

这些能力解决不同瓶颈，开启 Nanite/Lumen 不等于自动优化。先用 `stat unit/gpu`、GPU Visualizer、Unreal Insights、RenderDoc/平台工具定位。

## 高频追问

1. Game/Render/RHI Thread 如何交换数据？
2. Forward 与 Deferred 如何取舍？
3. Material Instance 哪些参数会重新编译？
4. ISM/HISM 为什么可能降低剔除灵活性？
5. Nanite 解决几何后为何仍可能像素瓶颈？
6. Lumen/VSM 缓存失效如何形成尖峰？
7. RDG 如何管理临时资源和 Pass 依赖？
8. 透明 Niagara 如何排查 Overdraw？

[上一章：物理与网络](./07-physics-collision-and-networking.md) | [下一章：工程与构建](./09-common-systems-performance-build-and-interview.md)
