# 多态的基本概念与分类

## 1. 从一个不断膨胀的判断开始

假设战斗系统要让不同单位行动：

```cpp
void update(Unit& unit, float dt) {
    if (unit.type == UnitType::Player) {
        updatePlayer(unit, dt);
    } else if (unit.type == UnitType::Monster) {
        updateMonster(unit, dt);
    } else if (unit.type == UnitType::Pet) {
        updatePet(unit, dt);
    }
}
```

这段代码的问题不是 `if` 本身，而是**调用者知道得太多**：

- 它知道全部单位类型；
- 它知道每种类型对应哪个函数；
- 每新增一个类型，它都要改；
- 类似判断很可能复制到渲染、序列化、碰撞等多个系统。

多态把它改写成：

```cpp
unit.update(dt);
```

至于调用 `Player::update` 还是 `Monster::update`，交给某种分派机制。调用者只表达意图，不再兼职类型户籍管理员。

## 2. 多态的三个组成部分

完整的多态设计通常包含三个角色：

| 角色 | 回答的问题 | 例子 |
|---|---|---|
| 接口 | 能做什么？ | `update(float)`、`draw()` |
| 实现 | 具体怎么做？ | 玩家读输入，怪物跑行为树 |
| 分派 | 这一次选谁做？ | 重载决议、模板实例化、查虚表、访问 `variant` |

可以用"插座"来理解：

- 插座规格是接口；
- 台灯、风扇和充电器是实现；
- 你插入哪个设备是分派；
- 把火锅底料塞进插座不属于多态，属于事故。

接口稳定不代表实现相同。恰恰相反，多态允许实现各自变化，只要求它们遵守同一个可调用约定。

## 3. "一个名字，多种形态"的三种经典分类

从类型理论角度，多态常被分成以下几类。工程中不必背术语，但理解它们能避免把所有机制都塞进"虚函数"这一个抽屉。

### 3.1 特设多态：同名操作有多份实现

函数重载和运算符重载属于这一类：

```cpp
void print(int value);
void print(std::string_view value);

print(42);       // 编译期选择 print(int)
print("hello");  // 编译期选择 print(string_view)
```

允许哪些类型、每种类型做什么，都是逐个定义的，所以称为"特设"。

### 3.2 参数多态：一份算法适用于许多类型

模板属于参数多态：

```cpp
template <typename T>
T clamp(T value, T low, T high) {
    return value < low ? low : (high < value ? high : value);
}
```

算法不关心 `T` 是 `int`、`float` 还是自定义定点数，只关心它支持比较和拷贝。编译器会为实际使用的类型实例化代码。

### 3.3 子类型多态：派生对象可以当作基类使用

虚函数是最常见的子类型多态：

```cpp
struct AudioSource {
    virtual ~AudioSource() = default;
    virtual void play() = 0;
};

struct FileAudio final : AudioSource {
    void play() override { /* 播放本地文件 */ }
};

struct StreamAudio final : AudioSource {
    void play() override { /* 拉取网络流 */ }
};

void start(AudioSource& source) {
    source.play();
}
```

`start` 只依赖 `AudioSource`，任何满足该基类契约的派生对象都可以传入。

## 4. 最实用的分类：何时决定调用目标

日常 C++ 讨论更常按**绑定时机**分成两大类。

### 4.1 静态多态

调用目标在编译期确定，也叫早绑定：

```cpp
template <typename T>
void tick(T& object) {
    object.update();
}

Player player;
tick(player);  // 编译器知道 T 是 Player
```

典型机制：

- 函数重载；
- 运算符重载；
- 函数模板和类模板；
- CRTP；
- C++20 Concepts 约束下的泛型代码。

### 4.2 动态多态

调用目标依赖运行时值，也叫晚绑定：

```cpp
void tick(Actor& actor) {
    actor.update();  // actor 实际引用谁，要到运行时才知道
}
```

典型机制：

- 继承与虚函数；
- 类型擦除，如 `std::function`；
- 封闭类型集合上的 `std::variant` + `std::visit`；
- 函数指针、回调表或手写标签分派。

`std::variant` 的候选类型在编译时已列出，但当前保存哪一种在运行时决定，因此它常被视作**运行时分派的封闭集合方案**。分类是为了帮助决策，不必为了给工具贴标签而争到编译器下班。

## 5. 静态类型与动态类型

理解动态多态必须分清两个概念：

```cpp
Dragon dragon;
Actor& actor = dragon;
actor.update();
```

- 表达式 `actor` 的**静态类型**是 `Actor&`，编译器从它判断哪些成员可调用。
- `actor` 当前引用对象的**动态类型**是 `Dragon`，虚分派从它判断最终执行哪个重写。

因此：

```text
静态类型决定："这个调用是否合法、虚表槽位是哪一个？"
动态类型决定："这个槽位里最终放的是谁的函数地址？"
```

非虚函数只看静态类型；虚函数才会继续根据动态类型分派。

## 6. 子类型替换：不只是"能编译"

动态多态通常隐含 **Liskov 替换原则（LSP）**：

> 如果调用者接受基类，那么换成任意派生类后，不应破坏调用者依赖的语义。

下面虽然能编译，设计却有问题：

```cpp
struct Bird {
    virtual void fly() = 0;
};

struct Penguin : Bird {
    void fly() override {
        throw std::logic_error("penguins cannot fly");
    }
};
```

`Penguin` 不是 `FlyingBird` 的可替换实现。更合理的抽象是把 `Bird` 和 `Flyable` 分开：

```cpp
struct Bird {
    virtual ~Bird() = default;
};

struct Flyable {
    virtual ~Flyable() = default;
    virtual void fly() = 0;
};
```

多态能隐藏类型差异，但不能把错误的"是一个"关系变正确。继承树画得再漂亮，企鹅也不会因为 UML 箭头而起飞。

## 7. 开放集合与封闭集合

选择多态方案前，先问：未来更常增加的是**类型**，还是**操作**？

### 7.1 开放类型集合

插件或第三方代码可以增加新类型，核心代码不能提前列全：

```text
Renderer
  |- OpenGLRenderer
  |- VulkanRenderer
  |- MetalRenderer
  `- 第三方未来新增的 Renderer
```

虚函数或类型擦除很合适。增加新类型时，调用者通常不用改。

### 7.2 封闭类型集合

所有类型由当前程序控制，例如 AST 节点、网络消息、状态机状态：

```cpp
using Message = std::variant<Login, Move, Attack, Logout>;
```

`std::variant` 很合适。新增一种操作只需增加一个 visitor；新增一种类型则会让所有不完整的 visitor 在编译期报错。

这对应经典的"表达式问题"：

| 设计 | 增加新类型 | 增加新操作 |
|---|---|---|
| 虚函数对象层次 | 容易 | 常要修改所有类型 |
| `variant` + visitor | 常要修改所有 visitor | 容易 |

没有永远占优的方向，只有项目变化轴是否判断正确。

## 8. 多态不负责什么

### 8.1 不负责对象所有权

`Actor*` 能触发虚分派，但看不出谁负责销毁。所有权应由 `std::unique_ptr<Actor>`、`std::shared_ptr<Actor>` 或明确的生命周期管理器表达。

### 8.2 不负责线程安全

虚函数不会自动加锁。两个线程同时修改同一个动态对象，照样需要同步。

### 8.3 不负责序列化

虚表指针是进程内实现细节，不能写入文件或网络。持久化应保存稳定的类型 ID 和数据，再由工厂重建对象。

### 8.4 不保证性能更好

消除一个 `switch` 不等于消除成本。虚分派可能降低内联机会，分散分配的对象也可能损害缓存局部性。性能必须结合调用频率和数据布局判断。

## 9. 第一轮选择指南

| 问题 | 倾向方案 |
|---|---|
| 类型在编译期已知，调用处是热路径 | 模板、重载、CRTP |
| 需要在一个容器中保存未知派生对象 | 虚函数或类型擦除 |
| 类型集合固定，需要穷尽检查 | `std::variant` + `std::visit` |
| 只有一个可替换行为 | 函数对象、回调、策略组合 |
| 跨动态库发布稳定 C API | 函数表或 PImpl，谨慎暴露 C++ 虚接口 |
| 数据量巨大，需要批处理 | 标签分组、ECS、SoA，避免逐对象虚调用 |

先选择最简单、最能表达变化方向的工具，再讨论微观开销。用一整套继承体系替换一个回调，就像为了挂一把钥匙先盖一座立体车库。

## 10. 本章小结

1. 多态由接口、实现和分派三部分组成。
2. 静态多态在编译期绑定，动态多态在运行期绑定。
3. 静态类型决定调用是否合法，动态类型决定虚调用落到哪个重写。
4. 继承必须满足可替换语义，"语法上是一个"不代表"设计上是一个"。
5. 开放类型集合常适合虚函数或类型擦除，封闭集合常适合 `variant`。
6. 多态与所有权、线程安全、序列化和数据布局是不同问题。

[上一章：专题总览](./README.md) | [返回 C++ 基础知识](../README.md) | [下一章：动态多态](./02-dynamic-polymorphism.md)
