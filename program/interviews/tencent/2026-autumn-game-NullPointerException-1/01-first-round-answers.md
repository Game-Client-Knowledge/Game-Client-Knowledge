# 腾讯游戏客户端一面参考答案（面试者：？）

## 1. 使用说明

本文按[一面原题](./01.md)的顺序回答。

回答目标不是背定义，而是在面试中能按下面节奏展开：

```text
先给结论
-> 解释底层机制
-> 补充常见追问
-> 手撕题给出复杂度和可运行实现
```

手撕题默认使用 C++17。

## 2. 实习拷打

### 2.1 介绍项目，怎么做的，遇到的最大问题

项目题没有标准答案，重点是让面试官看到"你主导了哪些部分、遇到问题怎么定位和
取舍"。建议结构：

```text
1. 一句话背景：项目是做什么的，解决什么问题。
2. 我的职责：具体负责哪个模块，为什么由我负责。
3. 技术方案：关键选型一句话说清理由，不要展开成文档。
4. 最大问题：选一个能体现排查深度的（崩溃、性能、兼容性、逆向未知结构），
   按"现象 -> 定位过程 -> 根因 -> 修复 -> 验证"讲。
5. 结果：量化产出（帧率、耗时、崩溃率、功能完成度）。
```

讲"最大问题"时，定位过程比结论重要。面试官想确认你不是"试出来的"，而是有
日志、二分、抓包、反汇编或小实验等可复述的排查手段。

## 3. 八股

### 3.1 C++ 动态多态的实现机制

结论先行：动态多态通过**虚函数表（vtable）加虚表指针（vptr）**实现，调用点
经过一次运行时间接跳转完成动态分派。

机制拆解：

```text
1. 类含虚函数时，编译器为该类生成一张虚函数表，存于只读数据段，
   表中按声明顺序存放各虚函数的入口地址，通常还包含 RTTI 类型信息。
2. 每个该类对象（或其基类子对象）额外携带一个 vptr，指向所属类型的虚表。
3. 构造函数执行时会完成 vptr 的初始化：先指向基类虚表，逐层构造后指向
   最终类型的虚表。
4. 通过基类指针/引用调用虚函数时，编译器不直接生成函数地址，而是生成
   "取 vptr -> 查表槽位 -> 间接调用" 的指令序列，因此运行时才确定目标函数。
```

示例：

```cpp
struct Animal {
    virtual void speak() { /* ... */ }
};

struct Dog : Animal {
    void speak() override { /* ... */ }
};

Animal* p = new Dog;
p->speak(); // 通过 p->vptr 查表，跳转到 Dog::speak
```

追问点：

- 静态绑定发生在编译期（非虚函数、对象实体调用），动态绑定发生在运行期。
- C++ 标准没有规定必须用 vptr/vtable，这是主流编译器的通用实现。
- 构造函数和析构函数中的虚调用不走动态分派，调用的是当前正在构造/析构的
  那一层的实现。
- 基类析构函数应声明为 `virtual`，否则通过基类指针 `delete` 派生对象只会
  调用基类析构，造成派生部分泄漏。

### 3.2 多继承时的虚表指针排布

结论：**每个"带虚函数的基类子对象"都有自己的 vptr**。派生类对象的排布
通常按基类声明顺序依次放置各基类子对象，之后是派生类自己的非静态成员。

示例：

```cpp
struct A { virtual void fa(); int a; };
struct B { virtual void fb(); int b; };
struct C : A, B { virtual void fc(); int c; };
```

典型对象布局：

```text
C 对象:
[A 子对象] vptr_A, a
[B 子对象] vptr_B, b
c
```

所以 C 对象里有 **2 个 vptr**：

- `vptr_A` 指向 C 的主虚表（primary vtable），继承链上第一个多态基类
  （这里是 A）作为主基类，其 vptr 位置通常与对象首地址重合。
- `vptr_B` 指向 C 的次级虚表（secondary vtable），对应 B 子对象。

派生类新增的虚函数（如 `fc`）通常并入主虚表。把 B 型指针传给需要 A 的
接口、或调用 B 子对象位置相关的虚函数时，编译器可能通过 thunk 调整 `this`
指针的偏移。

### 3.3 多继承时的虚表怎么存储

结论：虚表是**编译期生成、存放在只读数据段里的函数地址数组**，每个多态
类型对应若干张表（一张主表加若干次级表）。

主虚表大致包含：

```text
offset-to-top（子对象到对象头的偏移，虚基类场景更关键）
RTTI 类型信息指针
虚函数槽位：A 的虚函数、C 新增虚函数
```

次级虚表大致包含：

```text
offset-to-top：B 子对象相对 C 对象头的偏移（如 16）
RTTI 类型信息指针
虚函数槽位：B 的虚函数
```

当 B 型指针调用 C 重写的虚函数时，需要把 `this` 从 B 子对象位置调回 C 对象
起始位置。编译器在次级虚表的相应槽位填入 **thunk**（如 `add this, -16` 后
跳转到真实实现），而不是直接填函数地址。

总结：虚表本身是类级别数据、全局共享；对象级别只保存指向虚表的指针。

### 3.4 子类完全没有覆盖父类虚函数时，虚表指针指向哪

结论：子类对象的 vptr 指向的仍然是**父类的虚表**（与父类共享同一张表），
编译器不需要为子类生成新表——前提是子类也没有声明新的虚函数。

```text
struct A { virtual void f(); };
struct B : A { /* 未覆盖，也未新增虚函数 */ };

B 对象: [vptr -> A 的虚表]
```

此时通过 `B*` 调用 `f()` 与通过 `A*` 调用行为一致，查表得到 `A::f`。

如果子类新增了虚函数（即使一个都没覆盖），编译器就要为子类生成自己的虚表：
父类的槽位原样拷贝，新虚函数追加到新槽位。

### 3.5 部分覆盖时虚表表项是什么样的

结论：编译器为子类生成**一张新虚表**，表项规则是：

```text
未被覆盖的虚函数槽位：保留父类实现的地址
被覆盖的虚函数槽位：替换为子类重写版本的地址
新增虚函数槽位：追加在表尾（主虚表场景）
RTTI 类型信息：更新为子类类型
```

示例：

```cpp
struct A { virtual void f1(); virtual void f2(); };
struct B : A { void f1() override; };
```

B 的虚表：

```text
[f1] -> B::f1
[f2] -> A::f2
```

多继承下，覆盖来自次级基类的虚函数时，对应槽位可能填入 thunk 而不是直接填
子类函数地址，用于修正 `this` 偏移。

### 3.6 菱形继承怎么解决

问题：菱形继承会产生两份基类子对象，导致数据冗余和二义性。

```cpp
struct A { int x; virtual void f(); };
struct B : A {};
struct C : A {};
struct D : B, C {}; // D 里有两份 A：B::A 和 C::A
```

解决方式：**虚继承**，让最底层的派生类只保留一份共享的虚基类子对象。

```cpp
struct B : virtual A {};
struct C : virtual A {};
struct D : B, C {}; // 只有一份 A
```

实现要点：

```text
1. 虚基类子对象只构造一次，由最派生类（most derived class）负责构造。
2. 对象布局中虚基类子对象通常放在对象末尾（实现相关），B、C 子对象通过
   偏移信息间接访问它，不再把 A 直接嵌在自身内部。
3. 编译器在虚表中记录虚基类偏移（或引入 vbtable），访问虚基类成员时先
   算偏移再寻址，因此虚继承的访问比普通继承多一次间接寻址。
4. 最派生类的构造函数初始化虚基类，中间层的初始化列表对虚基类无效。
```

追问点：

- 虚继承解决数据冗余，但带来额外空间（偏移表）和间接访问成本。
- 菱形继承尽量通过组合或接口替代，实际工程中慎用多继承 + 虚继承的组合。

### 3.7 C++ 的内存模型

通常面试问的是**进程虚拟地址空间的划分**。一个 C++ 进程运行后，操作系统
提供独立虚拟地址空间，典型区域：

| 区域 | 内容 |
|---|---|
| 代码段 | 机器指令，只读可执行 |
| 只读数据段 | 字符串字面量、`const` 全局常量 |
| 已初始化数据段 | 已初始化全局变量、静态变量 |
| BSS 段 | 未初始化/零初始化的全局与静态变量，加载时清零 |
| 堆 | `new`/`malloc` 动态分配，程序控制生命周期 |
| 栈 | 函数调用栈，局部变量、返回地址、栈帧 |
| 内存映射区 | 动态库、文件映射、匿名映射 |
| TLS | `thread_local` 对象，每线程独立副本 |

从 C++ 对象视角补充：

```text
局部自动变量 -> 栈
new 出来的对象本体 -> 堆（指针变量本身可能在栈上）
全局/静态变量 -> 静态存储区
带虚函数的对象额外包含 vptr
```

注意区分另一种"内存模型"：如果面试官指的是 C++11 并发内存模型（memory
order、happens-before、原子操作），要说明那描述的是多线程下内存操作的可见性
和顺序规则，与地址空间划分是两回事。可以主动确认面试官问的是哪一个。

### 3.8 C++ 的内存对齐

结论：编译器为每个类型计算**对齐值（alignment）**，成员在对象内的偏移必须
是对齐值的整数倍，对象大小也要补齐到对齐值的整数倍。

```cpp
struct S {
    char c;  // 偏移 0
    int  i;  // 偏移 4（1 之后补 3 字节 padding）
};
// sizeof(S) == 8，alignof(S) == 4
```

规则要点：

```text
1. 每个成员的对齐值 = min(类型自然对齐, 编译期指定的 pack 值)。
2. 成员偏移必须是该成员对齐值的整数倍。
3. 结构体对齐值 = 各成员对齐值的最大值。
4. 结构体大小 = 对齐值的整数倍，末尾补 padding。
```

查询和控制手段：

```cpp
#include <cstddef>
alignof(S);                      // 查询对齐值
alignas(16) int x;               // 提高对齐
#pragma pack(push, 1)            // 降低对齐（非标准但主流支持）
#pragma pack(pop)
```

### 3.9 为什么需要内存对齐，强制对齐的好处和坏处

为什么需要：

```text
1. 硬件要求：部分架构（老 ARM 等）访问未对齐地址会直接异常；x86 允许但慢。
2. 性能：未对齐的读可能跨越两个缓存行，一次访存变两次内存事务。
3. 原子性：原子指令和 SIMD 指令通常要求自然对齐。
4. 可移植性：自然对齐是跨平台安全访问的保证。
```

"强制对齐"这里通常指 `#pragma pack(1)` 这类压缩对齐：

```text
好处：
- 节省内存，缩小结构体体积，对海量对象或网络序列化结构有明显收益。
- 按字节紧凑打包，便于与外部二进制格式（协议、文件头）一一对应。

坏处：
- 未对齐访问性能下降，编译器要插入额外指令拆分读写。
- 部分硬件直接报错，跨平台风险。
- 破坏自然对齐后，对需要原子性的字段不再安全，可能引入隐蔽问题。
```

反向的过度对齐（`alignas(64)`）也会浪费内存，但可以把热点变量放进独立的
缓存行，减少多线程下的伪共享。

### 3.10 线程和进程的区别

| 维度 | 进程 | 线程 |
|---|---|---|
| 资源 | 拥有独立地址空间、文件表等资源 | 共享所属进程的地址空间与资源 |
| 调度 | 资源分配单位 | CPU 调度的基本单位 |
| 隔离 | 一个进程崩溃不影响其它进程 | 一个线程崩溃可能拖垮整个进程 |
| 通信 | IPC（管道、消息队列、共享内存等），成本高 | 直接读写共享内存，成本低 |
| 切换开销 | 切换地址空间等，开销大 | 只切换寄存器和栈，开销小 |

补充：线程有独立的栈、寄存器和程序计数器，共享代码段、数据段、堆和文件
描述符。进程内多线程可以利用多核并行，但共享内存带来数据竞争问题，需要
锁或原子操作保护。

## 4. 虚幻

### 4.1 用 GAS 制作投掷烟雾弹动作

GAS 核心组成先交代清楚：

```text
ASC（AbilitySystemComponent）：能力的宿主组件，挂在拥有者 Actor 上。
GameplayAbility（GA）：一次"能力"的执行单元，即"投掷烟雾弹"。
GameplayEffect（GE）：修改属性/施加状态的手段，烟幕区域效果可用它实现。
GameplayCue（GC）：纯表现层特效、音效，客户端自动播放，不参与逻辑。
GameplayTag：能力的标签体系，用来匹配、取消、免疫等。
AttributeSet：拥有者属性集合（血量、弹药等）。
```

制作投掷烟雾弹的典型流程：

```text
1. 定义 GA_ThrowSmokeGrenade : UGameplayAbility
   - AbilityTags 打上 Ability.ThrowSmoke 标签。
   - 配置 Cost GE（消耗道具/弹药）与 Cooldown GE（冷却）。
   - 声明 ActivateAbility 重写。
2. 输入触发：玩家输入通过 ASC 按 AbilityTag 激活该能力。
3. 激活后：
   - 先 CommitAbility，检查消耗与冷却是否满足。
   - 用 PlayMontageAndWait 任务播放投掷动画，等待 AnimNotify "Throw"
     作为出手时机（比固定延迟更可靠）。
   - 出手时 SpawnActor 生成烟雾弹抛射物（初始速度/落点来自瞄准方向）。
4. 烟雾弹落地或命中后：
   - 对范围内目标施加区域 GameplayEffect：打上"处于烟幕中"的 Tag
     或修改可见性相关属性。
   - 烟雾的遮蔽效果属于玩法判定（视线检测读 Tag），特效由 GameplayCue
     播放烟幕粒子，网络同步走 GC 的预测与复制。
5. EndAbility 收尾，冷却进入 CD。
```

简化示意：

```cpp
UCLASS()
class UGA_ThrowSmokeGrenade : public UGameplayAbility
{
    GENERATED_BODY()

public:
    virtual void ActivateAbility(
        const FGameplayAbilitySpecHandle Handle,
        const FGameplayAbilityActorInfo* ActorInfo,
        const FGameplayAbilityActivationInfo ActivationInfo,
        const FGameplayEventData* TriggerEventData) override;

    UPROPERTY(EditDefaultsOnly) TSubclassOf<AActor> SmokeGrenadeClass;
    UPROPERTY(EditDefaultsOnly) UAnimMontage* ThrowMontage;
};
```

追问点：

- 能力通过 `GiveAbility` 授予，配置在 DefaultAbilities 数组或运行时授予。
- 简单做法可只让服务器执行逻辑、客户端通过 GameplayCue 表现；GAS 也支持
  客户端预测激活，需配合 prediction key。
- 烟雾弹本体只是"视觉抛射物 + 落地触发器"，判定权威在服务器。

### 4.2 动画蓝图中用于程序化动画制作的节点

程序化动画指用算法/物理实时调整骨骼，而不是纯播放动画资产。常用节点：

```text
Two Bone IK：对两段骨骼链求解 IK（脚贴地、手摸墙、持枪手部稳定）。
FABRIK：对多段骨骼链求解 IK，手臂抓取、脊骨贴合地形。
Modify Bone（Transform Bone）：在局部空间直接修改单根骨骼的变换。
AnimDynamics：给骨骼链加物理模拟，毛发、挂坠等自然摆动。
Physics Blend：在动画与物理模拟之间按权重混合。
Leg IK：UE5 内置的足部贴地/斜坡适配节点。
Layered Blend per Bone：按骨骼分层混合两个动画（偏混合而非生成）。
```

最稳妥的回答组合：**Two Bone IK 和 Modify Bone**。前者体现"程序化解算骨骼
姿态"，后者体现"程序化修改骨骼变换"，再补一句 FABRIK 作为扩展。

## 5. 手撕

### 5.1 LRU

要求：`get` 和 `put` 都是 O(1)。数据结构：`list` 维护访问顺序（表头最新、
表尾最旧），`unordered_map` 把 key 映射到链表节点迭代器。

```cpp
#include <list>
#include <unordered_map>
#include <utility>

class LRUCache {
    int capacity = 0;
    std::list<std::pair<int, int>> cache;                 // 表头最新
    std::unordered_map<int, std::list<std::pair<int, int>>::iterator> index;

public:
    explicit LRUCache(int cap) : capacity(cap) {}

    int get(int key) {
        auto it = index.find(key);
        if (it == index.end()) {
            return -1;
        }

        int value = it->second->second;
        cache.splice(cache.begin(), cache, it->second);   // 移到表头
        return value;
    }

    void put(int key, int value) {
        auto it = index.find(key);
        if (it != index.end()) {
            it->second->second = value;
            cache.splice(cache.begin(), cache, it->second);
            return;
        }

        if (static_cast<int>(cache.size()) == capacity) {
            int lastKey = cache.back().first;
            cache.pop_back();                              // 淘汰最久未使用
            index.erase(lastKey);
        }

        cache.emplace_front(key, value);
        index[key] = cache.begin();
    }
};
```

复杂度与追问：

```text
get / put 时间复杂度：O(1)
空间复杂度：O(capacity)

追问：
- splice 直接移动节点，不拷贝数据。
- 淘汰的是链表尾（最久未访问）。
- 并发场景需要外部加锁，或换成分段锁/近似 LRU。
```

### 5.2 单例模式

推荐 C++11 的局部静态变量（Meyers 单例）：

```cpp
class GameConfig {
public:
    static GameConfig& Instance() {
        static GameConfig instance;   // 首次调用时初始化
        return instance;
    }

    GameConfig(const GameConfig&) = delete;
    GameConfig& operator=(const GameConfig&) = delete;

private:
    GameConfig() = default;
};
```

要点：

```text
1. C++11 起局部静态变量初始化是线程安全的：多线程首次进入时只有一个线程
   执行构造，其余线程等待。
2. 懒汉式：第一次调用 Instance 才构造，避免启动开销。
3. 拷贝构造和赋值删掉，防止产生第二个实例。
```

追问点：

- 传统双重检查锁定（DCLP）在 C++11 前因指令重排可能返回半构造对象，现代
  写法用 `std::once_flag` 或直接局部静态变量替代。
- 单例的缺点：隐式全局状态、测试困难、析构顺序不确定（跨翻译单元静态对象
  析构顺序未定义）。
- 需要显式控制生命周期时可使用 `std::unique_ptr` 持有，但通常没必要。

## 6. 相关复习

- [C++ 专题](../../../knowledge/cpp/README.md)
- [UObject 反射、GC 与生命周期](../../../knowledge/interview-roadmap/unreal-engine/02-uobject-reflection-gc-and-lifecycle.md)
- [蓝图与 C++ 协作](../../../knowledge/interview-roadmap/unreal-engine/04-blueprints-and-cpp-collaboration.md)
