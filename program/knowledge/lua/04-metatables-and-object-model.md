# 元表与对象模型速记

## 元表解决什么

元表为值无法直接完成的操作提供后备规则。高频元方法包括 `__index`、`__newindex`、`__call`、`__tostring`、算术/比较、`__len`、`__pairs`、`__gc`。

它不是 C++ 虚表：元表本身是运行时 Table，查找规则动态、约束弱，没有静态类型、固定槽位、对象布局和构造/析构保证。

## `__index` 查找链

```text
instance.key
-> instance 自身
-> instance metatable.__index
   -> Table：继续查该 Table
   -> function：调用并返回结果
```

类式对象的最小结构：

```lua
local Player = {}
Player.__index = Player

function Player.new(name)
    return setmetatable({ name = name }, Player)
end

function Player:update(dt) end
```

实例保存状态，方法由 `Player` 共享。`player:update(dt)` 仍只是自动传入 `self` 的语法糖。

## 写入、原始访问与代理

- `__newindex` 通常只拦截目标 Table 中**尚不存在**的键；已有键直接写入。
- 无条件代理应把真实数据放在另一张 Table，代理本身保持无字段。
- 元方法内部用 `rawget/rawset` 避免再次触发自身；业务代码大量使用它们通常是在绕过对象合同。
- 属性代理可做校验和延迟计算，但会增加查找、调试、遍历和序列化成本，不宜滥用在热路径。

## 继承、组合与默认值

元表链可模拟继承：实例 → 派生方法表 → 基类方法表。调用基类实现要写 `Base.method(self)`，而不是 `Base:method()`。

工程建议：

- 继承层级保持浅；多变化维度优先组合策略/组件；
- 构造、open/close、dispose 都是框架约定，Lua 不会自动执行；
- 可变默认 Table 必须为每个实例创建，不能挂在共享原型上；
- 元方法行为应符合操作直觉，避免把有副作用的业务藏进普通字段读取。

## 弱表、终结与所有权

| 机制 | 适用 | 不能解决 |
|---|---|---|
| 弱键/弱值表 | 缓存、wrapper 映射、附加元数据 | 关键业务对象的确定生命周期 |
| `__gc` | userdata 最终兜底、遗漏检测 | GPU/文件/网络资源的及时释放 |
| `__metatable` | 普通代码的封装 | 对抗 debug、C API 或不可信宿主能力 |

稀缺原生资源应显式 `dispose`，wrapper 失效后所有调用都应可诊断地失败。GC 时机不能承担业务正确性。

## 元表与热更新

实例通过共享 Class Table 动态找方法时，原地替换方法可影响旧实例；以下内容仍保留旧代码：

- 已缓存到局部变量的函数；
- 闭包中的旧函数/upvalue；
- 挂起 coroutine 栈；
- 实例自身覆盖的方法；
- 已注册进原生层的 callback。

因此“类表可热更”只是部分条件，完整方案仍需引用图和状态迁移。

## 高频追问

1. `__index` 为 Table 与函数分别如何执行？
2. 为什么 `__newindex` 不能天然实现只读对象？
3. `rawget/rawset` 的用途和风险是什么？
4. 元表继承与 C++ 继承缺少哪些保证？
5. 弱表条目何时消失，为什么不能依赖其时机？
6. 现有实例在补丁后何时会调用新方法？

回答项目题时补充：对象模型层级、实例量、方法查找热点、生命周期协议，以及一次旧回调/共享默认值问题的治理证据。

[上一章：Table、闭包与模块](./03-tables-functions-and-modules.md) | [返回总览](./README.md) | [下一章：协程、事件与状态机](./05-coroutines-events-and-state-machines.md)
