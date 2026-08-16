# 动态多态：继承、虚函数与抽象接口

## 1. 最小可用模型

C++ 中最经典的动态多态由三件事组成：

1. 基类声明虚函数；
2. 派生类重写它；
3. 调用者通过基类指针或引用调用。

```cpp
#include <iostream>
#include <memory>
#include <vector>

struct Enemy {
    virtual ~Enemy() = default;
    virtual void attack() const = 0;
};

struct Slime final : Enemy {
    void attack() const override {
        std::cout << "Slime bounces into the player\n";
    }
};

struct Dragon final : Enemy {
    void attack() const override {
        std::cout << "Dragon breathes fire\n";
    }
};

int main() {
    std::vector<std::unique_ptr<Enemy>> enemies;
    enemies.push_back(std::make_unique<Slime>());
    enemies.push_back(std::make_unique<Dragon>());

    for (const auto& enemy : enemies) {
        enemy->attack();
    }
}
```

`enemy` 的静态类型始终是 `Enemy*`，但每个对象的动态类型不同，因此同一句 `enemy->attack()` 得到不同行为。

## 2. 虚函数到底改变了什么

普通成员函数采用静态绑定：

```cpp
struct Base {
    void speak() const { std::cout << "Base\n"; }
};

struct Derived : Base {
    void speak() const { std::cout << "Derived\n"; }
};

Derived derived;
Base& base = derived;
base.speak();  // Base：只看表达式的静态类型
```

加上 `virtual` 后采用动态绑定：

```cpp
struct Base {
    virtual void speak() const { std::cout << "Base\n"; }
};

struct Derived : Base {
    void speak() const override { std::cout << "Derived\n"; }
};

Base& base = derived;
base.speak();  // Derived：根据对象的动态类型分派
```

虚函数一旦在基类中声明，在后续派生类中始终保持虚函数性质；重写时不必重复写 `virtual`。现代 C++ 应写 `override`，让编译器核对"你以为在重写"是否真的在重写。

## 3. 抽象类与纯虚函数

### 3.1 用接口表达能力

纯虚函数用 `= 0` 声明：

```cpp
struct Renderer {
    virtual ~Renderer() = default;
    virtual void beginFrame() = 0;
    virtual void drawMesh(const Mesh& mesh) = 0;
    virtual void endFrame() = 0;
};
```

含有未实现纯虚函数的类是抽象类，不能直接创建对象。它表达"所有渲染器都必须提供这些操作"，但不替实现者决定具体 API。

### 3.2 纯虚函数也可以有定义

`= 0` 表示派生类必须重写，不表示函数体绝对不存在。纯虚析构尤其必须有定义，因为销毁派生对象时最终仍会执行基类析构：

```cpp
struct Interface {
    virtual ~Interface() = 0;
};

inline Interface::~Interface() = default;
```

普通纯虚函数也可以在类外提供定义，并通过限定名显式调用，但这种技巧少见，应避免让接口语义变得神秘。

## 4. `override` 与 `final`

### 4.1 `override` 防止"差一点就对了"

```cpp
struct Base {
    virtual void update(float dt) const;
};

struct Derived : Base {
    void update(double dt) const override;  // 编译错误：参数类型不一致
};
```

如果没有 `override`，这会悄悄声明一个新重载。程序能编译，但经基类调用时永远不会进入它。`override` 相当于让编译器做一次免费的代码审查。

常见不匹配包括：

- 少了 `const`；
- 参数类型不同；
- 引用限定符 `&` / `&&` 不同；
- `noexcept` 约束不兼容；
- 拼错函数名。

### 4.2 `final` 表达层次边界

```cpp
struct FixedRenderer final : Renderer {
    void beginFrame() override;
    void drawMesh(const Mesh&) override;
    void endFrame() override;
};
```

类上的 `final` 禁止继续继承；函数上的 `final` 禁止继续重写：

```cpp
void drawMesh(const Mesh&) final;
```

它既表达设计意图，也可能帮助编译器证明实际调用目标并去虚化。

## 5. 什么时候会发生虚分派

| 调用形式 | 是否虚分派 | 说明 |
|---|---|---|
| `basePtr->f()` | 是 | `basePtr` 指向多态对象且 `f` 为虚函数 |
| `baseRef.f()` | 是 | 引用保留对象的动态类型 |
| `derived.f()` | 可能被去虚化 | 语义仍是虚调用，但具体类型通常已知 |
| `baseObj.f()` | 不会到派生重写 | 对象本身就是 `Base` |
| `obj.Base::f()` | 否 | 限定名显式抑制虚分派 |
| 构造/析构中的 `f()` | 只到当前构造层 | 不分派到尚未构造或已析构的层 |

虚分派依赖对象身份。指针和引用指向原对象，因此保留动态类型；按值复制会生成一个新的基类对象。

## 6. 对象切片

### 6.1 切片如何发生

```cpp
struct Base {
    virtual ~Base() = default;
    virtual std::string name() const { return "Base"; }
};

struct Derived : Base {
    int extra = 42;
    std::string name() const override { return "Derived"; }
};

Derived derived;
Base copied = derived;  // 只复制 Base 子对象，Derived 部分被切掉
std::cout << copied.name();  // Base
```

这不是虚分派失灵，而是 `copied` 已经成为独立的 `Base` 对象，动态类型就是 `Base`。

### 6.2 如何避免

- 多态参数使用 `Base&` 或 `const Base&`；
- 可空观察关系使用 `Base*`；
- 独占所有权使用 `std::unique_ptr<Base>`；
- 共享所有权确有必要时使用 `std::shared_ptr<Base>`；
- 需要多态值语义时提供虚 `clone()`，或使用类型擦除容器。

```cpp
struct Base {
    virtual ~Base() = default;
    virtual std::unique_ptr<Base> clone() const = 0;
};

struct Derived final : Base {
    std::unique_ptr<Base> clone() const override {
        return std::make_unique<Derived>(*this);
    }
};
```

## 7. 为什么多态基类通常需要虚析构

```cpp
struct Base {
    ~Base() = default;  // 非虚
};

struct Derived : Base {
    std::vector<int> data;
};

Base* object = new Derived;
delete object;  // 未定义行为
```

通过基类指针删除派生对象，而基类析构非虚，会产生未定义行为。常见表现是只执行基类析构，派生资源没有释放，但标准允许出现更糟的结果。

经验规则：

> 基类析构应当是 **public virtual**，或者 **protected non-virtual**。

- `public virtual`：允许外部通过基类指针销毁多态对象。
- `protected non-virtual`：禁止外部经基类指针 `delete`，生命周期由派生类或其他机制管理。

如果一个类不准备被多态使用，不要只为了"看起来保险"添加虚析构；这会改变对象布局和 ABI。

## 8. 构造与析构期间的虚调用

```cpp
struct Base {
    Base() { initialize(); }
    virtual ~Base() { shutdown(); }

    virtual void initialize() { std::cout << "Base init\n"; }
    virtual void shutdown() { std::cout << "Base shutdown\n"; }
};

struct Derived : Base {
    void initialize() override { std::cout << "Derived init\n"; }
    void shutdown() override { std::cout << "Derived shutdown\n"; }
};
```

创建 `Derived` 时，`Base` 构造函数中的 `initialize()` 调用 `Base::initialize`，不会调用 `Derived::initialize`。析构时同理，进入 `Base` 析构后，`Derived` 部分已经结束生命周期。

原因不是编译器故意唱反调，而是安全性：

- 基类构造时，派生成员尚未初始化；
- 基类析构时，派生成员已经销毁；
- 此时调用派生实现会访问不存在的完整对象。

构造函数需要可变行为时，应使用工厂函数，在完整构造后再调用虚函数：

```cpp
class Object {
public:
    virtual ~Object() = default;

    template <typename T, typename... Args>
    static std::unique_ptr<Object> create(Args&&... args) {
        static_assert(std::is_base_of_v<Object, T>);

        auto object = std::make_unique<T>(
            std::forward<Args>(args)...
        );
        Object* base = object.get();
        base->postInitialize();
        return object;
    }

protected:
    virtual void postInitialize() = 0;
};
```

## 9. 默认参数不会动态分派

虚函数的函数体按动态类型选择，但默认参数在编译期按静态类型填入：

```cpp
struct Base {
    virtual void log(int level = 1) {
        std::cout << "Base " << level << '\n';
    }
};

struct Derived : Base {
    void log(int level = 2) override {
        std::cout << "Derived " << level << '\n';
    }
};

Derived derived;
Base& base = derived;
base.log();  // Derived 1
```

函数体来自 `Derived`，参数却来自 `Base`。这份"混搭套餐"合法但很容易误导。避免在虚函数的不同层级设置不同默认参数；更稳妥的做法是让非虚包装函数提供默认值。

## 10. 名称隐藏不是重写

派生类中出现同名函数，会隐藏基类的整组重载：

```cpp
struct Base {
    virtual void draw(int layer);
    void draw(std::string_view label);
};

struct Derived : Base {
    using Base::draw;  // 把基类重载重新引入作用域
    void draw(int layer) override;
};
```

没有 `using Base::draw` 时，`Derived` 对象直接调用 `draw("UI")` 可能无法找到基类版本。重写关注函数签名与虚槽位；名称查找发生得更早，是另一套规则。

## 11. 访问控制与虚分派是两步

```cpp
struct Base {
    virtual void execute() { std::cout << "Base\n"; }
};

struct Derived : Base {
private:
    void execute() override { std::cout << "Derived\n"; }
};

Derived derived;
Base& base = derived;
base.execute();  // 合法，执行 Derived::execute
```

编译器先根据静态类型 `Base` 检查 `execute` 是否可访问，再进行虚分派。派生重写即使是 `private`，也不阻止通过公开基类接口调用它。

这使 **非虚接口模式（NVI）** 成为可能：

```cpp
class Task {
public:
    void run() {
        validate();
        doRun();
        recordMetrics();
    }

    virtual ~Task() = default;

private:
    virtual void doRun() = 0;

    void validate();
    void recordMetrics();
};
```

公开非虚函数固定调用流程，私有虚函数只开放必要的变化点。这样比让派生类随意重写整个流程更容易维护不变量。

## 12. 协变返回类型

重写函数可以把"指向基类的指针/引用"返回值收窄为"指向派生类的指针/引用"：

```cpp
struct Enemy {
    virtual Enemy* cloneRaw() const = 0;
};

struct Dragon : Enemy {
    Dragon* cloneRaw() const override {
        return new Dragon(*this);
    }
};
```

这叫协变返回类型，只适用于指针或引用的类层次。`std::unique_ptr<Dragon>` 不能协变成 `std::unique_ptr<Enemy>` 的重写返回类型，因为它们是两个普通模板实例化类型。

现代代码通常仍优先返回 `std::unique_ptr<Base>`，换取清晰所有权：

```cpp
virtual std::unique_ptr<Enemy> clone() const = 0;
```

## 13. RTTI 与安全向下转型

多态基类可以使用 `dynamic_cast`：

```cpp
void inspect(Enemy& enemy) {
    if (auto* dragon = dynamic_cast<Dragon*>(&enemy)) {
        dragon->checkWings();
    }
}
```

- 指针转换失败返回 `nullptr`；
- 引用转换失败抛出 `std::bad_cast`；
- 需要运行时检查的向下或交叉转换要求源类型为多态类型，通常意味着至少有一个虚函数；单纯的公开向上转型不需要 RTTI；
- 转换依赖 RTTI，可能需要遍历类层次元数据。

偶尔在编辑器、序列化边界或兼容层使用是合理的；如果核心逻辑到处 `dynamic_cast`，通常说明基类接口缺少必要能力，或者对象层次承担了不相关职责。

能用虚函数表达行为时优先虚函数；能用 visitor 表达封闭集合操作时考虑 visitor。不要把 `dynamic_cast` 当作换了西装的 `if (type == ...)`。

## 14. 一次良好的动态接口设计

```cpp
class AssetImporter {
public:
    virtual ~AssetImporter() = default;

    // 稳定、窄且与实现无关的契约
    virtual bool supports(std::string_view extension) const = 0;
    virtual ImportResult import(const ImportRequest& request) = 0;
};

class PngImporter final : public AssetImporter {
public:
    bool supports(std::string_view extension) const override {
        return extension == ".png";
    }

    ImportResult import(const ImportRequest& request) override {
        // PNG 专属实现
        return {};
    }
};
```

它具有几个特点：

- 基类描述能力，不保存具体实现的偶然细节；
- 析构为虚，所有权可以安全交给 `unique_ptr<AssetImporter>`；
- 重写全部写 `override`；
- 叶子类不准备再扩展，因此写 `final`；
- 接口参数使用稳定值或视图，不暴露具体库内部类型；
- 调用者不需要通过向下转型才能完成日常工作。

## 15. 本章小结

1. 虚分派需要虚函数和保留动态类型的对象访问方式，通常是基类指针或引用。
2. 多态对象按值传递会切片；所有权与多态应分别用智能指针和虚接口表达。
3. 可通过基类删除对象时，基类析构必须是虚函数。
4. 构造和析构期间不会分派到更派生层。
5. 默认参数、名称查找、访问控制和虚分派是不同规则，不能混为一谈。
6. 新代码应使用 `override`，有明确层次边界时使用 `final`。
7. `dynamic_cast` 是边界工具，不应代替合理的行为接口。

[上一章：基本概念与分类](./01-concepts-and-classification.md) | [返回专题总览](./README.md) | [下一章：对象布局与调用链](./03-object-layout-and-dispatch.md)
