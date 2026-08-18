# Lua 语言陷阱速记

## 类型与真值

Lua 5.4 的八种基本类型：`nil`、`boolean`、`number`、`string`、`function`、`table`、`userdata`、`thread`。值有类型，变量没有固定静态类型；`thread` 指 coroutine。

只有 `false` 和 `nil` 为假，`0`、空字符串、空 Table 都为真。`and` / `or` 返回操作数本身，因此 `cond and a or b` 在 `a` 为 `false/nil` 时不能模拟三目运算。

## `nil` 的双重语义

- 不存在的键读取为 `nil`；
- 给 Table 键赋 `nil` 等于删除键；
- 因而“未提供”和“显式清空”需要哨兵值区分；
- 多返回值或可变参数中若需保留中间 `nil`，要显式记录数量。

```lua
local NULL = {}
local patch = { title = NULL }

local function pack(...)
    return { n = select("#", ...), ... }
end
```

## 数值与字符串

| 主题 | 复习结论 |
|---|---|
| number | 5.3/5.4 通常区分 integer/float；5.1/LuaJIT 常见双精度模型，协议前先确认 |
| `//` / 位运算 | 属于较新版本能力，不能复制到 5.1/LuaJIT |
| 负数除法 | `//` 向下取整，不等同于某些语言的向零截断 |
| 浮点 | 容差取决于量级；锁步还要处理跨平台确定性 |
| string | 不可变、二进制安全；循环拼接会产生中间对象 |
| `#text` | 返回字节长度，不是 Unicode 字符或字形数量 |

大量片段使用 `table.concat`；UI 文本只在数据变化时格式化。

## 作用域、赋值与返回值

- 默认赋值通常写入环境全局；业务代码应默认 `local`。
- 多重赋值先计算右侧，再统一赋值，因此 `a, b = b, a` 安全。
- 函数参数少了补 `nil`，多了丢弃，公共边界需主动校验。
- 只有表达式列表**最后一个**函数调用自然展开全部返回值；括号会收缩为一个值。

```lua
local a, b, c = values()      -- 全部展开
local x, y = values(), 9      -- values() 只取第一个
local one = (values())        -- 只取第一个
```

## 点、冒号与回调

核心等价式：

```text
object:method(a, b)
== object.method(object, a, b)
```

把 `object.method` 单独传给事件系统会丢失接收者。要么闭包绑定 `self`，要么让事件 API 显式接受 `owner + method`。

## 错误边界

- `error/assert` 表示程序错误或不变量失败，不应代替普通业务结果。
- `pcall` 捕获 Lua 错误，但不提供 traceback、事务回滚、超时或资源清理。
- `xpcall` 配合受控 traceback 适合主循环、消息分派、Feature 启动等隔离边界。
- 错误边界之后必须定义降级、熔断和状态恢复，而不只是打印日志。

## 版本与语义速查

| 易错说法 | 正确结论 |
|---|---|
| “0 是假” | 0 为真 |
| “`nil` 是空字段” | 赋值 `nil` 会删除键 |
| “`!=` 表示不等” | Lua 使用 `~=` |
| “多返回值总会展开” | 位置决定展开数量 |
| “`pcall` 能保证安全” | 只捕获可传播到该边界的 Lua 错误 |
| “5.4 语法所有项目可用” | 必须核对实际 VM 与绑定 |
| “`<const>` 冻结 Table” | 只限制变量重新绑定，不递归冻结对象 |

## 高频追问

1. 为什么 `0 or default` 不会得到 default？
2. 如何区分字段缺失与显式清空？
3. `local a, b = f(), 9` 中 `f` 的第二个返回值去哪了？
4. 从 Lua 5.1 迁移到 5.4，数值、环境、标准库和绑定要检查什么？
5. 你们如何在 CI 中发现未声明全局和类型拼写错误？

[上一章：客户端边界](./01-why-lua-in-game-clients.md) | [返回总览](./README.md) | [下一章：Table、闭包与模块](./03-tables-functions-and-modules.md)
