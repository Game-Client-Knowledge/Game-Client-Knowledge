# Unity 场景、资源与异步加载速记

## Scene

Single 替换当前场景集合，Additive 允许持久世界/分块叠加；Active Scene 影响新对象归属和部分环境设置，不等于唯一 Loaded Scene。

场景异步链：发起加载 → IO/反序列化 → 进度约到 0.9 等待激活（具体语义依 API）→ 激活对象/生命周期回调 → 卸载旧场景与释放资源。`allowSceneActivation=false` 可控制切换点，但长期阻塞会影响加载队列。

激活尖峰来自对象注册、Awake/OnEnable、Shader/资源上传等；异步 IO 不等于无主线程工作。

`DontDestroyOnLoad` 将根对象迁入持久场景，重复创建单例和残留订阅是常见故障。

## 资源方案

| 方案 | 适合 | 风险 |
|---|---|---|
| Inspector 引用 | 静态依赖、类型安全 | 形成硬依赖和加载闭包 |
| Resources | 小型固定资源/原型 | 全局路径、包体和卸载治理差 |
| AssetBundle | 底层打包与加载控制 | 依赖、版本、引用计数复杂 |
| Addressables | 地址、依赖、远端与异步抽象 | handle/release、目录与版本仍需治理 |

资源加载与实例化是两步；释放实例不一定释放依赖，释放 handle 也不能在仍有消费者时进行。避免混用多套所有权接口。

## 工程链

```text
Catalog/地址 -> Bundle 依赖 -> 下载/缓存/校验
-> 加载 Asset -> 实例化 -> 引用/释放
```

设计取消与晚到结果、重复请求去重、内存预算、版本回滚、磁盘缓存和网络失败。跨 Scene 引用优先 ID/服务/运行时绑定，避免持有已卸载对象。

## 高频追问

1. Active Scene 与 Loaded Scene 的区别？
2. 为什么异步场景加载仍会卡在激活？
3. Addressables handle 为什么必须成对释放？
4. AssetBundle 卸载时 Asset/实例会怎样？
5. `DontDestroyOnLoad` 单例如何避免重复和残留？
6. 如何定位加载峰值属于 IO、解压、实例化还是上传？

[上一章：协程与异步](./03-coroutines-async-and-time.md) | [下一章：输入与移动](./05-input-character-movement-and-camera.md)
