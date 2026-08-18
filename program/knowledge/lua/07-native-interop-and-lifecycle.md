# Lua 原生交互与对象生命周期

## 1. 为什么 Lua 能调用引擎

Lua VM 本身不知道 `GameObject`、`UObject`、纹理或网络连接。宿主通过 C API 或绑定框架把原生能力注册进 Lua：

```text
原生类型/函数
    -> 绑定描述或代码生成
    -> 包装函数
    -> 注册到 Lua 环境
    -> Lua 通过 Table/userdata 调用
```

Lua 侧：

```lua
local texture = assets:loadTexture("ui/avatar.png")
image:setTexture(texture)
```

背后可能经过几十步检查和转换。脚本看到的简单 API 是绑定层努力工作的结果。

## 2. Lua C API 的栈模型

Lua C API 使用一个虚拟栈交换参数和返回值。Lua 调用：

```lua
local result = native.add(10, 20)
```

C 包装函数概念代码：

```cpp
int Add(lua_State* L) {
    const lua_Number a = luaL_checknumber(L, 1);
    const lua_Number b = luaL_checknumber(L, 2);

    lua_pushnumber(L, a + b);
    return 1;
}
```

调用时栈：

```text
进入 Add:
+------------------+
| index 2: 20      | <- top / -1
+------------------+
| index 1: 10      |        -2
+------------------+

push result:
+------------------+
| index 3: 30      | <- top
+------------------+
| index 2: 20      |
+------------------+
| index 1: 10      |
+------------------+

return 1 表示最上方一个值是返回值
```

正索引从当前调用帧底部开始，负索引从栈顶倒数。包装函数必须：

- 检查参数数量和类型；
- 确保栈容量；
- 按正确顺序压入返回值；
- 返回返回值数量；
- 在错误路径保持栈约定。

栈不平衡可能让错误出现在距离根因很远的下一次调用。

## 3. 从原生调用 Lua

概念代码：

```cpp
lua_getglobal(L, "onMessage");
lua_pushstring(L, "connected");

const int status = lua_pcall(L, 1, 0, 0);
if (status != LUA_OK) {
    const char* message = lua_tostring(L, -1);
    LogLuaError(message);
    lua_pop(L, 1);
}
```

调用链：

```text
原生事件
    -> 把 Lua 函数压栈
    -> 压入参数
    -> lua_pcall
    -> Lua 执行
    -> 返回结果或错误对象
    -> 原生恢复栈
```

生产代码通常加入：

- traceback 错误处理函数；
- 栈守卫；
- 脚本版本和上下文日志；
- 每帧调用耗时；
- VM 是否仍有效的检查；
- 主线程断言。

## 4. `lua_State` 不只是"一个普通状态变量"

`lua_State*` 表示一个 Lua 执行线程/栈。主线程和 coroutine 可以拥有不同 `lua_State*`，但同一 Lua 全局状态中的 coroutine 共享：

- 全局对象；
- registry；
- 字符串；
- GC 管理的堆；
- 大部分运行时配置。

因此：

- coroutine 的 `lua_State*` 不等于独立 VM；
- 不能把任意 `lua_State*` 无同步地交给多个原生线程同时调用；
- VM 关闭后，所有关联栈和 registry 引用都失效；
- 绑定层必须知道对象属于哪个 VM。

## 5. Lua 值如何表示原生对象

### 5.1 完整 userdata

由 Lua 管理的一块原始内存，可关联元表：

```text
Lua userdata
+--------------------------+
| NativeHandle / pointer   |
| generation / type id     |
+--------------------------+
        |
        v
原生对象池或引擎对象
```

Lua 5.4 使用 `lua_newuserdatauv` 等 API；旧版本 API 名称不同。

优点：

- 可挂元表和方法；
- 可参与 GC；
- 可保存句柄、代数和额外状态；
- 易于做类型检查。

### 5.2 light userdata

本质上是一个裸 `void*` 值：

- 不由 Lua 拥有内存；
- 没有每实例元表和 `__gc`；
- 适合身份 token 或宿主完全控制的指针；
- 容易出现悬空指针和类型混淆。

公共绑定通常更适合完整 userdata 或安全句柄。

### 5.3 句柄优于裸指针

```text
handle = { index = 42, generation = 7 }
```

每次调用检查对象池中：

- index 是否有效；
- generation 是否匹配；
- 对象是否已销毁；
- 类型是否正确；
- VM/世界是否一致。

对象槽位复用时 generation 改变，旧 Lua wrapper 不会误操作新对象。裸指针虽然省一次查找，却可能在对象销毁后变成通往未定义行为的快捷通道。

## 6. 对象所有权模型

绑定 API 必须回答：

| 问题 | 可能答案 |
|---|---|
| 谁创建？ | Lua 请求原生工厂、引擎场景、资源系统 |
| 谁拥有？ | Lua wrapper、引擎 World、引用计数资源管理器 |
| 谁销毁？ | 显式 `dispose`、场景卸载、引用归零 |
| Lua wrapper 是否延长生命？ | 强引用、弱引用或不延长 |
| 原生对象先销毁怎么办？ | 句柄失效，后续调用返回错误 |
| VM 先关闭怎么办？ | 解除回调和 registry 引用 |

常见模型：

### 6.1 Lua 拥有

Lua wrapper 的终结或显式 `dispose` 销毁原生对象。适合独立小资源，但必须保证释放发生在正确线程和模块。

### 6.2 原生拥有，Lua 观察

场景或 World 控制对象生命，Lua 只持有弱句柄。对象销毁后调用应得到可诊断错误：

```lua
if player:isValid() then
    player:setPosition(x, y, z)
end
```

仍应让大部分 API 在内部验证，不能只依赖调用者先问 `isValid`，因为检查后对象也可能因重入失效。

### 6.3 共享所有权

Lua wrapper 增加原生引用计数。方便但容易：

- 延长场景对象生命；
- 与 C# delegate/事件形成环；
- 让资源释放时间依赖 Lua GC；
- 在多个 VM 间混淆引用。

共享所有权必须是真实业务语义，不应作为"暂时不知道谁负责"的默认选择。

## 7. Registry 引用

原生层需要长期保存 Lua 回调时，不能只记住临时栈索引。常用 registry 引用：

```cpp
lua_pushvalue(L, callbackIndex);
const int ref = luaL_ref(L, LUA_REGISTRYINDEX);

// 调用时
lua_rawgeti(L, LUA_REGISTRYINDEX, ref);

// 不再需要
luaL_unref(L, LUA_REGISTRYINDEX, ref);
```

忘记 `unref` 会让 Lua 对象一直可达：

```text
Lua registry
    -> callback closure
    -> Panel self
    -> 原生 UI wrapper
    -> 更多资源
```

反方向若 Panel 保存原生 listener，而原生 listener 又保存 registry 回调，就可能形成跨 GC 系统环。两个 GC/引用计数器都只看到"对方还在引用"，谁也不主动放手。

解决方式：

- 订阅返回 token，生命周期结束显式解绑；
- owner 销毁时批量释放 registry refs；
- VM shutdown 前先停止所有原生回调；
- 使用弱句柄时定义失效返回；
- 建立跨语言引用统计和泄漏报告。

## 8. Lua/C++ 绑定方式

### 8.1 手写 C API

优点：

- 控制精确；
- 无额外模板/反射层；
- 错误和所有权策略完全可定制；
- 适合少量稳定核心 API。

缺点：

- 代码量大；
- 栈操作容易出错；
- 重载、继承和异常处理繁琐；
- 接口变化要手动同步。

### 8.2 模板绑定库

例如项目可能使用 sol2、LuaBridge 或自研模板层。优点是声明简洁、类型转换自动；缺点是：

- 模板编译时间和错误信息；
- 隐藏分配和转换；
- 库版本与 Lua ABI 兼容；
- 异常、yield 和生命周期策略仍需理解。

### 8.3 代码生成

从注解、IDL 或反射元数据生成包装器：

- 接口一致；
- 可生成参数检查和文档；
- 适合大量引擎 API；
- 可针对 AOT 平台提前生成代码。

生成器仍需要版本控制和测试。自动生成错误只是以工业化速度生成错误。

## 9. Lua/C# 与 Unity 常见链路

典型流程：

```text
Lua VM
    -> C binding / PInvoke
    -> C# wrapper
    -> Unity managed object
    -> IL2CPP/AOT generated native code
```

可能涉及：

- Lua number/string/Table 到 C# 类型转换；
- C# object 包装为 userdata；
- 反射或生成 wrapper；
- delegate 适配为 Lua closure；
- exception 转成 Lua error；
- Unity fake-null 与真实 CLR 引用差异；
- IL2CPP stripping 和泛型实例化；
- 主线程对象访问限制。

### 9.1 AOT 与代码裁剪

IL2CPP 平台不能依赖运行时随意生成机器码。绑定框架通常要求：

- 预生成 wrapper；
- 保留反射访问类型；
- 声明泛型实例；
- 为 delegate 签名生成桥接；
- 配置 link.xml 或等价保留规则。

编辑器 Mono 环境能运行，不代表 AOT 真机一定拥有所需代码。

### 9.2 C# 事件与 delegate

Lua 订阅 C# 事件时，必须保存可用于取消订阅的同一个 delegate/wrapper：

```lua
self.onClick = function()
    self:handleClick()
end

button.onClick:AddListener(self.onClick)

-- 销毁时
button.onClick:RemoveListener(self.onClick)
self.onClick = nil
```

如果每次取消时重新创建匿名函数，它不是原监听对象，解绑可能失败。

## 10. 参数转换与封送成本

跨语言调用可能包括：

- 查找函数或属性；
- 检查参数数量；
- 类型判断；
- 数值精度转换；
- UTF-8/UTF-16 字符串转换；
- Table 遍历并创建数组/字典；
- 包装或查找对象代理；
- 异常边界；
- 返回值压栈；
- GC write barrier 或引用登记。

尤其昂贵的模式：

```lua
for i = 1, #entities do
    local entity = entities[i]
    local x, y, z = entity.transform:getPosition()
    entity.transform:setPosition(x + dx, y, z)
end
```

改为：

```lua
native.movement:translateBatch(entityIds, dx, 0, 0)
```

或者原生层暴露一次快照，Lua 在纯脚本中处理后一次提交。边界设计比在包装函数里省两条指令更重要。

## 11. Table 与原生容器

把 Lua Table 转为 `std::vector`/C# `List` 可能每次复制全部元素。API 应明确：

- 是快照还是视图；
- 索引从 0 还是 1；
- 谁拥有内存；
- 调用后视图是否失效；
- 元素类型如何校验；
- 是否允许保留引用；
- 大数据能否批量/流式传输。

高频数据可使用：

- userdata buffer；
- 原生数组视图；
- 批量命令；
- 序列化字节块；
- ECS query handle；
- 一次性快照。

零拷贝视图性能好，但生命周期最危险：底层 vector 扩容或场景卸载后，旧视图必须可靠失效。

## 12. 错误跨边界

### 12.1 原生错误转 Lua

可选择：

- 抛 Lua error；
- 返回 `nil, error`；
- 返回结构化 Result；
- 记录并禁用功能。

公共约定应一致：

```lua
local asset, err = assets:load(path)
if not asset then
    return nil, {
        code = "asset_not_found",
        path = path,
        detail = err,
    }
end
```

### 12.2 Lua 错误转原生

原生层应使用受保护调用，记录：

- Lua traceback；
- 模块和入口；
- 脚本包版本；
- 原生调用上下文；
- 玩家/场景阶段；
- 是否可以降级。

C++ 异常不能随意穿过 C ABI；绑定层必须捕获并转换。反过来，Lua longjmp 风格错误也不能跳过需要正常析构的错误边界，具体绑定要遵循对应 Lua API 约束。

## 13. 回调重入

```text
Lua 调用 native.destroyObject()
    -> 原生销毁对象
    -> 原生立即触发 onDestroyed 回调
    -> Lua 回调修改当前正在遍历的对象列表
    -> 返回原 Lua 调用
```

这是同步重入。代码还没从原生函数返回，Lua 世界已经发生变化。

防护方式：

- 明确哪些 API 会同步回调；
- 销毁和事件采用延迟队列；
- 遍历使用快照或结构变更缓冲；
- 关键操作设置状态机，拒绝非法重入；
- 不在持有原生锁时回调 Lua；
- 回调后重新验证对象和容器状态。

跨语言边界最大的风险有时不是慢，而是"你以为调用栈是直线，它其实折回来按了电梯"。

## 14. 多线程与异步

通常模型：

```text
Lua 主线程发请求
    -> 原生生成 requestId
    -> 工作线程执行
    -> 线程安全结果队列
    -> 主线程帧开始取结果
    -> 恢复 Lua task / 发事件
```

规则：

- 工作线程不直接操作主线程 Lua VM；
- 结果对象跨线程传输前转为安全数据；
- VM 关闭时取消或丢弃未完成请求；
- requestId 带 generation，避免复用串单；
- 回调到达时重新检查 owner；
- 取消不一定能中断底层工作，但可以阻止结果进入已销毁上下文。

## 15. API 设计原则

### 15.1 粗粒度

暴露 `playSkill(skillId, targetId)`，而不是让 Lua 分别调用二十个底层步骤。

### 15.2 窄接口

只暴露脚本需要的能力，避免把整个引擎对象图直接开放。

### 15.3 明确所有权

命名和文档说明 create/get/borrow/retain/dispose 的差异。

### 15.4 稳定数据合同

避免直接暴露容易改变的内部 C++ 类型和 STL 容器。

### 15.5 可观测

统计调用次数、总耗时、最大耗时、参数转换量和错误数。

### 15.6 可失效

wrapper 能检测原生对象销毁，并给出包含类型和 handle 的错误，而不是崩溃。

## 16. 本章小结

1. Lua C API 用栈交换参数和返回值，包装函数必须维护严格栈约定。
2. coroutine 的 `lua_State*` 通常共享同一全局状态，不等于独立 VM。
3. 完整 userdata 可关联元表和 GC；light userdata 只是裸指针值。
4. 句柄 + generation 比长期暴露裸指针更安全。
5. 每个绑定类型必须明确所有权、销毁者和失效行为。
6. registry 回调忘记 `unref` 会造成跨语言泄漏。
7. Unity/IL2CPP 绑定要考虑 AOT、代码裁剪、delegate 和主线程限制。
8. 跨语言成本主要来自检查、封送、包装和引用管理，应使用粗粒度批量 API。
9. 同步回调会导致重入，异步结果必须回到正确线程并重新验证生命周期。

[上一章：客户端架构与脚本更新](./06-client-architecture-and-hot-update.md) | [返回模块总览](./README.md) | [下一章：性能、GC 与工程实践](./08-performance-gc-and-engineering.md)
