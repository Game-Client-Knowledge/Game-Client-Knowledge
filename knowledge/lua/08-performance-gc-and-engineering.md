# Lua 性能、GC 与工程实践

## 1. 性能问题先分账

一段 Lua 逻辑变慢，可能来自完全不同的来源：

```text
总耗时
  = Lua 指令执行
  + Table/字符串/闭包分配
  + GC 工作
  + Lua/原生边界转换
  + 原生函数本身
  + 日志、序列化和调试钩子
  + 数据布局与缓存行为
```

如果每帧把一万个 Transform 从原生复制成 Lua Table，再把它们逐个写回，仅优化 `local math_abs = math.abs` 不会扭转架构。先用 profiler 确认是哪一笔账最大。

## 2. Lua GC 管什么

GC 管理常见的 Lua 堆对象：

- Table；
- 字符串；
- 闭包和 upvalue；
- coroutine；
- 完整 userdata；
- Lua 函数原型等 VM 对象。

GC 不自动决定：

- 一个原生 GPU 资源何时应释放；
- C# event 何时取消订阅；
- C registry ref 何时 `unref`；
- 业务对象何时失效；
- 文件、socket、场景对象的线程约束。

Lua wrapper 被回收与原生资源被释放可能是两套生命周期，必须由绑定合同连接起来。

## 3. 可达性：为什么"没在用了"仍不回收

GC 从根集合出发追踪引用：

```text
roots
  |- 全局环境
  |- registry
  |- Lua 栈
  |- 活跃 coroutine
  |- 原生保存的 Lua 引用
  `- 模块缓存 package.loaded
        |
        v
     Table / closure / userdata graph
```

只要对象仍可从根到达，就不会回收。

常见业务泄漏：

```text
全局 EventBus -> closure -> Panel
全局 Timer    -> coroutine -> Scene
package.loaded -> module cache -> old config
C registry    -> callback -> Lua object
C# event      -> delegate -> Lua closure
对象池         -> 未清理对象 -> 大量引用
```

这不是 GC 算法失灵，而是程序仍明确表示"我要保留它"。

## 4. Lua 5.4 的 GC 模式

Lua 5.4 提供增量和分代等 GC 模式，具体 API 与调参应查项目所用版本。

### 4.1 增量 GC

把标记、清扫工作分散到多个步骤，目标是避免一次完整回收造成长停顿。

可调思想通常包括：

- GC 相对分配速度；
- 何时开始新周期；
- 每步做多少工作。

调得太懒：

- 堆增长；
- 最后可能出现更重回收；
- 内存峰值升高。

调得太勤：

- 每帧 GC CPU 占用上升；
- 吞吐下降；
- 小场景也不断扫地。

### 4.2 分代 GC

利用"多数新对象很快死亡"的假设，更频繁处理年轻对象，较少扫描长期对象。适合短命临时对象较多的工作负载，但具体收益依赖版本和对象引用模式。

Lua 5.1、LuaJIT 和不同定制 VM 的 GC 能力与参数不同。不要把 Lua 5.4 的调参命令复制到旧项目，然后责怪虚拟机不配合。

## 5. `collectgarbage` 应如何使用

常见操作包括查询内存、执行一步、停止/恢复或触发完整回收；可用命令依 Lua 版本而异。

教学示例：

```lua
local kb = collectgarbage("count")
print("Lua memory (rough KB):", kb)
```

不推荐每帧无脑完整回收：

```lua
collectgarbage("collect")
```

合理场景可能包括：

- 场景卸载并清空大量对象后；
- 加载界面期间已有可接受停顿；
- 自动化测试比较前后可达对象；
- 内存压力信号触发受控策略。

完整回收只会回收不可达对象。监听未解绑时连续调用十次，也只是让 GC 十次确认它仍然健在。

## 6. 每帧常见分配来源

### 6.1 临时 Table

```lua
function update()
    local position = { x = 1, y = 2, z = 3 }
end
```

若每帧每实体创建，会形成大量短命对象。选择：

- 用多个返回值传少量标量；
- 复用明确所有权的缓冲；
- 批量交给原生；
- 只在状态变化时创建数据；
- 使用值型 userdata/原生结构，但要测量边界成本。

复用 Table 需要清理旧字段：

```lua
for key in pairs(buffer) do
    buffer[key] = nil
end
```

共享复用缓冲不能被调用者长期保存，否则下一次写入会悄悄修改旧结果。

### 6.2 闭包

```lua
for _, button in ipairs(buttons) do
    button:onClick(function()
        self:onButton(button)
    end)
end
```

每次创建新闭包。对于界面打开一次的几十个按钮通常可以接受；若在每帧循环中创建，就值得调整：

- 缓存回调；
- 事件 API 支持 owner + method + argument；
- 只在对象建立时绑定；
- 关闭时解绑。

不要为了消除几次初始化闭包把代码改成难懂的全局跳转表，优化要看频率。

### 6.3 字符串

```lua
local label = "hp=" .. hp .. ", mp=" .. mp
```

每次值变化都会产生新字符串。UI 只在数据变化时刷新；多段文本使用 `table.concat`；日志在级别关闭时不要先完成昂贵格式化。

```lua
if logger:isDebugEnabled() then
    logger:debug(string.format("entity=%d state=%s", id, state))
end
```

### 6.4 可变参数打包

`{ ... }`、`table.pack(...)` 会创建 Table。高频事件总线可以直接转发 `...`，但异步保存参数时仍要拥有一份稳定数据。

### 6.5 原生包装器

每次读取 `entity.transform` 都可能新建 wrapper，取决于绑定框架。需要确认：

- wrapper 是否缓存；
- 是强引用还是弱引用；
- 同一原生对象是否对应唯一代理；
- wrapper 分配和回收量；
- 访问失效对象的行为。

## 7. Table 性能习惯

### 7.1 选择正确形态

- 连续列表：1-based 紧凑数组；
- ID 查找：字典；
- 队列：head/tail 索引；
- 集合：`set[key] = true`；
- 有序输出：单独顺序数组；
- 高频组件数据：考虑原生连续存储/ECS。

不要让同一 Table 同时扮演队列、对象、配置和缓存。

### 7.2 避免头部插删

`table.remove(t, 1)` 会移动后续元素。队列用 head 指针，顺序无要求的删除可用末尾交换：

```lua
local function swapRemove(items, index)
    local last = #items
    items[index] = items[last]
    items[last] = nil
end
```

它会改变顺序，只有业务允许时才能使用。

### 7.3 避免热循环中的重复查找

```lua
-- 可读性和热点允许时缓存
local position = actor.position
for i = 1, count do
    position.x = position.x + velocities[i]
end
```

局部变量通常比多层全局/Table 查找直接。但把所有标准函数都提前缓存会制造样板并妨碍热更新。只对剖析确认的热点使用。

### 7.4 不依赖实现细节预分配

标准 Lua 没有通用 `table.reserve`。某些 C API、LuaJIT 或定制 VM 可指定数组/哈希初始容量，应封装在版本适配层，不让业务代码依赖。

## 8. 跨语言调用优化

最重要的原则：

```text
减少边界次数 > 减少包装函数内部几条指令
```

优化方式：

1. 批量传入实体 ID 和命令。
2. 原生返回一次快照，而不是逐字段 getter。
3. UI 使用 dirty data，一帧统一提交。
4. 网络消息在一侧完成解码，不来回转换。
5. 高频数学运算留在同一语言一侧。
6. 绑定生成器避免反射慢路径。
7. 缓存稳定方法入口，但考虑热更新失效。
8. 对字符串编码转换做次数和字节量统计。

错误示例：

```lua
for i = 1, count do
    native:setX(ids[i], xs[i])
    native:setY(ids[i], ys[i])
    native:setZ(ids[i], zs[i])
end
```

一次批处理可以把 `3 * count` 次边界切换降为 1 次。

## 9. LuaJIT 优化不能只看是否开启 JIT

LuaJIT 性能受到 trace 形成和退出影响。常见问题可能包括：

- 频繁跨 C 边界；
- 高度多态的 Table 形状；
- 不支持 JIT 的操作；
- trace 过长或分支复杂；
- FFI 对象生命周期；
- 热代码不断变化；
- debug hook 影响 JIT。

需要查看 JIT profiler、trace/exit 信息，而不是只比较编辑器里一个总耗时。某段代码从 1 ms 变成 0.2 ms 很有价值，但如果真正帧卡顿来自 20 ms 的资源同步加载，胜利仍然只发生在小数点里。

## 10. 性能测量方法

### 10.1 分层指标

至少记录：

| 层级 | 指标 |
|---|---|
| 帧 | Lua 总耗时、最大耗时、P95/P99 |
| 模块 | UI、战斗、活动、调度器各自耗时 |
| 函数 | 调用次数、总耗时、自耗时 |
| GC | 当前堆、分配速率、GC 步耗时、完整回收次数 |
| Bridge | 跨语言调用次数、转换字节、最热 API |
| 任务 | coroutine 数、恢复数、超时/取消/错误 |

平均值会掩盖偶发卡顿。实时客户端更应关注峰值和高分位。

### 10.2 测量环境

- 使用目标设备和发布构建；
- 区分编辑器、Mono、IL2CPP、LuaJIT/解释器；
- 保持场景和输入可复现；
- 预热 JIT 和资源缓存；
- 同时观察 CPU、内存和加载；
- 记录脚本版本与配置版本；
- 避免 profiler 本身改变太多行为。

### 10.3 优化闭环

```text
复现
    -> 采样/插桩定位
    -> 建立基线
    -> 提出一个假设
    -> 只改变一个关键变量
    -> 对比指标
    -> 回归正确性
```

不要一口气重写对象系统、GC 参数和事件总线，然后面对变快或变慢都不知道感谢谁。

## 11. 错误处理与日志

### 11.1 建立边界

适合受保护调用的边界：

- 主循环 Lua 入口；
- 网络消息分派；
- Feature 启动；
- 原生回调；
- 编辑器命令；
- 脚本测试用例。

### 11.2 错误信息

至少包含：

- 错误文本与 traceback；
- 模块/功能；
- 脚本包 commit 或版本；
- Lua VM/场景；
- 玩家操作上下文；
- 关键 ID；
- 是否降级或重试。

敏感字段、凭证和隐私数据不能直接写入日志。

### 11.3 防止错误风暴

一个每帧失败的 `update` 可以每秒产生几十条长栈日志。使用：

- 首次完整上报；
- 相同指纹限流；
- 熔断并禁用故障 Feature；
- 统计被抑制次数；
- 恢复时明确重置。

## 12. 测试策略

### 12.1 纯 Lua 单元测试

把领域逻辑与引擎 API 分离：

```lua
local function calculateDamage(base, attack, defense)
    return math.max(1, base + attack - defense)
end
```

测试边界：

- 零值和负值；
- `nil`；
- 表为空或有空洞；
- 配置缺字段；
- 重复事件；
- 取消后异步完成；
- 模块重复 start/stop；
- 更新前后状态迁移。

### 12.2 Fake 服务

```lua
local fakeNetwork = {
    requests = {},
}

function fakeNetwork:request(name, payload)
    self.requests[#self.requests + 1] = {
        name = name,
        payload = payload,
    }
    return 101
end
```

依赖注入让测试不需要启动完整引擎。

### 12.3 集成测试

验证：

- Lua/原生参数转换；
- AOT wrapper；
- 对象失效；
- 场景卸载；
- 跨线程结果投递；
- 资源包与模块 loader；
- 脚本更新和回滚；
- 真机 GC/性能。

## 13. 静态检查与格式化

动态语言也可以在运行前发现大量问题：

- LuaLS/Lua Language Server 注解；
- luacheck 等 lint；
- StyLua 等格式化；
- 模块依赖扫描；
- 未声明全局检测；
- 配置 schema 校验；
- 生成绑定 API 类型描述；
- CI 中编译所有 Lua chunk。

示例注解风格取决于工具：

```lua
---@class PlayerModel
---@field id integer
---@field hp number
---@field name string
```

注解不能改变运行时，但能改善补全、导航和错误提示。生成的绑定注解应与原生 API 同源，避免文档和代码各自成长。

## 14. 调试与可观测性

实用工具：

- 可过滤的结构化日志；
- Lua traceback；
- module/Feature 生命周期面板；
- 活跃 coroutine、timer、event listener 列表；
- registry ref 数量；
- Lua 堆快照或对象类型统计；
- 跨语言调用火焰图；
- 脚本包版本与模块哈希；
- 调试器断点和变量查看；
- 网络消息回放。

对象泄漏排查可比较：

```text
打开界面前
    -> 记录 listener/task/wrapper 数量
打开并关闭界面 N 次
    -> 强制安全点 GC（仅诊断）
    -> 再次记录
```

数量持续增长时，再沿 owner、事件和 registry 引用追踪。

## 15. 不可信脚本与沙箱

Lua 环境可以限制全局能力：

```lua
local safeEnvironment = {
    assert = assert,
    error = error,
    ipairs = ipairs,
    pairs = pairs,
    tonumber = tonumber,
    tostring = tostring,
    type = type,
    math = math,
    string = string,
    table = table,
}

local chunk = assert(load(source, "user_chunk", "t", safeEnvironment))
```

但安全沙箱远不止"删掉 `os.execute`"：

- `io`、`os`、`debug`、`package`、`load`、`dofile` 等能力；
- 原生 userdata 暴露的方法；
- 元表逃逸；
- LuaJIT FFI；
- CPU 死循环；
- 内存无限分配；
- 超深递归；
- 日志/网络滥用；
- 通过共享对象修改宿主状态。

宿主还需要：

- 指令/时间预算与中断机制；
- 自定义 allocator 或内存配额；
- API allowlist；
- 独立 VM；
- 输入/输出大小限制；
- 禁止 FFI 和危险 native module；
- 审计与终止；
- 进程级隔离用于高风险代码。

`pcall` 捕获不了一个永不返回的循环。真正不可信代码最好在进程或系统沙箱中隔离，而不是与主游戏 VM 共处一室后互相约定要礼貌。

## 16. 序列化与存档

不要直接序列化任意 Lua 对象图：

- function 和 coroutine 无通用持久格式；
- userdata 可能含进程内句柄；
- Table 可循环引用；
- 元表不等于数据；
- `pairs` 顺序未规定；
- 浮点和整数版本差异；
- 恶意数据可能构造巨大对象。

定义稳定 schema：

```lua
local save = {
    version = 3,
    player = {
        id = state.player.id,
        level = state.player.level,
    },
    quests = exportQuestProgress(state.quests),
}
```

加载时：

- 校验类型、范围和大小；
- 按版本迁移；
- 忽略或拒绝未知字段；
- 重新创建运行时对象和元表；
- 不信任本地存档作为服务端资产权威。

## 17. 性能反模式速查

| 反模式 | 后果 | 优先改法 |
|---|---|---|
| 每实体每帧跨语言 getter/setter | 边界切换爆炸 | 批量 API |
| 每帧创建大量临时 Table | 分配和 GC 压力 | 事件驱动、缓冲或原生批处理 |
| 字符串循环拼接 | 中间字符串 | `table.concat`、脏更新 |
| 所有对象注册 Update | 调度和生命周期复杂 | System/Scheduler |
| `table.remove(t, 1)` 做队列 | O(n) 搬移 | head/tail |
| 全局事件永不解绑 | 对象图泄漏 | owner scope/token |
| 每帧 `collectgarbage("collect")` | 长停顿和吞吐下降 | 增量策略 + 分配治理 |
| 无序 `pairs` 参与同步逻辑 | 非确定性 | 明确顺序 |
| 只在编辑器测性能 | 真机偏差 | 发布真机剖析 |
| 看到 Lua 就重写 C++ | 可能优化错层 | 先测量完整调用链 |

## 18. 本章小结

1. Lua 性能要分清解释执行、分配、GC、Bridge 和原生工作本身。
2. GC 只回收不可达 Lua 对象，业务泄漏通常来自全局、监听、task 和 registry 引用。
3. Lua 版本不同，GC 模式和调参 API 也不同。
4. 高频临时 Table、闭包、字符串和 wrapper 是常见分配源。
5. 批量跨语言 API 通常比微调局部 Lua 指令更有价值。
6. 性能应关注峰值、P95/P99、分配速率和边界调用次数。
7. 测试应覆盖纯 Lua 规则、生命周期、异步取消、绑定和真机 AOT。
8. 沙箱必须同时限制能力、CPU、内存和原生接口；高风险代码需要更强隔离。
9. 存档应使用版本化 schema，不序列化任意运行时对象图。

[上一章：原生交互与对象生命周期](./07-native-interop-and-lifecycle.md) | [返回模块总览](./README.md) | [下一章：面试复习与自测](./09-interview-review.md)
