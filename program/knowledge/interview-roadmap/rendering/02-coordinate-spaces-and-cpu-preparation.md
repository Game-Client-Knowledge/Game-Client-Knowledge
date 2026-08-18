# 坐标空间与 CPU 准备速记

## 变换链

```text
Model --M--> World --V--> View --P--> Clip
-> perspective divide -> NDC -> viewport -> Screen
```

- Clip Space 在透视除法前，裁剪可避免跨近平面的错误；NDC 是除以 w 后的标准空间。
- 矩阵乘法顺序、行/列向量、坐标手性、深度范围因 API/引擎而异，回答前声明约定。
- 透视投影近大远小；正交投影不随深度缩放。
- near plane 过小、far/near 比过大将恶化深度精度；Reversed-Z 可改善远距离精度。
- 法线遇非均匀缩放使用模型矩阵的逆转置（通常取 3x3），并重新归一化。

## CPU 应用阶段

1. 从游戏世界提取稳定渲染快照，避免 Render Thread 读写游戏对象；
2. 层级/包围体/遮挡剔除，选择 LOD；
3. 按 Pass、Pipeline/Material、深度等生成列表；
4. 合批、实例化并绑定资源；
5. 记录命令缓冲，提交 GPU 队列；
6. 通过 fence 管理在途帧和资源复用。

剔除越细 CPU 越贵，合批越大剔除粒度可能越差。排序透明物体服务正确性，排序不透明物体常服务 Early-Z 和状态局部性，目标不同。

## Draw 与同步

现代 API 将昂贵工作前移到 Pipeline State，并允许多线程记录命令；仍需管理 descriptor、资源状态和生命周期。CPU 等 GPU 常见于 readback、buffer 复用过早、Present 限流和显式 flush。

## 高频追问

1. 为什么透视除法在裁剪之后？
2. View 矩阵为何可理解为 Camera Transform 的逆？
3. 法线为什么不能总乘 Model 矩阵？
4. Frustum/Occlusion Culling 分别解决什么？
5. 合批与剔除为什么可能冲突？
6. CPU/GPU 在途多帧带来什么延迟与资源管理问题？

[上一章：基本概念](./01-rendering-fundamentals.md) | [下一章：GPU 几何阶段](./03-gpu-geometry-stage.md)
