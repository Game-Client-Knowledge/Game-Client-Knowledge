# 静态多态：重载、模板、CRTP 与 Concepts

## 1. 核心：在编译期选好答案

静态多态把"调用哪个实现"的问题交给编译器：

```cpp
template <typename T>
void render(const T& object) {
    object.draw();
}

Sprite sprite;
Mesh mesh;

render(sprite);  // 实例化 render<Sprite>
render(mesh);    // 实例化 render<Mesh>
```

编译结束后，两个调用目标已经确定。运行时不需要读取对象动态类型，也不需要查虚表。

```text
源代码 render(sprite)
    -> 模板实参推导 T = Sprite
    -> 检查 Sprite 是否支持 draw()
    -> 生成/复用 render<Sprite>
    -> 优化器尝试内联 Sprite::draw
    -> 直接机器码
```

这类"只要你能做，我就接受你"的接口常被称为**鸭子类型**：走起来像鸭子、叫起来像鸭子，就先让它参加模板实例化；如果它其实是个闹钟，编译器会给出意见。

## 2. 函数重载：最直接的静态分派

```cpp
void serialize(BinaryWriter& out, int value);
void serialize(BinaryWriter& out, const Transform& value);
void serialize(JsonWriter& out, const Transform& value);
```

编译器根据以下信息选择最佳可行函数：

- 函数名；
- 实参数量；
- 实参静态类型；
- 隐式转换序列；
- 模板与非模板候选的优先规则；
- ADL 等名称查找规则。

优点是明确、错误定位直接；缺点是每种组合往往需要单独实现。它适合类型数量少、每种行为确实不同的场景。

### 2.1 运算符重载也是特设多态

```cpp
Vec3 a;
Vec3 b;
Vec3 c = a + b;  // 编译期选择 Vec3 对应的 operator+
```

运算符重载不会改变运算符优先级，也不会自动获得数学正确性。接口形状统一，不代表实现可以不讲道理；让 `operator+` 删除文件依然是合法但糟糕的设计。

## 3. 模板：参数多态

### 3.1 隐式接口

C++17 模板通常通过表达式是否成立来定义接口：

```cpp
template <typename T>
void updateAndDraw(T& object, float dt) {
    object.update(dt);
    object.draw();
}
```

`T` 不必继承某个基类，只要在实例化点支持所需表达式即可。这降低了类型之间的耦合：

```cpp
struct Particle {
    void update(float);
    void draw() const;
};

struct PreviewWidget {
    void update(float);
    void draw() const;
};
```

两个类型没有共同父类，也能复用同一算法。

### 3.2 模板实例化与单态化

对 `updateAndDraw<Particle>` 和 `updateAndDraw<PreviewWidget>`，编译器通常产生各自适配类型的实例，这一过程也叫**单态化（monomorphization）**。

收益：

- 参数和成员偏移都具体可知；
- 小函数容易内联；
- 常量传播和死代码删除更充分；
- 不需要对象内 vptr。

代价：

- 每个类型组合都要实例化和优化；
- 编译时间、调试信息和二进制体积可能增长；
- 模板实现通常必须放在头文件中，扩大重编译范围。

链接器可能合并机器码完全相同的实例，但不能把这种优化当作语言保证。

## 4. C++17 中约束模板

没有约束时，错误经常出现在模板函数体深处：

```cpp
struct Rock {};
Rock rock;
updateAndDraw(rock, 0.016f);  // 报错可能一路展开到 object.update
```

C++17 可以用检测惯用法和 `std::void_t` 提前识别能力：

```cpp
template <typename, typename = void>
struct is_updatable : std::false_type {};

template <typename T>
struct is_updatable<T, std::void_t<
    decltype(std::declval<T&>().update(std::declval<float>()))
>> : std::true_type {};

template <typename T,
          std::enable_if_t<is_updatable<T>::value, int> = 0>
void update(T& object, float dt) {
    object.update(dt);
}
```

它能工作，但语法噪音较大。SFINAE 的含义是"替换失败不是错误"：某个模板候选替换失败后退出候选集，而不是让整个编译立即失败。

## 5. C++20 Concepts：给隐式接口起名字

Concepts 把模板要求写成可读契约：

```cpp
template <typename T>
concept Updatable = requires(T object, float dt) {
    { object.update(dt) } -> std::same_as<void>;
};

template <Updatable T>
void update(T& object, float dt) {
    object.update(dt);
}
```

它仍然是静态多态：

- 约束在编译期检查；
- 不产生共同基类；
- 不向对象添加 vptr；
- 调用目标仍可直接确定和内联。

Concept 描述的是**语法与部分类型性质**，无法自动证明复杂语义。例如它能检查 `operator<` 返回可转成 `bool`，不能证明该比较满足严格弱序。

## 6. CRTP：把派生类型传回基类模板

CRTP（Curiously Recurring Template Pattern，奇异递归模板模式）：

```cpp
template <typename Derived>
class ActorBase {
public:
    void update(float dt) {
        // 公共流程
        static_cast<Derived&>(*this).updateImpl(dt);
    }
};

class Player final : public ActorBase<Player> {
public:
    void updateImpl(float dt) {
        // Player 专属逻辑
    }
};

class Monster final : public ActorBase<Monster> {
public:
    void updateImpl(float dt) {
        // Monster 专属逻辑
    }
};
```

调用：

```cpp
Player player;
player.update(0.016f);
```

编译器知道 `Derived = Player`，因此 `static_cast<Derived&>(*this).updateImpl(dt)` 是直接调用，通常可完全内联。

### 6.1 CRTP 适合什么

- 在多个类型间复用固定算法骨架；
- 给派生类自动生成比较运算符或计数能力；
- 编译期策略组合；
- 对调用开销和内联极敏感的基础设施；
- 不需要把不同派生类型放进同一个基类容器。

### 6.2 CRTP 不等于虚函数的无脑替代

`ActorBase<Player>` 与 `ActorBase<Monster>` 是两个不同类型：

```cpp
using PlayerActorBase = ActorBase<Player>;
using MonsterActorBase = ActorBase<Monster>;

static_assert(
    !std::is_same_v<PlayerActorBase, MonsterActorBase>
);
```

因此不存在一个可以直接作为两者统一容器元素的 `ActorBase` 具体类型。若要统一存储，仍需 `variant`、类型擦除、指针间接层或重新引入动态接口。

CRTP 还依赖一项无法由这次 `static_cast` 自动证明的约定：`ActorBase<X>` 的实际派生类必须真的是 `X`。错误地写成 `class Npc : public ActorBase<Player>` 可能导致未定义行为，可以通过把基类构造函数设为私有并只友元正确派生类等方式收紧约束。

此外，CRTP 会把实现暴露在头文件中，增加耦合和编译成本。一个项目里 CRTP 套 CRTP 再加十层策略模板，确实可能零虚调用，也可能让报错信息先跑完一场马拉松。

## 7. 策略模式的静态版本

把可变行为作为模板参数：

```cpp
struct LinearMovement {
    void move(Position& position, float dt) const {
        position.x += 10.0f * dt;
    }
};

struct NoMovement {
    void move(Position&, float) const {}
};

template <typename MovementPolicy>
class Projectile {
public:
    void update(float dt) {
        movement_.move(position_, dt);
    }

private:
    Position position_;
    [[no_unique_address]] MovementPolicy movement_;
};
```

编译器可以：

- 内联 `move`；
- 对 `NoMovement` 删除整个调用；
- 通过空基类优化或 C++20 `[[no_unique_address]]` 让无状态策略不额外占空间。

缺点是每种策略组合都会形成不同类型。若有 4 个移动策略、3 个碰撞策略、5 个渲染策略，理论组合可达 60 种，需要控制实例化范围。

## 8. `if constexpr`：模板内部的编译期分支

C++17 的 `if constexpr` 只实例化选中的分支：

```cpp
template <typename T>
void debugPrint(const T& value) {
    if constexpr (std::is_integral_v<T>) {
        std::cout << "integer: " << value;
    } else if constexpr (std::is_floating_point_v<T>) {
        std::cout << "float: " << value;
    } else {
        value.debugPrint();
    }
}
```

这不是运行时 `if`：

- 条件必须是编译期常量；
- 未选分支不会针对当前 `T` 实例化；
- 生成代码通常只保留命中的路径。

它适合少量类型能力分支。分支增长到十几个时，应考虑重载、tag dispatch、定制点或 Concepts，让每项行为回到独立实现。

## 9. tag dispatch 与定制点

### 9.1 tag dispatch

通过类型标签在编译期选实现：

```cpp
template <typename Iterator>
void advanceImpl(Iterator& it, int n, std::random_access_iterator_tag) {
    it += n;
}

template <typename Iterator>
void advanceImpl(Iterator& it, int n, std::input_iterator_tag) {
    while (n-- > 0) {
        ++it;
    }
}
```

调用者根据迭代器类别传入对应 tag。标准库长期使用这种方式在保持统一接口的同时选择更高效算法。

### 9.2 定制点

泛型库常允许用户为自定义类型提供同名自由函数，再通过 ADL 找到：

```cpp
using std::swap;
swap(a, b);  // 优先发现类型所在命名空间的专用 swap
```

现代 C++ 还使用 customization point object（CPO）统一名称查找和约束。它们本质上仍是编译期分派工具。

## 10. 静态多态的内存与调用结构

静态多态对象通常不需要多态元数据：

```text
Player object
+-------------------+
| position          |
| health            |
| input state       |
+-------------------+

call updateOne<Player>(player)
    -> direct call / inline Player::update
```

与动态多态相比，成本从运行时转移到了构建阶段：

| 阶段 | 静态多态的主要成本 |
|---|---|
| 解析 | 读取更多头文件与模板定义 |
| 实例化 | 为实际类型组合生成中间表示 |
| 优化 | 分别优化每个实例 |
| 链接 | 合并弱符号、模板实例与调试信息 |
| 运行 | 通常直接调用，常可内联 |

所谓"零成本抽象"不是完全没有成本，而是**不为未使用能力付运行时成本，并让抽象后的代码接近手写专用代码**。编译服务器会替你记住其他账单。

## 11. 控制模板代码膨胀

常见方法：

1. 把与 `T` 无关的大段逻辑移到普通 `.cpp` 函数。
2. 只让很薄的模板层完成类型适配。
3. 对常用类型做显式实例化，并用 `extern template` 避免重复实例化。
4. 避免在模板参数中编码不必要的值和策略组合。
5. 在不敏感的边界使用类型擦除，把无限实例化收敛成固定运行时接口。
6. 用构建耗时、二进制尺寸和指令缓存数据验证，而不是只看源码行数。

```cpp
// header
template <typename T>
void processBuffer(std::span<const T>);

extern template void processBuffer<float>(std::span<const float>);

// source
template void processBuffer<float>(std::span<const float>);
```

`std::span` 是 C++20；C++17 项目可以用项目内视图类型表达同样思路。

## 12. 静态与动态可以组合

一个常见高效结构是：

```text
系统边界：动态多态
    -> 选择 Vulkan / Metal / D3D 后端
后端内部：静态多态
    -> 模板化资源格式、批次大小、着色参数
热循环：具体类型与连续数据
```

外层动态接口控制模块耦合，内层模板让热点代码专门化。二者不是竞争阵营，而是分别解决运行时变化和编译期复用。

```cpp
struct RenderBackend {
    virtual ~RenderBackend() = default;
    virtual void submit(const RenderPacket&) = 0;
};

template <typename Vertex>
RenderPacket buildPacket(std::span<const Vertex> vertices) {
    // 编译期针对顶点格式专门化
}
```

## 13. 何时优先静态多态

- 类型在编译期已知；
- 算法需要适配大量值类型；
- 调用位于可测量的热点；
- 希望内联、向量化或常量折叠；
- 类型不需要经统一基类容器存储；
- 接口可以作为头文件契约暴露；
- 可接受更长的编译时间和更大的模板诊断。

不应优先使用的信号：

- 类型由运行时插件提供；
- 需要稳定 ABI；
- 调用者不能看到具体类型；
- 策略组合导致实例化爆炸；
- 业务逻辑更看重可读性和迭代速度，分派成本可忽略。

## 14. 本章小结

1. 重载、模板、CRTP 和 Concepts 都在编译期选择实现。
2. 模板通过隐式接口复用算法，Concepts 给这种接口加上可读约束。
3. 单态化提高专门化和内联机会，也可能增加编译时间与二进制体积。
4. CRTP 能复用静态流程，但不同派生参数形成不同类型，不能直接统一存储。
5. `if constexpr` 和 tag dispatch 用于模板内部的编译期分支。
6. 静态与动态多态可以分层组合，不必二选一。
7. "零运行时分派"不等于"零工程成本"。

[上一章：对象布局与调用链](./03-object-layout-and-dispatch.md) | [返回专题总览](./README.md) | [下一章：类型擦除与替代方案](./05-type-erasure-and-alternatives.md)
