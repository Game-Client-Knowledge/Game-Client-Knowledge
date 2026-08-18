# 渲染流水线面试串讲

## 30 秒回答

> CPU 从游戏世界提取渲染快照，做可见性、LOD、排序和 Pass 构建，再记录 Draw/Dispatch 命令。GPU 读取顶点与索引，经过顶点变换、图元装配和裁剪，光栅化生成片元，片元着色器采样材质并计算光照，最后通过深度、模板和混合写入 Render Target。后处理和 Tone Mapping 生成交换链图像，再由 Present 交给显示系统。定位性能时需分别检查 CPU 提交、几何、像素/带宽和同步。

## 两分钟展开

```text
1. Game World 更新：Transform、动画、粒子、相机
2. Render Extract：生成线程安全快照
3. CPU：剔除/LOD/排序/批处理/Pass/命令
4. GPU Geometry：IA -> VS -> Primitive -> Clip
5. Raster：覆盖与 varying 插值
6. Fragment：纹理、材质、光照、MRT
7. Output：Depth/Stencil/Blend
8. Post：曝光、AA、Tone Mapping、UI
9. Present：交换链、VSync/VRR、显示
```

## 场景定位：角色 + 墙 + 烟雾

- 角色/墙：不透明，优先深度写入与 Early-Z；角色可能有骨骼蒙皮。
- 烟雾：透明，排序近似、关深度写、Overdraw 高，粒子数量少也可能像素昂贵。
- CPU 慢：检查对象数、剔除、Draw/状态、Render Thread 与资源同步。
- GPU 慢：用 capture 看 Pass 时间、像素覆盖、Shader、采样、带宽和几何。
- 偶发尖峰：检查 Shader/PSO 编译、上传、readback、GC、流送与 Present 等待。

## 高频快问

| 问题 | 关键结论 |
|---|---|
| Draw Call 为什么贵？ | CPU 命令/验证/状态与线程工作；不是固定常数 |
| VBO 后为何还要 IBO？ | 复用顶点，但受 cache 与拓扑影响 |
| 透明为何难？ | 混合顺序相关且通常不写深度 |
| Early-Z 何时受限？ | discard、深度写、副作用和状态可能限制 |
| CPU/GPU bound 怎么看？ | timeline + GPU timestamp/capture + 同步实验 |
| Overdraw 怎么查？ | overdraw view、capture、分辨率/粒子对比 |
| 降 Draw 后没变快？ | 真瓶颈可能在 GPU/同步，或合批引入新成本 |

## 容易失分

- 把 Fragment 直接称为最终像素；
- 混淆 CPU Frustum Culling 与 GPU Clipping；
- 说透明物体“关闭深度测试”；通常是保留测试、关闭写入；
- 只背阶段，不说输入输出和性能；
- 未声明坐标/API 约定就死背矩阵顺序；
- 不做 Capture 就断言“Shader 太复杂”。

## 项目证据

准备一张真实 capture：说明目标设备/分辨率、最贵 Pass、瓶颈证据、改动前后 GPU/CPU ms、画质或内存代价和回归场景。

[上一章：输出与呈现](./05-output-merger-and-presentation.md) | [返回专项](./README.md)
