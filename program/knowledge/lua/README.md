# Lua 客户端面试复习

## 模块定位

本模块面向**已经写过 Lua，希望在面试前快速恢复知识网络**的读者。正文不再逐项教授语法，而是集中回答四件事：

1. 机制是什么；
2. 工程边界在哪里；
3. 面试会怎样追问；
4. 项目经历需要拿出什么证据。

复习时先给结论，再解释原因，最后落到项目取舍。只背 API 名称通常只能扛住第一问。

## 复习路线

| 优先级 | 章节 | 必须回答的问题 |
|---|---|---|
| P0 | [语言陷阱](./02-basic-syntax-and-types.md) | `nil`、真值、多返回值、点/冒号有哪些边界？ |
| P0 | [Table、闭包与模块](./03-tables-functions-and-modules.md) | 序列、遍历、upvalue、`require` 缓存为何容易出错？ |
| P0 | [元表与对象模型](./04-metatables-and-object-model.md) | `__index` 如何工作，为什么不等于 C++ 虚表？ |
| P0 | [协程与调度](./05-coroutines-events-and-state-machines.md) | coroutine 为什么不是线程，取消由谁负责？ |
| P0 | [原生交互](./07-native-interop-and-lifecycle.md) | 调用成本、对象所有权、回调引用如何管理？ |
| P0 | [性能与 GC](./08-performance-gc-and-engineering.md) | 卡顿、逻辑泄漏和原生泄漏如何区分？ |
| P1 | [客户端架构与脚本更新](./06-client-architecture-and-hot-update.md) | 为什么重载文件不等于完成热更新？ |
| P1 | [Lua 在客户端中的边界](./01-why-lua-in-game-clients.md) | 什么放 Lua，什么留在原生层？ |
| 冲刺 | [高频题与场景题](./09-interview-review.md) | 能否在没有提示时完整表达？ |

- **30 分钟**：README → 09 → 07 → 08。
- **2 小时**：按 P0 顺序复习，再补 06。
- **半天**：完整阅读，并把每章追问替换成自己的项目证据。

配套代码仅用于查漏：[Lua 游戏客户端基础示例](../../examples/lua/game-client-basics/README.md)。

## 版本边界

面试回答应先声明项目运行时，不能把“Lua”当成唯一版本。

| 运行时 | 面试中最值得提的差异 |
|---|---|
| Lua 5.1 | 无原生位运算、`goto`、`//`；环境模型与 5.2+ 不同 |
| LuaJIT 2.x | 主要兼容 5.1；JIT/FFI 强，但受 trace、平台和安全边界约束 |
| Lua 5.3 | integer/float 子类型、原生位运算、`//` |
| Lua 5.4 | `<const>`、`<close>`、增量/分代 GC 等 |

框架名不等于 VM 版本。xLua、ToLua、SLua、UnLua 或自研绑定还会改变 AOT、yield、对象缓存和生命周期策略。

## 回答框架

面对任意 Lua 工程题，按以下顺序组织：

```text
结论：我会怎么选
机制：VM / Table / GC / Bridge 实际怎样工作
代价：性能、生命周期、确定性或平台限制
方案：接口、数据流、失败处理和观测指标
证据：项目规模、Profiler 数据、故障与改进结果
```

## 复习完成标准

- 能用 30 秒说明 Lua 在客户端中的定位。
- 能解释 Table、闭包、元表、coroutine，而不是只会写法。
- 能画出 Lua → Bridge → C++/C# 的调用和所有权链。
- 能区分 GC 抖动、Lua 引用泄漏、原生资源泄漏。
- 能说明脚本更新中的旧引用、状态迁移、安全点与回滚。
- 能用真实项目数据回答“为什么这样设计”和“如何验证”。

[下一章：Lua 在客户端中的边界](./01-why-lua-in-game-clients.md)
