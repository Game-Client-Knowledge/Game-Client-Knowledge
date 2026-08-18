# Unity 协程、异步与时间速记

## 协程本质

Unity coroutine 是由 PlayerLoop 驱动的 `IEnumerator` 状态机，不是线程。执行到 `yield` 暂停，满足条件后在约定阶段恢复；同步死循环仍会卡主线程。

常见等待：`null` 下一帧、`WaitForFixedUpdate` 物理阶段、`WaitForEndOfFrame` 帧尾、`WaitForSeconds` 缩放时间、`WaitForSecondsRealtime` 实时时间、`AsyncOperation` 完成。

## 生命周期与分配

协程依附宿主和调度器。关闭页面/销毁对象时要明确停止、取消请求和清理 finally/资源；不能假设所有停止路径都会像正常函数返回一样执行。

高频创建 iterator、等待对象、闭包会分配，但先用 Profiler 证明。缓存等待对象只适合参数固定且语义安全的情况。

## async/await

Task 表达可组合异步结果和异常，但 continuation 所在线程取决于 SynchronizationContext/awaiter；Unity API 通常要求主线程。需要：

- `CancellationToken` 与 owner 生命周期绑定；
- 捕获并观测异常，避免 `async void`（事件入口除外）；
- owner 销毁后重新检查；
- 后台计算返回纯数据，主线程应用结果；
- 区分 Task、coroutine 和引擎 AsyncOperation 的取消能力。

## 选择

| 工具 | 适合 |
|---|---|
| coroutine | 与帧阶段紧密的短流程、动画/时间等待 |
| Task/async | I/O、可组合结果、异常传播 |
| 显式状态机 | 长期、可保存/热更/调试的流程 |
| Job/Burst | 数据并行 CPU 计算，不访问普通 Unity Object |

## 高频追问

1. coroutine 为什么不是线程？
2. `WaitForSeconds` 受哪些时间因素影响？
3. GameObject 禁用/销毁后 coroutine 如何处理？
4. Task continuation 如何安全回到主线程？
5. 取消为什么不等于底层工作立即终止？
6. 协程异常和 Task 异常分别如何观测？

[上一章：生命周期](./02-monobehaviour-lifecycle-and-playerloop.md) | [下一章：场景与资源](./04-scenes-assets-and-async-loading.md)
