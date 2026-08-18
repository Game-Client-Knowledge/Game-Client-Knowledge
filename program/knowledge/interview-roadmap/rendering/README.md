# 图形渲染面试复习

## 主线

```text
场景/相机 -> CPU 可见性、排序、Pass、Draw
-> 顶点/图元 -> 光栅化/片元
-> 深度/模板/混合 -> 后处理 -> Present
```

复习重点不是背 API，而是说明每阶段的输入、输出、坐标空间、可编程性、瓶颈和调试工具。

## 章节

1. [基本概念](./01-rendering-fundamentals.md)：Mesh、Material、Shader、Buffer、Draw Call。
2. [坐标与 CPU 准备](./02-coordinate-spaces-and-cpu-preparation.md)：变换、剔除、排序、命令提交。
3. [GPU 几何阶段](./03-gpu-geometry-stage.md)：输入装配、VS、图元、裁剪。
4. [光栅与片元](./04-rasterization-and-fragment-stage.md)：覆盖、插值、纹理、光照、Overdraw。
5. [输出与呈现](./05-output-merger-and-presentation.md)：深度、模板、混合、AA、交换链。
6. [完整串讲](./06-pipeline-walkthrough-and-interview.md)：30 秒/2 分钟回答与定位方法。

## 岗位深度

- 玩法客户端：能串完整流水线，解释透明、Draw Call、Overdraw 与 CPU/GPU bound。
- 引擎客户端：补命令缓冲、Render Graph、资源同步、批处理和多线程提交。
- 渲染岗位：补 BRDF、阴影、GI、现代 GPU、平台带宽与 Capture 实证。

## 自检

能否从“一个带透明烟雾的角色为什么掉帧”一路追到 CPU 提交、几何、片元、混合和 Present，并说明用 RenderDoc/PIX/NSight/Xcode/引擎 Profiler 中哪个证据验证。

[返回总路线](../README.md)
