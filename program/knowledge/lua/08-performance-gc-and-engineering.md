# Lua 性能、GC 与工程速记

## 先分账，再优化

```text
Lua 总耗时 = VM 指令
+ Table/字符串/闭包分配
+ GC 工作
+ Bridge 检查与封送
+ 原生函数本身
+ 日志/序列化/调试钩子
```

先确认瓶颈属于哪一层。逐实体跨边界复制一万次时，缓存一个 `math.abs` 几乎没有意义。

## GC 与泄漏

GC 管理 Table、字符串、闭包/upvalue、coroutine、full userdata 等 Lua 堆对象，只回收**不可达**对象。根通常包括全局环境、registry、Lua 栈、活跃 coroutine、`package.loaded` 和原生保存的 Lua 引用。

必须区分：

- **GC 抖动**：分配导致回收工作占用帧时间；
- **Lua 逻辑泄漏**：对象仍被事件、timer、module、registry 引用；
- **原生泄漏**：wrapper 已回收，底层资源/引用没有释放。

Lua 5.4 的增量/分代模式可调整停顿与吞吐；5.1、LuaJIT 和定制 VM 不同。调 GC 参数不能修复仍可达对象，也不能替代显式资源生命周期。

## 高频分配与结构问题

| 热点 | 首选处理 |
|---|---|
| 每帧临时 Table | 状态变化驱动、复用明确缓冲、批量下沉 |
| 每帧闭包 | 构建期绑定，owner 关闭时解绑 |
| 字符串拼接/日志 | dirty 更新、`table.concat`、日志级别前置判断 |
| `{...}` 打包 | 同步直接转发；异步再拥有稳定副本 |
| wrapper 重建 | 核对代理缓存、强弱引用与失效策略 |
| `table.remove(t,1)` | head/tail 队列 |
| 每对象 Update | System/Scheduler/事件驱动 |

复用缓冲要清理旧字段并禁止调用者长期保存；否则省下分配却引入别名错误。

## Bridge 优化

**边界次数通常比单次 wrapper 指令数重要。** 使用批量命令、一次快照、同语言侧完成数学/解码、生成绑定和可观测计数。对 API 记录调用次数、总/最大耗时、转换字节和错误数。

LuaJIT 还要看 trace 形成、exit、C 边界、FFI 生命周期和 debug hook，不能只看“已开启 JIT”。

## 测量闭环

指标至少覆盖：

- 帧：Lua 总耗时、最大值、P95/P99；
- 模块/函数：调用数、自耗时、总耗时；
- GC：堆大小、分配速率、step/full GC 耗时；
- Bridge：调用次数、转换量、最热 API；
- 生命周期：task/listener/registry/wrapper 数量。

使用目标设备、发布构建和可复现场景，记录 VM/脚本/配置版本。流程是：复现 → 基线 → 单一假设 → 改动 → 对比 → 正确性回归。

## 泄漏排查

```text
记录打开前基线
-> 页面打开/关闭 N 次
-> 诊断安全点 GC
-> 对比 Lua 堆、listener/task/registry/wrapper/native object
-> 沿根引用或原生所有权追踪
-> 修复后自动循环回归
```

每帧 `collectgarbage("collect")` 只会制造停顿；合适的 full GC 通常放在场景卸载/加载等可接受安全点，并以目标机数据验证。

## 工程底线

- CI：编译所有 chunk、lint、格式化、未声明全局、schema 与依赖扫描。
- 测试：纯 Lua 规则、Fake Service、绑定/AOT、取消竞态、补丁迁移和真机性能。
- 错误：traceback、脚本版本、限流、熔断和降级。
- 存档：版本化 schema，不序列化任意函数、coroutine、userdata 或对象图。
- 沙箱：同时限制 API、CPU、内存、FFI 和输出；高风险代码用进程级隔离。`pcall` 捕获不了死循环。

## 高频追问

1. 内存持续增长时如何区分 GC 延迟和引用泄漏？
2. 增量 GC 与分代 GC 的取舍是什么？
3. 哪些每帧写法最容易制造短命对象？
4. 为什么批量 Bridge API 往往收益最大？
5. Profiler 为什么要看 P99 而不是只看平均值？
6. Lua 沙箱为什么不能只删除 `os.execute`？
7. 优化后如何证明没有改变确定性和生命周期？

[上一章：原生交互与生命周期](./07-native-interop-and-lifecycle.md) | [返回总览](./README.md) | [下一章：高频题与场景题](./09-interview-review.md)
