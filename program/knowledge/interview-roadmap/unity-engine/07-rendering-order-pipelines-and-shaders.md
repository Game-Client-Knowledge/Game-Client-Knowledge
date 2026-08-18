# Unity 渲染管线与 Shader 速记

## 对象到像素

MeshFilter/Mesh 提供几何，Renderer 提交可见实例，Material 选择 Shader/参数，Camera 与 Render Pipeline 组织 Pass。访问 `renderer.material` 可能克隆实例并破坏批处理；修改共享资源用 `sharedMaterial` 要避免污染所有对象，常用 MaterialPropertyBlock 表达每实例数据。

## 管线

- Built-in：传统固定管线扩展方式，兼容旧项目。
- URP：跨平台、移动/中端常用，可通过 Renderer Feature/Render Pass 扩展。
- HDRP：高端画质与复杂物理光照，成本和平台要求更高。
- SRP：C# 组织渲染流程，Shader 仍在 GPU；版本 API/包差异需声明。

## 排序与层级

GameObject Layer 服务剔除/光照/物理等筛选；Render Queue 决定材质大类顺序；Sorting Layer/Order 主要服务 2D/透明排序；Canvas 模式又有独立规则。不要把这些“Layer”混为一谈。

不透明通常前向后、写深度；透明通常后向前、关深度写。UI Overlay 不经过普通 Camera 深度，World/Camera Space Canvas 则不同。

## Shader 与性能

ShaderLab 描述 SubShader/Pass/Tag/状态，HLSL 编写程序。变体来自 keyword、管线、光照/阴影特性；组合爆炸会抬高构建、包体、加载和运行时卡顿。

优化分 CPU 提交（Batching、SRP Batcher、GPU Instancing、状态）和 GPU（顶点、像素、Overdraw、采样、带宽）。先用 Profiler、Frame Debugger 和 GPU capture 定位。

## 高频追问

1. `material` 与 `sharedMaterial` 的差异及泄漏风险？
2. SRP Batcher 与 GPU Instancing 分别优化什么？
3. Layer、Render Queue、Sorting Layer 如何协作？
4. URP Renderer Feature 在管线哪里插入？
5. Shader Variant 为什么会爆炸，如何收集/剥离？
6. 透明特效为何常是移动端 GPU 热点？
7. Frame Debugger 与 RenderDoc 各能证明什么？

[上一章：物理](./06-physics-and-physics-materials.md) | [下一章：系统与性能](./08-common-systems-performance-and-interview.md)
