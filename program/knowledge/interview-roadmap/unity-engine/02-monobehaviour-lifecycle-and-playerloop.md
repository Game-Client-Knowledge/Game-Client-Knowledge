# MonoBehaviour 生命周期与 PlayerLoop 速记

## 初始化与销毁

常见主线：`Awake`（实例初始化）→ `OnEnable`（每次启用订阅）→ `Start`（首次启用前）→ 更新阶段 → `OnDisable`（解绑/取消）→ `OnDestroy`（最终清理）。不同对象之间的具体顺序不要靠 Hierarchy 猜测。

- `activeSelf` 是自身标记，`activeInHierarchy` 才表示层级中实际激活。
- Component `enabled` 与 GameObject active 共同决定多数回调。
- `OnDisable` 可能多次发生，清理应幂等；`OnDestroy` 不是唯一解绑时机。

## 每帧阶段

| 回调 | 用途 | 常见错误 |
|---|---|---|
| `FixedUpdate` | 固定步物理输入/力 | 假设每渲染帧恰好一次 |
| `Update` | 输入、普通逻辑 | 逐对象空 Update 与每帧分配 |
| `LateUpdate` | 相机/跟随等后置逻辑 | 用脚本顺序隐式耦合 |

输入通常在 Update 采样，物理命令进入 FixedUpdate/物理步，渲染用 Rigidbody interpolation 平滑。直接混写 Transform 与 Rigidbody 会争夺状态。

## PlayerLoop 与执行顺序

PlayerLoop 包含更多阶段，协程、动画、物理、渲染回调的位置各不相同。Script Execution Order/`DefaultExecutionOrder` 可解决少量明确依赖，但大范围依赖应通过显式系统调度、事件或数据流表达。

大量 MonoBehaviour Update 会带来调度和 native-managed 边界成本；集中 System 只更新活跃对象更可控。

## 编辑器变量

Enter Play Mode 可关闭 Domain/Scene Reload，静态字段和事件可能残留。编辑器与 Player 的初始化、脚本重载和性能不同；生命周期测试需覆盖真实构建。

## 高频追问

1. `Awake`、`OnEnable`、`Start` 如何分工？
2. 禁用 Component 与禁用 GameObject 的差异？
3. FixedUpdate 为什么可能一帧执行多次或零次？
4. 相机抖动如何从更新阶段和插值排查？
5. 如何减少数千 MonoBehaviour Update？
6. 关闭 Domain Reload 后静态单例为什么污染下一次 Play？

[上一章：对象模型](./01-editor-scene-gameobject-and-components.md) | [下一章：协程与异步](./03-coroutines-async-and-time.md)
