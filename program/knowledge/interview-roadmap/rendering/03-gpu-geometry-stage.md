# GPU 几何阶段速记

## 数据流

```text
Vertex/Index Buffer + Input Layout
-> Vertex Shader -> 可选 Tessellation/Geometry
-> Primitive Assembly -> Culling/Clipping
-> Perspective Divide -> Viewport
```

索引复用顶点，减少存储与 VS 工作，但收益受 vertex cache 和拓扑顺序影响。VBO 存属性，IBO 存索引；VAO 是 OpenGL 常见状态封装，不是所有 API 的通用对象。

## Vertex Shader

每个顶点独立执行，输出至少包含裁剪空间位置，还可传法线、UV、颜色等 varying。它通常不能观察完整图元或任意改变拓扑；新增几何依赖其他阶段/方案。

常见工作：MVP 变换、骨骼蒙皮、morph、顶点动画、生成后续阶段属性。瓶颈可能来自顶点数量、属性带宽、蒙皮骨骼访问、复杂位移和低复用。

## 图元与裁剪

- Primitive Assembly 按拓扑组装点/线/三角形。
- Back-face Culling 依据屏幕绕序，负缩放和坐标手性会翻转判断。
- Frustum Culling 常在 CPU 对对象做粗剔除；GPU clipping 对跨裁剪平面的图元处理，两者不是同一阶段。
- 裁剪后做透视除法与 viewport 变换，再进入光栅化。

Tessellation 可按距离细分但增加几何成本；Geometry Shader 灵活但在许多 GPU 上吞吐不理想。现代方案可能使用 compute/mesh shader，但平台支持与数据流不同。

## 高频追问

1. 为什么使用索引仍可能重复执行 VS？
2. Vertex Shader 能否新增三角形？
3. 背面剔除与视锥剔除的差异？
4. 负缩放为什么可能让模型消失？
5. 蒙皮瓶颈如何判断在 CPU 还是 GPU？
6. Tessellation/Geometry/Mesh Shader 如何取舍？

[上一章：坐标与 CPU](./02-coordinate-spaces-and-cpu-preparation.md) | [下一章：光栅与片元](./04-rasterization-and-fragment-stage.md)
