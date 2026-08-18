# 类型擦除与多态替代方案

## 1. 为什么不能只在"模板还是虚函数"中二选一

真实项目经常同时提出这些要求：

- 调用者不想知道具体类型；
- 具体类型不能或不应继承共同基类；
- 对象需要放进同一个容器；
- 类型集合可能开放，也可能固定；
- 有时需要值语义，有时需要对象身份；
- 热路径更关心连续内存，而不是继承层次。

C++ 提供的是一组工具箱：

```text
编译期已知类型
    |- 模板 / CRTP
    `- 重载

运行时选择类型
    |- 虚函数：开放类型集合 + 侵入式接口
    |- 类型擦除：开放类型集合 + 非侵入式接口
    |- variant：封闭类型集合 + 值语义
    |- 函数对象/回调：只替换一项行为
    `- 标签 + switch：显式分派，便于连续数据布局
```

这里的"替代"不是说虚函数过时，而是不同问题应选择不同形状的答案。

## 2. `std::variant`：封闭集合的运行时多态

### 2.1 基本用法

```cpp
#include <variant>

struct Circle {
    double radius;
};

struct Rectangle {
    double width;
    double height;
};

using Shape = std::variant<Circle, Rectangle>;

double area(const Shape& shape) {
    return std::visit([](const auto& value) -> double {
        using T = std::decay_t<decltype(value)>;

        if constexpr (std::is_same_v<T, Circle>) {
            return 3.14159 * value.radius * value.radius;
        } else {
            return value.width * value.height;
        }
    }, shape);
}
```

候选类型 `Circle` 和 `Rectangle` 在编译期列出，当前保存哪一种在运行时决定。

### 2.2 典型内存结构

```text
Shape variant
+----------------------------------+
| 能容纳最大候选类型的内联存储     |
+----------------------------------+
| 当前候选类型的索引 discriminator |
+----------------------------------+
```

`variant` 通常直接在对象内部保存值，不要求每项单独堆分配。它的大小至少要容纳最大候选类型，再加类型索引与对齐填充。

### 2.3 分派链

```text
std::visit(visitor, shape)
    -> 读取当前 index
    -> 选择该候选对应的访问入口
    -> 把具体类型引用传给 visitor
    -> 编译期实例化的重载 / if constexpr 执行
```

实现可能使用跳转表、分支树或其他方式，标准只规定行为。

### 2.4 优点与局限

优点：

- 候选类型穷尽可知；
- 访问者遗漏类型通常在编译期报错；
- 自然提供值语义；
- 常可避免逐对象堆分配；
- visitor 内看到具体类型，便于内联和优化。

局限：

- 新增候选类型需要修改 `variant` 定义；
- 所有相关 visitor 可能都要重新编译或修改；
- 最大候选特别大时，每个 `variant` 都会变大；
- 递归数据结构需要指针或包装；
- 跨 ABI 边界暴露标准库模板类型通常不稳妥。

适用场景：AST 节点、网络消息、有限状态机状态、编辑器命令、资源描述等**类型集合封闭**的问题。

## 3. 更清晰的 `variant` visitor

C++17 常用重载集合辅助器：

```cpp
template <typename... Ts>
struct Overloaded : Ts... {
    using Ts::operator()...;
};

template <typename... Ts>
Overloaded(Ts...) -> Overloaded<Ts...>;

std::string describe(const Shape& shape) {
    return std::visit(Overloaded{
        [](const Circle& circle) {
            return "circle r=" + std::to_string(circle.radius);
        },
        [](const Rectangle& rectangle) {
            return "rectangle " + std::to_string(rectangle.width)
                 + "x" + std::to_string(rectangle.height);
        }
    }, shape);
}
```

相比巨大的 `if constexpr`，每种类型的处理更独立。没有兜底泛型 lambda 时，新增候选更容易触发编译错误，形成穷尽检查。

## 4. 类型擦除：保留能力，隐藏具体类型

### 4.1 直觉

`std::vector<T>` 要求调用者知道 `T`；基类指针要求 `T` 继承基类。类型擦除提供第三条路：

```cpp
Circle circle;
Sprite sprite;

Drawable a = circle;
Drawable b = sprite;

a.draw();  // 调用 Circle 的绘制
b.draw();  // 调用 Sprite 的绘制
```

`Circle` 与 `Sprite` 不需要共同父类，只需要支持约定的 `draw()` 表达式。包装时，具体类型被藏进统一容器，只留下"可绘制"这一能力。

这就像寄存行李：

- 柜台给所有箱子统一编号；
- 柜台不关心里面是衣服还是键盘；
- 取件规则被保留下来；
- 如果里面是一只没关闹钟的钟，抽象层也救不了值班人员。

### 4.2 一个最小类型擦除包装

```cpp
#include <memory>
#include <utility>

class Drawable {
public:
    template <typename T>
    explicit Drawable(T value)
        : self_(std::make_unique<Model<T>>(std::move(value))) {}

    void draw() const {
        self_->draw();
    }

private:
    struct Concept {
        virtual ~Concept() = default;
        virtual void draw() const = 0;
    };

    template <typename T>
    struct Model final : Concept {
        explicit Model(T value) : value_(std::move(value)) {}

        void draw() const override {
            value_.draw();
        }

        T value_;
    };

    std::unique_ptr<Concept> self_;
};
```

它对外隐藏了继承，对内仍可用虚函数完成擦除后的分派：

```text
Drawable
  -> unique_ptr<Concept>
  -> Model<Circle>
  -> Circle value

draw()
  -> Concept::draw 虚分派
  -> Model<Circle>::draw
  -> Circle::draw
```

也可以不用语言虚函数，而在包装对象中保存函数指针表：

```text
[storage pointer / inline buffer]
[draw function pointer]
[destroy function pointer]
[move/copy function pointer]
```

两种实现思想相同：模板在包装时捕获具体类型，运行时只通过统一操作表访问。

### 4.3 值语义与小对象优化

上面的最小版本：

- 每个对象进行一次堆分配；
- 默认不可复制；
- 有一次擦除分派；
- 包装对象本身大小固定。

完整实现常增加：

- `clone` 操作以支持深拷贝；
- 移动构造与异常安全；
- small buffer optimization（SBO），小对象直接放在包装器内部；
- 对齐与析构函数指针；
- 空状态检查；
- `noexcept` 条件传播。

`std::function` 就是标准库中的函数调用类型擦除容器。具体实现通常保存调用入口与管理操作，并可能用 SBO 避免小闭包分配；标准不保证 SBO 的大小或一定发生。

## 5. 类型擦除和虚继承的差别

| 维度 | 虚函数基类 | 类型擦除 |
|---|---|---|
| 接口参与方式 | 具体类型继承接口 | 包装时适配所需表达式 |
| 侵入性 | 需要修改或适配类型层次 | 原类型无需知道接口 |
| 对象语义 | 常经指针表达身份 | 容易包装成值语义 |
| 类型集合 | 开放 | 开放 |
| 实现复杂度 | 语言直接支持 | 正确复制、SBO、异常安全较复杂 |
| 调试可见性 | 类层次较直接 | 中间多一层 Model/操作表 |
| 常见工具 | 抽象基类、`unique_ptr<Base>` | `std::function`、`std::any` 上层封装、自研 poly type |

如果类型天然属于同一对象层次，虚接口通常最清楚。如果想让第三方类型满足某项能力而不改变其定义，类型擦除更灵活。

## 6. `std::function`：只擦除一个可调用行为

```cpp
using Completion = std::function<void(Result)>;

void loadAsset(AssetId id, Completion onComplete);

loadAsset(id, [screen = weakScreen](Result result) {
    if (auto owner = screen.lock()) {
        owner->show(result);
    }
});
```

它适合：

- 回调；
- 延迟任务；
- 事件处理；
- 一项可替换策略。

成本可能包括：

- 间接调用；
- 闭包过大时动态分配；
- 复制闭包状态；
- 擦除后难以内联；
- 捕获对象生命周期错误。

如果只需传入一次且不拥有 callable，可使用模板参数或项目提供的 `function_ref`；如果需要拥有 move-only callable，可使用 C++23 `std::move_only_function` 或项目内等价类型。

不必为了一个 `onClick` 建立 `AbstractClickHandlerFactoryProvider` 继承家族。函数本来就能当值传递。

## 7. 组合与策略对象

继承表达"是一个"，组合表达"拥有一种能力或策略"：

```cpp
class Character {
public:
    explicit Character(std::unique_ptr<Movement> movement)
        : movement_(std::move(movement)) {}

    void update(float dt) {
        movement_->move(position_, dt);
    }

private:
    Position position_;
    std::unique_ptr<Movement> movement_;
};
```

与让 `FlyingCharacter`、`SwimmingCharacter`、`FlyingSwimmingCharacter` 不断派生相比，组合有这些优势：

- 可在运行时替换单项策略；
- 不把多个变化维度乘成继承组合；
- 生命周期和所有权更清楚；
- 每个策略接口可以很窄；
- 更容易单独测试。

策略本身可以用虚函数、类型擦除、函数对象或模板实现。**组合决定结构，多态决定替换方式**，二者不是同一维度。

## 8. 函数指针与显式操作表

C 风格插件 ABI 常使用函数表：

```cpp
struct RendererApi {
    void* context;
    void (*beginFrame)(void* context);
    void (*drawMesh)(void* context, const Mesh*);
    void (*endFrame)(void* context);
    void (*destroy)(void* context);
};
```

调用：

```cpp
api.drawMesh(api.context, &mesh);
```

它与 vtable 思路相似，但布局由开发者显式控制：

- 易于做 ABI 版本号和结构体大小检查；
- 可跨 C 语言边界；
- 创建和销毁函数明确属于同一模块；
- 缺少 C++ 自动类型检查与生命周期封装，需要外层 RAII 包装。

适合插件、驱动接口、跨编译器边界；普通业务代码通常优先使用更安全的 C++ 抽象。

## 9. 标签 + `switch` 什么时候反而更好

手写分派不是天然低级：

```cpp
enum class ShapeType : std::uint8_t {
    Circle,
    Rectangle
};

struct ShapeData {
    ShapeType type;
    // 索引或紧凑 union 数据
};
```

在以下场景中，显式标签可能更合适：

- 类型集合很小且稳定；
- 数据需要序列化或通过网络发送；
- 百万级对象需要紧凑连续布局；
- 希望按类型分组批处理；
- 分派点集中且容易穷尽；
- GPU/脚本/跨语言边界要求朴素数据。

坏处是新增类型要修改所有 `switch`，且很容易漏分支。`std::variant` 能在许多封闭集合场景提供更安全的标签联合；ECS 则进一步把行为放到系统，把数据按组件类型组织。

```text
面向对象热循环：
for each Entity* -> virtual update()

数据导向热循环：
for each Velocity chunk -> 连续批量更新 Position
```

后者放弃逐对象多态，换取缓存局部性、SIMD 和调度能力。

## 10. `std::any` 不是完整多态接口

`std::any` 可以保存任意可复制类型：

```cpp
std::any value = Circle{2.0};
```

但它只擦除了**存储类型**，没有定义 `draw()`、`update()` 等行为。调用者需要知道类型并 `any_cast<Circle>`，否则什么也做不了。

因此：

- 用 `any` 保存元数据、编辑器属性、弱类型扩展点可能合理；
- 用 `any_cast` 铺满核心业务逻辑，通常只是把类型判断藏得更深；
- 需要统一行为时，应在 `any` 之上定义操作注册表，或直接使用专用类型擦除包装。

## 11. 外部多态：适配无法修改的类型

第三方类型没有共同接口时，可以写适配器：

```cpp
class LegacyTextureAdapter final : public Texture {
public:
    explicit LegacyTextureAdapter(LegacyImage image)
        : image_(std::move(image)) {}

    int width() const override {
        return image_.getWidthInPixels();
    }

private:
    LegacyImage image_;
};
```

也可以通过类型擦除在包装时适配。适配器把接口转换集中在边界，避免让整个系统知道第三方 API 的命名和生命周期规则。

## 12. 选择矩阵

| 需求 | 优先考虑 |
|---|---|
| 类型编译期已知，追求内联 | 模板 / CRTP |
| 开放类型集合，类型天然共享接口 | 虚函数 |
| 开放类型集合，不想要求继承 | 类型擦除 |
| 封闭类型集合，需要值语义和穷尽检查 | `std::variant` |
| 只替换一个操作 | 函数对象 / `std::function` / `function_ref` |
| 稳定的跨动态库 C ABI | 显式函数表 |
| 巨量同构数据与热循环 | 标签分组 / ECS / SoA |
| 需要适配第三方旧接口 | Adapter 或类型擦除 |
| 只保存未知值，不定义统一行为 | `std::any` |

再加三个现实问题：

1. **谁拥有对象？** 这决定值、`unique_ptr`、共享句柄或非拥有视图。
2. **类型集合是否真的开放？** "以后可能扩展"不等于必须从第一天设计插件系统。
3. **变化发生在哪个方向？** 更常加类型还是更常加操作，答案会改变最佳结构。

## 13. 混合方案示例

资源管线可以这样分层：

```text
插件边界
    -> C ABI 函数表，保证版本兼容
引擎内部导入器集合
    -> unique_ptr<AssetImporter>，开放类型集合
导入结果
    -> variant<MeshData, TextureData, AudioData>，封闭值集合
完成通知
    -> std::function<void(ImportResult)>
数据处理热循环
    -> 模板算法 + 连续数组
```

同一个系统同时使用五种多态机制并不矛盾。真正可疑的是不管边界、数据和变化方向，一律用同一把锤子。

## 14. 本章小结

1. `variant` 适合封闭类型集合，常提供内联值存储和穷尽访问。
2. 类型擦除允许不相关类型通过统一包装表现同一能力，类型集合仍可开放。
3. `std::function` 是针对可调用对象的类型擦除，不应承担整套对象接口。
4. 组合负责拆分变化维度，内部策略再选择静态或动态分派。
5. 显式函数表适合稳定 ABI；标签分派和 ECS 适合紧凑数据与批处理。
6. `std::any` 只解决未知值存储，不自动提供统一行为。
7. 一个大型系统通常在不同边界组合多种机制，而不是全局押注一种。

[上一章：静态多态](./04-static-polymorphism.md) | [返回专题总览](./README.md) | [下一章：工程权衡与陷阱](./06-tradeoffs-and-pitfalls.md)
