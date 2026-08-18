# Blueprint 与 C++ 协作速记

## 定位

Blueprint 是基于反射类型和 Blueprint VM 的可视化逻辑/资产系统，适合配置、表现、关卡编排和高频迭代。C++ 适合稳定框架、复杂算法、批量热路径、网络权威和底层资源生命周期。

推荐边界：**C++ 定义安全窄接口与默认行为，Blueprint 配置数据并扩展表现。** 不是“全部蓝图”或“全部 C++”。

## 通信方式

| 方式 | 适合 | 风险 |
|---|---|---|
| 直接引用 | 明确一对一依赖 | 硬引用/生命周期耦合 |
| Cast | 已知具体类型的局部判断 | 到处 Cast 暴露边界差 |
| Blueprint Interface | 多类型同合同 | 无状态，调用链仍需治理 |
| Event Dispatcher | 一对多通知 | 解绑、顺序、重入 |
| Component/Subsystem | 复用能力/服务 | 生命周期选择错误会万能化 |

Event 表达入口，Function 有返回与局部调用，Macro 是图展开；Pure Node 可能被多次求值，不应隐藏昂贵或有副作用逻辑。Construction Script 在编辑器频繁执行，保持幂等。

## C++ 暴露

`UPROPERTY` 控制编辑/读取/写入/序列化等；避免把内部可变状态无约束公开。`UFUNCTION(BlueprintCallable/Pure)` 设计粗粒度、可验证 API。

- `BlueprintImplementableEvent`：C++ 声明，Blueprint 实现。
- `BlueprintNativeEvent`：C++ 提供 `_Implementation` 默认实现，Blueprint 可覆盖。
- Blueprint Function Library 适合无状态通用函数，不应变成全局服务仓库。

## 性能与维护

Blueprint VM 单次开销通常不是首要问题，真正风险常是高频 Tick、跨边界细粒度调用、遍历/Spawn、Pure Node 重算和隐藏硬引用。先用 Insights/Profiler 定位再下沉。

维护措施：限制图规模、职责单一、数据/表现分离、接口与 Dispatcher、命名/注释、自动测试、避免深层 Blueprint 继承。Latent/Async 节点必须绑定 World/owner 生命周期并处理取消。

## 高频追问

1. Event、Function、Macro 的差异？
2. Interface 与 Dispatcher 如何选择？
3. Pure Node 为什么可能重复执行？
4. `BlueprintImplementableEvent` 与 `BlueprintNativeEvent` 的差异？
5. 怎样避免 Blueprint 形成硬引用加载链？
6. 什么证据支持把逻辑下沉 C++？
7. 如何让策划扩展表现而不破坏服务器权威？

[上一章：Gameplay Framework](./03-gameplay-framework-and-game-loop.md) | [下一章：资源与大世界](./05-assets-async-loading-and-world-partition.md)
