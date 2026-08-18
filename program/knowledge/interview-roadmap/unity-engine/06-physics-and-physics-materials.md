# Unity 物理系统速记

## 主线

```text
Collider + Layer/Mask
-> Broad Phase -> Narrow Phase
-> Solver(刚体/接触/Joint) -> 状态写回 -> Callback
```

Static Collider 不应频繁移动；Dynamic Rigidbody 由物理控制；Kinematic Rigidbody 由脚本/动画驱动但可参与查询与接触。复杂物体优先 Compound Primitive，动态非凸 MeshCollider 约束更多且昂贵。

## 时间与回调

物理使用 Fixed Timestep。单帧卡顿可能追多个物理步；步长更小提高稳定/精度但增加成本。Collision 参与响应，Trigger 只报告重叠；是否收到事件还受 Rigidbody、Layer Matrix 和 Query 配置影响。

回调中销毁/增删对象可能重入，复杂结构变更延迟处理。物理查询使用 LayerMask、最大数量和 NonAlloc/批处理方案（以版本与测量为准）。

## 材质、CCD 与 Joint

Physics Material 控制摩擦、弹性及 combine 规则，不决定质量。运行时访问材质时注意是否实例化/共享。

Discrete 可能让高速小物体穿透；Continuous 系列或 sweep 提高可靠性但更贵。Joint 是 Solver 约束，迭代不足、质量比极端或时间步过大可能抖动。

## 性能

控制活跃刚体、碰撞层、形状复杂度、查询范围、solver iteration 和固定步；使用 Physics Debug/Profiler 确认 broad phase、solver 或 callback 成本。不要用 MeshCollider 覆盖所有场景，也不要每帧无筛选全局 Overlap。

## 高频追问

1. Static/Kinematic/Dynamic 的控制权差异？
2. Trigger 为什么没有回调？
3. Fixed Timestep 如何影响稳定性与性能？
4. Raycast、SphereCast、Overlap 分别适合什么？
5. CCD 为什么不能全局开启？
6. 摩擦/弹性 combine 如何影响最终结果？
7. Transform 直改 Rigidbody 对象会怎样？

[上一章：输入与移动](./05-input-character-movement-and-camera.md) | [下一章：渲染](./07-rendering-order-pipelines-and-shaders.md)
