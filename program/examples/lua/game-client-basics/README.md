# Lua 游戏客户端基础示例

## 示例目标

这个小程序把 Lua 基础语法放进一个简化客户端循环中，演示：

1. 模块通过 `require` 返回公开 Table；
2. 元表和冒号方法构造类式对象；
3. 事件总线注册、发射和注销监听；
4. 状态机执行 `enter/update/exit`；
5. coroutine 调度跨帧任务；
6. owner 生命周期结束时批量清理监听和任务。

对应原理见 [Lua 基础与游戏客户端应用](../../../knowledge/lua/README.md)。

## 文件

| 文件 | 内容 |
|---|---|
| `main.lua` | 固定时间步客户端循环 |
| `event_bus.lua` | 支持 token 和 owner 清理的事件总线 |
| `state_machine.lua` | 带重入保护的有限状态机 |
| `scheduler.lua` | coroutine 时间调度器 |

## 环境

- Lua 5.1 或更新版本；
- 或兼容 Lua 5.1 语法的 LuaJIT；
- 不依赖第三方库。

## 运行

在本目录执行：

```bash
lua main.lua
```

LuaJIT：

```bash
luajit main.lua
```

## 预期输出

```text
== Lua game client basics ==
state enter: idle
hp changed: 100 -> 72
frame 1
quest: start
frame 2
state exit: idle
state enter: moving
frame 3
moving distance: 1.0
quest: dialogue
frame 4
moving distance: 2.0
frame 5
moving distance: 3.0
state exit: moving
state enter: arrived
quest: complete
active tasks: 0
```

## 值得继续实验

1. 在事件回调中注销自己，观察快照如何保持本次发射稳定。
2. 给 Scheduler 增加"等待事件"而不只是等待秒数。
3. 在状态机 `enter` 中直接再次转移，观察重入保护。
4. 删除 `scope` 的清理调用，模拟监听和任务泄漏。
5. 让 task 主动报错，并为调度器加入 traceback 和错误上报。
