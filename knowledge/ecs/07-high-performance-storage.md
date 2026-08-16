# 高性能 ECS 存储设计

## 1. 设计目标

高性能 ECS 通常需要同时满足：

| 目标 | 所需能力 |
|---|---|
| 高效批处理 | 同一 Query 的组件连续存储、顺序遍历 |
| 高 Cache 命中 | 热数据紧凑，避免加载无关字段 |
| 高效查询 | 不扫描全部 Entity，不逐实体判断组件组合 |
| O(1) 单实体访问 | 由 Entity ID 快速定位组件 |
| 高空间利用率 | 控制 Chunk 空洞、元数据和 Archetype 碎片 |
| 可接受的结构变更 | 添加、删除组件时能批量迁移 |

这些目标存在冲突：

- 连续存储利于遍历，但添加或删除组件需要搬迁数据。
- 预留空间利于插入，但会降低空间利用率。
- 组件拆得越细，Query 越精确，但元数据和组合数量越多。

工程上通常采用：

```text
Entity Location Table
+ Archetype
+ Fixed-size Chunk
+ Component Columns
+ Query Cache
+ Sparse Side Storage
```

## 2. 推荐的总体结构

```text
World
├── EntityTable[index]
│   └── { generation, archetype, chunk, row }
├── ArchetypeRegistry
│   └── ComponentSignature -> Archetype
├── QueryCache
│   └── QuerySignature -> MatchingArchetypes
└── SparseStores
    └── 冷组件或频繁增删组件

Archetype(Position, Velocity, Health)
├── Chunk 0
│   ├── Entity[]        连续
│   ├── Position[]      连续
│   ├── Velocity[]      连续
│   └── Health[]        连续
└── Chunk 1
    └── ...
```

各层职责明确：

- `EntityTable`：按 Entity ID 直接定位数据。
- `Archetype`：把组件组合相同的实体归为一组。
- `Chunk`：限制分配粒度并提供连续内存。
- `Component Columns`：让 System 只读取需要的组件。
- `QueryCache`：缓存 Query 匹配的 Archetype。
- `SparseStores`：避免冷数据或高频变更破坏热数据布局。

## 3. Entity Location Table：O(1) 定位

Entity 仍然由 `index + generation` 组成：

```cpp
struct Entity {
    uint32_t index;
    uint32_t generation;
};

struct EntityLocation {
    uint32_t generation;
    Archetype* archetype;
    Chunk* chunk;
    uint32_t row;
    bool alive;
};

std::vector<EntityLocation> entityTable;
```

获取组件时不需要扫描：

```cpp
template <typename T>
T* get(Entity entity) {
    EntityLocation& location = entityTable[entity.index];

    if (!location.alive ||
        location.generation != entity.generation) {
        return nullptr;
    }

    if (!location.archetype->contains(componentId<T>())) {
        return nullptr;
    }

    T* column = location.chunk->column<T>();
    return &column[location.row];
}
```

访问路径为：

```text
Entity.index
-> EntityTable[index]
-> Chunk + row
-> ComponentColumn[row]
```

时间复杂度近似 O(1)，代价是每个实体需要一条位置记录。

## 4. Archetype：预先完成组件组合匹配

每个 Archetype 对应一种确定的组件组合：

```text
A = Position + Velocity
B = Position + Velocity + Health
C = Position + Collider
```

为组件类型分配稳定整数 ID，并用 BitSet 表示 Archetype：

```text
Position = bit 0
Velocity = bit 1
Health   = bit 2
Sleeping = bit 3

Archetype B signature = 0111
```

Query：

```text
With(Position, Velocity)
Without(Sleeping)
```

匹配条件：

```cpp
bool matches(
    BitSet archetype,
    BitSet required,
    BitSet excluded
) {
    return (archetype & required) == required &&
           (archetype & excluded).none();
}
```

匹配发生在 **Archetype 级别**，而不是 Entity 级别。一个 Archetype 匹配后，其所有 Chunk 都可以直接遍历，不需要在内层循环重复调用 `has(Position)`。

## 5. Query Cache：避免每帧重新匹配

Query 第一次创建时扫描现有 Archetype，并缓存匹配结果：

```cpp
struct QueryPlan {
    BitSet required;
    BitSet excluded;
    std::vector<Archetype*> matches;
};
```

当新 Archetype 创建时，只需让它与已注册 Query 匹配一次：

```cpp
void onArchetypeCreated(Archetype* archetype) {
    for (QueryPlan& query : queryCache) {
        if (matches(
            archetype->signature,
            query.required,
            query.excluded
        )) {
            query.matches.push_back(archetype);
        }
    }
}
```

每帧执行 Query 时直接遍历：

```text
QueryPlan
-> Matching Archetypes
-> Chunks
-> Component Columns
```

查询开销主要与匹配数据量有关，而不是与 World 中的 Entity 总数有关。

## 6. Chunk：连续存储与空间控制

Archetype 不应为每个实体单独分配内存。通常将数据放入固定大小的 Chunk，例如 16 KiB、32 KiB 或 64 KiB：

```text
Chunk Header
├── count
├── capacity
├── change versions
└── enabled masks

Chunk Data
├── Entity[capacity]
├── Position[capacity]
├── Velocity[capacity]
└── Health[capacity]
```

固定大小 Chunk 的优点：

- 一次分配可容纳多个实体。
- 组件列连续，便于顺序预取。
- Chunk 可以直接作为并行任务单位。
- 空 Chunk 可以整体回收。
- Changed Filter 可以记录到 Chunk 级别。

### 6.1 容量计算

不能只用总大小除以单实体大小，因为每列还需要满足对齐要求。应寻找最大的 `capacity`，使所有列布局后仍能放入 Chunk：

```cpp
bool fits(size_t capacity) {
    size_t offset = sizeof(ChunkHeader);

    offset = alignUp(offset, alignof(Entity));
    offset += capacity * sizeof(Entity);

    for (ComponentInfo component : components) {
        if (component.isTag) {
            continue;
        }

        offset = alignUp(offset, component.alignment);
        offset += capacity * component.size;
    }

    return offset <= CHUNK_SIZE;
}
```

可以通过二分搜索求最大 capacity。

### 6.2 Chunk 大小选择

| Chunk 大小 | 优点 | 缺点 |
|---|---|---|
| 较小 | 尾部浪费少，任务粒度细 | Chunk 和调度元数据更多 |
| 较大 | 容量高，顺序遍历长 | 低占用 Archetype 浪费更明显 |

没有通用最优值。16 至 64 KiB 是可用于起步测试的范围，最终应根据组件大小、Archetype 数量和目标 CPU 测量。

## 7. SoA、AoS 与 AoSoA

### 7.1 不推荐：完整 Entity AoS

```cpp
struct GameObject {
    Position position;
    Velocity velocity;
    Health health;
    Animation animation;
    Name name;
};

GameObject entities[];
```

`MovementSystem` 只需要 Position 和 Velocity，但 CPU Cache Line 会载入 Health、Animation 和 Name。

### 7.2 推荐起点：按组件列存储

```text
Entity[]   = E0 E1 E2 E3 ...
Position[] = P0 P1 P2 P3 ...
Velocity[] = V0 V1 V2 V3 ...
Health[]   = H0 H1 H2 H3 ...
```

这属于组件级 SoA。`MovementSystem` 只访问 Position 和 Velocity 两列：

```cpp
for (Chunk* chunk : query.chunks()) {
    Position* positions = chunk->column<Position>();
    Velocity* velocities = chunk->column<Velocity>();

    for (uint32_t i = 0; i < chunk->count; ++i) {
        positions[i].x += velocities[i].x * deltaTime;
        positions[i].y += velocities[i].y * deltaTime;
    }
}
```

内层循环没有哈希查找、组件存在判断和跨实体指针跳转。

### 7.3 SIMD 密集计算：字段级 SoA 或 AoSoA

如果 System 经常只处理 Position 的某个字段，可进一步拆分：

```text
positionX[] = x0 x1 x2 x3 ...
positionY[] = y0 y1 y2 y3 ...
```

但字段级 SoA 会增加布局、序列化和组件访问复杂度。更平衡的方案是 AoSoA，按 SIMD 宽度分块：

```cpp
struct PositionBlock {
    float x[8];
    float y[8];
};

PositionBlock positions[];
```

建议：

1. 默认使用组件列。
2. 通过性能分析找到计算热点。
3. 只对热点组件使用字段级 SoA 或 AoSoA。

## 8. 删除与空间压缩

删除 Chunk 中间的实体时，不应留下长期空洞。常用 `swap-remove`：

```text
删除 row 2：

删除前：[E0, E1, E2, E3, E4]
移动 E4：[E0, E1, E4, E3]
```

伪代码：

```cpp
void removeRow(Chunk& chunk, uint32_t row) {
    uint32_t last = chunk.count - 1;

    if (row != last) {
        for (ComponentColumn& column : chunk.columns) {
            column.move(row, last);
        }

        Entity moved = chunk.entities[row];
        entityTable[moved.index].row = row;
    }

    chunk.destroy(last);
    --chunk.count;
}
```

这样 Chunk 内始终保持 `[0, count)` 连续，不需要在查询内层跳过空槽。

空间回收策略：

- 立即回收空 Chunk。
- 低占用 Chunk 在维护阶段合并。
- 不要每次删除后立即跨 Chunk 压缩，避免频繁搬迁。
- 为高频创建/销毁场景保留少量空 Chunk，减少分配抖动。

## 9. 添加或删除组件：Archetype 迁移

给实体添加 `Health`：

```text
源 Archetype：Position + Velocity
目标 Archetype：Position + Velocity + Health
```

执行过程：

```text
1. 在目标 Archetype 的 Chunk 中分配一行
2. 复制 Position 和 Velocity
3. 初始化 Health
4. 更新 EntityTable 中的 archetype、chunk、row
5. 从源 Chunk swap-remove
```

结构变更涉及数据搬迁，优化重点不是让单次迁移完全免费，而是减少次数并进行批量处理：

- System 写入 Command Buffer。
- 在固定同步点统一提交。
- 按“源 Archetype -> 目标 Archetype”对命令分组。
- 批量预留目标 Chunk 空间。
- 避免每帧反复添加和删除同一组件。

## 10. 高空间利用率的关键策略

### 10.1 Tag 不占实体行数据

`Player`、`Sleeping` 等无字段 Tag 只参与 Archetype Signature，不在每个 Entity 行中分配字节。

### 10.2 拆分热数据和冷数据

```text
热数据：Position、Velocity、短小状态
冷数据：Name、Description、复杂配置、历史记录
大数据：Mesh、AnimationClip、导航图
```

热数据放 Archetype Chunk；大对象放资源池，组件只保存句柄：

```cpp
struct MeshRef {
    uint32_t handle;
};
```

这样不会因为一个大组件显著降低每个 Chunk 的 capacity。

### 10.3 稀有或频繁变更组件使用 Sparse Set

如果某组件满足以下特征，可以放在 Archetype 外的 Sparse Set：

- 只有极少数实体拥有。
- 添加和删除频率很高。
- 很少参与核心批处理 Query。
- 数据较大或生命周期独立。

这种混合方案避免为少量数据创建大量低占用 Archetype。

### 10.4 控制 Archetype 组合爆炸

若有 N 个可独立出现的 Tag，理论上可能产生 `2^N` 种 Archetype。大量低占用 Archetype 会浪费 Chunk。

对于频繁开关的状态，可以考虑：

- 组件内部状态字段。
- Chunk 级 enabled bitset。
- 独立 Sparse Set。
- 将互斥状态合并为枚举。

只有当 Tag 能显著减少 Query 工作量且变化不频繁时，才值得让它改变 Archetype。

## 11. Cache 命中的进一步优化

### 11.1 Query 不需要 Entity 时不要读取 Entity 列

```cpp
for (uint32_t i = 0; i < count; ++i) {
    positions[i] += velocities[i] * deltaTime;
}
```

只有需要发送事件或建立引用时才读取 `Entity[i]`。

### 11.2 按数据流安排 System

连续执行访问相同热组件的 System，可能复用仍在 Cache 中的数据。但必须同时考虑写依赖和并行机会，最终以分析工具结果为准。

### 11.3 减少内层分支

不要在每个实体上判断可选组件：

```cpp
if (hasAcceleration(entity)) {
    // ...
}
```

可以为不同 Archetype 生成不同循环，或者拆成两个 Query，使内层循环保持稳定。

哈希访问模式与 CPU 分支预测的详细分析参见[哈希查询与分支预测](./09-hash-and-branch-performance.md)。

### 11.4 使用 Chunk 级版本过滤

如果某 Chunk 的 Transform 自上次运行后未被写入，渲染同步 System 可以跳过整个 Chunk，而不是检查每个 Entity。

## 12. 推荐的混合方案

对于以移动、物理或仿真为主的系统，可采用：

```text
EntityTable
  O(1) Entity -> Location

Archetype Chunks
  存放高频查询、组合稳定的热组件

Query Cache
  Signature -> Matching Archetypes + Column Metadata

Sparse Sets
  存放稀有、冷门、频繁增删的组件

Resource / Blob Storage
  存放大型共享对象，组件只保存句柄

Command Buffer
  批量执行结构变更
```

这套设计实现的不是单一维度上的极致，而是较均衡地满足：

```text
批处理性能：连续 Component Columns
查询性能：Archetype Signature + Query Cache
随机访问：Entity Location Table
空间利用：固定 Chunk + swap-remove + 冷热分离
变更成本：Sparse Set + 批量 Archetype 迁移
```

## 13. 实施顺序

建议按以下顺序实现和测量：

1. Entity generation 与 Location Table。
2. Archetype Signature 和注册表。
3. 固定大小 Chunk 与组件列布局。
4. Query Cache 和无分支内层循环。
5. swap-remove 与批量结构变更。
6. 冷热组件拆分和 Sparse Set。
7. Chunk 版本、并行任务和 SIMD。

先建立基准：

```text
每秒处理实体数
每实体加载字节数
L1/L2/LLC miss rate
平均 Chunk occupancy
Archetype 数量
每帧结构迁移数量与字节数
```

只有在这些指标可观测后，才能判断优化是否真正改善了系统。

[上一章：组件存储与查询实现](./06-component-query-implementation.md) | [返回目录](./README.md) | [下一章：调度 DAG 方案分析](./08-scheduler-dag-analysis.md)
