# 热循环中的哈希查询与分支预测

## 1. 结论

“避免哈希查询和不可预测分支”不是指完全禁止它们，而是指：

```text
不要在处理大量实体的最内层循环中，
为每个实体重复执行哈希、字符串比较和随机分支。
```

以下使用方式通常没有问题：

- 初始化或注册阶段使用哈希表。
- 编辑器、调试工具等低频路径使用哈希表。
- 每帧只查询几次。
- 哈希表很小并稳定驻留 CPU Cache。
- 在进入 Chunk 循环前查询一次，并缓存结果。
- 分支高度可预测，例如几乎总为 true。

性能判断必须考虑：

```text
单次成本
× 每帧调用次数
× 实体数量
× System 数量
```

## 2. 哈希表不一定是连续存储

哈希表有多种实现。

### 2.1 链式哈希

bucket 数组可能连续，但每个 bucket 指向独立节点：

```text
Buckets（连续）
[0] -> Node A -> Node B
[1] -> null
[2] -> Node C
[3] -> Node D -> Node E
```

节点通常单独分配，位置可能分散。查找过程包含：

```text
计算 hash
-> 读取 bucket
-> 跟随节点指针
-> 比较 key
-> 可能继续跟随下一个指针
```

例如传统的节点式 `std::unordered_map` 通常具有这类特征。

### 2.2 开放寻址哈希

开放寻址表通常将 bucket 或条目放在连续数组中：

```text
[empty][K7][K3][empty][K9][K1][empty]...
```

这比链式哈希更紧凑，但查找仍需要：

```text
hash(key)
-> 跳到 hash 决定的 bucket
-> 检查控制信息
-> 发生冲突时继续探测
-> 比较 key
```

Swiss Table、Robin Hood Hashing 等设计通过紧凑控制字节、SIMD 比较和更好的探测策略改善局部性，但仍不是免费的数组索引。

JavaScript 的 `Map` 具体布局由引擎决定，代码不能假设它一定使用某种连续结构。

## 3. 连续分配不等于连续访问

这是最关键的区别。

假设哈希表的 bucket 数组在内存中完全连续：

```text
bucket[0], bucket[1], bucket[2], ... bucket[1023]
```

查询不同 key 时：

```text
hash(keyA) -> bucket[731]
hash(keyB) -> bucket[12]
hash(keyC) -> bucket[884]
hash(keyD) -> bucket[203]
```

访问地址近似随机。

而顺序遍历组件数组：

```text
Position[0]
Position[1]
Position[2]
Position[3]
...
```

CPU 可以：

- 一次 Cache Line 加载多个相邻组件。
- 预测下一次内存访问地址。
- 提前预取后续 Cache Line。
- 合并或并行执行连续加载。
- 更容易生成 SIMD 指令。

因此需要区分：

| 概念 | 含义 |
|---|---|
| 连续分配 | 数据物理上位于同一片内存 |
| 连续访问 | 程序按相邻地址顺序读取数据 |

哈希表可能满足前者，但通常不保证后者。

## 4. 哈希查询的具体成本

平均 O(1) 只表示查找次数不会随元素数量线性增长，不表示单次操作与数组索引一样便宜。

一次哈希查询可能包括：

1. 读取 key。
2. 计算哈希值。
3. 根据容量计算 bucket 位置。
4. 加载 bucket 或控制字节。
5. 判断 bucket 状态。
6. 比较 key。
7. 冲突时继续探测。
8. 加载最终 value。

数组直接索引通常更接近：

```text
baseAddress + index * elementSize
-> load
```

### 4.1 Cache Miss

若目标 bucket 不在 CPU Cache 中，需要从更远的层级读取。

大致趋势如下，具体数值随 CPU 而变化：

```text
L1 Cache：数个周期
L2 Cache：十余个周期
Last-Level Cache：数十个周期
主内存：可能超过百个周期
```

随机访问还会降低硬件预取的效果。

### 4.2 哈希冲突

两个 key 映射到相同或相邻区域时，需要额外探测或遍历链表。表的负载因子越高，平均探测距离通常越长。

```text
较低负载因子：
查询更快，但空 bucket 多，空间利用率较低

较高负载因子：
空间利用率较高，但冲突和探测增加
```

### 4.3 依赖链

哈希查询常形成数据依赖：

```text
先得到 hash
才能计算 bucket
读取 bucket 后
才能知道下一次探测位置
```

CPU 的乱序执行难以提前执行尚不知道地址的后续加载，因此内存延迟不容易被隐藏。

## 5. 字符串 Key 的额外成本

字符串哈希不仅查询表，还要处理字符串本身：

```text
读取字符串字节
-> 计算 hash
-> 查找 bucket
-> 必要时比较长度和内容
```

其成本与以下因素有关：

- 字符串长度。
- 哈希值是否缓存。
- 字符串内容是否已经在 Cache 中。
- 冲突时是否需要完整比较。
- 字符串对象是否需要指针跳转。

例如在每个实体上执行：

```typescript
componentStores.get("Position");
```

即使引擎可能缓存字符串哈希，也仍需查找 Map。更好的做法是把组件类型注册为整数 ID 或 Symbol，并在外层只查找一次。

### 5.1 字符串驻留

可将字符串在注册阶段转换成稳定 ID：

```text
"Position" -> ComponentTypeId 3
"Velocity" -> ComponentTypeId 7
```

运行时使用：

```typescript
const store = storesById[POSITION_ID];
```

字符串只用于：

- 配置解析。
- 调试显示。
- 序列化名称。
- 注册阶段。

## 6. 哈希表很短时能否使用

可以，而且很多情况下完全合理。

“很短”需要区分三种含义。

### 6.1 key 字符串很短

短字符串会降低哈希计算和比较成本，但不能消除：

- bucket 定位。
- Cache Miss。
- 冲突探测。
- 分支判断。

因此短 key 有帮助，但不是唯一判断条件。

### 6.2 哈希表元素很少

如果整张表和控制信息能稳定放入 L1 或 L2 Cache，随机访问成本会显著下降。

但元素极少时，连续数组线性扫描可能反而更快：

```cpp
for (const Entry& entry : smallEntries) {
    if (entry.key == key) {
        return entry.value;
    }
}
```

原因是：

- 数据连续。
- 无需计算复杂 hash。
- 循环短。
- 编译器可能展开或向量化。

对于 4、8、16 个元素，没有通用分界点，需要根据 key 类型、查询频率和目标 CPU 基准测试。

### 6.3 每帧查询次数很少

如果每帧只查询一次，即使单次较慢也通常不重要。

真正需要避免的是：

```text
10 万实体
× 每实体 3 次哈希查询
× 每秒 60 帧
= 每秒 1800 万次查询
```

## 7. ECS 中哈希查询应该放在哪里

### 7.1 不推荐：实体内层循环查询组件存储

```typescript
for (const entity of entities) {
  const positions = stores.get(POSITION);
  const velocities = stores.get(VELOCITY);

  const position = positions?.get(entity.index);
  const velocity = velocities?.get(entity.index);

  if (position && velocity) {
    position.x += velocity.x * deltaTime;
  }
}
```

每个实体都会重复：

- 两次组件类型哈希查询。
- 两次 Entity index 哈希查询。
- 两个存在性分支。

### 7.2 较好：在外层查询一次

```typescript
const positions = stores.get(POSITION);
const velocities = stores.get(VELOCITY);

if (positions !== undefined && velocities !== undefined) {
  for (const entity of matchedEntities) {
    const position = positions.get(entity.index);
    const velocity = velocities.get(entity.index);

    // ...
  }
}
```

组件类型查找移出了实体循环，但单实体组件定位仍是哈希查询。

### 7.3 更好：Query Cache 直接保存组件列

```cpp
for (Chunk* chunk : query.matchingChunks()) {
    Position* positions = chunk->column<Position>();
    Velocity* velocities = chunk->column<Velocity>();

    for (uint32_t i = 0; i < chunk->count; ++i) {
        positions[i].x += velocities[i].x * deltaTime;
        positions[i].y += velocities[i].y * deltaTime;
    }
}
```

哈希或 Archetype 匹配发生在 Query 创建、Archetype 注册或 Chunk 外层，不发生在每个实体上。

核心优化原则是：

```text
可以保留哈希表，
但把查询次数从“每实体一次”
降低到“每 Query、每 Archetype 或每 Chunk 一次”。
```

## 8. 什么是 CPU 分支预测

现代 CPU 使用流水线同时处理多条指令：

```text
取指 -> 解码 -> 调度 -> 执行 -> 提交
```

遇到条件分支时：

```cpp
if (condition) {
    pathA();
} else {
    pathB();
}
```

CPU 在 `condition` 的最终结果出来前，可能已经需要继续取后面的指令。为了不停止流水线，它会根据历史模式预测将执行哪个分支，并提前执行该路径。

```text
预测正确：
提前执行的工作可以保留

预测错误：
丢弃错误路径上的工作
从正确地址重新取指和执行
```

## 9. 为什么不可预测分支代价高

### 9.1 流水线清空

预测错误时，CPU 需要取消错误路径上已经进入流水线的指令。一次误预测可能损失十余个甚至更多周期，具体取决于 CPU 微架构和分支位置。

### 9.2 降低指令级并行

CPU 原本可以同时执行多条互不依赖的指令。频繁误预测会不断中断这种并行，使执行单元等待正确路径重新进入流水线。

### 9.3 阻碍自动向量化

简单循环容易使用 SIMD 同时处理多个实体：

```text
一次处理 Position[0..7]
```

如果每个实体走不同分支：

```text
Entity 0 -> path A
Entity 1 -> path B
Entity 2 -> path A
Entity 3 -> path C
```

编译器可能：

- 放弃向量化。
- 使用掩码执行多个路径。
- 同时计算多个结果后再选择。

后两种方式会浪费部分 SIMD Lane 或执行不需要的计算。

### 9.4 增加指令 Cache 压力

复杂分支包含多条较大的代码路径时，需要加载更多指令。热循环不再是一小段稳定代码，可能增加 Instruction Cache Miss。

### 9.5 与随机内存访问叠加

哈希表探测本身通常包含数据相关分支：

```text
bucket 是否为空？
key 是否相同？
是否继续探测？
```

如果同时发生 Cache Miss 和分支误预测，CPU 很难保持稳定吞吐。

## 10. 什么分支是可预测的

### 10.1 通常容易预测

循环结束条件：

```cpp
for (uint32_t i = 0; i < count; ++i) {
    // ...
}
```

分支通常连续多次为 true，最后一次为 false。

低频错误检查：

```cpp
if (UNLIKELY(error)) {
    handleError();
}
```

正常情况下几乎总为 false。

稳定状态：

```cpp
if (gamePaused) {
    // 整帧内通常保持不变
}
```

### 10.2 通常难以预测

实体数据近似随机分布：

```cpp
for (Entity entity : entities) {
    if (entity.isActive) {
        update(entity);
    }
}
```

若 active 状态以接近 50% 且无规律的方式交错，预测器难以学习稳定模式。

多路且数据相关的状态机：

```cpp
switch (entity.state) {
    case Idle: ...
    case Chase: ...
    case Attack: ...
    case Flee: ...
}
```

不同实体状态随机交错时，间接跳转或多路分支可能难以预测。

分支的危险程度取决于 **结果模式**，不只取决于代码中是否出现 `if`。

## 11. ECS 如何减少不可预测分支

### 11.1 按组件组合拆分 Query

不推荐：

```cpp
for (Entity entity : movableEntities) {
    if (hasAcceleration(entity)) {
        applyAcceleration(entity);
    }

    move(entity);
}
```

改成：

```text
Query A：Position + Velocity + Acceleration
Query B：Position + Velocity - Acceleration
```

每个循环内部不再检查 `hasAcceleration`。

### 11.2 按状态分组

将不同状态实体分入不同 Archetype、Chunk、队列或索引列表：

```text
Active Entities
Sleeping Entities
Pending Entities
```

用较低频的结构维护成本换取高频循环的稳定执行。

注意：如果状态每帧频繁切换，Archetype 迁移成本可能高于分支成本。此时可使用：

- Chunk enabled bitset。
- 独立索引列表。
- 批量分区。
- 分支保留并依赖测量。

### 11.3 把分支提到 Chunk 外层

```cpp
if (chunk.hasAcceleration()) {
    updateWithAcceleration(chunk);
} else {
    updateWithoutAcceleration(chunk);
}
```

每个 Chunk 判断一次，比每个实体判断一次更便宜，并让内层循环保持一致。

### 11.4 使用数据分区

如果数组允许重排，可以把满足条件的实体放在前半段：

```text
[active, active, active, inactive, inactive]
```

随后只遍历 active 范围，或者让分支呈现长时间连续模式。

## 12. 不要盲目改成无分支代码

Branchless 并不总是更快。

例如：

```cpp
result = condition ? expensiveA() : expensiveB();
```

为了消除分支而同时计算 A 和 B，可能比一次高度可预测的分支更慢。

无分支代码可能：

- 执行两个路径的全部计算。
- 增加指令数量。
- 增加寄存器压力。
- 产生更长的数据依赖链。
- 降低代码可读性。

正确原则是：

```text
保留便宜且可预测的分支
优化高频且误预测率高的分支
最终依据性能计数器判断
```

## 13. 选择建议

| 场景 | 建议 |
|---|---|
| 初始化时按名称注册组件 | 字符串 Map 完全合理 |
| 每帧查询几次全局资源 | 哈希表通常合理 |
| 小表可稳定驻留 Cache | 可以使用，必要时与数组扫描对比 |
| 每个实体查询组件类型 | 移到 Query 或 Chunk 外层 |
| 每个实体按 index 查组件 | 使用 Sparse Set、Location Table 或 Archetype 列 |
| 固定组件类型集合 | 注册为整数 ID，使用数组直接索引 |
| 随机状态分支 | 按 Query、Chunk 或状态分组 |
| 几乎总为 true/false 的分支 | 通常无需优化 |
| 冷路径或低频工具逻辑 | 优先可读性，不必消除哈希和分支 |

## 14. 如何测量

不要只看总运行时间。可以同时观察：

```text
每帧哈希查询次数
每实体处理时间
L1/L2/LLC Cache Miss
Branch Instructions
Branch Mispredictions
Instructions Per Cycle
SIMD 利用率
```

比较以下版本：

```text
Map 查询
连续小数组扫描
整数 ID 数组索引
Query Cache 直接组件列
```

最终目标不是消灭所有哈希和分支，而是让实体热循环接近：

```cpp
for (uint32_t i = 0; i < count; ++i) {
    output[i] = compute(inputA[i], inputB[i]);
}
```

这种循环访问连续、控制流稳定，CPU 更容易预取、乱序执行和向量化。

[上一章：调度 DAG 方案分析](./08-scheduler-dag-analysis.md) | [返回目录](./README.md)

