#include <cassert>
#include <cstdint>
#include <iostream>
#include <limits>
#include <memory>
#include <stdexcept>
#include <utility>
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

void CollectPreorder(
    const BstNode* node,
    std::vector<std::int32_t>& values
) {
    if (!node) {
        return;
    }

    values.push_back(node->value);
    CollectPreorder(node->left.get(), values);
    CollectPreorder(node->right.get(), values);
}

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

std::unique_ptr<BstNode> BuildExampleTree() {
    auto root = std::make_unique<BstNode>(8);
    root->left = std::make_unique<BstNode>(3);
    root->left->left = std::make_unique<BstNode>(1);
    root->left->right = std::make_unique<BstNode>(6);
    root->right = std::make_unique<BstNode>(10);
    root->right->right = std::make_unique<BstNode>(14);
    return root;
}

int main() {
    const auto tree = BuildExampleTree();
    const Bytes bytes = Serialize(tree.get());
    assert(bytes.size() == 6 * sizeof(std::int32_t));

    const auto restored = Deserialize(bytes);
    std::vector<std::int32_t> preorder;
    CollectPreorder(restored.get(), preorder);
    assert((
        preorder ==
        std::vector<std::int32_t>{8, 3, 1, 6, 10, 14}
    ));

    const std::vector<std::vector<int>> matrix{
        {1, 0, 1, 1},
        {1, 1, 1, 1},
        {1, 1, 1, 0},
    };
    const Rectangle rectangle = MaxRectangleOfOnes(matrix);
    assert(rectangle.row == 1);
    assert(rectangle.column == 0);
    assert(rectangle.width == 3);
    assert(rectangle.height == 2);
    assert(rectangle.area == 6);

    std::cout << "All third-round algorithm tests passed.\n";
}

