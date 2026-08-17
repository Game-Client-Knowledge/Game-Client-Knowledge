# Lua 基本语法与数据类型

## 1. 最小程序

```lua
local playerName = "Ada"
local level = 12

print("player:", playerName, "level:", level)
```

Lua 语句通常不需要分号，代码块使用关键字结束：

```lua
if level >= 10 then
    print("feature unlocked")
end
```

注释写法：

```lua
-- 单行注释

--[[
多行注释
]]
```

Lua 标识符区分大小写，`player` 和 `Player` 是两个名字。团队常用：

- 局部变量和函数：`camelCase` 或 `snake_case`，按项目统一；
- 模块/类式 Table：`PascalCase`；
- 常量：`UPPER_SNAKE_CASE`；
- 私有概念：通过 `local` 隐藏，而不是只靠下划线假装门锁。

## 2. 八种基本类型

Lua 5.4 的 `type` 可能返回：

| 类型 | 用途 | 示例 |
|---|---|---|
| `nil` | 不存在、无值 | `local target = nil` |
| `boolean` | 布尔值 | `true`、`false` |
| `number` | 数值 | `42`、`3.14` |
| `string` | 不可变字节串 | `"hello"` |
| `function` | 可调用函数/闭包 | `function() end` |
| `table` | 关联数组 | `{ hp = 100 }` |
| `userdata` | 宿主提供的原生对象 | C++ 对象包装 |
| `thread` | Lua coroutine | `coroutine.create(fn)` |

```lua
print(type(nil))             -- nil
print(type(42))              -- number
print(type({}))              -- table
print(type(function() end))  -- function
```

Lua 是动态类型语言：**值有类型，变量槽位没有固定类型**。

```lua
local value = 10
value = "ten"   -- 合法，但不代表一定是好主意
```

动态类型方便组合，也让字段拼写错误可能拖到运行时才出现。大型项目通常配合 LuaLS 注解、EmmyLua 注解、类型检查器或代码生成接口减少这类问题。

## 3. `nil`：没有值，也能删除键

未初始化的局部变量和不存在的 Table 键会得到 `nil`：

```lua
local target
print(target)  -- nil

local config = {}
print(config.timeout)  -- nil
```

把 Table 键赋为 `nil` 等价于删除该键：

```lua
local player = { name = "Ada", title = "Rookie" }
player.title = nil
```

这意味着 Table 不能直接区分：

- 这个字段从未设置；
- 这个字段显式设置为"空"。

需要三态时，使用专门哨兵值：

```lua
local NULL = {}

local patch = {
    title = NULL,  -- 表示显式清空
}
```

不要用字符串 `"nil"` 代替空值；它只是四个普通字符，穿上了空值的戏服。

## 4. 布尔与真值

Lua 中只有 `false` 和 `nil` 为假，其余值全部为真，包括：

- 数字 `0`；
- 空字符串 `""`；
- 空 Table `{}`。

```lua
if 0 then
    print("0 is truthy")
end

if "" then
    print("empty string is truthy")
end
```

这是从 C/C++、JavaScript 转来时最常见的误区之一：

```lua
-- 错误：0 也会进入分支
if player.hp then
    drawHealth(player.hp)
end

-- 明确表达条件
if player.hp ~= nil then
    drawHealth(player.hp)
end
```

逻辑运算符 `and` 和 `or` 返回操作数本身，而不强制返回 boolean：

```lua
local displayName = player.nickname or player.name or "Unknown"
local result = ready and "go" or "wait"
```

经典的三目模拟有陷阱：

```lua
local value = condition and false or true
```

如果"真分支"本身是 `false` 或 `nil`，表达式会继续返回后半部分。复杂分支直接写 `if`，可读性比炫技更耐用。

## 5. 数值

Lua 5.3/5.4 的 `number` 内部通常包含整数和浮点两种子类型：

```lua
print(math.type(10))    -- integer
print(math.type(10.0))  -- float
```

具体位宽可在编译 Lua 时配置。Lua 5.1 和常见 LuaJIT 配置通常把 number 视为双精度浮点；项目不能凭语言名字假设协议字段一定是 64 位整数。

### 5.1 算术运算

```lua
local a = 7 + 3   -- 10
local b = 7 - 3   -- 4
local c = 7 * 3   -- 21
local d = 7 / 3   -- 浮点除法
local e = 7 // 3  -- 2，向下取整除法，Lua 5.3+
local f = 7 % 3   -- 1
local g = 2 ^ 3   -- 8
local h = -a
```

Lua 的 `%` 与向下取整除法配套，负数结果可能不同于只向零截断的语言：

```lua
print(-3 // 2)  -- -2
print(-3 % 2)   -- 1
```

### 5.2 位运算

Lua 5.3+：

```lua
local mask = 0x01 | 0x04
local enabled = (mask & 0x04) ~= 0
local shifted = mask << 2
```

Lua 5.1 没有这套原生语法，项目可能使用 `bit`、`bit32` 或宿主 API。跨版本脚本不要直接假设可用。

### 5.3 浮点比较

```lua
local function nearlyEqual(a, b, epsilon)
    return math.abs(a - b) <= epsilon
end
```

不要用固定 epsilon 解决所有量级问题。游戏数学通常根据单位、量级和算法误差设计容差；网络同步还要考虑不同平台的数值确定性。

## 6. 字符串

Lua 字符串是不可变的二进制安全字节序列，可以包含 `\0`：

```lua
local name = "Lua"
local path = 'assets/ui/main'
local block = [[
line one
line two
]]
```

拼接使用 `..`：

```lua
local label = "HP: " .. tostring(100)
```

长度运算符 `#` 对字符串返回字节长度，不是 Unicode 字符数：

```lua
print(#"Lua")   -- 3
print(#"你好")  -- UTF-8 下通常为 6 字节，不是 2
```

Lua 5.3+ 提供 `utf8` 标准库：

```lua
print(utf8.len("你好"))  -- 2
```

实际 UI 文本还涉及字形簇、组合字符、emoji 和断行，`utf8.len` 也不等于玩家眼中可见字符数。排版应交给成熟文本系统。

字符串不可变，因此循环中反复拼接会产生中间字符串：

```lua
-- 大量片段时不理想
local text = ""
for i = 1, 1000 do
    text = text .. i .. ","
end

-- 收集后一次连接
local parts = {}
for i = 1, 1000 do
    parts[i] = tostring(i)
end
local text = table.concat(parts, ",")
```

常用格式化：

```lua
local text = string.format("HP: %d/%d", currentHp, maxHp)
```

格式化也会创建字符串，不要在未测量的每帧热循环里到处生产 UI 文案。

## 7. 局部变量与全局变量

### 7.1 默认赋值可能创建全局变量

```lua
score = 100        -- 全局
local level = 10   -- 当前词法作用域内局部
```

全局变量通常存入环境表。Lua 5.2+ 通过 `_ENV` 解析全局名；Lua 5.1 使用不同环境机制。

大型项目应默认使用 `local`：

- 查找通常更直接；
- 生命周期更清楚；
- 不会污染全局命名空间；
- 拼写错误更容易暴露；
- 模块卸载和测试隔离更容易。

```lua
local function calculateDamage(base, scale)
    local result = base * scale
    return result
end
```

### 7.2 词法作用域

`do`、`if`、循环和函数体都会形成作用域：

```lua
local value = "outer"

do
    local value = "inner"
    print(value)  -- inner
end

print(value)      -- outer
```

局部变量声明在初始化表达式完成后才进入作用域：

```lua
local x = 10
local x = x + 1  -- 右侧 x 是上一层的 x，结果为 11
```

递归局部函数优先使用语法糖：

```lua
local function factorial(n)
    if n <= 1 then
        return 1
    end
    return n * factorial(n - 1)
end
```

它能正确让函数体捕获正在声明的局部函数。

### 7.3 多重赋值

```lua
local x, y = 10, 20
x, y = y, x
```

右侧先求值，再统一赋给左侧，因此交换不需要临时变量。

数量不一致时：

```lua
local a, b, c = 1, 2  -- c = nil
local x = 1, 2, 3      -- x = 1，多余值丢弃
```

## 8. 比较运算

```lua
a == b
a ~= b
a < b
a <= b
a > b
a >= b
```

Lua 使用 `~=` 表示不等于，不是 `!=`。

默认情况下：

- number 与 number 按数值比较；
- string 与 string 按字节序比较；
- Table、function、thread、userdata 通常按身份比较；
- 不同类型的值通常不相等；
- 大小比较要求可比较类型，否则报错。

```lua
print({} == {})  -- false，它们是两个不同 Table

local t = {}
local same = t
print(t == same) -- true
```

元表可以参与部分比较行为，详见 [元表与对象模型](./04-metatables-and-object-model.md)。

## 9. 条件与循环

### 9.1 `if`

```lua
if hp <= 0 then
    state = "dead"
elseif hp < 30 then
    state = "danger"
else
    state = "normal"
end
```

Lua 没有内建 `switch`。少量分支用 `if`，命令分派可用函数 Table：

```lua
local commands = {
    move = handleMove,
    attack = handleAttack,
}

local handler = commands[message.kind]
if handler then
    handler(message)
end
```

### 9.2 数值 `for`

```lua
for i = 1, 5 do
    print(i)
end

for i = 10, 1, -2 do
    print(i)
end
```

边界和步长在循环开始前求值，不要在循环体里修改它们并期待迭代规则自动改变。

### 9.3 泛型 `for`

```lua
for index, value in ipairs(items) do
    print(index, value)
end

for key, value in pairs(config) do
    print(key, value)
end
```

`ipairs` 和 `pairs` 的边界会在下一章详细解释。

### 9.4 `while` 与 `repeat`

```lua
while queue:hasItems() do
    process(queue:pop())
end

repeat
    attempts = attempts + 1
until connected or attempts >= maxAttempts
```

`repeat ... until` 至少执行一次，而且条件可以访问循环体中声明的局部变量：

```lua
repeat
    local message = receive()
until message ~= nil
```

### 9.5 `break`、`goto` 与缺少的 `continue`

`break` 结束当前循环。Lua 没有通用 `continue` 关键字。Lua 5.2+ 可谨慎使用局部 `goto`：

```lua
for _, actor in ipairs(actors) do
    if actor.disabled then
        goto continue
    end

    actor:update()

    ::continue::
end
```

Lua 5.1/LuaJIT 没有标准 `goto`。跨版本代码可以把主体包进条件，或提取成函数。不要用 `goto` 编织流程迷宫。

## 10. 函数、参数与返回值

### 10.1 函数定义

```lua
local function add(a, b)
    return a + b
end

local multiply = function(a, b)
    return a * b
end
```

参数数量不严格匹配：

```lua
local function show(a, b)
    print(a, b)
end

show(1)        -- b = nil
show(1, 2, 3)  -- 第三个参数被忽略
```

公共接口应主动校验关键参数，避免错误在更深处以"attempt to index a nil value"的形式出现。

### 10.2 多返回值

```lua
local function divide(a, b)
    if b == 0 then
        return nil, "division by zero"
    end
    return a / b, nil
end

local value, err = divide(10, 2)
if not value then
    print(err)
end
```

多返回值常用于：

- 值 + 错误；
- x/y/z 多个分量；
- 解析结果 + 剩余位置；
- 查找值 + 是否存在。

只有表达式列表最后一个函数调用会自然展开全部返回值：

```lua
local function values()
    return 1, 2, 3
end

local a, b, c = values()      -- 1, 2, 3
local x, y, z = values(), 9   -- 1, 9, nil
local one = (values())        -- 括号强制只取第一个值
```

Table 构造和函数参数中也有类似规则：

```lua
local t1 = { values() }     -- { 1, 2, 3 }
local t2 = { values(), 9 }  -- { 1, 9 }
```

多返回值很方便，也很容易在重构表达式顺序时悄悄改变结果数量。

### 10.3 可变参数

```lua
local function log(tag, ...)
    local count = select("#", ...)
    print(tag, "argument count:", count)

    for i = 1, count do
        print(i, select(i, ...))
    end
end
```

要保留中间的 `nil`，不能只依赖普通数组长度：

```lua
local function pack(...)
    return { n = select("#", ...), ... }
end

local args = pack(1, nil, 3)
print(args.n)  -- 3
```

Lua 5.2+ 提供 `table.pack`，Lua 5.1 项目常自定义兼容实现。

## 11. 方法调用的点与冒号

```lua
local Player = {}

function Player.takeDamage(self, amount)
    self.hp = self.hp - amount
end

local player = { hp = 100 }
Player.takeDamage(player, 20)
```

冒号定义和调用只是 `self` 语法糖：

```lua
function Player:takeDamage(amount)
    self.hp = self.hp - amount
end

Player.takeDamage(player, 20)
player.takeDamage = Player.takeDamage
player:takeDamage(20)
```

最重要的等价关系：

```text
object:method(a, b)
等价于
object.method(object, a, b)
```

常见错误：

```lua
local callback = player.takeDamage
callback(20)  -- self 变成 20，参数整体错位
```

修复方式：

```lua
local callback = function(amount)
    player:takeDamage(amount)
end
```

或者让事件系统显式支持 `owner + method` 绑定。

## 12. 错误与受保护调用

### 12.1 抛出错误

```lua
local function setHealth(value)
    assert(type(value) == "number", "health must be a number")
    if value < 0 then
        error("health cannot be negative")
    end
end
```

`assert(condition, message)` 在条件为假时抛错，否则返回所有参数，适合检查程序不变量，不适合把正常业务失败都变成崩溃。

### 12.2 `pcall`

```lua
local ok, resultOrError = pcall(function()
    return loadPlayerConfig()
end)

if not ok then
    print("load failed:", resultOrError)
end
```

`pcall` 捕获 Lua 错误，返回 `false + error object`。它不会：

- 自动记录完整调用栈；
- 回滚已修改的 Table；
- 取消已经发出的原生命令；
- 阻止死循环；
- 修复被破坏的业务状态。

### 12.3 `xpcall` 与 traceback

```lua
local function onError(err)
    return debug.traceback(tostring(err), 2)
end

local ok, result = xpcall(runFeature, onError)
if not ok then
    logger:error(result)
end
```

生产环境常由宿主提供受控的 traceback 函数，而不是把完整 `debug` 库暴露给所有脚本。

## 13. Lua 5.4 的局部变量属性

Lua 5.4 支持：

```lua
local MAX_COUNT <const> = 100
```

再次赋值会报错。`<const>` 约束变量绑定，不会递归冻结 Table：

```lua
local config <const> = {}
config.enabled = true  -- 仍然合法
```

`<close>` 支持作用域结束时关闭资源：

```lua
local file <close> = assert(io.open("data.txt", "r"))
```

它依赖值的 `__close` 元方法，类似受限的作用域清理机制。游戏项目常用 Lua 5.1/LuaJIT，不能假设这套语法存在；引擎资源生命周期也通常由宿主包装器明确管理。

## 14. 常见语法陷阱速查

| 陷阱 | 错误理解 | 正确结论 |
|---|---|---|
| `0` | 是假值 | 是真值 |
| `""` | 是假值 | 是真值 |
| `nil` 字段 | 保存了空值 | 键实际上不存在 |
| `!=` | 不等于 | Lua 使用 `~=` |
| `a.b` / `a:b()` | 只是写法不同 | 冒号会额外传入 `self` |
| 多返回值 | 到处都会全部展开 | 通常只在表达式列表最后位置展开 |
| `#text` | Unicode 字符数 | 字节长度 |
| `pcall` | 事务和超时保护 | 只捕获错误 |
| 未写 `local` | 当前文件变量 | 通常会污染环境全局 |
| Lua 5.4 语法 | 所有 Lua 都能运行 | 5.1/LuaJIT 可能不支持 |

## 15. 本章小结

1. Lua 有八种基本类型，值有类型，变量没有固定静态类型。
2. 只有 `false` 和 `nil` 为假，`0` 与空字符串都为真。
3. `nil` 既表示无值，也会删除 Table 键。
4. 默认应使用 `local` 控制作用域和生命周期。
5. 函数支持多返回值和可变参数，但展开位置有明确规则。
6. 冒号调用会自动传入 `self`，回调中尤其容易用错。
7. `pcall`/`xpcall` 负责错误边界，不负责状态回滚或死循环中断。
8. 数值、位运算、`goto`、`utf8`、`<const>` 等能力必须结合 Lua 版本判断。

[上一章：Lua 为什么适合游戏客户端](./01-why-lua-in-game-clients.md) | [返回模块总览](./README.md) | [下一章：Table、函数、闭包与模块](./03-tables-functions-and-modules.md)
