# Lua 面试复习与自测

## 1. 30 秒说明 Lua 在客户端中的作用

> Lua 是一门小型、动态、可嵌入的脚本语言。游戏客户端通常让 C++ 或 C# 宿主创建 Lua VM、注册引擎 API，再由 Lua 负责 UI、活动、任务、技能和异步流程等高频变化逻辑。Lua 的主要收益是迭代速度和运行时组合能力，不是替代原生层性能。渲染、物理、海量实体热循环和平台底层能力仍适合原生实现。工程重点包括 Table/闭包/元表、协程调度、Lua 与原生的封送成本、跨语言对象生命周期、GC 抖动，以及脚本更新时旧闭包和状态迁移。

## 2. 高频问题与回答骨架

### 2.1 Lua 有哪些基本类型

Lua 5.4 有：

```text
nil、boolean、number、string、
function、table、userdata、thread
```

补充：

- 值有类型，变量没有固定静态类型；
- `thread` 指 coroutine，不等于操作系统线程；
- userdata 用于承载宿主原生对象；
- Lua 5.3/5.4 的 number 通常有整数和浮点子类型；
- Lua 5.1/LuaJIT 的数值模型可能不同。

### 2.2 Lua 中哪些值是假

只有 `false` 和 `nil`。`0`、空字符串和空 Table 都是真。

### 2.3 Table 如何同时表示数组和哈希表

语义上 Table 是任意非 `nil` 键到值的映射。主流实现为了效率通常包含数组部分和哈希部分，但这是实现细节。连续整数键常作为 1-based 序列，其他键走关联映射。

有空洞时不能依赖 `#` 得到元素数量，`ipairs` 会在第一个 `nil` 停止，`pairs` 顺序未规定。

### 2.4 `pairs` 与 `ipairs` 的区别

- `ipairs`：按 1、2、3 连续整数键遍历，遇到 `nil` 停止；
- `pairs`：遍历所有键，顺序未规定。

确定性战斗、序列化或回放不能依赖 `pairs` 当前看起来稳定的顺序。

### 2.5 点和冒号有什么区别

```text
object:method(a)
等价于
object.method(object, a)
```

冒号自动传 `self`。把方法提取成回调时若丢失接收者，参数会整体错位。

### 2.6 什么是闭包和 upvalue

闭包是函数加上它捕获的词法环境。外层局部变量被内部函数捕获后成为 upvalue，其生命周期可超过外层函数调用。

优点：封装状态、回调、异步流程。

风险：全局事件持有闭包，闭包捕获 Panel，导致整个界面对象图一直可达。

### 2.7 元表和 `__index` 如何实现对象方法

实例自身找不到字段时，Lua 查询元表的 `__index`。常见写法：

```lua
local Player = {}
Player.__index = Player

function Player.new()
    return setmetatable({}, Player)
end

function Player:update(dt)
end
```

实例方法存放在共享的 `Player` Table 中，实例只保存自己的字段。它模拟类式查找，但没有 C++ 类的静态类型、内存布局和构造规则。

### 2.8 coroutine 和线程有什么区别

coroutine 是协作式调度：

- 主动 `yield` 才暂停；
- `resume` 后从暂停点继续；
- 默认同一时刻只执行一个；
- 不自动并行，也不会使用多个 CPU 核；
- 死循环仍会卡住所在宿主线程。

它适合跨帧等待和异步流程；后台 CPU 工作仍由原生线程/Job 完成，再把结果投递回 Lua 线程。

### 2.9 Lua C API 为什么使用栈

C 和 Lua 类型系统不同，虚拟栈提供统一参数/返回值协议：

```text
Lua 参数入栈
    -> C 函数按索引检查并读取
    -> C 把返回值压栈
    -> 返回返回值数量
```

包装层还负责类型转换、错误处理、对象查找和生命周期检查。

### 2.10 userdata 与 light userdata

- 完整 userdata：Lua 管理的一块内存，可有关联元表和 `__gc`；
- light userdata：一个裸 `void*` 值，不由 Lua 管理，没有每实例终结逻辑。

对引擎对象，安全句柄和 generation 往往比长期裸指针更可靠。

### 2.11 跨语言调用为什么贵

不只是函数跳转，还可能包含：

- Lua 字段查找；
- 参数数量/类型检查；
- 字符串编码和 Table 转换；
- userdata/句柄查找；
- wrapper 创建；
- registry/引用管理；
- 异常转换；
- 返回值压栈。

优化重点是减少边界次数，使用高层批量 API。

### 2.12 Lua GC 为什么会引起卡顿

脚本持续创建 Table、字符串、闭包和 wrapper，GC 必须扫描和回收。增量/分代模式可分散工作，但调参不能替代减少无意义分配。

还要区分：

- GC 抖动：回收工作占用帧时间；
- 内存泄漏：对象仍被全局、监听、task、registry 或 delegate 引用；
- 原生泄漏：Lua wrapper 回收了，但底层资源没有正确释放。

### 2.13 如何减少 Lua GC 压力

1. 用事件和 dirty flag 减少每帧创建。
2. 批量跨语言调用。
3. 避免热循环临时 Table/闭包/字符串。
4. 使用正确队列和数据结构。
5. 生命周期结束时解绑事件、取消 task、释放 registry ref。
6. 在真实设备测量分配速率和 GC 峰值。
7. 只在合适安全点考虑完整回收。

### 2.14 为什么热更新不只是清空 `package.loaded`

清空缓存只影响未来 `require`。运行时还可能保存：

- 旧模块 Table；
- 缓存函数；
- 旧闭包/upvalue；
- 挂起 coroutine 栈；
- 已注册到原生层的 callback；
- 旧实例字段和元表；
- 与新代码不兼容的状态。

完整方案需要原地补丁或重建、状态版本迁移、安全点、签名校验、自检与回滚。

### 2.15 Lua 如何发生内存泄漏

GC 语言也会发生逻辑泄漏：

```text
EventBus -> callback -> Panel
Timer -> coroutine -> Scene
registry ref -> closure -> object
C# event -> delegate -> Lua callback
module cache -> old state
```

只要根引用仍存在，GC 就不会回收。解决关键是明确 owner 和注销协议。

### 2.16 Lua 适合做锁步战斗吗

可以，但要建立确定性约束：

- 固定 Lua 版本和数值配置；
- 不依赖 `pairs` 顺序；
- 使用确定性随机数；
- 不读取本地时间和设备差异；
- 控制浮点或使用定点方案；
- 固定原生容器返回顺序；
- 输入、状态和协议可回放验证。

语言本身不会自动提供确定性。

## 3. 场景设计题

### 3.1 设计一个 Lua UI 页面生命周期

回答应覆盖：

```text
new/create
    -> 注入服务，建立纯 Lua 状态
open/start
    -> 创建/绑定节点，订阅事件，启动任务
refresh
    -> dirty 合并，更新展示
close/stop
    -> 解绑事件，取消 timer/coroutine/request
destroy/dispose
    -> 释放原生句柄，断开引用
```

补充列表复用、异步结果回到已关闭页面、重复关闭幂等、错误降级和泄漏监控。

### 3.2 设计 Lua 事件总线

应说明：

- `on` 返回 token；
- `off(token)` 和 owner 批量解绑；
- 发射时监听集合修改规则；
- 回调顺序是否稳定；
- 错误是否隔离；
- 重入策略；
- 事件载荷合同；
- Feature 关闭后的自动清理。

### 3.3 设计脚本更新系统

应覆盖：

```text
构建与测试
    -> manifest/hash/signature
    -> 下载与暂存
    -> 版本/依赖校验
    -> 安全点
    -> 新代码隔离加载
    -> 模块补丁 + 状态迁移
    -> callback/task 处理
    -> smoke test
    -> 原子提交或回滚
    -> 版本和错误上报
```

同时说明平台合规和服务端协议兼容。

### 3.4 排查界面打开关闭后内存持续增长

步骤：

1. 稳定复现，记录 Lua 堆、原生对象、listener、task、registry ref。
2. 重复打开关闭并在诊断安全点触发 GC。
3. 若 Lua 堆不降，沿全局根和闭包引用追踪。
4. 检查 EventBus、Timer、coroutine、C# delegate、资源回调。
5. 若 Lua wrapper 降而原生不降，检查所有权和 dispose。
6. 修复后用自动化循环做回归。

## 4. 容易失分的回答

| 回答 | 问题 |
|---|---|
| "Lua 主要就是热更新" | 忽略 UI、配置、流程、嵌入和平台限制 |
| "Table 就是哈希表" | 忽略序列语义和常见实现的数组部分 |
| "协程是轻量线程" | 容易让人误解为并行执行 |
| "GC 语言不会泄漏" | 忽略可达但业务无用对象和原生资源 |
| "虚拟机慢，所以都改 C++" | 没有剖析边界、分配和数据规模 |
| "清空 package.loaded 就热更了" | 忽略旧引用、闭包、coroutine 和状态 |
| "用 pcall 就安全了" | 捕获不了死循环，也不提供资源和状态回滚 |
| "客户端 Lua 加密后就安全" | 客户端仍不可信，加密不等于权威 |

## 5. 自测清单

### 5.1 语法与运行时

- [ ] 能列出八种基本类型和真假规则。
- [ ] 能解释多返回值何时展开。
- [ ] 能说明 `#`、`pairs`、`ipairs` 在空洞 Table 上的边界。
- [ ] 能解释闭包与 upvalue。
- [ ] 能手写模块、类式元表和队列。
- [ ] 能解释 coroutine 状态及 `yield/resume` 数据流。

### 5.2 客户端

- [ ] 能说明 Lua 适合和不适合的系统。
- [ ] 能画出 Lua -> Bridge -> C++/C# 调用链。
- [ ] 能设计 UI/Feature 生命周期。
- [ ] 能区分事件、命令、查询和消息。
- [ ] 能解释脚本更新中的旧引用和状态迁移。
- [ ] 能说明锁步确定性要求。

### 5.3 性能与工程

- [ ] 能列出每帧常见分配源。
- [ ] 能说明 GC 抖动与内存泄漏的区别。
- [ ] 能说明跨语言批处理的价值。
- [ ] 能设计错误边界、日志和熔断。
- [ ] 能说明 Lua 沙箱为什么困难。
- [ ] 能给出真机性能验证和泄漏回归方案。

## 6. 一分钟综合回答

> Lua 在游戏客户端中通常作为嵌入式业务脚本层。宿主通过 C API 或绑定框架注册引擎能力，Lua 负责 UI、活动、任务、技能和跨帧流程，原生层负责渲染、物理、批量数据和平台底层能力。Lua 的核心语言机制是 Table、函数闭包、元表和 coroutine。Table 语义上是映射，连续整数键可表示序列，但空洞时不能依赖长度，`pairs` 也没有稳定顺序；元表的 `__index` 可实现方法共享；coroutine 是协作式调度，不是线程。
>
> 工程上最重要的是边界和生命周期。一次 Lua/原生调用还包含类型检查、封送、wrapper 和引用管理，所以应使用粗粒度批量 API。Lua GC 只回收不可达对象，全局事件、计时器、registry 和 C# delegate 仍会保活业务对象。脚本更新也不只是重新 `require`，旧闭包、挂起 coroutine、回调和运行状态都需要重建或迁移。最终要在真机上同时观察脚本耗时、分配速率、GC 峰值和跨语言调用次数。

## 7. 延伸资料

- [Lua 5.4 Reference Manual](https://www.lua.org/manual/5.4/)
- [Programming in Lua](https://www.lua.org/pil/)
- 项目实际使用的 Lua/LuaJIT 版本文档、绑定框架文档和平台发布规则。

阅读官方资料时注意版本。`Programming in Lua` 在线第一版基于较早 Lua，适合学习思想，但新语法与 API 应以对应版本手册为准。

[上一章：性能、GC 与工程实践](./08-performance-gc-and-engineering.md) | [返回模块总览](./README.md) | [返回计算机基础面试路线](../interview-roadmap/foundations/01-language-runtime-and-data-structures.md)
