# 协程、异步与时间

## 1. 为什么需要跨帧流程

普通方法会在当前调用中一直执行到返回：

```csharp
private void FadeImmediately()
{
    for (float alpha = 1f; alpha >= 0f; alpha -= 0.1f)
    {
        SetAlpha(alpha);
    }
}
```

这段循环通常在同一帧完成，玩家只看到最终结果。若希望淡出持续一秒，需要：

```text
改一点透明度
-> 把控制权还给 Unity
-> 下一帧从原位置继续
-> 重复直到完成
```

协程就是 Unity 提供的跨帧流程表达方式之一。

## 2. 协程的本质

Unity 协程通常是返回 `IEnumerator` 的 C# 迭代器：

```csharp
private IEnumerator FadeRoutine(float duration)
{
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        SetAlpha(1f - elapsed / duration);
        yield return null;
    }

    SetAlpha(0f);
}
```

启动：

```csharp
Coroutine runningFade = StartCoroutine(FadeRoutine(1f));
```

C# 编译器会把包含 `yield` 的方法转换为状态机对象，保存：

- 当前执行位置。
- 局部变量。
- 参数。
- `this` 引用。
- 当前等待对象。

```text
MoveNext()
-> 执行到 yield
-> 保存状态并返回
-> Unity 根据 yield 内容等待
-> 之后再次调用 MoveNext()
```

协程像把书签夹进方法里，不是雇了一个新线程替你读书。

## 3. 协程不是线程

协程中的普通代码通常仍在 Unity 主线程执行：

```csharp
private IEnumerator BadRoutine()
{
    HeavyCpuCalculation(); // 仍可能卡住当前帧
    yield return null;
}
```

`yield return null` 只能把后续工作推到下一帧，不能让前面的重计算自动并行。

适合协程：

- 等待若干帧或时间。
- 串联动画、提示、技能流程。
- 等待 Unity `AsyncOperation`。
- 周期性执行低频检查。
- 把可分片工作明确拆到多帧。

不适合直接放进协程：

- 大规模寻路或压缩计算。
- 阻塞文件 IO。
- 同步网络请求。
- 睡眠主线程。

CPU 重任务可考虑 C# 线程池、Task、C# Job System、Burst 或原生插件，但后台
线程通常不能随意访问 GameObject、Transform、Renderer 等 Unity 对象。

## 4. 常见 `yield` 对象

| 写法 | 含义 |
|---|---|
| `yield return null` | 通常下一帧继续 |
| `yield return new WaitForSeconds(t)` | 等待受 `timeScale` 影响的时间 |
| `yield return new WaitForSecondsRealtime(t)` | 等待真实时间 |
| `yield return new WaitForFixedUpdate()` | 固定更新阶段后恢复 |
| `yield return new WaitForEndOfFrame()` | 帧渲染末尾附近恢复 |
| `yield return asyncOperation` | 等待 Unity 异步操作完成 |
| `yield return anotherIEnumerator` | 等待嵌套流程完成 |
| `yield return new WaitUntil(predicate)` | 条件为真时继续 |

示例：

```csharp
private IEnumerator RespawnRoutine()
{
    player.SetActive(false);
    yield return new WaitForSecondsRealtime(2f);
    player.transform.position = spawnPoint.position;
    player.SetActive(true);
}
```

这里使用真实时间，避免游戏暂停时复活倒计时也暂停。具体是否应该暂停取决于玩法，
API 不能替策划做决定。

## 5. 协程在哪个阶段恢复

不同 yield 指令会在 PlayerLoop 的不同位置恢复。概念上：

```text
FixedUpdate / 物理
        |
        +-- WaitForFixedUpdate 恢复
        |
      Update
        |
        +-- yield null / WaitForSeconds 等按条件恢复
        |
    LateUpdate
        |
      渲染
        |
        +-- WaitForEndOfFrame 恢复
```

不要把这张简图当成每个 Unity 版本的全部内部阶段。重要的是：协程恢复时机由
yield 类型和 PlayerLoop 决定，不是每个协程都统一“在 Update 里运行”。

## 6. 启停与宿主生命周期

保存句柄：

```csharp
private Coroutine reloadRoutine;

public void BeginReload()
{
    if (reloadRoutine != null)
    {
        StopCoroutine(reloadRoutine);
    }

    reloadRoutine = StartCoroutine(ReloadRoutine());
}
```

常见规则：

- `StopCoroutine` 停止指定协程。
- `StopAllCoroutines` 只停止当前 MonoBehaviour 启动的协程。
- GameObject 变为 inactive 或被销毁时，其协程会停止。
- 仅把 MonoBehaviour 的 `enabled` 设为 false，已经运行的协程通常不会自动停止。
- Scene 卸载导致宿主销毁时，协程也随之结束。

若流程必须跨 Scene 存活，应把宿主和数据放到明确的持久服务中，而不是期待一个
已经卸载的按钮继续完成世界拯救任务。

## 7. 协程的异常与清理

协程中抛出未处理异常时，该协程通常会终止并输出日志。复杂流程应：

- 对可预期失败返回明确状态。
- 在停止或销毁时取消外部请求。
- 把事件解绑放在稳定的生命周期位置。
- 不依赖协程停止时一定执行所有后续代码。

例如技能流程不要只在最后恢复输入：

```csharp
private IEnumerator CastRoutine()
{
    inputLocked = true;

    // 如果中途对象被销毁，下面的恢复逻辑可能走不到。
    yield return PlayCastAnimation();

    inputLocked = false;
}
```

更稳妥的是让输入锁成为拥有明确 token 或计数的独立系统，并在 `OnDisable` 等
生命周期进行兜底释放。

## 8. 避免不必要的分配

以下代码每轮创建等待对象：

```csharp
while (true)
{
    ScanEnemies();
    yield return new WaitForSeconds(0.2f);
}
```

某些固定等待对象可以缓存：

```csharp
private readonly WaitForSeconds scanInterval = new(0.2f);

private IEnumerator ScanRoutine()
{
    while (true)
    {
        ScanEnemies();
        yield return scanInterval;
    }
}
```

是否值得优化要看版本、调用频率和 Profiler。不要为了省一个小对象创建十层
“万能等待池”，然后让代码可读性先被回收。

## 9. 协程与 `async`/`await`

两者都能表达异步流程，但抽象不同：

| 维度 | Unity Coroutine | `async`/`await` |
|---|---|---|
| 返回 | `IEnumerator` / `Coroutine` | `Task`、`Task<T>`、ValueTask 等 |
| 调度 | Unity PlayerLoop 和 yield 对象 | Task/awaiter 与同步上下文 |
| 结果 | 通常自行保存或回调 | 自然返回 `T` |
| 异常 | 日志后终止，传播方式有限 | 保存在 Task 中，可 `try/catch` |
| 取消 | `StopCoroutine` 或自定义标记 | `CancellationToken` |
| 生命周期 | 绑定启动它的 MonoBehaviour | 默认不自动绑定 GameObject 生命周期 |

简单异步方法：

```csharp
private async Task<string> LoadTextAsync(
    string path,
    CancellationToken cancellationToken)
{
    string text = await File.ReadAllTextAsync(path, cancellationToken);
    return text;
}
```

注意：

- Task 在对象销毁后仍可能继续。
- 后台线程不要直接修改 Unity 对象。
- `async void` 除事件入口外通常难以等待和捕获错误。
- Scene 切换或对象销毁时应取消属于它的任务。
- WebGL、主机和移动平台的线程/IO 能力不同。

## 10. 将 Task 绑定到 MonoBehaviour 生命周期

```csharp
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public sealed class ProfilePanel : MonoBehaviour
{
    private CancellationTokenSource lifetimeCts;

    private void OnEnable()
    {
        lifetimeCts = new CancellationTokenSource();
        _ = RefreshAsync(lifetimeCts.Token);
    }

    private void OnDisable()
    {
        lifetimeCts.Cancel();
        lifetimeCts.Dispose();
        lifetimeCts = null;
    }

    private async Task RefreshAsync(CancellationToken token)
    {
        try
        {
            ProfileData data = await profileService.FetchAsync(token);
            token.ThrowIfCancellationRequested();
            Render(data);
        }
        catch (OperationCanceledException)
        {
            // 生命周期结束导致的正常取消。
        }
    }
}
```

示例省略了服务注入细节，重点是“任务属于谁，谁负责取消”。异步代码最危险的
问题往往不是慢，而是结果回来时原来的页面已经不存在。

## 11. Unity `AsyncOperation`

许多 Unity API 返回 `AsyncOperation` 或相似句柄：

```csharp
private IEnumerator LoadSceneRoutine(string sceneName)
{
    AsyncOperation operation =
        SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

    yield return operation;

    Debug.Log("Scene loaded.");
}
```

也可以使用完成事件：

```csharp
operation.completed += HandleCompleted;
```

具体 API 是否支持直接 `await` 取决于 Unity 版本、包和 awaiter 支持。不要因为
类型名带 Async 就默认它一定返回 Task、支持取消或在后台线程执行。

## 12. 状态机、协程还是 Task

| 场景 | 更常见选择 |
|---|---|
| 持续存在、每帧可观察的角色状态 | 显式状态机 |
| 短期线性演出：等待、播放、继续 | 协程 |
| 有返回值、异常和取消的服务请求 | Task |
| 大量可并行数值计算 | Job System/Burst |
| 一次操作跨多系统且需可恢复 | 显式流程对象或状态机 |

协程很适合写“先 A，再等两秒，再 B”，但如果流程有十个分支、可回滚、可存档、
可断线恢复，继续堆 `yield` 会让它变成一根很长的意大利面。此时应提升为显式
状态机或任务图。

## 13. 本章检查

1. 协程为什么不是线程？
2. C# 编译器如何让局部变量跨 `yield` 保存？
3. `WaitForSeconds` 与 `WaitForSecondsRealtime` 有何区别？
4. 禁用 MonoBehaviour 和禁用 GameObject 对协程有何不同？
5. 协程里执行重计算为什么仍会卡主线程？
6. Task 为什么要绑定 GameObject 或页面生命周期？
7. 什么场景应从协程升级为显式状态机？

[上一章：MonoBehaviour 生命周期与主循环](./02-monobehaviour-lifecycle-and-playerloop.md) |
[返回 Unity 引擎基础](./README.md) |
[下一章：场景、资源与异步加载](./04-scenes-assets-and-async-loading.md)
