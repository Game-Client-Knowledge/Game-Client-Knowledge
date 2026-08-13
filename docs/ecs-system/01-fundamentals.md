# ECS 基本原理

## 1. ECS 解决什么问题

传统面向对象设计通常把数据和行为放在同一个对象中：

```text
Player
├── position
├── velocity
├── health
├── move()
├── attack()
└── render()
```

系统规模较小时，这种模型直观且易于实现。随着对象种类增加，常见问题会逐渐出现：

- 继承层级越来越深，父类很难准确表达所有差异。
- 多个对象拥有相似能力，但无法自然复用同一份行为。
- 对象包含大量并非始终需要的数据。
- 数据分散在内存各处，批量更新时缓存命中率较低。
- 行为之间的执行顺序和依赖关系隐藏在对象调用中。

ECS 的核心做法是将“对象”改成“数据组合”，再由独立系统统一处理这些数据。

## 2. 三个基本角色

### 2.1 Entity：标识

Entity 通常只是一个唯一 ID：

```text
Entity = 1001
```

Entity 本身不保存业务数据，也不实现业务行为。它的作用是把属于同一对象的组件关联起来。

工程实现中，Entity 常由两部分组成：

```text
Entity = index + generation
```

- `index`：定位实体槽位。
- `generation`：识别已销毁实体的旧引用，避免 ID 复用导致误访问。

#### 2.1.1 generation 的具体作用

可以把 `generation` 理解为实体槽位的“版本号”。假设实体管理器中有一个编号为 42 的槽位：

```text
Entity A = (index: 42, generation: 3)

槽位 42：
generation = 3
alive = true
```

其他组件或系统可能保存了 Entity A 的引用 `(42, 3)`。当 Entity A 被销毁时，管理器回收槽位 42，并增加它的 generation：

```text
销毁 Entity A

槽位 42：
generation = 4
alive = false
```

之后创建 Entity B 时，可以复用槽位 42：

```text
Entity B = (index: 42, generation: 4)

槽位 42：
generation = 4
alive = true
```

此时，Entity A 和 Entity B 的 `index` 相同，但 `generation` 不同：

| Entity | index | generation | 状态 |
|---|---:|---:|---|
| Entity A | 42 | 3 | 已销毁 |
| Entity B | 42 | 4 | 当前有效 |

访问实体时，管理器同时检查两个值：

```text
function isAlive(entity):
    slot = slots[entity.index]
    return slot.alive
        and slot.generation == entity.generation
```

使用旧引用 `(42, 3)` 访问时，槽位当前 generation 是 4，校验失败，管理器可以安全地拒绝访问。

如果 Entity 只有 `index`，旧引用 42 会误以为新创建的 Entity B 就是原来的 Entity A，进而读取或修改 Entity B 的组件。这类问题通常称为 **陈旧句柄（stale handle）** 问题。

因此：

```text
index      解决“数据存在哪里”
generation 解决“这个槽位中的对象还是不是原来的对象”
```

### 2.2 Component：数据

Component 是一类结构化数据，通常不包含复杂业务方法：

```text
Position { x, y }
Velocity { x, y }
Health { current, maximum }
```

组件描述实体当前具有什么状态或能力。例如：

| 实体 | 组件组合 | 含义 |
|---|---|---|
| 玩家 | Position + Velocity + PlayerInput | 可由玩家控制和移动 |
| 子弹 | Position + Velocity + Damage | 可移动并造成伤害 |
| 障碍物 | Position + Collider | 不移动但可发生碰撞 |

组件组合本身就是实体类型，不需要再定义 `Player extends Character` 之类的继承关系。

### 2.3 System：行为

System 查询包含特定组件的实体，并批量处理其数据：

```text
MovementSystem 查询 Position + Velocity
对每个匹配实体执行：
Position += Velocity × deltaTime
```

System 不关心实体叫玩家、敌人还是子弹，只关心它是否具有所需组件。

关于 Entity ID 如何关联组件存储，以及 `MovementSystem` 如何查询同时具有两个组件的实体，参见[组件存储与查询实现](./06-component-query-implementation.md)。

## 3. 核心设计思想

### 3.1 组合优于继承

新增能力通常只需增加组件和对应系统：

```text
普通敌人 = Position + Velocity + Health
可燃敌人 = Position + Velocity + Health + Flammable
飞行敌人 = Position + Velocity + Health + Flying
```

这样可以避免为每种能力组合创建一个新子类。

### 3.2 数据与行为分离

组件保存状态，系统表达规则。这种分离带来三个直接结果：

- 同一系统可以处理不同类型的实体。
- 数据布局可以独立于业务行为进行优化。
- 系统依赖和执行顺序可以集中管理。

### 3.3 面向数据设计

ECS 的性能优势不是由三个名称自动产生的，而是来自合理的数据布局和批量访问。

假设移动系统只需要位置和速度：

```text
传统对象布局：
[位置, 速度, 生命, 动画, 名称, AI ...]
[位置, 速度, 生命, 动画, 名称, AI ...]

紧凑组件布局：
Position: [p0, p1, p2, p3 ...]
Velocity: [v0, v1, v2, v3 ...]
```

紧凑布局减少了无关数据加载，更容易利用 CPU Cache 和 SIMD。

## 4. ECS 与传统对象模型对比

| 维度 | 传统对象模型 | ECS |
|---|---|---|
| 对象定义 | 类和继承 | 组件组合 |
| 数据位置 | 分散在对象内部 | 按组件或原型集中存储 |
| 行为位置 | 对象方法 | 独立 System |
| 能力复用 | 继承、接口、组合 | 添加组件 |
| 批量处理 | 通常需要遍历对象并判断 | Query 直接获得匹配数据 |
| 执行依赖 | 容易隐藏在调用链中 | 通常由调度器显式声明 |
| 适合场景 | 复杂单体行为、对象数量少 | 大量相似对象、实时批处理 |

两者并非互斥。实际项目常用普通对象管理 UI、网络连接或资源服务，用 ECS 管理大规模仿真实体。

## 5. ECS 的适用边界

### 5.1 适合使用

- 同时更新大量结构相似的对象。
- 对象能力需要频繁组合。
- 对 CPU 缓存、并行计算或稳定帧耗时有要求。
- 希望将仿真状态与表现层解耦。
- 实体会在运行时频繁创建、销毁或改变能力。

### 5.2 不一定适合

- 业务规模小，只有少量对象。
- 行为高度独特，几乎无法批量处理。
- 团队更需要快速交付，而性能和组合扩展不是瓶颈。
- 业务主要由复杂工作流、事务或远程调用组成。

ECS 会引入查询、调度、生命周期和调试成本。是否采用它，应由数据规模和变化模式决定，而不是由架构流行度决定。

## 6. 常见误解

### 6.1 组件不是传统对象的零件类

组件的重点是可查询的数据类型，而不是拥有生命周期和复杂行为的独立对象。

### 6.2 系统不是全局单例的别名

System 应围绕明确的数据读写集合工作。其价值在于批量处理和显式依赖，而不只是把方法搬到另一个类中。

### 6.3 ECS 不必完全消灭面向对象

ECS 可以只负责高频仿真部分。资源管理、编辑器、网络协议和应用服务仍可采用更合适的架构。

### 6.4 ECS 不天然高性能

如果组件大量使用指针、查询结果不连续、频繁进行结构变更，ECS 仍可能很慢。性能来自具体实现，而不是命名。

[返回目录](./README.md) | [下一章：核心结构](./02-core-model.md)
