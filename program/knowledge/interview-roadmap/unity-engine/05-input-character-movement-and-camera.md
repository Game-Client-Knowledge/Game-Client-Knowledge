# Unity 输入、移动与相机速记

## 输入与运动分层

输入层把设备信号映射为 Action/意图，运动层消费意图并决定碰撞、速度和权威。新 Input System 支持 Action Map、binding、device 和 rebinding；事件回调同样需要生命周期解绑。

## 移动方案

| 方案 | 适合 | 注意 |
|---|---|---|
| Transform | 非物理表现/传送 | 绕过物理连续性 |
| CharacterController | 可控角色运动 | 手动重力、地面和推挤规则 |
| Rigidbody | 真实物理交互 | 固定步、力/速度、插值、约束 |
| NavMeshAgent | 导航代理 | 路径异步、避障、与动画/物理协调 |
| Root Motion | 动画驱动位移 | 网络权威、碰撞与纠错复杂 |

Rigidbody 在 FixedUpdate 接收物理操作；Update 采样输入。插值只平滑显示，不提高仿真频率。高速物体用合适 collision detection/sweep，不通过增加无上限固定步解决。

## 坐标与相机

相机相对移动通常将输入投影到水平面并归一化，避免斜向更快；注意本地/世界空间和斜坡法线。

相机跟随放 LateUpdate 或 Cinemachine 对应阶段，消费角色最终表现位置；抖动首先检查物理步、插值和更新顺序。遮挡、碰撞、阻尼和瞬移重置都要定义。

## 动画协作

代码/物理产生权威速度与 grounded，Animator 消费参数；动画 Notify 触发表现，关键伤害判定由游戏状态驱动。Root Motion 开启时明确谁写 Transform，避免多写者。

## 高频追问

1. 输入采样为什么常在 Update，而物理在 FixedUpdate？
2. AddForce、设置 velocity、MovePosition 如何选择？
3. CharacterController 为什么不等同 Rigidbody？
4. 物理角色和相机为何会抖？
5. NavMeshAgent 与 Root Motion 如何同步？
6. 联机角色的本地预测与表现插值如何分层？

[上一章：场景与资源](./04-scenes-assets-and-async-loading.md) | [下一章：物理](./06-physics-and-physics-materials.md)
