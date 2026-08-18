# Unity 对象模型速记

## 核心关系

Scene 组织运行对象；GameObject 提供身份、层级、激活和组件容器；Component 提供行为/数据；Transform 是每个 GameObject 必有的层级与空间组件。

```text
Project Asset --GUID/.meta--> Serialized Reference
Prefab/Scene 实例化 -> GameObject
GameObject -> Transform + Components
```

`.meta` 中的 GUID 保持资源引用；丢失/重建会导致引用断裂。Library 是可重建缓存，不应提交；Assets/Packages/ProjectSettings 承载项目输入。

## 托管与原生对象

`UnityEngine.Object` 常是托管 wrapper + 原生对象。`Destroy` 标记原生对象在安全时机销毁，wrapper 可能仍存在并表现为 fake-null。普通 C# `null`、Unity 重载相等和 `ReferenceEquals` 语义不同。

生命周期责任：

- `Instantiate` 创建对象图和序列化字段；
- `Awake/OnEnable/Start` 完成运行时初始化；
- `Destroy` 不等于 C# 立即释放；
- GC 回收 wrapper 不等于及时释放纹理、Mesh 等原生资源。

## Component 与查找

`GetComponent` 适合初始化/低频查找，热路径应缓存；层级搜索和全局查找更贵且隐藏依赖。`RequireComponent` 约束同对象依赖，但不能替代架构边界。

MonoBehaviour 回调由 Unity 消息分派，不是普通 C# virtual override。拼写/签名错误可能静默失效。

## Prefab 与实例

Prefab 是可复用序列化模板，实例可有 override/variant。运行时修改实例不会自动回写 Asset。实例化尖峰可能来自克隆、反序列化、Awake/OnEnable、资源依赖和渲染注册；对象池只在测量证明创建/销毁频繁时使用，并必须完整重置状态。

## 高频追问

1. GameObject 与 Component 为什么偏向组合？
2. GUID/.meta 丢失会怎样？
3. Unity fake-null 的根因是什么？
4. `Destroy` 与 `DestroyImmediate` 的适用边界？
5. Prefab 实例化为何可能卡顿？
6. 对象池可能如何制造泄漏和高常驻内存？

[返回模块](./README.md) | [下一章：生命周期](./02-monobehaviour-lifecycle-and-playerloop.md)
