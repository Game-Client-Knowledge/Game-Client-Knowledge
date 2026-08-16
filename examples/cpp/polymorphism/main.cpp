#include <iostream>
#include <memory>
#include <string_view>
#include <type_traits>
#include <variant>
#include <vector>

namespace dynamic_polymorphism {

class Enemy {
public:
    virtual ~Enemy() = default;
    virtual std::string_view name() const = 0;
    virtual std::string_view attack() const = 0;
};

class Slime final : public Enemy {
public:
    std::string_view name() const override {
        return "Slime";
    }

    std::string_view attack() const override {
        return "Bounce";
    }
};

class Dragon final : public Enemy {
public:
    std::string_view name() const override {
        return "Dragon";
    }

    std::string_view attack() const override {
        return "Fire Breath";
    }
};

void run() {
    std::vector<std::unique_ptr<Enemy>> enemies;
    enemies.push_back(std::make_unique<Slime>());
    enemies.push_back(std::make_unique<Dragon>());

    for (const auto& enemy : enemies) {
        std::cout << enemy->name() << " uses " << enemy->attack() << '\n';
    }
}

}  // namespace dynamic_polymorphism

namespace static_polymorphism {

struct Warrior {
    std::string_view name() const {
        return "Warrior";
    }

    std::string_view attack() const {
        return "Shield Bash";
    }
};

struct Mage {
    std::string_view name() const {
        return "Mage";
    }

    std::string_view attack() const {
        return "Arcane Bolt";
    }
};

template <typename T>
void act(const T& actor) {
    std::cout << actor.name() << " uses " << actor.attack() << '\n';
}

void run() {
    act(Warrior{});
    act(Mage{});
}

}  // namespace static_polymorphism

namespace closed_set_polymorphism {

struct Circle {
    double radius;
};

struct Rectangle {
    double width;
    double height;
};

using Shape = std::variant<Circle, Rectangle>;

double area(const Shape& shape) {
    return std::visit([](const auto& value) {
        using T = std::decay_t<decltype(value)>;

        if constexpr (std::is_same_v<T, Circle>) {
            constexpr double pi = 3.141592653589793;
            return pi * value.radius * value.radius;
        } else {
            return value.width * value.height;
        }
    }, shape);
}

void run() {
    const Shape circle = Circle{2.0};
    const Shape rectangle = Rectangle{3.0, 4.0};

    std::cout << "Circle area: " << area(circle) << '\n';
    std::cout << "Rectangle area: " << area(rectangle) << '\n';
}

}  // namespace closed_set_polymorphism

int main() {
    std::cout << "== Dynamic polymorphism ==\n";
    dynamic_polymorphism::run();

    std::cout << "\n== Static polymorphism ==\n";
    static_polymorphism::run();

    std::cout << "\n== Closed-set runtime polymorphism ==\n";
    closed_set_polymorphism::run();
}
