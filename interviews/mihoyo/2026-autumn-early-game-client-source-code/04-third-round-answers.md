# 米哈游2026秋招提前批游戏客户端三面参考答案

## 1. 使用说明

本文按[三面原题](./三面.md)的顺序回答。

回答目标不是只给定义，而是覆盖：

```text
结论
-> 原理
-> 易错点
-> 面试追问
-> 可落地实现
```

算法题默认使用 C++17。

## 2. 八股

### 2.1 什么是补码

补码是现代计算机表示有符号整数的常用编码。

对于 N 位整数：

- 非负数的补码与普通二进制表示相同。
- 负数 `-x` 的补码可以通过 `x` 的二进制按位取反再加 1 得到。

例如 8 位整数：

```text
+5 = 0000 0101
取反  1111 1010
加 1  1111 1011
-5 = 1111 1011
```

更统一的理解是模 `2^N`：

```text
-x 的 N 位补码 = 2^N - x
```

若把一个 N 位比特模式先解释为无符号数 `U`，再解释为补码有符号数：

```text
U < 2^(N-1)  -> 值为 U
U >= 2^(N-1) -> 值为 U - 2^N
```

最高位因此同时体现符号：

```text
0：非负数
1：负数
```

但补码不是“单独存一个符号位再存绝对值”，最高位参与整个数值编码。

### 2.2 8 位补码范围

8 位补码范围是：

```text
-128 到 127
```

原因：

```text
最小值：1000 0000 = 128 - 256 = -128
最大值：0111 1111 = 127
```

范围不对称：

```text
负数有 128 个：-128 到 -1
非负数有 128 个：0 到 127
```

因此 8 位补码中 `-128` 没有对应的 `+128`。对最小负数直接取负会溢出。

### 2.3 补码的作用

补码主要解决三个问题。

#### 统一加法和减法

减法可以转化为加上负数：

```text
a - b = a + (-b)
```

CPU 可以复用同一套二进制加法器，不必为符号和减法单独设计一整套运算路径。

例如 8 位：

```text
  5 = 0000 0101
 -3 = 1111 1101
相加 1 0000 0010
丢弃最高进位后得到 0000 0010，即 2
```

#### 只有一个零

原码和反码可能存在 `+0` 与 `-0`。补码只有：

```text
0000 0000
```

#### 符号扩展和溢出规则自然

补码负数扩展位宽时复制最高位即可：

```text
8 位 -5：  1111 1011
16 位 -5：1111 1111 1111 1011
```

固定宽度整数运算本质上按模 `2^N` 截断，也与硬件寄存器行为一致。

### 2.4 C++ 中的左值和右值

面试中的实用定义：

- **左值**：表达式表示一个有身份、可被持续定位的对象。
- **右值**：临时值或即将失效、资源可以被转移的对象。

```cpp
int x = 10;

x;             // 左值：有稳定身份
42;            // 纯右值
x + 1;         // 纯右值
std::move(x);  // 将 x 转换成将亡值
```

C++11 之后更精确的分类是：

```text
glvalue
├── lvalue
└── xvalue

rvalue
├── prvalue
└── xvalue
```

其中：

- `lvalue`：普通左值。
- `prvalue`：用于初始化对象或计算结果的纯右值。
- `xvalue`：仍有身份，但资源可以被复用的将亡值。

常见误区：

1. 左值不等于“可以放在赋值号左边”，`const` 左值不能赋值。
2. 右值不等于“绝对不能取地址”，更准确的标准是身份和生命周期。
3. `std::move` 本身不移动数据，只做类型转换。
4. 有名字的右值引用变量在表达式中是左值。

```cpp
void f(std::string&& value) {
    consume(value);            // value 是左值表达式
    consume(std::move(value)); // 再转成右值
}
```

### 2.5 拷贝构造函数和移动构造函数

拷贝构造从已有对象复制状态：

```cpp
class Buffer {
public:
    Buffer(const Buffer& other);
};
```

移动构造从即将失效的对象转移资源：

```cpp
class Buffer {
public:
    Buffer(Buffer&& other) noexcept;
};
```

假设对象持有堆内存：

```text
拷贝构造：
重新分配一块内存
-> 复制全部数据
-> 两个对象各自拥有一份资源

移动构造：
接管源对象的指针和长度
-> 将源对象置为可析构的空状态
-> 通常不复制底层数据
```

示例：

```cpp
class Buffer {
public:
    explicit Buffer(std::size_t size)
        : data_(std::make_unique<int[]>(size)), size_(size) {}

    Buffer(const Buffer& other)
        : data_(std::make_unique<int[]>(other.size_)),
          size_(other.size_) {
        std::copy_n(other.data_.get(), size_, data_.get());
    }

    Buffer(Buffer&& other) noexcept
        : data_(std::move(other.data_)),
          size_(std::exchange(other.size_, 0)) {}

private:
    std::unique_ptr<int[]> data_;
    std::size_t size_ = 0;
};
```

移动后的源对象：

- 必须仍然有效，可以安全析构或重新赋值。
- 其具体值通常处于“有效但未指定状态”。
- 不应假设仍保留移动前内容。

`noexcept` 很重要：`std::vector` 扩容时，为保证异常安全，若移动构造可能抛异常且类型可拷贝，通常会选择拷贝。

### 2.6 两者在形式上的区别

最直接的区别是参数类型：

```cpp
T(const T& other); // 拷贝构造：接收 const 左值引用
T(T&& other);      // 移动构造：接收右值引用
```

典型调用：

```cpp
T a;
T b = a;            // 拷贝
T c = std::move(a); // 移动
T d = T{};          // 可能移动，也可能直接消除构造
```

还应补充：

- 拷贝通常不能修改源对象，因此参数是 `const T&`。
- 移动需要清空或修改源对象，因此参数通常是 `T&&`。
- 编译器可能执行 Copy Elision，直接在目标位置构造，既不拷贝也不移动。
- 自定义析构、拷贝操作可能影响移动操作的隐式生成，应理解 Rule of Five。

### 2.7 C++ 的智能指针

#### `std::unique_ptr`

表示独占所有权：

```cpp
auto object = std::make_unique<GameObject>();
```

特点：

- 不可拷贝，可以移动。
- 离开作用域自动释放资源。
- 默认开销接近裸指针。
- 最适合作为默认所有权类型。

#### `std::shared_ptr`

表示共享所有权：

```cpp
auto object = std::make_shared<GameObject>();
```

控制块通常包含：

```text
强引用计数
弱引用计数
删除器
分配器等元数据
```

最后一个强引用消失时销毁对象；最后一个弱引用消失时释放控制块。

注意：

- 引用计数更新通常是原子的，有额外成本。
- 控制块线程安全不等于对象本身线程安全。
- 不应为“省事”到处使用共享所有权。

#### `std::weak_ptr`

观察 `shared_ptr` 管理的对象，但不增加强引用计数：

```cpp
std::weak_ptr<GameObject> weak = object;

if (auto locked = weak.lock()) {
    locked->Update();
}
```

主要用途：

- 打破 `shared_ptr` 循环引用。
- 安全观察一个可能已经销毁的对象。

#### 常见追问

循环引用：

```text
A shared_ptr 持有 B
B shared_ptr 持有 A
-> 强引用计数永远不归零
```

应让非拥有关系使用 `weak_ptr`。

推荐创建方式：

- `make_unique`：表达独占所有权并避免裸 `new`。
- `make_shared`：通常将对象和控制块一次分配，减少分配次数。

### 2.8 二叉树前序、中序和后序遍历

假设节点访问记作 `Root`：

| 遍历 | 顺序 | 常见用途 |
|---|---|---|
| 前序 | Root -> Left -> Right | 序列化、复制树、表达式前缀 |
| 中序 | Left -> Root -> Right | BST 得到升序序列 |
| 后序 | Left -> Right -> Root | 删除树、计算子树结果 |

递归实现：

```cpp
struct Node {
    int value;
    Node* left = nullptr;
    Node* right = nullptr;
};

void Preorder(const Node* node) {
    if (!node) return;
    Visit(node);
    Preorder(node->left);
    Preorder(node->right);
}

void Inorder(const Node* node) {
    if (!node) return;
    Inorder(node->left);
    Visit(node);
    Inorder(node->right);
}

void Postorder(const Node* node) {
    if (!node) return;
    Postorder(node->left);
    Postorder(node->right);
    Visit(node);
}
```

复杂度：

```text
时间：O(n)
递归栈：平均 O(log n)，最坏退化树 O(n)
```

## 3. 算法一：BST 序列化与反序列化

### 3.1 先澄清 4n 的单位

若节点值是任意 32 位 `int`：

- 仅存 n 个值就需要 `32n` bit。
- 因此输出不可能小于等于 `4n` bit。

合理理解应是：

```text
输出字节数组长度 <= 4n 字节
```

每个 `int32_t` 占 4 字节。BST 的结构由前序值序列和大小关系恢复，因此无需额外空节点标记，最终恰好使用 `4n` 字节。

前提：

- BST 中不允许重复值。
- 协议明确使用 32 位有符号整数。
- 传输采用固定字节序。

### 3.2 为什么只存前序值就够

BST 满足：

```text
左子树所有值 < 根
右子树所有值 > 根
```

前序遍历的第一个值是根。之后可以用合法值范围把序列唯一切分为左右子树：

```text
前序：[8, 3, 1, 6, 10, 14]

根：8
小于 8 的连续区间属于左子树：[3, 1, 6]
大于 8 的后续区间属于右子树：[10, 14]
```

反序列化时使用上下界递归，每个值只消费一次，时间复杂度 O(n)。

### 3.3 C++ 实现

```cpp
#include <cstdint>
#include <limits>
#include <memory>
#include <stdexcept>
#include <vector>

struct BstNode {
    explicit BstNode(std::int32_t input) : value(input) {}

    std::int32_t value;
    std::unique_ptr<BstNode> left;
    std::unique_ptr<BstNode> right;
};

using Bytes = std::vector<std::uint8_t>;

void WriteInt32BigEndian(Bytes& output, std::int32_t value) {
    const auto bits = static_cast<std::uint32_t>(value);

    output.push_back(static_cast<std::uint8_t>(bits >> 24));
    output.push_back(static_cast<std::uint8_t>(bits >> 16));
    output.push_back(static_cast<std::uint8_t>(bits >> 8));
    output.push_back(static_cast<std::uint8_t>(bits));
}

std::int32_t ReadInt32BigEndian(
    const Bytes& input,
    std::size_t offset
) {
    const std::uint32_t bits =
        (std::uint32_t{input[offset]} << 24) |
        (std::uint32_t{input[offset + 1]} << 16) |
        (std::uint32_t{input[offset + 2]} << 8) |
        std::uint32_t{input[offset + 3]};

    const std::int64_t signedValue =
        (bits & 0x8000'0000U)
            ? static_cast<std::int64_t>(bits) - 0x1'0000'0000LL
            : static_cast<std::int64_t>(bits);

    return static_cast<std::int32_t>(signedValue);
}

void SerializePreorder(const BstNode* node, Bytes& output) {
    if (!node) {
        return;
    }

    WriteInt32BigEndian(output, node->value);
    SerializePreorder(node->left.get(), output);
    SerializePreorder(node->right.get(), output);
}

Bytes Serialize(const BstNode* root) {
    Bytes output;
    SerializePreorder(root, output);
    return output;
}

std::unique_ptr<BstNode> BuildFromPreorder(
    const std::vector<std::int32_t>& values,
    std::size_t& next,
    std::int64_t lowerExclusive,
    std::int64_t upperExclusive
) {
    if (next >= values.size()) {
        return nullptr;
    }

    const std::int64_t value = values[next];
    if (value <= lowerExclusive || value >= upperExclusive) {
        return nullptr;
    }

    ++next;
    auto node = std::make_unique<BstNode>(
        static_cast<std::int32_t>(value)
    );
    node->left = BuildFromPreorder(
        values,
        next,
        lowerExclusive,
        value
    );
    node->right = BuildFromPreorder(
        values,
        next,
        value,
        upperExclusive
    );
    return node;
}

std::unique_ptr<BstNode> Deserialize(const Bytes& input) {
    if (input.size() % sizeof(std::int32_t) != 0) {
        throw std::invalid_argument("invalid byte length");
    }

    std::vector<std::int32_t> values;
    values.reserve(input.size() / sizeof(std::int32_t));

    for (std::size_t offset = 0; offset < input.size(); offset += 4) {
        values.push_back(ReadInt32BigEndian(input, offset));
    }

    std::size_t next = 0;
    auto root = BuildFromPreorder(
        values,
        next,
        std::numeric_limits<std::int64_t>::min(),
        std::numeric_limits<std::int64_t>::max()
    );

    if (next != values.size()) {
        throw std::invalid_argument("input is not a valid BST preorder");
    }

    return root;
}
```

复杂度：

```text
序列化时间：O(n)
反序列化时间：O(n)
输出大小：4n 字节
辅助空间：O(n)，递归深度最坏 O(n)
```

可以进一步直接从字节流按索引构建，避免中间 `values` 数组。

### 3.4 为什么不使用空节点标记

普通二叉树必须记录结构，例如：

```text
8, 3, 1, null, null, 6, null, null, ...
```

这会增加额外空间。

BST 已通过大小关系携带结构信息，所以前序值本身足以恢复结构。中序遍历只有排序结果，无法单独恢复原树。

层序遍历也可利用“父节点先于子节点”重建，但通常需要维护候选区间，或者逐个插入导致最坏 O(n²)。前序加上下界实现更直接且为 O(n)。

### 3.5 了解哪些压缩算法

可按类型回答：

| 类型 | 典型算法 | 特点 |
|---|---|---|
| 熵编码 | Huffman、Arithmetic Coding | 根据符号概率使用更短编码 |
| 字典压缩 | LZ77、LZ4、DEFLATE、Zstd | 利用重复子串 |
| 整数编码 | Varint、ZigZag、Delta Encoding | 适合小整数和相邻差值 |
| 游程编码 | RLE | 适合连续重复值 |

针对 BST 整数：

- 小正整数可使用 Varint。
- 正负小整数可先 ZigZag 再 Varint。
- 若值相近，可考虑排序后 Delta，但还需单独保存树结构。
- 整包数据可再使用 LZ4 或 Zstd，但小包可能因头部和字典成本反而变大。

压缩选择应考虑：

```text
压缩率
编码/解码耗时
临时内存
随机访问需求
包体大小
网络带宽和延迟
```

### 3.6 Protobuf 是什么

Protocol Buffers 是一种基于 Schema 的二进制序列化协议。

开发者在 `.proto` 中定义消息：

```protobuf
message Player {
  int32 id = 1;
  string name = 2;
}
```

生成代码负责：

- 序列化和反序列化。
- 字段类型检查。
- 多语言互操作。
- 未知字段处理。
- 向前和向后兼容。

每个字段由：

```text
field number + wire type + payload
```

组成。兼容性的关键是字段编号稳定：

- 新增可选字段通常兼容旧版本。
- 删除字段后不应复用原编号。
- 修改字段含义或不兼容类型会破坏协议。

### 3.7 Protobuf 的“压缩算法”

Protobuf 本身主要是紧凑编码，不等于通用压缩算法。

主要机制：

#### Varint

整数每 7 bit 数据配一个 continuation bit。小整数使用更少字节：

```text
0 到 127：1 字节
128 以上：继续占更多字节
```

普通负数使用 `int32/int64` 时，补码高位大量为 1，Varint 可能占很多字节。

#### ZigZag

`sint32/sint64` 先把有符号数映射为无符号小整数：

```text
 0 -> 0
-1 -> 1
 1 -> 2
-2 -> 3
```

再进行 Varint，适合绝对值较小的正负整数。

#### Packed repeated

重复数值字段可以只写一次字段头，再连续存 payload，减少 tag 开销。

#### 省略默认值

Proto3 通常不编码默认值字段，例如数值 0、false、空字符串。

如需进一步压缩，通常在 Protobuf 消息外层使用 gzip、LZ4 或 Zstd。是否值得要看包大小和 CPU 预算。

### 3.8 该 BST 传输算法有什么问题

#### 协议边界

TCP 是字节流，没有消息边界。需要额外提供：

```text
长度前缀
消息类型
协议版本
```

并处理半包、粘包和分段读取。

#### 字节序和整数宽度

不能直接传本机 `int` 内存：

- `int` 宽度并非协议级保证。
- 大小端可能不同。
- 对齐和对象布局不能作为网络协议。

应固定为 `int32` 和网络字节序。

#### 重复值策略

BST 若允许重复值，必须规定：

```text
重复值总在左侧
或
重复值总在右侧
或
节点额外保存 count
```

否则无法唯一恢复。

#### 退化树和栈溢出

有序输入可能形成高度为 n 的链表。递归序列化或反序列化可能栈溢出，应考虑迭代实现或限制深度。

#### 非法和恶意数据

接收端应验证：

- 长度是否为 4 的倍数。
- 节点数上限。
- 前序序列是否满足 BST 范围。
- 递归深度和内存预算。
- 包完整性和身份认证。

否则可能造成大内存分配、栈溢出或拒绝服务。

#### 版本与可扩展性

裸 `int` 数组难以新增字段。若以后节点需要颜色、权重或版本信息，应设计消息头或使用 Schema 协议。

#### 压缩效果

固定 4 字节对小整数可能浪费；但改用 Varint 后输出不再保证固定 `4n`，需要重新定义协议约束。

## 4. 算法二：全 1 最大矩形

### 4.1 思路

把每一行当作柱状图底部。

遍历到第 `r` 行时，维护每一列连续 1 的高度：

```text
matrix[r][c] == 1 -> heights[c] += 1
matrix[r][c] == 0 -> heights[c] = 0
```

然后对 `heights` 使用单调递增栈求柱状图最大矩形。

例如：

```text
矩阵：
1 0 1 1
1 1 1 1
1 1 1 0

逐行高度：
1 0 1 1
2 1 2 2
3 2 3 0
```

每行都求一次最大柱状图矩形，即可得到全局最大矩形。

### 4.2 C++ 实现

```cpp
#include <algorithm>
#include <utility>
#include <vector>

struct Rectangle {
    int row = -1;
    int column = -1;
    int width = 0;
    int height = 0;
    int area = 0;
};

Rectangle MaxRectangleOfOnes(
    const std::vector<std::vector<int>>& matrix
) {
    if (matrix.empty() || matrix.front().empty()) {
        return {};
    }

    const int rows = static_cast<int>(matrix.size());
    const int columns = static_cast<int>(matrix.front().size());
    std::vector<int> heights(columns, 0);
    Rectangle best;

    for (int row = 0; row < rows; ++row) {
        for (int column = 0; column < columns; ++column) {
            heights[column] =
                matrix[row][column] == 1
                    ? heights[column] + 1
                    : 0;
        }

        // pair = {起始列, 高度}
        std::vector<std::pair<int, int>> stack;

        for (int column = 0; column <= columns; ++column) {
            const int currentHeight =
                column == columns ? 0 : heights[column];
            int start = column;

            while (
                !stack.empty() &&
                stack.back().second > currentHeight
            ) {
                const auto [left, height] = stack.back();
                stack.pop_back();

                const int width = column - left;
                const int area = width * height;

                if (area > best.area) {
                    best.row = row - height + 1;
                    best.column = left;
                    best.width = width;
                    best.height = height;
                    best.area = area;
                }

                start = left;
            }

            if (
                currentHeight > 0 &&
                (
                    stack.empty() ||
                    stack.back().second < currentHeight
                )
            ) {
                stack.emplace_back(start, currentHeight);
            }
        }
    }

    return best;
}
```

返回：

- `row`、`column`：矩形左上角。
- `width`：宽。
- `height`：高。
- `area`：面积。

如果最大矩形重复，只在 `area > best.area` 时更新，自然保留任意一个。

### 4.3 正确性

当栈顶高度大于当前高度时，说明该高度无法继续向右延伸。

对于弹出的 `(left, height)`：

```text
左边界：left
右边界：当前列 column，不包含 column
宽度：column - left
```

此时正好是以该高度为最矮柱子的最大可扩展宽度，因此不会漏掉候选矩形。

复杂度：

```text
时间：O(rows * columns)
空间：O(columns)
```

每个柱子每行最多入栈一次、出栈一次。

## 5. Unity 与游戏客户端追问

### 5.1 Unity Shader、Material 与 3D 物体 Material 的区别

先纠正术语：

- **Shader**：定义 GPU 如何处理顶点和像素，以及渲染 Pass、状态和可配置属性。
- **Material**：引用某个 Shader，并保存该 Shader 的具体参数、纹理、关键字和渲染队列。
- **3D 物体**：通常由 `MeshFilter` 提供几何数据，由 `MeshRenderer` 持有一个或多个 Material 引用。

因此“Shader 的 Material”和“3D 物体的 Material”不是两套材质。3D 物体使用的就是 Unity `Material` 对象。

关系：

```text
Mesh：顶点、索引、法线、UV
Shader：渲染算法
Material：Shader + 参数 + 纹理 + 状态
Renderer：把 Mesh 和 Material 组合后提交渲染
```

还应区分：

```csharp
renderer.sharedMaterial
```

- 返回共享材质资源。
- 修改后会影响所有引用该材质的 Renderer。

```csharp
renderer.material
```

- Unity 可能为该 Renderer 创建独立材质实例。
- 修改只影响当前 Renderer。
- 大量访问和修改可能产生材质实例、破坏合批并增加内存。

如果只需给单个 Renderer 设置少量参数，可考虑 `MaterialPropertyBlock`，避免复制完整 Material。

### 5.2 3D 项目中如何实现角色移动

先根据角色类型选择方案：

| 角色类型 | 常见方案 |
|---|---|
| 玩家动作角色 | CharacterController 或 Kinematic Rigidbody |
| 受完整物理影响的物体 | Dynamic Rigidbody |
| AI 导航角色 | NavMeshAgent 或自研导航 + CharacterController |
| 动画主导角色 | Root Motion + 碰撞/位移校正 |

典型 CharacterController 流程：

```csharp
public class PlayerMovement : MonoBehaviour {
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float gravity = -20.0f;

    private float verticalVelocity;

    private void Update() {
        Vector2 input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        ).normalized;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0.0f;
        right.y = 0.0f;
        forward.Normalize();
        right.Normalize();

        Vector3 horizontal =
            forward * input.y + right * input.x;

        if (controller.isGrounded && verticalVelocity < 0.0f) {
            verticalVelocity = -2.0f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 displacement =
            horizontal * speed * Time.deltaTime +
            Vector3.up * verticalVelocity * Time.deltaTime;

        controller.Move(displacement);
    }
}
```

需要考虑：

- 输入采集和相机相对方向。
- Delta Time。
- 地面检测、重力、台阶和斜坡。
- 碰撞处理。
- 动画参数和角色朝向。
- 联机时输入序列、本地预测与服务器校正。

### 5.3 角色移动需要什么物理组件

不存在唯一答案。

#### Dynamic Rigidbody

组件：

```text
Rigidbody + Collider
```

适合：

- 角色应受力、冲量、碰撞和物理约束影响。

移动应优先使用：

- 力或速度。
- `Rigidbody.MovePosition`。
- 在 `FixedUpdate` 中更新物理。

#### Kinematic Rigidbody

组件：

```text
Rigidbody(isKinematic = true) + Collider
```

适合：

- 位移由游戏逻辑或动画控制。
- 仍需进入物理世界并参与碰撞查询。

#### CharacterController

组件：

```text
CharacterController
```

它不是普通 Rigidbody，适合可控角色：

- 提供胶囊碰撞。
- 支持斜坡和台阶。
- 由代码显式处理重力和移动。
- 不会自动受到 Rigidbody 力学模拟。

### 5.4 Rigidbody 有几个形态

从物理世界角度通常分三类：

| 类型 | Unity 表现 | 行为 |
|---|---|---|
| Static | 只有 Collider，没有 Rigidbody | 不移动，作为静态碰撞体 |
| Dynamic | Rigidbody 且 `isKinematic = false` | 受力、重力和求解器控制 |
| Kinematic | Rigidbody 且 `isKinematic = true` | 由脚本或动画控制，不受普通力推动 |

严格来说，Static 不是一种 Rigidbody 组件状态，而是一种物理 Body 类型。面试中最好先说明这一点。

### 5.5 Kinematic 如何移动

Kinematic Rigidbody 通常在 `FixedUpdate` 中使用：

```csharp
private void FixedUpdate() {
    Vector3 next =
        rigidbody.position +
        moveDirection * speed * Time.fixedDeltaTime;

    rigidbody.MovePosition(next);
}
```

旋转使用：

```csharp
rigidbody.MoveRotation(nextRotation);
```

不推荐每帧直接修改 `transform.position`：

- 可能绕过物理插值和连续的碰撞处理。
- 物理系统只看到对象瞬移。
- 容易与 Rigidbody 状态不同步。

Kinematic Body：

- 不由重力和普通力驱动。
- 可以推动或影响 Dynamic Body，具体行为取决于移动方式和引擎设置。
- 自身是否响应碰撞位移需要由代码处理。

CharacterController 则使用 `CharacterController.Move`，不要与 Kinematic Rigidbody 混为一谈。

### 5.6 多人联机同步方式

常见方式：

#### 状态同步

服务器周期性下发权威状态：

```text
位置
旋转
速度
生命值
技能状态
动画状态
```

客户端通过插值、外推和本地预测改善表现。

#### 帧同步

服务器收集玩家输入并按逻辑 Tick 广播：

```text
第 100 Tick：
玩家 A 向前
玩家 B 释放技能 3
```

各端使用相同初始状态和确定性逻辑计算结果。

#### 混合同步

不同系统使用不同方式：

- 战斗核心使用帧同步或 Rollback。
- 非关键对象使用状态同步。
- 服务端定期下发快照或校验 Hash。

现代项目通常不是纯粹二选一。

### 5.7 帧同步和状态同步的区别

| 维度 | 帧同步 | 状态同步 |
|---|---|---|
| 主要同步内容 | 玩家输入或操作指令 | 世界状态 |
| 逻辑执行位置 | 多端都执行 | 服务器权威执行 |
| 确定性要求 | 高 | 较低 |
| 带宽 | 输入小，但重连快照可能大 | 状态数量多，需增量和压缩 |
| 客户端作弊风险 | 纯客户端计算时较高 | 服务器权威时较低 |
| 延迟处理 | 输入等待、预测或 Rollback | 插值、预测、校正 |
| 断线重连 | 需要输入历史或状态快照 | 获取最新状态快照 |
| 适用场景 | RTS、格斗、部分竞技 | MMO、射击、动作、开放世界 |

关键区别：

```text
帧同步同步“导致状态变化的输入”
状态同步同步“变化后的状态结果”
```

### 5.8 客户端如何进行本地预测

以玩家移动为例：

1. 客户端采集输入，分配递增序号或 Tick。
2. 客户端立即用该输入模拟移动，不等待服务器。
3. 将输入和序号发送给服务器。
4. 服务器按权威状态执行输入。
5. 服务器返回权威状态和“已处理到哪个输入”。
6. 客户端把本地状态回退到服务器状态。
7. 删除已经确认的输入。
8. 重新执行尚未确认的输入。
9. 将视觉表现平滑到校正后的结果。

```text
Input #101 -> 本地立即执行
Input #102 -> 本地立即执行
Input #103 -> 本地立即执行

服务器回复：
权威状态 S
已处理到 #101

客户端：
设置状态为 S
重放 #102、#103
```

需要区分：

- **Simulation State**：立即校正，保证逻辑正确。
- **Presentation State**：平滑追赶，避免画面瞬移。

### 5.9 服务器会下发哪些权威信息

取决于同步模型。

#### 状态同步

服务器通常会下发位置，但不一定每帧发送完整状态。

可下发：

```text
位置、旋转
线速度、角速度
时间戳或服务器 Tick
运动模式
地面状态
最近处理的输入序号
关键技能或动画状态
```

常见优化：

- 固定频率快照，而非每渲染帧。
- Delta Compression。
- 量化坐标和角度。
- 只同步发生变化的字段。
- 超过误差阈值才校正。

对于本地玩家，最关键的是：

```text
权威状态
+ 已确认输入序号
```

客户端据此进行 Reconciliation。

#### 帧同步

服务器主要下发各玩家输入，不需要持续下发每个对象的位置。

但工程上仍可能周期性下发：

- 状态 Hash。
- 关键帧快照。
- 重连快照。
- 反作弊或不同步时的纠正状态。

因此“帧同步绝不下发位置”也不是绝对结论。

### 5.10 帧同步如何防作弊

首先修正一个过度概括：

> 帧同步不天然等于防作弊差。真正的安全性取决于服务器是否验证输入、是否运行权威模拟，以及客户端能看到多少不该看到的信息。

可采用以下设计。

#### 只接收输入，不接收结果

客户端只能上报：

```text
按键
方向
技能编号
目标选择
```

不能直接上报：

```text
我的位置是 X
我造成了 10000 伤害
技能已经命中
```

#### 服务器验证输入合法性

检查：

- 输入频率和 Tick 是否正常。
- 移动速度和转向是否超过上限。
- 技能冷却、资源和状态是否允许。
- 目标距离和视野是否合法。
- 指令顺序是否可能。

#### 服务器运行确定性权威模拟

最强方案是服务器也执行同一套逻辑：

```text
客户端负责表现和预测
服务器负责最终判定
```

服务器可以比较：

- 周期性状态 Hash。
- 关键实体状态。
- 战斗结果。

纯粹只转发输入、不做验证的服务器防作弊能力较弱。

#### 信息隔离

即使逻辑确定，客户端也不应提前获得：

- 战争迷雾外单位。
- 未揭示掉落。
- 对手隐藏状态。
- 服务器随机结果的未来信息。

必要时由服务器控制可见性和随机数。

#### 输入承诺和防重放

使用：

- Tick 和递增序号。
- 重复包过滤。
- 消息认证。
- 会话密钥。
- 超时和速率限制。

这可以降低伪造、重放和加速输入。

#### 行为检测与回放审计

保存输入流后可以：

- 复现比赛。
- 检查异常操作频率。
- 分析不可能的反应时间。
- 对举报对局离线重演。

#### 客户端完整性保护

代码签名、反调试、反注入和完整性校验只能提高作弊成本，不能替代服务器权威验证。安全边界不应建立在“客户端代码不会被修改”的假设上。

## 6. 面试回答总结

三面重点不是孤立背诵，而是验证候选人能否把知识连起来：

```text
补码
-> 固定宽度整数和网络编码

左值/右值
-> 拷贝/移动
-> 智能指针和资源所有权

树遍历
-> BST 序列化
-> 网络协议与压缩

Unity 物理
-> 角色移动
-> 联机预测和权威校正
```

高质量回答应主动说明前提、边界和工程风险，而不是只给一个术语。

## 7. 相关专题

- [C++ 基础知识](../../../cpp-fundamentals/README.md)
- [游戏客户端八股分类](../../../game-client-interview/01-knowledge-map.md)
- [ECS 高性能存储设计](../../../ecs-system/07-high-performance-storage.md)

[返回本轮题目](./三面.md)
