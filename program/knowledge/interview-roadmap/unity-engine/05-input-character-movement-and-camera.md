# 输入、角色移动与摄像机

## 1. 先区分输入与移动

输入只表达玩家意图：

```text
摇杆向右
空格按下
鼠标移动
触屏点击
```

移动系统再结合角色状态、地面、速度和网络权威计算结果：

```text
Input
-> MoveIntent
-> Character State
-> Movement Solver
-> Position / Velocity
-> Animation
-> Camera
```

把输入读取和实际位移分开，才能支持 AI、回放、网络预测、按键重映射和自动测试。

## 2. 旧 Input Manager 与新 Input System

### 2.1 旧 Input Manager

```csharp
float horizontal = Input.GetAxisRaw("Horizontal");
bool jumpPressed = Input.GetButtonDown("Jump");
```

优点是简单、旧项目常见；缺点是配置和设备抽象能力有限。

### 2.2 新 Input System

核心概念：

- Input Action：例如 Move、Jump、Attack。
- Binding：键盘、手柄、触屏等具体绑定。
- Action Map：Gameplay、UI、Vehicle 等上下文。
- Control Scheme：设备组合。
- Callback 或轮询读取。

```text
Move Action
├── WASD
├── Left Stick
└── Virtual Joystick
```

无论使用哪套系统，都应在合适阶段把输入变成稳定的意图数据，而不是让物理、
动画、UI 各自直接问键盘“玩家到底想干什么”。

## 3. 五种常见移动方式

| 方式 | 是否由物理解算 | 适合场景 | 主要代价 |
|---|---|---|---|
| 直接改 Transform | 否 | 无碰撞表现、机关、镜头、原型 | 可能穿透，与物理不同步 |
| CharacterController | 部分碰撞处理 | 传统第一/第三人称角色 | 重力、跳跃和推力需自行设计 |
| Rigidbody | 是 | 受力、碰撞和动态交互明显的对象 | 需要固定步、插值和约束 |
| NavMeshAgent | 导航驱动 | NPC 自动寻路 | 不适合直接替代所有物理移动 |
| Animator Root Motion | 动画位移驱动 | 动作距离与动画强绑定 | 网络、碰撞与玩法控制更复杂 |

没有“最 Unity”的移动方式，只有与玩法约束匹配的方案。

## 4. 直接修改 Transform

```csharp
private void Update()
{
    Vector3 delta = moveDirection * speed * Time.deltaTime;
    transform.position += delta;
}
```

适合：

- 不参与碰撞的装饰或镜头。
- 编辑器工具和简单机关。
- 快速原型。

风险：

- 可能穿过 Collider。
- 若对象同时有非 Kinematic Rigidbody，会与物理引擎争夺位置。
- 没有自动滑动、地面和台阶处理。

一边让 Rigidbody 解算，一边每帧强改 Transform，像两个人同时抢方向盘：
最终路径很有创造力，但不一定可控。

## 5. CharacterController

CharacterController 是专门的角色碰撞控制组件，不是 Rigidbody。它提供：

- 胶囊形状。
- `Move` 碰撞移动。
- 坡度限制。
- 台阶高度。
- `isGrounded` 等状态。

它不会自动施加重力，需要脚本维护垂直速度。

### 5.1 摄像机相对移动示例

```csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class ThirdPersonMover : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpSpeed = 7f;

    private CharacterController controller;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Vector2 input = new(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 horizontal =
            cameraForward * input.y + cameraRight * input.x;

        if (controller.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }

        if (controller.isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalSpeed = jumpSpeed;
        }

        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity =
            horizontal * moveSpeed + Vector3.up * verticalSpeed;

        controller.Move(velocity * Time.deltaTime);

        if (horizontal.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(horizontal);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }
    }
}
```

### 5.2 注意事项

- `CharacterController.Move` 接收位移，不是速度，因此示例乘以 `deltaTime`。
- `isGrounded` 是上次 Move 后的接地结果，不应被当成完美地面判定。
- 复杂跳跃可能需要 SphereCast、坡面法线和 Coyote Time。
- 推动 Rigidbody 需要自己施力或编写交互规则。
- 改变 Controller 高度时要处理头顶空间。

## 6. Rigidbody 移动

Rigidbody 适合需要真实碰撞响应或受力的对象。常见方式：

### 6.1 `AddForce`

```csharp
body.AddForce(direction * acceleration, ForceMode.Acceleration);
```

适合加速、惯性和受力明显的对象。

### 6.2 设置速度

```csharp
Vector3 velocity = body.velocity;
velocity.x = desired.x;
velocity.z = desired.z;
body.velocity = velocity;
```

适合需要直接控制平面速度的角色。较新 Unity 版本可能提供
`linearVelocity` 命名，使用时以项目 Editor API 为准。

### 6.3 `MovePosition`

```csharp
private Vector3 pendingMove;

private void Update()
{
    pendingMove = ReadMoveIntent();
}

private void FixedUpdate()
{
    Vector3 next =
        body.position
        + pendingMove * speed * Time.fixedDeltaTime;

    body.MovePosition(next);
}
```

适合 Kinematic Rigidbody 或希望通过物理接口移动的对象，具体碰撞响应取决于
Rigidbody 类型和版本行为。

### 6.4 旋转约束

直立角色常冻结 X/Z 旋转，避免碰到小石头后像一块认真翻面的煎饼：

```text
Rigidbody Constraints
-> Freeze Rotation X
-> Freeze Rotation Z
```

约束是玩法选择，不是所有刚体的默认正确答案。

## 7. Rigidbody 插值与抖动

物理以固定步更新，画面以渲染帧显示。两者频率不同会出现视觉跳动。

Rigidbody Interpolation 可在渲染时使用物理状态插值：

```text
Physics State N -------- Physics State N+1
          \                /
           Rendered pose
```

常见抖动来源：

- 摄像机在 Update 跟随，而角色在 FixedUpdate 移动。
- 同时修改 Rigidbody 和 Transform。
- 固定步太低。
- 网络校正直接瞬移。
- 动画 Root Motion 与物理争夺位置。

通常让物理对象在固定步更新，启用合适插值，摄像机在 LateUpdate 跟随最终表现
位置。

## 8. NavMeshAgent

用于导航网格上的自动寻路：

```csharp
using UnityEngine.AI;

public sealed class EnemyNavigator : MonoBehaviour
{
    [SerializeField] private Transform target;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        agent.SetDestination(target.position);
    }
}
```

实际项目不应每个 NPC 每帧无条件重新寻路。可以：

- 目标移动超过阈值再更新。
- 错开请求时机。
- 低频更新远处 NPC。
- 使用局部避障与行为层决定目标。

NavMesh 解决“沿可行走区域怎么走”，不负责决定“为什么要去那里”。

## 9. Root Motion

Animator 可把动画根骨骼的位移应用到 GameObject：

```text
动画片段中的根位移
-> Animator
-> GameObject Transform / 自定义 OnAnimatorMove
```

适合：

- 翻滚、处决等动作距离与节奏强绑定。
- 高质量近战动作。

代价：

- 碰撞和网络预测更复杂。
- 需要处理障碍、斜坡和动画修正。
- 动画资产必须遵循统一根运动规范。

常见混合方案：

```text
普通跑动：代码控制速度，动画匹配表现
特定技能：Root Motion 提供位移
网络层：发送输入/状态并做权威校正
```

## 10. 摄像机跟随

一个最小 LateUpdate 跟随：

```csharp
public sealed class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref velocity,
            smoothTime
        );

        transform.LookAt(target);
    }
}
```

正式第三人称摄像机还需处理：

- 鼠标/手柄旋转。
- Pitch 限制。
- 墙体避障和镜头拉近。
- 锁定目标。
- FOV 变化和震屏。
- 输入延迟与网络表现。

避障常从目标向期望摄像机位置做 SphereCast，而不是只用一条 Ray。球形检测更
接近摄像机实际体积，减少镜头边缘穿墙。

## 11. 移动与动画参数

移动系统输出稳定参数给 Animator：

```text
Speed
VerticalSpeed
IsGrounded
MoveX / MoveY
TurnRate
```

不要让 Animator 再去读取键盘，因为角色可能由 AI、网络回放或自动导航驱动。
动画应消费角色运动状态，而不是偷偷兼职输入系统。

## 12. 移动方案选择题

### 12.1 高速物理球

选择 Rigidbody + 连续碰撞检测，因为碰撞响应和受力是核心。

### 12.2 第三人称动作角色

CharacterController 或定制 Kinematic Controller 常见；特定技能可混合 Root
Motion。是否使用 Rigidbody 取决于项目对物理交互和网络预测的要求。

### 12.3 RTS 小兵

NavMeshAgent 或自研导航/群体系统，配合动画表现。大量单位需要降低寻路和避障
更新频率。

### 12.4 电梯平台

Kinematic Rigidbody 或平台系统，必须处理站在上面的角色如何继承位移。

## 13. 本章检查

1. 输入意图为什么要与移动实现分离？
2. 直接改 Transform 和 Rigidbody 移动有何差异？
3. CharacterController 为什么需要自己实现重力？
4. Rigidbody 角色为何常在 Update 采输入、FixedUpdate 应用？
5. 物理插值解决什么问题？
6. NavMeshAgent 为什么不等于完整 AI？
7. Root Motion 的优势和网络代价是什么？
8. 第三人称摄像机为什么更适合 SphereCast 避障？

[上一章：场景、资源与异步加载](./04-scenes-assets-and-async-loading.md) |
[返回 Unity 引擎基础](./README.md) |
[下一章：物理引擎与物理材质](./06-physics-and-physics-materials.md)
