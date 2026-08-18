# 协程、事件与状态机

## 1. 游戏逻辑为什么需要跨帧流程

许多客户端流程不是一次函数调用就结束：

```text
播放开门动画
    -> 等待 0.8 秒
    -> 角色走进房间
    -> 等待移动完成事件
    -> 播放对白
    -> 等待玩家点击
    -> 开始战斗
```

如果只靠嵌套回调：

```lua
playDoorAnimation(function()
    movePlayer(function()
        showDialogue(function()
            startBattle()
        end)
    end)
end)
```

错误处理、取消、超时和清理会逐层缩进。Lua coroutine 可以把"等待后继续"写成接近顺序代码，但调度器仍要由宿主或框架实现。

## 2. coroutine 不是线程

coroutine 是**协作式执行单元**：

- 只有当前代码主动 `yield` 才让出执行；
- `resume` 后从上次暂停位置继续；
- 默认不会并行；
- 不会自动利用多个 CPU 核；
- 一个 coroutine 死循环仍会卡住驱动它的线程；
- 同一 VM 内的 coroutine 通常共享全局环境和 GC。

比喻：

```text
操作系统线程：厨房里有多位厨师，可能同时做菜，需要协调抢锅。
Lua coroutine：只有一位厨师，菜谱夹了多个书签，做到"等待烤箱"就换另一页。
```

书签能减少等待时的流程碎片，不能凭空多出厨师。

## 3. coroutine 的生命周期

```lua
local task = coroutine.create(function(a, b)
    print("start", a, b)

    local reason = coroutine.yield("waiting")
    print("resume because", reason)

    return "done"
end)

print(coroutine.status(task))  -- suspended

local ok, value = coroutine.resume(task, 10, 20)
print(ok, value)               -- true, waiting
print(coroutine.status(task))  -- suspended

ok, value = coroutine.resume(task, "event arrived")
print(ok, value)               -- true, done
print(coroutine.status(task))  -- dead
```

状态：

| 状态 | 含义 |
|---|---|
| `suspended` | 新建未运行，或已 `yield` |
| `running` | 当前正在执行 |
| `normal` | 当前 coroutine 恢复了另一个 coroutine |
| `dead` | 正常返回或因错误终止 |

`resume` 不会直接把 coroutine 内错误抛给调用者，而是返回 `false, error`：

```lua
local ok, err = coroutine.resume(task)
if not ok then
    logger:error(debug.traceback(task, tostring(err)))
end
```

调度器必须检查 `ok`，否则任务报错可能变成安静躺平。

## 4. `yield` 与 `resume` 的数据传递

```text
resume(co, A, B)
    -> 首次进入函数参数 A, B

yield(X, Y)
    -> resume 返回 true, X, Y

resume(co, C, D)
    -> yield 表达式返回 C, D

return R
    -> 最后一次 resume 返回 true, R
```

示例：

```lua
local co = coroutine.create(function()
    local command = coroutine.yield("ready")
    return "handled " .. command
end)

print(coroutine.resume(co))           -- true ready
print(coroutine.resume(co, "jump"))   -- true handled jump
```

理解双向传值后，等待对象可设计为普通 Table：

```lua
coroutine.yield({
    kind = "seconds",
    value = 1.5,
})
```

调度器看到 `kind` 后决定何时恢复。

## 5. 最小跨帧调度器

```lua
local Scheduler = {}
Scheduler.__index = Scheduler

function Scheduler.new()
    return setmetatable({
        now = 0,
        tasks = {},
    }, Scheduler)
end

function Scheduler:start(fn)
    local task = {
        coroutine = coroutine.create(fn),
        wakeAt = self.now,
    }
    self.tasks[#self.tasks + 1] = task
    return task
end

function Scheduler:update(dt)
    self.now = self.now + dt

    for i = #self.tasks, 1, -1 do
        local task = self.tasks[i]
        if not task.cancelled and self.now >= task.wakeAt then
            local ok, waitSeconds = coroutine.resume(task.coroutine)
            if not ok then
                task.error = waitSeconds
            elseif coroutine.status(task.coroutine) ~= "dead" then
                task.wakeAt = self.now + (waitSeconds or 0)
            end
        end

        if task.cancelled
            or task.error
            or coroutine.status(task.coroutine) == "dead"
        then
            table.remove(self.tasks, i)
        end
    end
end
```

任务：

```lua
scheduler:start(function()
    print("show title")
    coroutine.yield(1.0)
    print("show button")
end)
```

这是教学模型，生产调度器还要处理：

- 取消和取消原因；
- 等事件、网络请求、资源加载；
- 场景或界面销毁时批量取消；
- 超时；
- 错误栈；
- 每帧恢复预算；
- coroutine 在原生调用边界能否 yield；
- 热更新时挂起栈如何处理。

## 6. 取消必须是第一等能力

界面关闭后，仍挂起的任务可能在几秒后恢复并访问已销毁节点：

```lua
function Panel:onOpen()
    self.task = scheduler:start(function()
        coroutine.yield(5)
        self.label:setText("finished")
    end)
end
```

关闭时：

```lua
function Panel:onClose()
    if self.task then
        scheduler:cancel(self.task, "panel closed")
        self.task = nil
    end
end
```

更好的框架让任务绑定 owner 或 scope：

```lua
self.scope:spawn(function()
    waitSeconds(5)
    self.label:setText("finished")
end)

-- scope 关闭时取消全部任务、监听和计时器
self.scope:close()
```

结构化生命周期比要求每位开发者记住十个 token 更可靠。

## 7. 事件总线

最小接口：

```lua
local token = eventBus:on("player_hp_changed", function(hp)
    panel:updateHp(hp)
end)

eventBus:emit("player_hp_changed", 80)
eventBus:off(token)
```

事件总线解耦发送者和接收者，但全局事件过多会带来：

- 事件名称拼写错误；
- 载荷结构隐式；
- 调用链难追踪；
- 监听忘记注销；
- 顺序依赖；
- 回调中再发事件造成重入；
- 某个监听错误阻断后续监听。

### 7.1 事件合同

至少明确：

```lua
-- event: player_hp_changed
-- payload:
--   entityId: integer
--   current: number
--   maximum: number
```

更大型项目可以生成事件 ID、注解和参数检查。

### 7.2 监听修改安全

发事件时，回调可能注销自己或添加新回调。常见策略：

- 发射前复制监听快照；
- 用 generation/version 检测变化；
- 标记删除，发射结束后压缩；
- 明确新监听是否参与本次发射。

配套示例使用快照，让本次发射集合稳定。

### 7.3 错误隔离

```lua
for _, listener in ipairs(snapshot) do
    local ok, err = xpcall(listener.callback, traceback, ...)
    if not ok then
        logger:error(err)
    end
end
```

是否继续调用后续监听取决于事件语义。通知型事件通常隔离错误；可取消命令或事务型流程不应伪装成普通广播。

## 8. 观察者、命令和消息不要混为一谈

| 形式 | 语义 | 示例 |
|---|---|---|
| 事件/通知 | 某事已经发生，可能有多个订阅者 | `currency_changed` |
| 命令 | 请求某个处理者执行动作 | `purchase_item` |
| 查询 | 请求返回数据 | `get_inventory` |
| 消息 | 跨线程/网络边界传输的数据 | `asset_loaded` |

如果购买请求使用全局广播，多个监听者可能各扣一次钱；这不是促销，是模型错误。

命令应有明确处理者和结果，事件用于发布已经完成的事实。

## 9. 有限状态机

状态机适合互斥状态和明确转移：

```lua
local states = {}

states.idle = {
    enter = function(ctx)
        ctx.animation:play("idle")
    end,
    update = function(ctx)
        if ctx.input:hasMove() then
            return "moving"
        end
    end,
}

states.moving = {
    enter = function(ctx)
        ctx.animation:play("run")
    end,
    update = function(ctx, dt)
        ctx:move(dt)
        if not ctx.input:hasMove() then
            return "idle"
        end
    end,
}
```

驱动器：

```lua
local StateMachine = {}
StateMachine.__index = StateMachine

function StateMachine.new(states, initial, context)
    local self = setmetatable({
        states = states,
        current = initial,
        context = context,
    }, StateMachine)

    local state = assert(states[initial])
    if state.enter then
        state.enter(context)
    end
    return self
end

function StateMachine:update(dt)
    local state = assert(self.states[self.current])
    local nextState = state.update and state.update(self.context, dt)

    if nextState and nextState ~= self.current then
        self:transition(nextState)
    end
end
```

完整实现应集中处理 `exit -> current 改变 -> enter`，并防止：

- `enter` 中再次转移导致重入；
- 转移到不存在状态；
- 状态更新中销毁自身；
- 同帧无限转移；
- 热更新后当前状态名失效。

## 10. 状态机、协程和行为树如何选择

| 工具 | 适合 | 不适合 |
|---|---|---|
| 状态机 | 互斥状态、明确转移、动画状态 | 很长的一次性剧情步骤 |
| coroutine | 顺序异步流程、等待时间/事件 | 复杂并行决策和可视化分析 |
| 事件 | 解耦通知、跨模块结果 | 需要明确返回值和唯一处理者 |
| 行为树 | AI 决策、可组合条件与动作 | 简单 UI 页面生命周期 |
| 普通函数 | 同步、短小、无等待逻辑 | 强行表达跨帧等待 |

可以组合：

```text
状态机：角色处于 Combat 状态
    -> 行为树：选择追击、躲避或攻击
    -> coroutine：执行一次带前摇/后摇的技能流程
    -> 事件：通知 UI 技能已完成
```

每一层只表达自己最擅长的控制关系。

## 11. 一帧中的调度顺序

顺序应由框架固定并记录：

```text
1. 收集原生异步结果
2. 投递网络/资源消息
3. 更新脚本计时器
4. 恢复满足条件的 coroutine
5. 更新状态机或脚本 System
6. 刷新 UI 脏标记
7. 提交脚本生成的原生命令
8. 记录错误、耗时和分配指标
```

顺序不同可能导致：

- 事件早一帧或晚一帧；
- UI 读到旧状态；
- 同一 coroutine 在一帧内被多次恢复；
- 对象销毁后仍收到事件。

调度顺序是运行时合同，不是随便排得整齐的列表。

## 12. coroutine 与原生边界

能否在 Lua -> C/C++/C# -> Lua 的嵌套调用中 `yield`，取决于 Lua 版本、C API 调用方式和绑定框架。Lua C API 某些路径需要 continuation 形式，许多自动绑定直接禁止跨边界 yield。

更稳妥的模型：

```text
Lua 发起异步请求并获得 requestId
    -> Lua yield WaitRequest(requestId)
原生异步执行
    -> 完成后投递 requestId + result
主线程调度器
    -> 找到等待任务
    -> resume(task, result)
```

不要让原生函数阻塞主线程等待网络或磁盘，也不要假设任何绑定函数里都能随意 `yield`。

## 13. 热更新中的挂起 coroutine

coroutine 保存：

- 当前执行函数；
- Lua 调用栈；
- 局部变量；
- upvalue 引用；
- 暂停位置对应的旧字节码。

更新模块文件不会把挂起 coroutine 的栈帧变成新代码。常见策略：

1. 更新前取消可重建任务。
2. 只在安全点应用更新。
3. 给长流程保存显式状态，而不是永久挂起 coroutine。
4. 版本不兼容时重启脚本域或场景。
5. 关键任务由状态机/数据记录进度，更新后重新构建执行。

coroutine 适合运行时流程，不天然是持久化工作流。

## 14. 本章小结

1. coroutine 是协作式调度，不是线程，也不会自动并行。
2. `yield`/`resume` 可双向传值，调度器负责时间、事件、错误和恢复。
3. 所有跨帧任务都需要明确取消和 owner 生命周期。
4. 事件用于通知事实，命令用于请求动作，查询用于获取结果。
5. 事件发射必须定义修改监听集合、错误隔离和重入规则。
6. 状态机适合互斥状态，coroutine 适合顺序等待，二者可以组合。
7. Lua 与原生边界能否 yield 取决于具体版本和绑定。
8. 挂起 coroutine 保留旧代码栈，热更新时通常要取消或迁移。

[上一章：元表与对象模型](./04-metatables-and-object-model.md) | [返回模块总览](./README.md) | [下一章：客户端架构与脚本更新](./06-client-architecture-and-hot-update.md)
