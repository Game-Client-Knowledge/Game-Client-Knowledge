# Lua 原生交互与生命周期速记

## C API 栈模型

Lua 与 C 通过虚拟栈交换参数和返回值：参数按索引读取，原生把结果压栈，返回值数量由 C 函数返回值声明。包装器必须维护类型、栈容量、返回数量和错误路径的一致性。

```cpp
int Add(lua_State* L) {
    const lua_Number a = luaL_checknumber(L, 1);
    const lua_Number b = luaL_checknumber(L, 2);
    lua_pushnumber(L, a + b);
    return 1;
}
```

原生调用 Lua 必须使用受保护调用，并配置 traceback、栈守卫、线程/VM 有效性检查和脚本版本上下文。

## `lua_State` 与 VM

`lua_State*` 表示一个 Lua 执行栈。主线程与 coroutine 可有不同指针，但通常共享全局对象、registry、字符串和 GC 堆。因此 coroutine 不是独立 VM，同一 VM 也不能被多个原生线程随意并发调用。

VM 关闭前应先停止异步结果和原生回调，再释放 registry 引用与 wrapper；顺序反了就会回调已销毁世界。

## userdata、句柄与所有权

| 表示 | 特征 | 风险 |
|---|---|---|
| full userdata | Lua 管理内存，可挂元表/`__gc` | 仍要定义底层资源所有权 |
| light userdata | 裸 `void*`，无实例终结 | 悬空指针、类型混淆 |
| handle + generation | 间接查对象池并验证代数 | 多一次查找，但可可靠失效 |

绑定类型必须回答：谁创建、谁拥有、谁销毁、wrapper 是否延长生命、原生先销毁如何失效、VM 先关闭如何解绑。

常见模型：Lua 拥有并显式 dispose；原生拥有、Lua 只观察弱句柄；真实共享所有权。不要用共享引用掩盖责任不清。

## Registry 与跨 GC 引用

原生长期保存 Lua callback 时使用 registry ref，不再需要必须 `unref`。典型泄漏：

```text
C/C# listener -> registry/delegate -> Lua closure
-> Panel -> native wrapper -> listener
```

两个内存管理系统都看到对方仍在引用，必须靠 token、owner 批量解绑和 VM shutdown 协议打断。C# 事件取消订阅还必须使用同一个 delegate/wrapper 实例。

## Bridge 成本

一次调用可能包含字段查找、参数检查、编码转换、Table/容器复制、wrapper 创建、句柄验证、异常转换和引用登记。优化优先级：

1. 批量、高层 API；
2. 减少字符串与容器来回转换；
3. 快照或受控 buffer，而不是逐字段 getter；
4. 生成 wrapper，绕开反射慢路径；
5. 统计每个 API 的次数、耗时和转换字节量。

零拷贝视图必须定义底层扩容、场景卸载后的失效行为。

## Unity/AOT 与异步边界

Unity 常见链路是 Lua → C/PInvoke → C# wrapper → Unity 对象 → IL2CPP。真机还要处理 wrapper 预生成、泛型实例、delegate 桥、代码裁剪、`link.xml` 和 Unity fake-null。编辑器能跑不能证明 AOT 包可用。

工作线程不直接调用主 VM：返回安全数据到主线程队列，再校验 requestId generation、owner 与 VM 状态后恢复任务。

## 重入与错误

原生调用可能同步触发 Lua callback，使调用栈“折返”。遍历和销毁要使用快照/延迟队列，不能持有原生锁回调 Lua，callback 返回后要重新验证对象。

C++ 异常不能穿越 C ABI；Lua 错误也不能绕过需要析构的原生边界。绑定层统一转换为 Lua error、`nil,error` 或结构化 Result。

## 高频追问

1. C API 为什么使用栈，负索引表示什么？
2. full/light userdata 如何选择？
3. 为什么 handle + generation 比裸指针安全？
4. registry ref 为什么会造成 Lua 泄漏？
5. Table 转 `vector/List` 是快照还是视图，谁拥有内存？
6. 同步回调重入会破坏哪些假设？
7. IL2CPP 下为何需要代码生成和保留配置？

项目回答应给出绑定方式、最热 API、wrapper 缓存策略、对象失效协议和跨语言泄漏监控。

[上一章：客户端架构与脚本更新](./06-client-architecture-and-hot-update.md) | [返回总览](./README.md) | [下一章：性能、GC 与工程](./08-performance-gc-and-engineering.md)
