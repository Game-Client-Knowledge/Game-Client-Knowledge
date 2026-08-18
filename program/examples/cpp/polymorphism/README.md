# C++ 多态综合示例

## 示例目标

本示例用三种方式表达"同一调用面对不同类型产生不同行为"：

1. 继承与虚函数：类型在运行时选择，适合开放类型集合。
2. 函数模板：类型在编译期确定，调用可以专门化和内联。
3. `std::variant` + `std::visit`：候选类型在编译期封闭，当前值在运行时选择。

相关原理可结合 [C++98/03 的继承与动态多态](../../../knowledge/cpp/01-cpp98.md)、
[C++11 的函数类型擦除](../../../knowledge/cpp/02-cpp11.md)和
[C++17 的 `std::variant`](../../../knowledge/cpp/04-cpp17.md)复习。

## 环境

- C++17 或更新标准；
- Clang、GCC 或兼容编译器；
- 不依赖第三方库。

## 编译运行

Clang：

```bash
clang++ -std=c++17 -Wall -Wextra -Wpedantic main.cpp -o polymorphism-demo
./polymorphism-demo
```

GCC：

```bash
g++ -std=c++17 -Wall -Wextra -Wpedantic main.cpp -o polymorphism-demo
./polymorphism-demo
```

## 预期输出

```text
== Dynamic polymorphism ==
Slime uses Bounce
Dragon uses Fire Breath

== Static polymorphism ==
Warrior uses Shield Bash
Mage uses Arcane Bolt

== Closed-set runtime polymorphism ==
Circle area: 12.5664
Rectangle area: 12
```

浮点数最后几位可能因标准库格式化行为略有差异。
