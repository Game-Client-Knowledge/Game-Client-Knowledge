# 2026 网易游戏客户端二面参考答案

## 1. 使用说明

本文按[二面原题](./02.md)的顺序回答。

二面的重点不是背概念，而是看你能不能把基础知识接到真实客户端工程里。建议回答时多使用这个结构：

```text
先给方案
-> 说明为什么这样做
-> 补充风险和边界
-> 回到游戏客户端场景
```

算法题默认使用 C++17。

## 2. 八股与项目迁移

### 2.1 推荐系统架构经验如何迁移到游戏客户端

可以举“数据驱动的内容分发和本地策略执行”这个例子。

推荐系统通常会做这些事：

```text
采集行为数据
-> 离线/在线计算特征
-> 下发策略或排序结果
-> 客户端展示并回传反馈
```

迁移到游戏客户端，可以变成：

```text
采集玩家行为和性能数据
-> 服务端计算活动、商城、任务、引导策略
-> 客户端本地缓存策略并按场景执行
-> 客户端继续回传点击、完成率、卡顿率、流失点
```

具体例子：

- **新手引导**：根据玩家失败点、停留时间和关卡进度，下发不同引导步骤。
- **活动入口排序**：根据玩家等级、付费阶段、最近玩法偏好，调整活动入口优先级。
- **资源预加载**：根据玩家接下来最可能进入的玩法，提前加载对应场景、角色、技能资源。
- **性能降级策略**：根据机型、帧率和温度数据，下发画质档位和特效开关。

客户端要注意：

- 服务端策略不能让客户端每帧做重计算，热路径只做轻量查表。
- 策略需要版本号、灰度、回滚和默认兜底。
- 不能把关键安全逻辑完全放在客户端，客户端策略只负责体验和展示。

可以这样收尾：

```text
推荐系统经验迁移到客户端，本质是把“人群、特征、策略、反馈闭环”这套思路，用在内容展示、资源调度、性能降级和新手体验上。
```

### 2.2 C++ 的多态

C++ 多态主要分两类：

- **静态多态**：编译期确定调用目标，如函数重载、模板、CRTP。
- **动态多态**：运行期通过基类指针或引用调用虚函数，根据对象真实类型分派。

动态多态示例：

```cpp
struct Skill {
    virtual ~Skill() = default;
    virtual void cast() = 0;
};

struct Fireball : Skill {
    void cast() override {
        // cast fireball
    }
};

void play(Skill& skill) {
    skill.cast(); // 运行期根据真实对象类型调用
}
```

底层常见实现是 `vptr + vtable`：

```text
对象内部有虚表指针 vptr
vptr 指向该动态类型的虚函数表 vtable
虚函数表中保存虚函数入口地址
```

面试中要补一句：C++ 标准没有强制规定必须这样实现，但主流编译器基本采用这种模型。

工程取舍：

- 动态多态扩展性好，适合稳定接口和多实现类型。
- 虚调用通常不能像普通函数一样轻易内联，有一点间接调用成本。
- 对性能极敏感、数据量极大的热路径，可以考虑模板、数据导向设计或 ECS。

### 2.3 虚函数表是什么

虚函数表可以理解成“这个类的虚函数跳转表”。每个多态类型通常有一张或多张虚表，表里按固定槽位保存虚函数地址。

例子：

```cpp
struct Base {
    virtual void update();
    virtual void render();
};

struct Player : Base {
    void update() override;
    void render() override;
};
```

抽象布局：

```text
Base vtable:
  slot 0 -> Base::update
  slot 1 -> Base::render

Player vtable:
  slot 0 -> Player::update
  slot 1 -> Player::render
```

当执行：

```cpp
Base* p = new Player();
p->update();
```

常见过程是：

```text
读取对象里的 vptr
-> 找到 Player vtable
-> 取 update 槽位函数地址
-> 间接调用 Player::update
```

### 2.4 虚表指针是什么，存在哪里

虚表指针通常叫 `vptr`，它是对象内部的一个隐藏指针，指向当前对象动态类型对应的虚函数表。

常见单继承布局：

```text
Player object:
  [vptr -> Player vtable]
  [hp]
  [position]
```

注意点：

- `vptr` 通常存在对象实例里，不是存在类对象里。
- `vtable` 通常是编译器生成的静态表，放在只读数据段或类似区域。
- 一个类可以有很多对象，这些对象的 `vptr` 通常指向同一张虚表。
- 多继承或虚继承场景可能有多个 `vptr`，布局会复杂很多。
- 构造和析构过程中，`vptr` 会随着构造阶段变化，所以构造/析构函数里调用虚函数不会分派到“尚未构造好或已经析构掉”的派生部分。

### 2.5 `class` 和 `struct` 有什么区别

在 C++ 中，`class` 和 `struct` 的能力几乎一样，核心区别是默认访问权限不同：

```text
struct 默认 public 成员、public 继承
class  默认 private 成员、private 继承
```

示例：

```cpp
struct A {
    int x; // public
};

class B {
    int x; // private
};
```

工程习惯：

- `struct` 常用于简单数据聚合、配置、消息、组件数据。
- `class` 常用于有不变量、有封装、有生命周期管理的对象。

可以补充：这只是团队风格习惯，不是语言强制限制。`struct` 也可以有构造函数、成员函数、虚函数和访问控制。

### 2.6 万人访问面板，读多写少，加锁性能低如何优化

先判断数据特征：

```text
读多写少
-> 读路径要尽量无锁或低锁
-> 写路径可以承担更高成本
-> 需要明确一致性要求：强一致、最终一致、还是帧级一致
```

常见优化方案：

1. **读写锁**

读操作共享锁，写操作独占锁：

```cpp
#include <shared_mutex>

std::shared_mutex mutex;

void readPanel() {
    std::shared_lock lock(mutex);
    // read data
}

void updatePanel() {
    std::unique_lock lock(mutex);
    // write data
}
```

适合读多写少，但写线程可能被大量读线程饿住，需要看实现和策略。

2. **快照 + 原子切换**

写线程构建新快照，构建完成后一次性替换指针；读线程只读当前快照。

```cpp
#include <atomic>
#include <memory>
#include <vector>

struct PanelSnapshot {
    std::vector<int> ranks;
};

std::atomic<std::shared_ptr<const PanelSnapshot>> current;

std::shared_ptr<const PanelSnapshot> getSnapshot() {
    return current.load(std::memory_order_acquire);
}

void publishSnapshot(std::shared_ptr<const PanelSnapshot> next) {
    current.store(std::move(next), std::memory_order_release);
}
```

这类方案适合排行榜、活动面板、配置面板等读多写少数据。读侧基本不阻塞，写侧成本是复制或重建快照。

3. **双缓冲**

一份前台给读线程使用，一份后台给写线程更新，到安全点交换。

```text
front buffer：本帧读取
back buffer：后台写入
frame end：交换 front/back
```

游戏客户端很适合，因为很多状态只要求“下一帧可见”，不要求写完立刻被所有读者看到。

4. **分片锁**

如果数据很大，可以按用户、区域、页码或哈希分片，降低锁竞争。

5. **缓存和增量更新**

面板不一定每次都全量刷新，可以拆成：

```text
基础静态配置
动态计数
用户个性化状态
```

只更新变化部分，减少锁内工作量。

面试收尾：

```text
我会优先确认一致性要求。如果允许帧级或最终一致，优先用不可变快照/双缓冲；如果必须强一致，再考虑读写锁或分片锁。
```

## 3. 算法：设计扫雷棋盘

### 3.1 数据表示

棋盘每个格子需要表示：

- 是否有雷
- 周围雷数
- 当前状态：未打开、已打开、旗标

```cpp
#include <queue>
#include <stdexcept>
#include <utility>
#include <vector>

class Minesweeper {
public:
    enum class State {
        Hidden,
        Revealed,
        Flagged
    };

    struct Cell {
        bool hasMine = false;
        int aroundMines = 0;
        State state = State::Hidden;
    };

    Minesweeper(int rows, int cols)
        : rows_(rows), cols_(cols), board_(rows, std::vector<Cell>(cols)) {
        if (rows <= 0 || cols <= 0) {
            throw std::invalid_argument("invalid board size");
        }
    }

    void init(const std::vector<std::pair<int, int>>& mines) {
        for (auto& row : board_) {
            for (auto& cell : row) {
                cell = Cell{};
            }
        }

        for (auto [r, c] : mines) {
            if (!inside(r, c)) {
                continue;
            }
            board_[r][c].hasMine = true;
        }

        for (int r = 0; r < rows_; ++r) {
            for (int c = 0; c < cols_; ++c) {
                if (!board_[r][c].hasMine) {
                    board_[r][c].aroundMines = countAroundMines(r, c);
                }
            }
        }
    }

    const std::vector<std::vector<Cell>>& board() const {
        return board_;
    }

    void reveal(int startRow, int startCol) {
        if (!inside(startRow, startCol)) {
            return;
        }

        Cell& start = board_[startRow][startCol];
        if (start.state == State::Flagged || start.state == State::Revealed) {
            return;
        }

        start.state = State::Revealed;
        if (start.hasMine || start.aroundMines > 0) {
            return;
        }

        floodReveal(startRow, startCol);
    }

private:
    int rows_ = 0;
    int cols_ = 0;
    std::vector<std::vector<Cell>> board_;

    static constexpr int directions_[8][2] = {
        {-1, -1}, {-1, 0}, {-1, 1},
        {0, -1},           {0, 1},
        {1, -1},  {1, 0},  {1, 1}
    };

    bool inside(int r, int c) const {
        return r >= 0 && r < rows_ && c >= 0 && c < cols_;
    }

    int countAroundMines(int r, int c) const {
        int count = 0;
        for (auto& dir : directions_) {
            int nr = r + dir[0];
            int nc = c + dir[1];
            if (inside(nr, nc) && board_[nr][nc].hasMine) {
                ++count;
            }
        }
        return count;
    }

    void floodReveal(int startRow, int startCol) {
        std::queue<std::pair<int, int>> q;
        q.push({startRow, startCol});

        while (!q.empty()) {
            auto [r, c] = q.front();
            q.pop();

            for (auto& dir : directions_) {
                int nr = r + dir[0];
                int nc = c + dir[1];
                if (!inside(nr, nc)) {
                    continue;
                }

                Cell& next = board_[nr][nc];
                if (next.state != State::Hidden || next.hasMine) {
                    continue;
                }

                next.state = State::Revealed;
                if (next.aroundMines == 0) {
                    q.push({nr, nc});
                }
            }
        }
    }
};
```

### 3.2 复杂度和边界

初始化：

```text
时间复杂度：O(R * C * 8 + M)
空间复杂度：O(R * C)
```

点击展开：

```text
时间复杂度：O(R * C)，最坏打开整张棋盘
空间复杂度：O(R * C)，队列最坏存很多格子
```

关键边界：

- 点击雷：直接打开雷，后续可由上层判断游戏失败。
- 点击数字：只打开当前格。
- 点击空白：BFS/DFS 打开连通空白，并打开边界数字。
- 已插旗格子不应被自动打开。
- 正式项目里还要处理首点不为雷、随机布雷、胜利判断和 UI 表现。

## 4. 八股追问

### 4.1 某一帧延迟很高，如何定位问题

回答思路：

```text
先确认现象
-> 判断 CPU/GPU/IO/GC/同步等待哪类瓶颈
-> 缩小到系统和函数
-> 复现并验证优化
```

定位步骤：

1. **看帧时间拆分**

在 Unity Profiler、Unreal Insights、RenderDoc、Xcode Instruments 或平台性能工具里查看这一帧：

- 主线程耗时
- 渲染线程耗时
- GPU 耗时
- GC Alloc 和 GC 时间
- 资源加载、文件 IO、网络等待
- Job/Task 是否有同步等待

2. **判断 CPU 还是 GPU**

```text
CPU 高：脚本、AI、物理、动画、资源加载、对象创建销毁、锁等待
GPU 高：DrawCall、Overdraw、后处理、阴影、粒子、带宽、复杂 Shader
```

3. **定位具体模块**

- 如果是脚本热路径，看函数调用树和 GC Alloc。
- 如果是渲染，看 draw call、batch、材质切换、overdraw、shader variant。
- 如果是资源加载，看同步加载、解压、反序列化、纹理上传。
- 如果是锁等待，看哪个线程持锁、等待多久、是否主线程等待后台任务。

4. **建立可复现样本**

只看一次尖峰不够，要记录：

- 场景、机型、画质、角色数量
- 触发操作
- 是否第一次进入
- 是否伴随资源加载或 shader warmup

5. **优化后验证**

用同一场景同一机型对比 P50/P90/P99 帧时间，不只看平均值。卡顿问题像地板上的钉子，平均高度不重要，扎脚的是最高那几颗。

### 4.2 CPU 瓶颈中实例创建/销毁过多，怎么做

先说结论：**减少高频创建销毁，把生命周期从“每次 new/delete”改成“预创建、复用、延迟回收”。**

常见方案：

- 对子弹、特效、飘字、伤害数字、音效源使用对象池。
- 初始化阶段预热对象，避免战斗中突然分配。
- 对短生命周期临时数据使用栈分配或帧分配器。
- 批量创建/销毁，避免每帧零散操作。
- 对资源加载和实例化拆分：资源提前加载，实例按需激活。
- 避免构造/析构里做重逻辑，把重置逻辑显式放到 `reset()`。

对象池基本接口：

```text
acquire：取一个可用对象，没有则扩容或返回空
release：归还对象，清理状态并标记可复用
clear：统一释放池内资源
```

注意：对象池不是万能药。如果对象数量长期只增不减，池会变成“精致版内存泄漏”。需要上限、回收策略和监控。

### 4.3 做过内存池吗，能用内存池构建对象池吗

可以这样回答：

```text
对象池管理对象生命周期和业务状态；内存池管理内存块的分配与回收。对象池可以建立在内存池之上，但两者不是一个层级的问题。
```

一个简单固定大小内存池：

```cpp
#include <cstddef>
#include <memory>
#include <vector>

class FixedBlockPool {
public:
    FixedBlockPool(std::size_t blockSize, std::size_t blockCount)
        : blockSize_(blockSize),
          storage_(std::make_unique<std::byte[]>(blockSize * blockCount)) {
        for (std::size_t i = 0; i < blockCount; ++i) {
            freeList_.push_back(storage_.get() + i * blockSize);
        }
    }

    void* allocate() {
        if (freeList_.empty()) {
            return nullptr;
        }
        void* p = freeList_.back();
        freeList_.pop_back();
        return p;
    }

    void deallocate(void* p) {
        if (p != nullptr) {
            freeList_.push_back(static_cast<std::byte*>(p));
        }
    }

private:
    std::size_t blockSize_ = 0;
    std::unique_ptr<std::byte[]> storage_;
    std::vector<std::byte*> freeList_;
};
```

用内存池构建对象：

```cpp
template <class T, class... Args>
T* create(FixedBlockPool& pool, Args&&... args) {
    void* mem = pool.allocate();
    if (mem == nullptr) {
        return nullptr;
    }
    return new (mem) T(std::forward<Args>(args)...);
}

template <class T>
void destroy(FixedBlockPool& pool, T* obj) {
    if (obj == nullptr) {
        return;
    }
    obj->~T();
    pool.deallocate(obj);
}
```

关键点：

- `allocate` 只拿内存，不会自动构造对象。
- placement new 负责在这块内存上构造对象。
- 归还前必须显式调用析构函数。
- 需要考虑对齐，真实实现不能只按 `blockSize` 粗暴切块。

### 4.4 用内存池思路管理对象

可以设计为“句柄 + 槽位 + 代际”的结构，避免外部长期持有裸指针。

```text
ObjectHandle:
  index      -> 槽位下标
  generation -> 代际版本

Slot:
  storage    -> 对象内存
  occupied   -> 是否占用
  generation -> 当前代际
```

创建对象：

```text
从 free list 拿一个槽位
-> placement new 构造对象
-> occupied = true
-> 返回 {index, generation}
```

销毁对象：

```text
校验 handle.index 和 handle.generation
-> 调用析构函数
-> occupied = false
-> generation++
-> index 放回 free list
```

访问对象：

```text
根据 index 找槽位
-> generation 一致且 occupied 才返回对象
-> 否则说明 handle 已过期
```

这样可以解决“删除一个对象后，旧引用误操作新对象”的问题。游戏里实体 ID、资源句柄、UI 节点句柄经常使用类似思路。

### 4.5 C# GC 的主要瓶颈在对象迁移吗，迁移会有什么问题

C# GC 的瓶颈不只是对象迁移。常见成本包括：

- 标记阶段扫描对象图。
- 暂停托管线程，也就是 Stop-The-World。
- 压缩阶段移动对象。
- 更新引用。
- 大对象堆碎片。
- Finalizer 和资源释放延迟。

对象迁移可以减少碎片，提高内存局部性，但也带来问题：

- 所有引用都要被正确更新。
- 移动过程中需要暂停或使用复杂的并发屏障。
- 和原生代码交互时，如果对象地址被传给 native，需要 pin 住对象；pin 多了会影响压缩效果。
- 大对象移动成本高，所以大对象堆策略通常更谨慎。

如果把这个思路搬到 C++ 内存池里，要非常小心：C++ 外部可能有裸指针、引用、迭代器、指针偏移。一旦移动对象，所有旧地址都会失效。没有运行时帮你全局更新引用，搬家就像把整个小区门牌号换了，但没有通知快递员。

### 4.6 删除一个数据会产生什么问题，有什么解决方式

如果直接把数组或池中的对象删除并移动后续对象，会导致：

- 外部裸指针悬空。
- 数组下标变化，旧索引指向错误对象。
- 迭代器失效。
- 对象析构时机不清晰。
- 多线程读写时出现 use-after-free。

解决方式：

- **句柄 + generation**：旧句柄可以被识别为失效。
- **swap-remove + handle 修正**：用最后一个对象覆盖删除位置，同时更新被移动对象的索引映射。
- **空洞 + free list**：删除后留下空槽，后续创建复用，避免移动其他对象。
- **延迟删除**：当前帧只标记死亡，到安全点统一析构和回收。
- **引用计数或生命周期系统**：适合共享资源，但不适合所有游戏对象。

工程里要根据访问模式选：

```text
需要稳定句柄：generation handle
需要遍历连续性：swap-remove + 映射
需要指针稳定：空洞 + free list
需要帧内安全：延迟删除
```

### 4.7 标记为脏的对象需要析构吗

要看“脏”的含义。

如果对象已经从业务逻辑上死亡，并且持有资源，就应该在合适时机析构：

- 释放堆内存。
- 关闭文件、句柄、网络连接。
- 归还 GPU/音频/物理资源。
- 解除事件订阅。

如果只是“这个槽位暂时不可用，等待同类型对象覆盖”，也不能跳过析构，除非满足非常严格条件：

- 类型是平凡析构的，比如纯 POD 数据。
- 没有持有外部资源。
- 不依赖析构做状态回滚。

对普通 C++ 对象，正确流程是：

```text
标记待删除
-> 到安全点调用析构
-> 槽位进入 free list
-> 下次复用时 placement new 重新构造
```

如果不析构就覆盖，轻则资源泄漏，重则订阅回调还在，死对象半夜起来收消息，调试体验很刺激。

### 4.8 ECS 是什么

ECS 通常指 Entity-Component-System：

- **Entity**：实体 ID，本身不承载逻辑，像游戏世界里的身份证号。
- **Component**：纯数据，如 Transform、Velocity、Health。
- **System**：处理拥有某些组件的一批实体，如 MovementSystem 更新所有 Transform + Velocity。

传统 OOP：

```text
Player.update()
Monster.update()
Bullet.update()
```

ECS：

```text
MovementSystem:
  遍历所有有 Transform 和 Velocity 的实体
  批量更新 position += velocity * dt
```

优势：

- 数据连续存放，缓存命中率更好。
- 系统批处理，适合大量实体。
- 组件组合灵活，不需要很深的继承树。
- 更容易做并行调度，因为系统依赖可以显式声明。

### 4.9 按自己的思路设计 ECS

一个简化设计：

```text
EntityManager
  创建/销毁 Entity
  维护 Entity 的 generation

ComponentStorage<T>
  按类型存储组件数组
  Entity -> component index 映射

System
  声明需要哪些组件
  遍历匹配实体并处理

Scheduler
  根据系统读写组件集合安排执行顺序或并行
```

组件示例：

```cpp
struct Entity {
    int index = 0;
    int generation = 0;
};

struct Transform {
    float x = 0.0f;
    float y = 0.0f;
};

struct Velocity {
    float vx = 0.0f;
    float vy = 0.0f;
};

void movementSystem(std::vector<Transform>& transforms,
                    const std::vector<Velocity>& velocities,
                    float dt) {
    for (std::size_t i = 0; i < transforms.size(); ++i) {
        transforms[i].x += velocities[i].vx * dt;
        transforms[i].y += velocities[i].vy * dt;
    }
}
```

更接近真实 ECS 的存储通常会按 Archetype/Chunk 组织：

```text
Archetype: 拥有同一组组件的实体集合
Chunk: 一段连续内存，存放这些实体的组件列
```

比如所有拥有 `Transform + Velocity + RenderMesh` 的实体放在一类 chunk 中。`MovementSystem` 只扫描包含 `Transform + Velocity` 的 chunk。

和面试中“分帧、优先级、chunk”的关系：

- **分帧**：低优先级系统可以隔帧或分批处理，如远处 NPC 感知。
- **优先级**：输入、物理、动画、渲染提交有明确顺序。
- **chunk**：让同类组件连续存储，提高缓存效率，并方便批量并行。

回答时可以承认：

```text
我之前把 ECS 理解成统一管理实体更新，这只说中了调度层。更核心的是把对象拆成 Entity ID、纯数据 Component 和批处理 System，用数据布局换性能。
```

