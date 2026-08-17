# 2026 网易游戏客户端一面参考答案

## 1. 使用说明

本文按[一面原题](./01.md)的顺序回答。

面试回答建议按这个节奏展开：

```text
先给一句话结论
-> 解释底层机制
-> 主动补充工程注意点
-> 算法题给出复杂度和边界条件
```

算法题默认使用 C++17。

## 2. 八股

### 2.1 C# 的引用类型和值类型有什么区别

一句话回答：**值类型变量通常直接保存数据本身，引用类型变量保存对象的引用；赋值时值类型复制数据，引用类型复制引用。**

常见值类型：

- `int`、`float`、`bool`、`char`
- `struct`
- `enum`
- `Nullable<T>`

常见引用类型：

- `class`
- `string`
- `array`
- `delegate`
- `interface`

可以这样展开：

```text
值类型像把道具本体放进背包格子里，复制时是再拷贝一把剑。
引用类型像背包格子里放的是仓库地址，复制时是多拿了一张同一个仓库的门牌。
```

主要区别：

- **存储内容不同**：值类型变量保存值本身；引用类型变量保存对象引用。
- **赋值语义不同**：值类型赋值会复制数据；引用类型赋值后两个变量通常指向同一个对象。
- **默认值不同**：值类型默认是零值；引用类型默认是 `null`。
- **继承能力不同**：`struct` 不能继承其他 `struct` 或 `class`，但可以实现接口；`class` 支持继承。
- **GC 压力不同**：普通值类型作为局部变量、字段或数组元素时不一定产生独立堆对象；引用类型对象通常在托管堆上，由 GC 管理。

需要注意：不要简单说“值类型一定在栈上，引用类型一定在堆上”。这句话太粗糙。比如值类型作为类字段时会跟随对象一起在堆上；引用类型变量本身也可能是栈上的一个引用。

示例：

```csharp
struct Pos {
    public int X;
}

class Player {
    public int Hp;
}

var a = new Pos { X = 1 };
var b = a;
b.X = 2;
// a.X 仍然是 1

var p1 = new Player { Hp = 100 };
var p2 = p1;
p2.Hp = 50;
// p1.Hp 也变成 50
```

游戏客户端里常见取舍：

- 小而不可变的数据，如坐标、颜色、矩形，适合用 `struct`。
- 有身份、生命周期和共享状态的对象，如角色、技能、UI 面板，适合用 `class`。
- 频繁装箱的值类型会产生额外 GC 压力，比如把 `int` 放进非泛型 `ArrayList`。

### 2.2 C# 的 GC

C# 的 GC 是托管堆的自动内存管理机制。它负责找出已经不可达的对象并回收内存，减少手动 `delete/free` 带来的泄漏、悬空指针和重复释放问题。

核心流程可以概括为：

```text
从根对象出发
-> 标记所有仍可访问的对象
-> 回收不可达对象
-> 必要时压缩/整理堆内存
```

常见 GC Roots 包括：

- 当前线程栈上的引用
- 静态字段
- CPU 寄存器中的托管引用
- 运行时内部结构引用
- 已注册的句柄

.NET/Unity 常见托管 GC 具有分代思想：

- **Gen 0**：新创建对象，大部分对象“朝生暮死”，优先回收这里。
- **Gen 1**：中间层，作为 Gen 0 和 Gen 2 的缓冲。
- **Gen 2**：长期存活对象，回收成本更高。
- **Large Object Heap**：大对象堆，大对象分配和碎片问题更敏感。

面试中可以补充 Unity 场景：

- 频繁 `new` 临时对象、字符串拼接、LINQ、闭包、装箱、临时数组，都会增加 GC 压力。
- 一帧内 GC 触发可能造成卡顿，表现为 Profiler 里某一帧突然升高。
- 优化方向不是“完全不用 GC”，而是控制分配频率和生命周期。

常见优化：

- 复用容器，如 `List<T>.Clear()` 后复用容量。
- 避免每帧创建临时字符串，UI 文本变化可以做节流。
- 对高频对象使用对象池。
- 避免装箱，例如泛型集合替代非泛型集合。
- 谨慎使用 LINQ 和闭包，尤其是 `Update()` 热路径。

### 2.3 C++ 的智能指针

智能指针的核心是用 RAII 管理资源生命周期：对象构造时获得资源，析构时释放资源。这样异常返回、提前 `return` 时也能自动清理。

常见智能指针：

- `std::unique_ptr<T>`：独占所有权，不能复制，可以移动。适合表达“这个对象只有一个主人”。
- `std::shared_ptr<T>`：共享所有权，通过控制块里的引用计数管理对象生命周期。
- `std::weak_ptr<T>`：弱引用，不增加强引用计数，常用于打破 `shared_ptr` 循环引用。

`shared_ptr` 可以这样解释：

```text
shared_ptr 对象
-> 指向被管理对象
-> 指向控制块 control block
-> 控制块保存强引用计数、弱引用计数、删除器、分配器等
```

常见追问：

- 引用计数通常是原子增减，但被管理对象本身不是自动线程安全的。
- 不要让两个独立 `shared_ptr` 接管同一个裸指针，否则会出现两个控制块，最终重复释放。
- 优先使用 `std::make_shared<T>()`，通常能把对象和控制块合并分配，减少堆分配次数。
- `shared_ptr` 循环引用需要用 `weak_ptr` 打断。

示例：

```cpp
#include <memory>

struct Node {
    int value = 0;
    std::weak_ptr<Node> parent;
    std::shared_ptr<Node> child;
};

auto root = std::make_shared<Node>();
auto child = std::make_shared<Node>();
root->child = child;
child->parent = root; // weak_ptr 不延长 root 生命周期
```

### 2.4 C++ 的左值和右值

简单说：**左值有稳定身份，通常可以取地址；右值更偏临时结果，常用于移动语义。**

例子：

```cpp
int x = 1;
int y = x + 2;
```

- `x` 是左值，因为它有名字、有稳定地址。
- `1` 和 `x + 2` 是右值，因为它们是临时值。

现代 C++ 里更准确的分类包括：

- **lvalue**：有身份，不能直接绑定到普通右值引用。
- **prvalue**：纯右值，如 `42`、`makeObj()` 返回的临时对象。
- **xvalue**：将亡值，如 `std::move(x)`，有身份但资源可以被移动。

`std::move` 本身不移动对象，它只是把表达式强制转换成右值引用，让移动构造/移动赋值有机会被调用：

```cpp
std::string a = "hello";
std::string b = std::move(a); // 调用移动构造，a 进入有效但未指定状态
```

一个常见面试坑：**有名字的右值引用变量本身是左值表达式。**

```cpp
void f(std::string&& s) {
    g(s);            // s 是左值表达式
    g(std::move(s)); // 这里才是右值表达式
}
```

这个点会直接影响下一道情景代码。

### 2.5 构造、拷贝、移动的输出顺序

原题代码省略了分号和访问控制，按意图补成类似结构：

```cpp
#include <iostream>
#include <utility>

struct A {
    A() { std::cout << "a"; }
    A(A& a) { std::cout << "b"; }
    A(A&& a) { std::cout << "c"; }
    A& operator=(A&& a) {
        std::cout << "d";
        return *this;
    }
};

struct B : A {
    B() : A() { std::cout << "1"; }
    B(B& b) : A(b) { std::cout << "2"; }
    B(B&& b) : A(b) { std::cout << "3"; }
    B& operator=(B&& b) {
        std::cout << "4";
        return *this;
    }
};
```

逐句分析：

```cpp
B b1;
```

先构造基类 `A`，再构造派生类 `B`：

```text
a1
```

```cpp
B b2 = b1;
```

`b1` 是左值，调用 `B(B& b)`。在 `B` 的拷贝构造里，`A(b)` 中的 `b` 是一个 `B&` 左值，可以向上转成 `A&`，所以调用 `A(A&)`：

```text
b2
```

```cpp
B b3(b1);
```

同样调用 `B(B& b)`：

```text
b2
```

```cpp
B b4(std::move(b1));
```

`std::move(b1)` 让外层选择了 `B(B&& b)`。但是进入函数后，参数 `b` 有名字，所以表达式 `b` 是左值。初始化列表写的是 `A(b)`，因此调用的是 `A(A&)`，不是 `A(A&&)`：

```text
b3
```

所以连续输出为：

```text
a1b2b2b3
```

面试里可以主动补充：

- 如果 `B(B&& b) : A(std::move(b))`，最后一行基类会调用 `A(A&&)`，输出会变成 `c3`。
- 正常拷贝构造应写成 `A(const A&)`、`B(const B&)`，否则无法拷贝 `const` 对象和很多临时对象。
- 移动构造通常要把资源从源对象转移出来，并让源对象保持“可析构、可赋值”的有效状态。

### 2.6 函数模板中的局部静态变量

题目：

```cpp
template<typename T>
void f(T t)
{
    static int i = 0;
    i++;
}

f(1);
f(1.0);
f(2);
```

严格说，这段代码没有打印语句，所以**没有输出**。

如果面试官问的是 `i` 的变化，则关键点是：**函数模板每个不同模板实例都有自己独立的局部静态变量。**

调用过程：

```text
f(1)   -> T 是 int    -> f<int> 的 i 从 0 变 1
f(1.0) -> T 是 double -> f<double> 的 i 从 0 变 1
f(2)   -> T 是 int    -> f<int> 的 i 从 1 变 2
```

如果函数里加上打印：

```cpp
std::cout << i << "\n";
```

那么输出是：

```text
1
1
2
```

可以补充：局部静态变量具有静态存储期，C++11 起初始化是线程安全的；但这里的 `i++` 本身不是线程安全的，如果多个线程同时调用同一个 `f<int>`，仍然需要同步或原子变量。

### 2.7 TCP 和 UDP

一句话回答：

```text
TCP 面向连接、可靠、有序、字节流；UDP 无连接、不保证可靠和有序、面向报文，但开销低、延迟小。
```

TCP 特点：

- 三次握手建立连接，四次挥手释放连接。
- 通过序列号、确认应答、超时重传保证可靠传输。
- 保证数据按顺序交付给应用层。
- 有流量控制和拥塞控制。
- 是字节流协议，没有天然消息边界。

UDP 特点：

- 不需要建立连接。
- 不保证到达、不保证顺序、不自动重传。
- 保留报文边界。
- 头部更小，延迟更低。
- 可以广播、多播。

游戏客户端常见选择：

- 登录、支付、背包、商城等强一致请求适合 TCP 或基于 HTTP/HTTPS。
- 实时位置同步、技能方向、帧同步输入等更偏低延迟场景，可以用 UDP 或基于 UDP 的可靠协议。
- 真正的线上游戏经常不是“纯 TCP 或纯 UDP”的选择，而是在 UDP 上按业务自己做可靠、重传、冗余、插值和预测。

可以用一句话收尾：

```text
TCP 像挂号信，稳但流程多；UDP 像喊话，快但要自己确认对方有没有听见。
```

## 3. 算法

### 3.1 前序遍历和中序遍历构建二叉树

核心思路：

```text
前序遍历：根 -> 左 -> 右
中序遍历：左 -> 根 -> 右
```

所以前序数组的第一个元素一定是当前子树的根。再到中序数组里找到这个根，根左边就是左子树，根右边就是右子树。递归构建即可。

为了避免每次在线性扫描中序数组，可以先用哈希表记录值到下标的映射。

实现：

```cpp
#include <unordered_map>
#include <vector>

struct TreeNode {
    int val = 0;
    TreeNode* left = nullptr;
    TreeNode* right = nullptr;

    explicit TreeNode(int v) : val(v) {}
};

class Solution {
public:
    TreeNode* buildTree(const std::vector<int>& preorder,
                        const std::vector<int>& inorder) {
        if (preorder.size() != inorder.size()) {
            return nullptr;
        }

        for (int i = 0; i < static_cast<int>(inorder.size()); ++i) {
            inorderIndex_[inorder[i]] = i;
        }

        return build(preorder, 0, static_cast<int>(preorder.size()) - 1,
                     0, static_cast<int>(inorder.size()) - 1);
    }

private:
    std::unordered_map<int, int> inorderIndex_;

    TreeNode* build(const std::vector<int>& preorder,
                    int preLeft,
                    int preRight,
                    int inLeft,
                    int inRight) {
        if (preLeft > preRight) {
            return nullptr;
        }

        int rootValue = preorder[preLeft];
        int rootIndex = inorderIndex_[rootValue];
        int leftSize = rootIndex - inLeft;

        auto* root = new TreeNode(rootValue);
        root->left = build(preorder,
                           preLeft + 1,
                           preLeft + leftSize,
                           inLeft,
                           rootIndex - 1);
        root->right = build(preorder,
                            preLeft + leftSize + 1,
                            preRight,
                            rootIndex + 1,
                            inRight);
        return root;
    }
};
```

复杂度：

```text
时间复杂度：O(n)，每个节点处理一次
空间复杂度：O(n)，哈希表和递归栈
```

边界条件：

- 空数组返回空树。
- 前序和中序长度不同，输入非法。
- 如果节点值可能重复，仅靠值到下标的哈希表不够，需要额外约束或改用更复杂的匹配方式。常见面试题默认节点值不重复。

