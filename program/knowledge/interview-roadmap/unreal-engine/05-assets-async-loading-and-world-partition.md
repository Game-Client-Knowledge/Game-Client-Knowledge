# UE 资源、异步加载与大世界速记

## Asset、Package 与引用

Asset 通常存于 Package（`.uasset/.umap`）。硬引用使依赖进入加载/Cook 闭包；软引用（`TSoftObjectPtr/TSoftClassPtr`）保存路径，解析后仍需显式异步加载和生命周期管理。

同步加载可能在 Game Thread 触发 IO、反序列化、依赖和对象创建，造成 hitch。异步加载完成回调也要验证 owner/World，并管理 `FStreamableHandle`；取消不保证底层 IO 立即停止。

## Asset Manager

Primary Asset 可被 Asset Manager 直接标识与规则管理，Secondary Asset 随依赖被引用。PrimaryDataAsset 常用于带类型/ID 的配置；Asset Bundle 为同一 Primary Asset 定义场景化依赖集合，不等于磁盘 AssetBundle 文件。

```text
PrimaryAssetId -> Asset Manager Rule/Bundle
-> Streamable Manager -> Async Package
-> UObject 创建 -> Handle/引用 -> Unload
```

Asset Registry 查询元数据和依赖，不要求加载全部对象。Reference Viewer/Size Map 用于发现意外硬引用和依赖体积，但最终内存/加载以运行时 trace 为准。

## World Streaming

Level Streaming 管显式关卡；World Partition 将世界划分网格并按 Streaming Source 加载 Cell。Data Layer 表达逻辑分组/状态，HLOD 合并远景代理降低 Draw/对象量。

跨 Cell 硬引用会破坏独立流送。持久对象使用稳定 ID/Subsystem/软引用，不能长期保存可能卸载 Actor 裸引用。

大世界指标：Cell 加载/激活 ms、IO 带宽、内存峰值、Actor 数、HLOD 切换、源移动速度和最坏传送场景。

## Cook 与版本

Cook 将编辑器内容转换为目标平台数据；缺失引用/规则会导致开发环境可用而包体缺资源。补丁需处理 IoStore/Pak、Asset Registry、版本、哈希、依赖、磁盘缓存与回滚。

## 高频追问

1. 硬引用如何导致启动/包体/内存膨胀？
2. 软引用与弱指针的差异？
3. Streamable Handle 为什么不能随意丢弃？
4. Primary/Secondary Asset 和 Bundle 如何协作？
5. World Partition、Data Layer、HLOD 各解决什么？
6. 编辑器能加载但 Cook 包缺资源如何排查？
7. 传送导致流送尖峰如何治理？

[上一章：Blueprint/C++](./04-blueprints-and-cpp-collaboration.md) | [下一章：输入、动画与 AI](./06-input-character-animation-and-ai.md)
