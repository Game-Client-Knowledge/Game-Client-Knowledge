# Unity 客户端面试复习

## 定位

面向有 Unity 项目经验的复习者。目标是把 API 使用还原为对象、PlayerLoop、资源、物理和渲染运行模型。

## 主线

```text
Scene/Prefab -> GameObject + Component
-> PlayerLoop 生命周期/输入/物理/逻辑
-> 资源异步与场景切换
-> Renderer/SRP -> Profiler/构建
```

## 章节

1. [对象模型](./01-editor-scene-gameobject-and-components.md)
2. [生命周期与 PlayerLoop](./02-monobehaviour-lifecycle-and-playerloop.md)
3. [协程、异步与时间](./03-coroutines-async-and-time.md)
4. [场景、资源与异步加载](./04-scenes-assets-and-async-loading.md)
5. [输入、移动与相机](./05-input-character-movement-and-camera.md)
6. [物理](./06-physics-and-physics-materials.md)
7. [渲染与 Shader](./07-rendering-order-pipelines-and-shaders.md)
8. [常用系统、性能与面试](./08-common-systems-performance-and-interview.md)

## 版本边界

回答时声明 Unity LTS、渲染管线、输入系统、Mono/IL2CPP、Addressables 和目标平台版本。不同版本的 Enter Play Mode、物理 API、包和 SRP 行为可能不同。

## 项目证据

准备一次 Profiler/Memory Profiler/Frame Debugger 或 RenderDoc 的完整案例，给目标设备、发布构建、基线、根因、结果与副作用。

[返回总路线](../README.md)
