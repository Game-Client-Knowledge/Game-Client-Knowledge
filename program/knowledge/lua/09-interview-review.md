# Lua 面试冲刺清单

## 一分钟主回答

> Lua 在客户端中通常是嵌入式业务脚本层。宿主创建 VM 并通过 Bridge 暴露窄接口，Lua 负责 UI、活动、任务、技能和跨帧流程，原生层负责渲染、物理、批量数据与平台能力。语言核心是 Table、闭包、元表和 coroutine；工程核心是边界、生命周期和可观测性。Table 的空洞、`pairs` 顺序、闭包引用、coroutine 取消都可能影响正确性。跨语言调用还包含检查、封送、wrapper 和句柄验证，应使用粗粒度批量 API。GC 只回收不可达对象，事件、timer、registry 和 delegate 仍会保活对象。脚本更新也必须处理旧闭包、挂起栈、状态迁移、安全点和回滚。

## 高频题速答

| 问题 | 回答抓手 |
|---|---|
| Lua 有哪些类型？ | 8 类；`thread` 是 coroutine；值有类型、变量无固定类型 |
| 哪些值是假？ | 只有 `false` 和 `nil` |
| Table 是什么？ | 语义是映射；连续整数键约定为序列，数组/哈希分区是实现细节 |
| `#` / `ipairs` / `pairs`？ | 空洞时长度不可靠；`ipairs` 遇 nil 停；`pairs` 无序 |
| 点与冒号？ | `a:b(x) == a.b(a,x)`，回调提取时易丢 self |
| 闭包/upvalue？ | 函数捕获词法变量；全局 callback 可保活整张对象图 |
| `__index`？ | 缺失读取的后备规则，可指向 Table 或函数 |
| coroutine 与线程？ | 协作式、默认串行、不提供并行或抢占 |
| C API 为什么用栈？ | 统一跨类型系统的参数/返回协议 |
| userdata？ | full 可挂元表/GC；light 是裸指针值；引擎对象更宜安全句柄 |
| Bridge 为什么贵？ | 查找、检查、封送、复制、wrapper、引用和错误转换 |
| GC 卡顿怎么治理？ | 先控分配和根引用，再按版本/负载调步进；目标机看峰值 |
| GC 语言会泄漏吗？ | 会，可达但业务无用；还可能存在原生资源泄漏 |
| 热更为何不等于 reload？ | 旧 Table、函数、upvalue、coroutine、callback 和状态仍存在 |
| Lua 能做锁步吗？ | 可以，但必须固定版本、数值、顺序、随机、时间和原生返回 |
| `pcall` 安全吗？ | 只捕错，不提供超时、回滚、资源清理或可信隔离 |

## 场景题回答骨架

### UI 页面生命周期

```text
create 注入服务
-> open 绑定节点/事件/task
-> dirty refresh
-> close 取消请求、timer、coroutine、listener
-> dispose 释放 native handle、断引用
```

补充重复关闭幂等、列表项复用、异步晚到、wrapper 失效和泄漏计数。

### 内存持续增长

1. 固定路径重复打开关闭；
2. 同时记录 Lua heap、listener、task、registry、wrapper 和原生对象；
3. 诊断安全点 GC 后判断对象是否仍可达；
4. 查 EventBus/Timer/coroutine/delegate/资源回调；
5. wrapper 降而原生不降则查所有权/dispose；
6. 自动循环回归并比较峰值。

### 脚本更新系统

```text
构建测试 -> manifest/hash/signature -> 下载暂存
-> 版本/依赖校验 -> 安全点 -> 隔离加载
-> patch/rebuild + 状态迁移 + callback/task 处理
-> smoke test -> 提交或回滚 -> 版本观测
```

同时说明平台合规、协议兼容、灰度与失败恢复。

### 性能优化

先用发布真机定位 VM、分配、GC、Bridge 还是原生函数；建立 P95/P99 与调用量基线。优先减少每帧对象和边界次数，再验证正确性、内存峰值和回归场景。

## 容易失分的说法

| 说法 | 缺失内容 |
|---|---|
| “Lua 就是热更新” | 嵌入、业务编排、平台限制和状态迁移 |
| “Table 就是哈希表” | 序列语义与实现边界 |
| “协程是轻量线程” | 协作式、串行与调度责任 |
| “GC 不会泄漏” | 根引用和跨语言资源 |
| “Lua 慢，改 C++” | 数据、Profiler 和边界设计 |
| “清 package.loaded 即可” | 旧引用、栈和运行状态 |
| “加密后客户端可信” | 客户端权威边界没有改变 |

## 面试前自检

- [ ] 每个核心机制能先用一句话下定义，再解释边界。
- [ ] 能画出 VM、Bridge、原生对象和 GC 根的关系。
- [ ] 能给出至少一个取消竞态、泄漏或性能问题的真实排查过程。
- [ ] 能报出目标机、脚本耗时、分配或调用量，而非只说“优化明显”。
- [ ] 能说明没采用的备选方案及原因。
- [ ] 不确定版本细节时会先声明运行时，不混用 5.1、LuaJIT 与 5.4。

## 延伸资料

- [Lua 5.4 Reference Manual](https://www.lua.org/manual/5.4/)
- [Programming in Lua](https://www.lua.org/pil/)
- 项目实际 VM、绑定框架、AOT 和平台发布文档

[上一章：性能、GC 与工程](./08-performance-gc-and-engineering.md) | [返回总览](./README.md) | [返回面试路线](../interview-roadmap/foundations/01-language-runtime-and-data-structures.md)
