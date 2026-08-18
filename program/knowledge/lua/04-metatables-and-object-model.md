# 元表与 Lua 对象模型

## 1. 元表是什么

普通 Table 在执行某些操作时，如果自身无法直接完成，Lua 会查询它关联的 **metatable（元表）**：

```lua
local object = {}
local meta = {}

setmetatable(object, meta)
print(getmetatable(object) == meta)  -- true
```

元表中的特殊键称为 **metamethod（元方法）**，例如：

- `__index`：读取不存在的键；
- `__newindex`：写入不存在的键；
- `__call`：把值当函数调用；
- `__tostring`：字符串表示；
- `__add`、`__sub`：算术运算；
- `__eq`、`__lt`、`__le`：比较；
- `__len`：长度；
- `__pairs`：定制 `pairs`；
- `__gc`：终结；
- `__close`：Lua 5.4 的关闭行为。

元表不是 C++ 虚表：

- vtable 通常由编译器生成并按固定槽位分派；
- Lua 元表本身也是普通 Table；
- 元方法通过运行时查找参与语言操作；
- 元表可在运行时替换，语义更灵活，约束也更弱。

可以把元表理解成 Table 的"客服中心"：对象自己找不到字段时才打电话过去。客服中心如果再转接到另一个客服中心，问题就开始具有架构感。

## 2. `__index`：读取缺失字段时的后备规则

### 2.1 `__index` 是 Table

```lua
local defaults = {
    speed = 5,
    color = "white",
}

local actor = {
    name = "Ada",
}

setmetatable(actor, {
    __index = defaults,
})

print(actor.name)   -- Ada，来自自身
print(actor.speed)  -- 5，来自 defaults
```

查找链：

```text
actor.speed
    -> actor 自身是否有 "speed"?
    -> 没有，读取 actor 元表的 __index
    -> __index 是 defaults Table
    -> 返回 defaults.speed
```

如果对象自身后来写入 `speed`，它会遮蔽默认值：

```lua
actor.speed = 8
print(actor.speed)  -- 8
```

### 2.2 `__index` 是函数

```lua
local actor = {}

setmetatable(actor, {
    __index = function(_, key)
        error("unknown actor field: " .. tostring(key), 2)
    end,
})
```

函数形式可用于：

- 计算属性；
- 延迟加载；
- 字段别名；
- 严格字段检查；
- 多来源查找。

每次缺失访问都执行函数，不能把昂贵操作藏进看起来普通的字段读取中。

## 3. `__newindex`：拦截写入缺失字段

```lua
local values = {}
local proxy = {}

setmetatable(proxy, {
    __index = values,
    __newindex = function(_, key, value)
        if key == "hp" then
            assert(type(value) == "number")
            value = math.max(0, value)
        end
        values[key] = value
    end,
})

proxy.hp = -10
print(proxy.hp)  -- 0
```

关键点：`__newindex` 通常只在目标 Table **当前没有该键**时触发。如果键已经直接存在于 `proxy`，普通赋值会绕过元方法。

需要无条件代理时，把实际数据存在另一个 Table，代理自身保持无字段。

## 4. `rawget` 与 `rawset`

元方法内部若再次使用普通索引，可能递归调用自己：

```lua
setmetatable(object, {
    __index = function(target, key)
        return target[key]  -- 再次触发 __index，直到栈溢出
    end,
})
```

绕过元方法：

```lua
local value = rawget(object, key)
rawset(object, key, newValue)
```

`rawget`/`rawset` 常用于元方法实现和底层工具。普通业务代码大量使用它们，往往意味着正在绕过对象自己声明的合同。

## 5. 用元表实现类式对象

Lua 没有内建 `class` 关键字，但可通过"方法表 + 实例元表"实现常见对象写法：

```lua
local Player = {}
Player.__index = Player

function Player.new(name, hp)
    local self = {
        name = name,
        hp = hp or 100,
    }
    return setmetatable(self, Player)
end

function Player:takeDamage(amount)
    self.hp = math.max(0, self.hp - amount)
end

function Player:isAlive()
    return self.hp > 0
end

return Player
```

使用：

```lua
local Player = require("player")
local ada = Player.new("Ada", 120)

ada:takeDamage(30)
print(ada.hp)         -- 90
print(ada:isAlive())  -- true
```

查找 `ada.takeDamage`：

```text
ada 自身没有 takeDamage
    -> ada 的元表是 Player
    -> Player.__index 是 Player
    -> 找到 Player.takeDamage
    -> ada:takeDamage(30) 自动把 ada 作为 self
```

每个实例只保存自己的数据，方法函数由 `Player` Table 共享。

## 6. 构造函数不是语言强制机制

`Player.new` 只是普通函数：

- 名字可以是 `create`、`New` 或其他；
- Lua 不会自动调用它；
- 它可返回 Table、userdata、代理甚至 `nil`；
- 它不会自动执行基类构造；
- 对象销毁也不会自动调用 `dispose`。

团队应统一生命周期约定：

```lua
local panel = Panel.new(services)
panel:onCreate()
panel:onOpen(args)
panel:onClose()
panel:onDestroy()
```

哪些方法只调用一次、能否重复打开、失败时如何清理，都应由框架明确，不应靠命名猜测。

## 7. 继承效果

```lua
local Actor = {}
Actor.__index = Actor

function Actor.new(name)
    return setmetatable({
        name = name,
    }, Actor)
end

function Actor:describe()
    return "Actor(" .. self.name .. ")"
end

local Player = setmetatable({}, {
    __index = Actor,
})
Player.__index = Player

function Player.new(name, level)
    local self = Actor.new(name)
    self.level = level
    return setmetatable(self, Player)
end

function Player:describe()
    return "Player(" .. self.name .. ", level=" .. self.level .. ")"
end
```

这里有两条不同关系：

```text
实例 player 的元表 -> Player
Player 自己的元表  -> { __index = Actor }
```

查找：

```text
player.someMethod
    -> player
    -> Player
    -> Actor
```

这能模拟继承查找，但 Lua 不会自动提供：

- 静态类型检查；
- 访问控制；
- 构造顺序；
- 虚析构；
- 接口实现校验；
- C++ 式对象布局。

层次过深时，每个方法查找和维护者都要沿链爬楼。游戏业务通常更适合浅层对象模型和组合。

## 8. 调用基类实现

```lua
function Player:describe()
    local base = Actor.describe(self)
    return base .. ", level=" .. self.level
end
```

不要写：

```lua
Actor:describe()
```

这会把 `Actor` Table 本身作为 `self`，不是当前实例。正确形式是点调用并显式传入实例：

```text
Actor.describe(self)
```

## 9. 组合通常比继承更直接

继承：

```text
FlyingSwimmingCombatPet
    -> FlyingSwimmingPet
    -> FlyingPet
    -> Pet
    -> Actor
```

组合：

```lua
local pet = {
    movement = FlyingMovement.new(),
    combat = MeleeCombat.new(),
    owner = owner,
}
```

或直接组合函数策略：

```lua
local function createPet(options)
    return {
        move = assert(options.move),
        attack = assert(options.attack),
    }
end
```

组合优势：

- 每个变化维度独立；
- 测试可单独替换策略；
- 不需要多重继承模拟；
- 生命周期和依赖更显式；
- 热更新时可替换单项行为。

元表继承适合共享稳定方法和默认行为，不应成为把所有功能连接起来的万能胶。

## 10. `__call`：让 Table 像函数

```lua
local Player = {}
Player.__index = Player

function Player.new(name)
    return setmetatable({ name = name }, Player)
end

setmetatable(Player, {
    __call = function(_, ...)
        return Player.new(...)
    end,
})

local ada = Player("Ada")
```

这种写法让"类 Table"可直接调用。它可以简化构造，但新读者需要知道 `Player` 不是函数。团队应优先一致性，不必为了少写四个字符让每个模块都有不同魔法。

## 11. 运算符元方法

向量示例：

```lua
local Vec2 = {}
Vec2.__index = Vec2

function Vec2.new(x, y)
    return setmetatable({
        x = x,
        y = y,
    }, Vec2)
end

function Vec2.__add(a, b)
    return Vec2.new(a.x + b.x, a.y + b.y)
end

function Vec2.__tostring(value)
    return string.format("Vec2(%g, %g)", value.x, value.y)
end

local a = Vec2.new(1, 2)
local b = Vec2.new(3, 4)
print(a + b)  -- Vec2(4, 6)
```

常见元方法：

| 操作 | 元方法 |
|---|---|
| `a + b` | `__add` |
| `a - b` | `__sub` |
| `a * b` | `__mul` |
| `a / b` | `__div` |
| `a // b` | `__idiv` |
| `a % b` | `__mod` |
| `a == b` | `__eq` |
| `a < b` | `__lt` |
| `#a` | `__len` |
| `tostring(a)` | `__tostring` |
| `a(...)` | `__call` |

元方法应符合操作直觉。`player + potion` 如果偷偷发起网络支付，虽然充满创造力，但不适合作为公共 API。

## 12. 属性代理

```lua
local function createHealthComponent(maxHp)
    local data = {
        hp = maxHp,
        maxHp = maxHp,
    }

    return setmetatable({}, {
        __index = function(_, key)
            return data[key]
        end,
        __newindex = function(_, key, value)
            if key == "hp" then
                data.hp = math.max(0, math.min(value, data.maxHp))
                return
            end
            error("unknown or read-only field: " .. tostring(key), 2)
        end,
        __pairs = function()
            return pairs(data)
        end,
    })
end
```

属性代理可以做校验和只读字段，但也有成本：

- 每次缺失字段都进入元方法；
- 调试器看到的是代理而非真实存储；
- 序列化和 `pairs` 需要额外支持；
- `rawget` 看到的结果与普通访问不同；
- 热路径大量属性代理可能变慢。

数据约束优先在明确的 setter 或边界校验中完成；代理适合小而稳定的抽象。

## 13. 元表保护

```lua
local meta = {
    __metatable = "protected",
}

local object = setmetatable({}, meta)

print(getmetatable(object))  -- protected
setmetatable(object, {})     -- error
```

`__metatable` 可以阻止普通 Lua 代码获取和替换真实元表。这是封装手段，不是面对不可信代码的绝对安全边界；宿主 C API、debug 能力或其他暴露接口仍可能绕过。

## 14. 弱表

默认 Table 的键和值都是强引用。元表的 `__mode` 可以创建弱键或弱值：

```lua
local cache = setmetatable({}, {
    __mode = "k",  -- weak keys
})
```

模式：

| `__mode` | 含义 |
|---|---|
| `"k"` | 弱键 |
| `"v"` | 弱值 |
| `"kv"` | 键和值都弱 |

弱引用对象如果只剩弱关联，GC 可回收它，相应条目会消失。

典型用途：

- 给原生对象附加 Lua 侧数据，但不延长原生包装器生命；
- memoization 缓存；
- 对象到代理的映射；
- 调试元数据。

不要用弱表掩盖不清楚的生命周期：

- 条目何时消失由 GC 时机决定；
- 弱值可能在下一次访问前消失；
- 字符串和某些值的回收行为有特殊细节；
- 键值互相引用时可能涉及 ephemeron 语义；
- 业务关键对象不应靠"希望它还没被 GC"保持存在。

## 15. `__gc` 与资源释放

完整 userdata 可关联 `__gc` 终结逻辑；较新 Lua 版本也扩展了相关能力。它适合最终兜底，不适合唯一的资源释放协议：

```text
Lua wrapper 不再可达
    -> 某次 GC 发现
    -> 安排/执行 __gc
    -> 释放或减少原生引用
```

问题：

- 执行时间不确定；
- 程序退出和错误路径更复杂；
- 终结逻辑可能让对象复活；
- GPU/文件/网络资源通常需要及时释放；
- 原生对象可能已由引擎销毁。

更好的方式：

```lua
handle:dispose()
handle = nil
```

框架在界面关闭、场景退出或作用域结束时显式释放，`__gc` 仅用于检测遗漏和最终兜底。

## 16. 元表和热更新

如果实例的方法通过 `__index = ClassTable` 动态查找：

```lua
function Player:update(dt)
    -- new implementation
end
```

已有实例下一次查找可能直接使用新方法，因为它们共享 `Player` Table。

但以下情况仍保存旧行为：

```lua
local cachedUpdate = player.update
local callback = function(dt)
    return cachedUpdate(player, dt)
end
```

或实例自身覆盖了方法：

```lua
player.update = oldCustomUpdate
```

热更新是否生效取决于引用图，不只是文件是否替换。旧闭包、协程栈、回调注册和复制出去的方法都需要单独处理。

## 17. 常见对象模型错误

### 17.1 忘记设置 `__index`

```lua
local Player = {}
local player = setmetatable({}, Player)

function Player:update() end
player:update()  -- 找不到 update
```

需要：

```lua
Player.__index = Player
```

### 17.2 点与冒号混用

```lua
function Player:update(dt) end
Player.update(0.016)  -- 0.016 被当作 self
```

### 17.3 所有默认值共享可变 Table

```lua
local defaults = {
    inventory = {},
}

local a = setmetatable({}, { __index = defaults })
local b = setmetatable({}, { __index = defaults })

table.insert(a.inventory, "sword")
print(#b.inventory)  -- 1，共享了同一 inventory
```

可变实例字段必须在构造时为每个对象创建：

```lua
local self = {
    inventory = {},
}
```

### 17.4 继承链过深

方法来源难追踪、字段名互相遮蔽、初始化顺序隐式。优先使用浅层继承 + 组合。

### 17.5 把元方法当安全系统

`__newindex` 只拦截缺失键，`rawset` 可绕过，宿主绑定还可能直接访问。它适合约束普通代码，不应作为对抗不可信脚本的唯一手段。

## 18. 本章小结

1. 元表定义值在缺失索引、赋值、调用、算术和比较等操作中的后备行为。
2. `__index` 是 Lua 类式对象和原型查找的核心。
3. 冒号只是传入 `self` 的语法糖，不是独立的方法类型。
4. 元表链可模拟继承，但没有静态类型、构造顺序和访问控制保证。
5. 对象系统应保持浅层，变化维度多时优先组合。
6. `rawget`/`rawset` 用于绕过元方法，元方法实现应防止递归。
7. 弱表适合缓存和映射，不适合模糊业务所有权。
8. `__gc` 是不确定时机的兜底，稀缺原生资源应显式释放。
9. 共享方法 Table 有利于热更新，但旧闭包和缓存函数仍可能保留旧代码。

[上一章：Table、函数、闭包与模块](./03-tables-functions-and-modules.md) | [返回模块总览](./README.md) | [下一章：协程、事件与状态机](./05-coroutines-events-and-state-machines.md)
