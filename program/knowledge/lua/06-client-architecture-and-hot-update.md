# Lua 客户端架构与脚本更新速记

## 运行时结构

宿主只依赖少量稳定入口，例如 `bootstrap.onUpdate/onMessage/onShutdown`；Lua 内部再组织 App、Service、Feature、Scheduler、UI Router 和 Diagnostics。

推荐依赖方向：

```text
UI / Feature -> Domain -> Script Service
                         -> Native Adapter -> Engine API
```

底层不反向依赖具体页面。Service 封装网络/资源，Domain 保持可测试规则，Panel 只处理展示与输入。依赖注入比通过 `_G` 隐藏依赖更容易测试和替换。

## 生命周期与更新循环

长期模块统一约定：

```text
create/new：建立纯 Lua 状态
start/open：订阅、创建 UI、启动任务
stop/close：解绑、取消、停止接收结果
 dispose：释放原生句柄并断开永久引用
```

关闭应幂等。列表复用要清理旧监听和异步回调；原生节点失效后 wrapper 必须可检测。

不要让每个对象注册 Update。优先使用事件/dirty flag、少量 Scheduler、按 Feature 激活注册，以及由 System/原生层批量处理同类对象。

## 四类更新对象

| 对象 | 典型策略 |
|---|---|
| 配置 | schema、版本、构建期校验 |
| 代码 | 模块替换、接口兼容检查 |
| 运行状态 | 显式迁移或重建 |
| 原生资源 | 资源系统版本、依赖和引用管理 |

把四者都叫“热更新”会掩盖真正的失败模式。

## 为什么清空 `package.loaded` 不够

它只影响后续 `require`，不会修改：旧模块 Table、缓存函数、闭包/upvalue、挂起 coroutine、实例覆盖方法、原生 callback 和旧状态。

原地 patch 保持模块 Table 身份，可更新动态查找的方法，但仍无法自动升级上述引用。长期状态应放在显式、带版本的 state 中，而不是藏在无法可靠迁移的 upvalue 里。

## 更新事务

```text
构建/测试 -> manifest/hash/signature -> 灰度下载
-> 暂存与依赖校验 -> 进入安全点
-> 隔离加载新代码 -> 迁移状态/重绑回调
-> smoke test -> 原子提交
-> 失败回滚、禁用或重启脚本域
```

安全点可选登录前、场景切换、战斗结算、页面关闭或完整脚本域重启。补丁越影响核心状态，安全点越保守。

更新系统还必须处理平台合规、服务端能力协商、配置/脚本版本配对，以及日志中记录脚本包版本。

## 确定性与错误隔离

锁步/回放需固定：Lua/数值版本、遍历顺序、随机数、时间源、原生容器顺序、异步投递顺序和序列化规则。

错误边界应与可独立降级的 Feature 一致：记录 traceback 与版本，熔断重复失败，展示 fallback；核心模块失败则受控返回登录或重启。`pcall` 不是事务，也不会恢复半写状态。

## 高频追问

1. 如何防止模块循环依赖和初始化顺序耦合？
2. 原地 patch 与重建脚本域分别适合什么场景？
3. 状态迁移如何做到可观测、幂等和可回滚？
4. 补丁应用一半失败怎么办？
5. 如何避免每帧数千 Lua callback 和跨语言调用？
6. 新脚本如何兼容旧服务器协议？
7. 平台不允许动态脚本时，架构如何退化？

项目证据应包含模块规模、依赖治理、一次补丁故障演练、回滚耗时和版本观测方式。

[上一章：协程、事件与状态机](./05-coroutines-events-and-state-machines.md) | [返回总览](./README.md) | [下一章：原生交互与生命周期](./07-native-interop-and-lifecycle.md)
