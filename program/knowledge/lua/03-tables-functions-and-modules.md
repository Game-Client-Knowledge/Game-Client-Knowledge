# Table、函数、闭包与模块

## 1. Table 是 Lua 的万能积木

Lua 没有分别内建数组、字典、集合、类和模块对象。它们大多由 Table 表达：

```lua
local array = { "sword", "shield", "potion" }

local dictionary = {
    player = 1001,
    boss = 9001,
}

local mixed = {
    "first",
    "second",
    name = "inventory",
    [100] = "special slot",
}
```

Table 是从非 `nil` 键映射到值的关联容器：

- 键可以是除 `nil` 和 NaN 之外的大多数 Lua 值；
- 值可以是任何 Lua 值；
- 赋值 `nil` 会删除键；
- Table 是引用语义对象。

```lua
local a = { hp = 100 }
local b = a
b.hp = 50

print(a.hp)  -- 50，a 和 b 指向同一个 Table
```

赋值不会自动深拷贝。需要副本时，必须先定义"浅拷贝、深拷贝、共享引用、循环引用、元表是否保留"分别意味着什么。

## 2. 构造与访问

以下写法大致等价：

```lua
local player = {
    name = "Ada",
    ["level"] = 12,
    [1] = "first item",
}

print(player.name)
print(player["name"])
print(player.level)
print(player[1])
```

点语法只适用于合法标识符形式的字符串键：

```lua
local config = {
    ["max-player-count"] = 4,
}

print(config["max-player-count"])
```

数字键和字符串键不同：

```lua
local values = {
    [1] = "number key",
    ["1"] = "string key",
}

print(values[1])
print(values["1"])
```

来自 JSON、网络协议或原生绑定的数据经常混入字符串数字键，排查"明明有值却读不到"时应先检查键类型。

## 3. 数组从 1 开始，但 Table 不只接受整数

Lua 习惯使用从 1 开始的连续整数键表示序列：

```lua
local items = { "sword", "shield", "potion" }
print(items[1])  -- sword
```

语言并不禁止：

```lua
items[0] = "hidden"
items[-1] = "debug"
items[1.5] = "fraction"
```

但这些键不属于常规序列部分。团队应明确：

- 序列统一从 1 开始；
- Entity ID、配置 ID 等整数键是字典，不假装数组；
- 需要 0-based 原生索引时在绑定边界集中转换；
- 不在同一个 Table 中混杂多个含义。

## 4. `#` 与空洞

连续序列：

```lua
local items = { "a", "b", "c" }
print(#items)  -- 3
```

有空洞时：

```lua
local items = {
    [1] = "a",
    [3] = "c",
}

print(#items)  -- 不应依赖具体结果
```

Lua 对有空洞 Table 的长度只保证返回某个"边界"，不保证等于最大整数键，也不保证等于非 `nil` 元素数量。不同版本、容量和插入历史都可能影响结果。

稳定做法：

### 4.1 保持紧凑序列

```lua
table.remove(items, index)
```

它会移动后续元素，复杂度可能是 O(n)。

### 4.2 显式保存数量

```lua
local sparse = {
    count = 0,
    values = {},
}
```

### 4.3 需要空位时使用哨兵

```lua
local EMPTY = {}
local slots = { "a", EMPTY, "c" }
```

这时 `#slots` 仍可表示槽位数量，业务通过 `EMPTY` 判断空位。

## 5. `ipairs`、`pairs` 与遍历顺序

### 5.1 `ipairs`

```lua
for index, value in ipairs(items) do
    print(index, value)
end
```

按 `1, 2, 3...` 遍历，遇到第一个 `nil` 停止。因此它适合紧凑序列，不适合稀疏 ID 表。

### 5.2 `pairs`

```lua
for key, value in pairs(config) do
    print(key, value)
end
```

遍历所有键，但**顺序未规定**。不要依赖当前机器上看起来稳定的结果：

```lua
-- 不适合需要确定顺序的协议、回放或锁步逻辑
for id, command in pairs(commands) do
    execute(command)
end
```

需要稳定顺序时：

```lua
local keys = {}
for key in pairs(commands) do
    keys[#keys + 1] = key
end
table.sort(keys)

for _, key in ipairs(keys) do
    execute(commands[key])
end
```

排序有成本。如果每帧都需要稳定顺序，应维护有序数组或使用适合的数据结构，而不是每帧临时收集和排序。

### 5.3 遍历时修改

遍历期间新增或删除键的行为不适合依赖。常见安全策略：

- 延迟到遍历后统一修改；
- 遍历副本；
- 使用待添加/待删除队列；
- 对紧凑数组倒序删除。

```lua
for i = #items, 1, -1 do
    if items[i].expired then
        table.remove(items, i)
    end
end
```

事件系统尤其要处理"回调执行时注销自己或添加新回调"，配套示例展示了一种快照方案。

## 6. 常用 Table 操作

```lua
local items = {}

table.insert(items, "sword")
table.insert(items, 1, "shield")

local removed = table.remove(items, 1)
table.sort(items)

local text = table.concat(items, ",")
```

高频尾部操作可直接写：

```lua
items[#items + 1] = value
local last = items[#items]
items[#items] = nil
```

注意 `table.insert(items, 1, value)` 和 `table.remove(items, 1)` 都要移动大量元素。把数组头部当队列会让每次出队都搬家。更合适的队列：

```lua
local Queue = {}
Queue.__index = Queue

function Queue.new()
    return setmetatable({
        first = 1,
        last = 0,
        values = {},
    }, Queue)
end

function Queue:push(value)
    self.last = self.last + 1
    self.values[self.last] = value
end

function Queue:pop()
    if self.first > self.last then
        return nil
    end

    local value = self.values[self.first]
    self.values[self.first] = nil
    self.first = self.first + 1
    return value
end
```

长期运行时可在队列变空后重置索引，避免数字无限增长。

## 7. Table 的数组部分与哈希部分

主流 Lua 实现会针对 Table 内的整数键和其他键采用数组部分、哈希部分等内部结构，以兼顾序列和字典访问。这是实现层优化，不是语言承诺。

概念示意：

```text
Table
+-------------------------+
| array part              |
| [1] [2] [3] ...         |
+-------------------------+
| hash part               |
| "name" -> "Ada"         |
| 1001   -> Entity        |
+-------------------------+
```

需要记住：

- Table 不是两个独立容器，语义上仍是一张映射；
- 插入和删除可能触发扩容、rehash 或内部重排；
- `pairs` 顺序不能从内部布局推导；
- Lua 标准 API 没有统一的 `reserve`；
- 某些绑定或 LuaJIT API 提供预分配能力，但不具备通用可移植性。

性能优化应先测量分配和访问热点，不要围绕某个版本的内部结构写脆弱逻辑。

## 8. 函数是一等值

```lua
local function add(a, b)
    return a + b
end

local operations = {
    add = add,
    multiply = function(a, b)
        return a * b
    end,
}

local operation = operations["add"]
print(operation(2, 3))
```

这使得策略和命令分派很自然：

```lua
local stateHandlers = {
    idle = updateIdle,
    moving = updateMoving,
    attacking = updateAttacking,
}

local handler = assert(stateHandlers[currentState])
handler(context, dt)
```

函数 Table 比不断增长的 `if/elseif` 更易扩展，但仍需要：

- 对不存在的 key 给出清晰错误；
- 明确函数签名；
- 避免把整个系统的所有行为塞进一张巨型 Table；
- 控制热更时旧函数引用。

## 9. 闭包与 upvalue

内部函数可以捕获外部局部变量：

```lua
local function createCounter(initial)
    local value = initial or 0

    return function()
        value = value + 1
        return value
    end
end

local nextId = createCounter(1000)
print(nextId())  -- 1001
print(nextId())  -- 1002
```

被捕获的外部局部变量称为 upvalue。闭包保存的是变量关联，不只是创建时数值的文本副本：

```lua
local value = 10
local function read()
    return value
end

value = 20
print(read())  -- 20
```

### 9.1 闭包的游戏用途

- 按钮回调捕获界面实例；
- 计时器捕获任务上下文；
- 工厂函数封装私有状态；
- 异步完成回调捕获请求 ID；
- 函数式组合。

### 9.2 闭包的生命周期风险

```lua
function Panel:onOpen()
    EventBus:on("currency_changed", function(value)
        self:updateCurrency(value)
    end)
end
```

引用链：

```text
全局 EventBus
    -> 回调闭包
    -> upvalue self
    -> Panel
    -> UI 节点、资源和更多回调
```

即使 Panel 从屏幕移除，只要监听没有注销，整条对象图仍可达。解决方案：

- `onClose` 使用 token 注销；
- 事件总线支持 owner 批量解绑；
- 使用弱引用时明确对象失效行为；
- 不让全局服务永久保存匿名闭包却不返回句柄。

GC 只能看到引用，不懂"这个界面已经关了"。

## 10. 循环变量与回调捕获

创建回调列表时，应明确每个回调捕获的值：

```lua
local callbacks = {}

for i = 1, 3 do
    local index = i
    callbacks[#callbacks + 1] = function()
        print(index)
    end
end
```

不同 Lua 版本和不同循环形式对控制变量闭包的细节曾有差异，显式创建当前迭代局部变量最容易读懂，也便于跨项目迁移。

## 11. 尾调用

函数直接返回另一个函数调用的所有结果时，Lua 可进行正确尾调用：

```lua
local function runState(state, context)
    return state(context)
end
```

如果调用后还要做运算，就不是尾调用：

```lua
return 1 + state(context)
```

尾调用可以避免持续增加 Lua 调用栈，但：

- 调试堆栈可能不保留中间帧；
- 它不能自动优化普通递归；
- 复杂状态流程更适合显式循环、状态机或 coroutine。

不要把尾调用当作把所有循环写成递归的许可证。

## 12. 模块：每个文件返回自己的公开接口

推荐模块写法：

```lua
-- damage.lua
local Damage = {}

local DEFAULT_SCALE = 1.0

local function clamp(value, low, high)
    return math.max(low, math.min(value, high))
end

function Damage.calculate(base, scale)
    scale = scale or DEFAULT_SCALE
    return clamp(base * scale, 0, math.huge)
end

return Damage
```

使用：

```lua
local Damage = require("damage")
print(Damage.calculate(100, 1.5))
```

优点：

- 私有实现通过 `local` 隐藏；
- 公开 API 集中在返回 Table；
- 不依赖隐式全局；
- 易于测试和替换；
- 模块对象身份可在热更新时原地保留。

## 13. `require` 做了什么

概念流程：

```text
require("game.damage")
    -> 查询 package.loaded["game.damage"]
    -> 已缓存：直接返回
    -> 未缓存：按 package.searchers 查找 loader
    -> 执行模块 chunk
    -> 缓存模块返回值
    -> 返回给调用者
```

### 13.1 同名模块通常只执行一次

```lua
local A = require("damage")
local B = require("damage")
print(A == B)  -- 通常为 true
```

重复 `require` 不会自动重新执行文件。脚本更新要显式处理缓存和旧引用。

### 13.2 模块名不是随便拼的文件路径

`require("game.damage")` 如何映射到文件、资源包或加密脚本，由 `package.path`、`package.cpath` 和宿主自定义 searcher 决定。

游戏中常自定义 loader：

```text
module name
    -> 版本化资源清单
    -> 包体/补丁包查找
    -> 解密与校验
    -> 源码或字节码加载
```

不要让业务模块直接依赖真实磁盘路径；移动资源目录时会非常痛苦。

### 13.3 循环依赖

```text
module A require B
module B require A
```

循环依赖可能得到未完成模块、错误或版本相关行为。解决思路：

- 抽出共同依赖 C；
- 通过依赖注入传入；
- 把初始化与模块定义分开；
- 使用事件或服务接口降低双向依赖。

如果两个模块必须同时出生才能互相介绍，通常说明边界还没有划清。

## 14. `require`、`dofile` 与 `load`

| API | 作用 | 常见用途 |
|---|---|---|
| `require(name)` | 按模块名加载并缓存 | 正常模块依赖 |
| `dofile(path)` | 每次读取并执行文件 | 简单工具脚本，不适合受控资源系统 |
| `load(chunk)` | 把字符串/reader 编译成函数 | 宿主 loader、受控动态代码 |
| `loadfile(path)` | 从文件编译成函数 | 桌面工具；游戏包内常由自定义 loader 代替 |

动态执行文本会扩大安全边界。来自网络、聊天、配置或玩家输入的字符串不应直接交给 `load`。

## 15. 依赖注入比全局查找更清楚

不推荐：

```lua
function ShopPanel:buy(itemId)
    GlobalNetwork:send("buy", itemId)
    GlobalAudio:play("click")
end
```

更容易测试的形式：

```lua
function ShopPanel.new(services)
    return {
        network = assert(services.network),
        audio = assert(services.audio),
    }
end

function ShopPanel:buy(itemId)
    self.network:send("buy", itemId)
    self.audio:play("click")
end
```

Lua 动态查找很方便，但全局服务越多，模块真实依赖越隐蔽。依赖注入不要求引入庞大容器；一个明确的 `services` Table 已经比到处访问 `_G` 更诚实。

## 16. 配置 Table 的验证

Lua 配置有语法，不代表有业务约束：

```lua
local function validateSkill(config)
    assert(type(config.id) == "number", "skill.id must be number")
    assert(type(config.name) == "string", "skill.name must be string")
    assert(type(config.cooldown) == "number", "skill.cooldown must be number")
    assert(config.cooldown >= 0, "skill.cooldown must be non-negative")
end
```

大型项目更适合：

- 构建阶段 schema 校验；
- 生成只读索引；
- 重复 ID 和引用完整性检查；
- 版本字段与迁移；
- 生产包只加载已验证数据；
- 错误信息包含文件、字段路径和配置 ID。

把错误留到战斗中第一次点击技能时再发现，属于让玩家兼职配置测试。

## 17. 浅拷贝、深拷贝与冻结

### 17.1 浅拷贝

```lua
local function shallowCopy(source)
    local target = {}
    for key, value in pairs(source) do
        target[key] = value
    end
    return target
end
```

嵌套 Table 仍共享：

```lua
local a = { stats = { hp = 100 } }
local b = shallowCopy(a)
b.stats.hp = 50
print(a.stats.hp)  -- 50
```

### 17.2 深拷贝不是几行递归就结束

完整深拷贝要决定：

- 循环引用如何处理；
- 共享子对象是否保持共享；
- 元表是否复制或复用；
- userdata/thread/function 如何处理；
- 键本身是 Table 时是否复制；
- 弱表语义是否保留。

```lua
local function deepCopy(value, seen)
    if type(value) ~= "table" then
        return value
    end

    seen = seen or {}
    if seen[value] then
        return seen[value]
    end

    local copy = {}
    seen[value] = copy

    for key, item in pairs(value) do
        copy[deepCopy(key, seen)] = deepCopy(item, seen)
    end

    return setmetatable(copy, getmetatable(value))
end
```

这只是一个通用起点，不一定符合引擎对象和配置语义。

### 17.3 只读代理

可用元表阻止通过代理赋值，但这不是深层不可变，也可能被 `rawset` 或持有原表者绕开：

```lua
local function readOnly(source)
    return setmetatable({}, {
        __index = source,
        __newindex = function()
            error("attempt to modify read-only table", 2)
        end,
    })
end
```

安全边界不能只靠元表代理；它更适合尽早发现普通编程错误。

## 18. 本章小结

1. Table 是映射，可模拟数组、字典、对象和模块，但不同语义应明确区分。
2. 紧凑序列从 1 开始；有空洞时不要依赖 `#` 和 `ipairs`。
3. `pairs` 遍历顺序未规定，确定性逻辑应维护明确顺序。
4. 函数是一等值，闭包通过 upvalue 捕获状态。
5. 全局事件、计时器和闭包很容易形成业务层内存泄漏。
6. 模块应返回公开 Table，私有实现使用 `local`。
7. `require` 会缓存模块，循环依赖和脚本更新都必须考虑缓存与旧引用。
8. 配置需要 schema 和构建期验证，能执行不等于数据正确。
9. 深拷贝、只读和依赖注入都需要先定义语义，不应只追求一段看起来聪明的工具函数。

[上一章：基本语法与数据类型](./02-basic-syntax-and-types.md) | [返回模块总览](./README.md) | [下一章：元表与对象模型](./04-metatables-and-object-model.md)
