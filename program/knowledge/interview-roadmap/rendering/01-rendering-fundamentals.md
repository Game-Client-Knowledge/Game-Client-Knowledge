# 渲染基本概念速记

## 一句话模型

渲染把场景状态转换为屏幕图像：CPU 决定画什么与以何种状态提交，GPU 对大量顶点/片元执行并行计算，输出写入 Render Target 后呈现。

## 核心对象

| 对象 | 作用 | 高频边界 |
|---|---|---|
| Mesh | 顶点属性、索引和拓扑 | 布局、精度、LOD、顶点复用 |
| Shader | GPU 阶段程序 | 变体、指令、采样、平台编译 |
| Material | Shader + 参数/纹理/状态 | 状态切换、实例化、批处理 |
| Texture/Sampler | 图像数据与采样规则 | 格式、mip、过滤、寻址、带宽 |
| Camera | View/Projection 与可见范围 | 坐标约定、near/far、裁剪 |
| Buffer | 顶点/索引/常量/结构化数据 | 更新频率、对齐、上传与同步 |
| Render Target | 颜色/深度等阶段输出 | 格式、分辨率、带宽、生命周期 |

Draw Call 是 CPU 记录/提交一批使用指定管线状态与资源的绘制命令，不等于“三角形数量”。成本可来自 API/驱动验证、状态切换、命令构建和 Render Thread 工作；降低 Draw Call 可能牺牲剔除粒度、内存或动态性。

## CPU/GPU 分工

CPU：场景更新、可见性、LOD、排序、批处理、Pass 和命令。GPU：顶点/片元/计算、纹理采样、深度混合与后处理。两者通常流水并行，flush/readback/同步创建会打断并行。

帧时间看毫秒而非只看 FPS：60 FPS 预算约 16.67 ms，120 FPS 约 8.33 ms。平均达标但 P99 超预算仍会卡顿。

## 高频追问

1. Material 与 Shader 的区别？
2. Draw Call 为什么贵，什么时候不是主要瓶颈？
3. 顶点数少为什么仍可能 GPU 慢？
4. Render Target 与普通 Texture 有什么关系？
5. 如何判断 CPU bound、GPU bound 或同步等待？

[返回专项](./README.md) | [下一章：坐标与 CPU 准备](./02-coordinate-spaces-and-cpu-preparation.md)
