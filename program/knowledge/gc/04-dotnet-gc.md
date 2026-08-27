# .NET（C#）GC：分代压缩式收集器

## 1. 定位

.NET GC 是**精确、分代、可压缩、支持后台并发的追踪式收集器**。它只管理托管堆，不碰非托管内存；靠可达性分析自动处理环引用。

## 2. 托管堆布局

```text
┌────────────────── 托管堆 ──────────────────┐
│ ┌──────── 小对象堆 SOH ────────┐  ┌LOH──┐┌POH┐│
│ │ Gen0 │ Gen1 │      Gen2     │  │≥85KB││固定││
│ └─────────────────────────────┘  └─────┘└───┘│
└──────────────────────────────────────────────┘
```

- **Gen0**：新对象，回收最频繁、最廉价；
- **Gen1**：Gen0 幸存者，充当 Gen0 与 Gen2 之间的缓冲；
- **Gen2**：长命对象，回收最贵；
- **LOH**：≥ 85,000 字节的对象，默认**不压缩**；
- **POH**（.NET 5+）：专放需要固定的数组，用于原生互操作。

对象布局（64 位）：

```text
┌──────────────┬──────────────┬───────────────────────┐
│ Object Header│ MethodTable* │        字段数据        │
│   8 字节     │   8 字节     │（引用字段被精确标记）   │
└──────────────┴──────────────┴───────────────────────┘
```

GC 靠 MethodTable 里的元数据精确知道"对象的哪些偏移是引用字段"——这就是"精确 GC"的底气：既不会漏真引用，也不会认假指针。

## 3. 分配：Bump Pointer

```csharp
var obj = new MyClass();  // 本质: p = allocPtr; allocPtr += size;
```

每个线程有自己的**分配上下文**（类似 TLAB），在线程本地 Gen0 内存里"指针一拨"完成分配，无锁、无空闲链表查找。这是三种 GC 中最快的分配路径。

## 4. 根集合与精确标记

```
根 = 线程栈（JIT 生成的栈槽元数据精确指出哪个是引用）
   + CPU 寄存器
   + 静态字段
   + GC Handle 表（Normal/Weak/Pinned）
   + 终结队列 / f-reachable 队列
```

值类型（struct）不单独在堆上，其引用字段作为父对象布局的一部分被递归描述；装箱后才成为独立堆对象。

## 5. 写屏障与卡片表：跨代引用追踪

问题：Gen2 里的老对象 `old.y = new Young()` 之后，Gen0 回收只扫根和 Gen0 自己，会看不到这条来自 Gen2 的引用，把新对象误回收。

解法：JIT 把引用赋值编译成"写入 + 写屏障"：

```csharp
old.y = new Young();
// ① 写入 old.y
// ② 若 old 在老代且 y 在年轻代 → 把 old 所在卡片在卡片表里标脏
```

收集年轻代时**只扫脏卡片上的对象**，而不是全 Gen2。代价：每次引用字段赋值多几条指令——这是分代 GC 的固定税。

## 6. 三代如何运转

### 6.1 晋升

```
Gen0 收集 → 幸存者 → Gen1
Gen1 收集 → 幸存者 → Gen2
Gen2 收集 → 幸存者留在 Gen2
```

### 6.2 触发

| 触发条件 | 收集范围 |
|---|---|
| Gen0 分配预算耗尽 | Gen0（最常见，亚毫秒级） |
| Gen1 阈值到达 | Gen0 + Gen1 |
| Gen2 阈值、`GC.Collect(2)`、LOH 压力、低内存 | 全堆 |

预算**动态自适应**：根据上一轮存活率调整下一轮 Gen0 预算——存活率高则加大预算（少回收）。

### 6.3 Gen0 收集流程

```mermaid
flowchart LR
    A[暂停全部托管线程] --> B[标记: 从根 + 脏卡片出发]
    B --> C[计划: 决定幸存对象移动位置]
    C --> D[重定位: 搬移对象并精确更新所有引用]
    D --> E[清扫: 重置 Gen0 分配指针]
    E --> F[恢复线程]
```

Gen0 收集通常不到 1 毫秒。

## 7. 压缩与 Pinning

- 幸存对象向低地址紧凑排列 → 自动整理内存、抗碎片、缓存友好；
- 移动后所有引用被精确更新（精确 GC 的收益：每个引用它都知道）；
- **被固定的对象不能移动**，成为压缩的路障：

```csharp
fixed (byte* p = buf) { /* 传给原生代码 */ }
```

长期大量 pinning 导致碎片化，所以 .NET 5 引入 POH：需要长期固定的数组直接进专门堆，从根上避免 pinning 弄脏年轻代。

## 8. LOH 与碎片

- 85KB 阈值以上进 LOH，默认不压缩，用空闲链表管理；
- 连续分配大对象可能触发 Gen2 GC（LOH 与 Gen2 生命周期绑定）；
- 手动补救：

```csharp
GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
GC.Collect();  // 下一次全堆 GC 时压缩一次 LOH
```

## 9. 两种模式与后台 GC

| 模式 | 特点 | 适用 |
|---|---|---|
| Workstation（默认） | 单堆；Gen2 标记与应用程序**并发**执行 | 桌面、客户端 |
| Server | 每逻辑 CPU 一个堆、一个 GC 线程 | 高吞吐常驻服务 |

后台 GC 的工作方式：GC 线程并发标记 Gen2（同时用写屏障记录新引用），标记完成后短暂 stop-the-world 收尾——把全堆回收的大卡顿摊薄成一段并发期。

## 10. 终结（Finalization）

1. `new Foo()` 时对象登记进终结队列；
2. GC 判定不可达时不直接释放，移入 f-reachable 队列；
3. 专用终结线程依次调用 `~Foo()`；
4. 对象再等下一轮 GC 才真正回收（至少多活一个周期）；
5. 特殊能力：`GC.ReRegisterForFinalize`（复活）、`CriticalFinalizerObject`、`GC.SuppressFinalize`。

终结器时机不确定，所以确定性释放靠 IDisposable：

```csharp
using var conn = new SqlConnection(...);  // 编译成 try/finally + Dispose()
```

## 11. 常用旋钮与观测

```csharp
GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency; // 避免阻塞式 Gen2
GC.TryStartNoGCRegion(64 * 1024 * 1024);   // 关键路径暂停 GC
GC.GetGCMemoryInfo();       // 各代大小、碎片、压缩次数
GC.CollectionCount(0);      // 各代回收次数
GC.GetGeneration(obj);      // 对象在哪一代
```

排查工具：dotnet-counters（实时堆大小与分配速率）、dotnet-gcdump（堆快照）、PerfView / Visual Studio 诊断工具 / dotMemory、SOS（`!dumpheap`、`!gcroot` 查引用链）。

## 12. 逃逸通道

```csharp
Span<byte> span = stackalloc byte[128];   // 栈上分配，零 GC
ref struct A { public int x; }            // 只能活在栈上，禁止装箱
ArrayPool<byte>.Shared;                   // 租借大缓冲，不反复 new
ValueTask<int> v;                         // 同步完成时零分配
```

## 13. 阅读结论

1. .NET GC = 精确追踪 + 三代分代 + 压缩 + 后台并发 + 动态预算。
2. 分代假说让平均回收近乎免费（Gen0 亚毫秒），压缩根治碎片，精确元数据杜绝假指针。
3. 代价是写屏障与对象头的固定税、pinning 限制，以及"每次分配都计入预算"这条纪律——高频分配场景仍需池化与结构体。
