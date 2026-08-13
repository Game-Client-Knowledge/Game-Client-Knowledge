# 组件存储与查询实现

## 1. Entity 是 ID，组件数据在哪里

Entity 确实只是一个标识，例如：

```text
Entity = (index: 7, generation: 2)
```

组件数据不存放在 Entity 内部，而是存放在各自的 **Component Storage（组件存储）** 中。Entity 的 `index` 是关联这些存储的键：

```text
Position Storage
Entity index -> Position
1            -> { x: 10, y: 20 }
3            -> { x: 50, y: 60 }
7            -> { x: 12, y: 8 }

Velocity Storage
Entity index -> Velocity
1            -> { x: 2, y: 0 }
7            -> { x: 0, y: 4 }

Health Storage
Entity index -> Health
3            -> { current: 100 }
7            -> { current: 80 }
```

因此，Entity 7 具有 `Position`、`Velocity` 和 `Health`，Entity 3 只具有 `Position` 和 `Health`。

可以将这种结构理解为关系数据库：

```text
Entity ID = 关联键
Component Storage = 按组件类型拆分的表
Query = 对多个组件表进行关联和过滤
```

## 2. 如何判断实体具有某个组件

判断 Entity 7 是否具有 `Velocity`，只需检查 `Velocity Storage` 中是否存在键 7：

```text
hasComponent(entity, Velocity)
    = isAlive(entity)
      and VelocityStorage.contains(entity.index)
```

这里必须先调用 `isAlive` 校验 `generation`，否则旧 Entity 引用可能误访问复用同一 index 的新实体。

### 2.1 generation 与谁比较

`generation` 不是一个要求所有实体保持一致的全局版本号。每个 `index` 对应的槽位都有自己的 generation：

```text
slots
index 0 -> { generation: 5,  alive: true  }
index 1 -> { generation: 0,  alive: true  }
index 2 -> { generation: 12, alive: true  }
index 3 -> { generation: 3,  alive: false }
```

这些槽位的 generation 不同是正常现象。当前有效的 Entity 句柄分别可以是：

```text
Entity(index: 0, generation: 5)
Entity(index: 1, generation: 0)
Entity(index: 2, generation: 12)
```

校验时只比较 **同一个 index 的两个 generation**：

```text
句柄中的 entity.generation
            对比
slots[entity.index].generation
```

不会拿 Entity 0 的 generation 与 Entity 1 或 Entity 2 比较。

### 2.2 两份 generation 分别来自哪里

校验需要两份数据：

1. **句柄中的 generation**：创建 Entity 时，从槽位复制出来，之后不会自动改变。
2. **槽位中的 generation**：Entity Manager 保存的当前权威版本，销毁该槽位中的实体时递增。

```typescript
type Entity = Readonly<{
  index: number;
  generation: number; // 句柄创建时保存的版本快照
}>;

type EntitySlot = {
  generation: number; // 这个 index 当前的权威版本
  alive: boolean;
};
```

创建实体时，两者相等：

```typescript
const entity: Entity = {
  index,
  generation: slots[index].generation,
};
```

销毁实体时，只更新槽位的权威版本，外部保存的旧句柄不会改变：

```typescript
function destroyEntity(entity: Entity): void {
  assertAlive(entity);

  const slot = slots[entity.index];
  slot.alive = false;
  slot.generation += 1;
}
```

这正是旧句柄能够被识别的原因。

### 2.3 完整校验过程

假设第一次使用槽位 7：

```text
slots[7] = { generation: 0, alive: true }
player   = { index: 7, generation: 0 }
```

此时：

```text
isAlive(player)
= slots[7].alive
  && slots[7].generation == player.generation
= true && 0 == 0
= true
```

销毁 `player`：

```text
slots[7] = { generation: 1, alive: false }
player   = { index: 7, generation: 0 }  // 外部旧句柄没有改变
```

随后复用槽位 7 创建 `enemy`：

```text
slots[7] = { generation: 1, alive: true }
enemy    = { index: 7, generation: 1 }
player   = { index: 7, generation: 0 }  // 仍然可能被其他代码持有
```

分别校验：

```text
isAlive(player)
= true && 1 == 0
= false

isAlive(enemy)
= true && 1 == 1
= true
```

虽然 `player` 和 `enemy` 的 index 都是 7，但 generation 表明它们是这个槽位在不同时期承载的两个实体。

对应实现：

```typescript
isAlive(entity: Entity): boolean {
  const slot = this.slots[entity.index];

  return (
    slot !== undefined &&
    slot.alive &&
    slot.generation === entity.generation
  );
}
```

三个条件分别防止：

| 条件 | 防止的问题 |
|---|---|
| `slot !== undefined` | index 超出槽位表范围 |
| `slot.alive` | 槽位当前没有存活实体 |
| generation 相等 | 槽位已被销毁并复用于新实体 |

### 2.4 为什么组件存储只用 index 作为键

本示例的组件存储是：

```text
Map<Entity index, Component>
```

它没有把 generation 放入键中，是因为 World 保证两个不变量：

1. 销毁实体时，先删除该 index 对应的全部旧组件。
2. 对外执行 `has`、`get`、`add` 或 `remove` 前，先调用 `isAlive`。

因此访问顺序必须是：

```text
先校验 (index, generation)
再用 index 定位组件
```

不能绕过 World，直接用旧句柄的 index 读取内部组件 Map。生产级实现还可以在调试模式下给组件记录附加 generation，以更早发现存储不变量被破坏的问题。

### 2.5 generation 溢出

若 generation 使用 32 位无符号整数，同一槽位销毁约 `2^32` 次后会回绕。多数应用中很难达到，但生命周期极长或安全要求较高的系统可以：

- 使用 64 位 generation。
- generation 回绕后永久停用该槽位。
- 在调试或测试中检测回绕。

## 3. 如何查询 Position + Velocity

`MovementSystem` 需要同时拥有 `Position` 和 `Velocity` 的实体：

```text
Position 的 Entity index 集合 = {1, 3, 7}
Velocity 的 Entity index 集合 = {1, 7}

交集 = {1, 7}
```

所以 `MovementSystem` 只处理 Entity 1 和 Entity 7。

实际实现通常遍历较小的组件存储，再去另一个存储中检查同一个 Entity index：

```text
遍历 Velocity Storage：{1, 7}
├── 1 在 Position Storage 中：匹配
└── 7 在 Position Storage 中：匹配
```

这样不需要遍历所有 Entity。

## 4. TypeScript 完整示例

下面使用 `Map<EntityIndex, Component>` 演示组件存储。生产级 ECS 可将 Map 替换为 Sparse Set 或 Archetype，但 `has`、`get` 和 `query` 的语义不变。

### 4.1 基础类型

```typescript
type ComponentType<T> = symbol & {
  readonly __componentType?: T;
};

type Entity = Readonly<{
  index: number;
  generation: number;
}>;

type EntitySlot = {
  generation: number;
  alive: boolean;
};

type Position = {
  x: number;
  y: number;
};

type Velocity = {
  x: number;
  y: number;
};

const POSITION = Symbol("Position") as ComponentType<Position>;
const VELOCITY = Symbol("Velocity") as ComponentType<Velocity>;
```

`ComponentType<T>` 使用唯一的 `Symbol` 作为组件类型的运行时标识。因为 TypeScript 的类型在运行时会被擦除，所以不能直接使用 `Position` 这样的类型声明作为 Map 的键。

#### 4.1.1 编译期与运行时

TypeScript 程序涉及两个阶段：

```text
编写和编译阶段
TypeScript 源码
  -> TypeScript 编译器检查类型
  -> 生成 JavaScript

运行阶段
JavaScript 引擎执行生成的 JavaScript
```

浏览器或 Node.js 通常执行 JavaScript，而不是执行 TypeScript 的类型系统。因此：

- `type`、`interface` 和泛型参数用于编译期检查。
- JavaScript 引擎只接收编译后的值和表达式。
- 运行时不能直接查询一个 `type` 声明。

例如，TypeScript 源码：

```typescript
type Position = {
  x: number;
  y: number;
};

function printPosition(position: Position): void {
  console.log(position.x, position.y);
}

const value: Position = { x: 10, y: 20 };
printPosition(value);
```

编译后大致是：

```javascript
function printPosition(position) {
  console.log(position.x, position.y);
}

const value = { x: 10, y: 20 };
printPosition(value);
```

以下内容都消失了：

```text
type Position = ...
: Position
: void
```

这个过程称为 **类型擦除（type erasure）**：编译器使用类型信息完成静态检查后，不把这些纯类型声明输出到 JavaScript。

注意，并非 TypeScript 文件中的所有声明都会消失：

- `type` 和 `interface` 是纯类型，通常完全擦除。
- 类型注解和大多数泛型参数会擦除。
- `const`、`function`、对象和数组是运行时值，会保留。
- `class` 既可表示类型，也会生成运行时构造函数。
- 某些 TypeScript 特性（如非 `const enum`）可能生成 JavaScript。

所以更准确的说法是：**只存在于 TypeScript 类型空间中的信息会被擦除。**

#### 4.1.2 为什么 TypeScript 要擦除类型

主要原因是 TypeScript 的设计目标是兼容 JavaScript 生态：

1. TypeScript 是 JavaScript 的静态类型层，而不是一套独立运行时。
2. 编译结果需要由现有浏览器和 Node.js 直接执行。
3. JavaScript 引擎不认识 `type Position` 或 `Map<Key, Value>` 这样的 TypeScript 语法。
4. 不生成运行时类型对象，可以避免默认增加类型元数据、内存和检查成本。

代价是 TypeScript 默认只能保证编译期约束，不能自动验证运行时外部数据。例如：

```typescript
const value = JSON.parse(input) as Position;
```

`as Position` 不会在运行时检查 `value` 是否真的有数值类型的 `x` 和 `y`。若数据来自网络、文件或用户输入，需要额外使用校验函数或 Schema 库。

#### 4.1.3 为什么不能直接用 Position 作为 Map 的键

下面的 `Position` 只存在于类型空间：

```typescript
type Position = {
  x: number;
  y: number;
};
```

因此可以在类型位置使用：

```typescript
const position: Position = { x: 1, y: 2 };
//              ^ 类型位置
```

但不能在需要运行时值的位置使用：

```typescript
stores.set(Position, new Map());
//         ^ 编译错误：Position 只表示类型，却被当作值使用
```

运行时必须有一个真实的键，例如：

```typescript
const POSITION = Symbol("Position");
stores.set(POSITION, new Map());
```

编译后的 JavaScript 中，`POSITION` 仍然存在：

```javascript
const POSITION = Symbol("Position");
stores.set(POSITION, new Map());
```

#### 4.1.4 Symbol 是什么

`symbol` 是 JavaScript 的一种原始数据类型，与 `string`、`number` 和 `boolean` 类似。调用 `Symbol(description)` 会创建一个新的唯一值：

```typescript
const a = Symbol("Position");
const b = Symbol("Position");

console.log(a === b); // false
```

即使 description 都是 `"Position"`，`a` 和 `b` 仍然不同。description 主要用于日志和调试，不决定 Symbol 的身份：

```text
Symbol("Position") #1 !== Symbol("Position") #2
```

这使它适合作为组件类型 ID：

```typescript
const POSITION = Symbol("Position");
const VELOCITY = Symbol("Velocity");

POSITION !== VELOCITY;
```

`Symbol.for("Position")` 的行为不同。它会从全局 Symbol 注册表中获取同名值：

```typescript
Symbol.for("Position") === Symbol.for("Position"); // true
```

ECS 内部通常使用 `Symbol()` 保证本地注册的组件 token 不会意外重名。只有确实需要跨模块或跨运行上下文共享注册名称时，才考虑 `Symbol.for()`。

#### 4.1.5 为什么 Symbol 可以作为 Map 的键

JavaScript `Map` 允许任意 JavaScript 值作为键：

```typescript
const map = new Map<unknown, string>();

map.set("name", "string key");
map.set(42, "number key");
map.set(Symbol("id"), "symbol key");
map.set({}, "object key");
```

Map 根据键值或对象身份区分条目。每次 `Symbol()` 都产生唯一身份，因此不会像字符串一样发生同名碰撞：

```typescript
const POSITION = Symbol("Position");
const ALSO_POSITION = Symbol("Position");

const stores = new Map<symbol, string>();
stores.set(POSITION, "first store");
stores.set(ALSO_POSITION, "second store");

console.log(stores.size); // 2
console.log(stores.get(POSITION)); // "first store"
```

在 ECS 中：

```text
POSITION Symbol -> Position 组件存储
VELOCITY Symbol -> Velocity 组件存储
```

外层 Map 使用组件类型 token 找到对应存储，内层 Map 再使用 Entity index 找到具体组件：

```typescript
Map<
  symbol,
  Map<number, unknown>
>
```

#### 4.1.6 ComponentType<T> 逐部分解释

原定义：

```typescript
type ComponentType<T> = symbol & {
  readonly __componentType?: T;
};
```

可以拆成三部分：

##### `symbol`

表示运行时 token 实际是一个 Symbol，因此可以作为 Map 的键：

```typescript
const POSITION = Symbol("Position");
```

##### `&`

`&` 是交叉类型，表示一个值需要同时被 TypeScript 看成：

```text
symbol
并且
{ readonly __componentType?: T }
```

这里不是在运行时给 Symbol 合并一个对象，而是在编译期为 Symbol 类型附加一条类型关联。

##### `readonly __componentType?: T`

这是一个 **幻影字段（phantom field）** 或类型品牌标记：

- `__componentType`：让泛型 `T` 出现在 token 的类型中。
- `?`：字段可选，因此不要求运行时真的创建该字段。
- `readonly`：如果字段存在，编译器不允许修改。
- `T`：记录该 token 对应哪一种组件数据。

它的作用是让编译器建立关联：

```text
POSITION -> ComponentType<Position>
VELOCITY -> ComponentType<Velocity>
```

实际 Symbol 上并不存在 `__componentType` 属性。它只存在于 TypeScript 的类型描述中，编译后也会被擦除。

#### 4.1.7 类型关联如何帮助泛型推导

定义：

```typescript
add<T>(
  entity: Entity,
  type: ComponentType<T>,
  component: NoInfer<T>,
): void;
```

`NoInfer<T>` 是 TypeScript 5.4 引入的工具类型。它表示这个位置需要满足 `T`，但不要从这个参数反向推导 `T`。

调用：

```typescript
world.add(
  player,
  POSITION,
  { x: 10, y: 20 },
);
```

编译器处理过程：

```text
POSITION 的类型是 ComponentType<Position>
-> 推导 T = Position
-> NoInfer<T> 阻止从 component 重新推导或拓宽 T
-> 第三个参数必须满足 Position
```

因此下面会产生编译错误：

```typescript
world.add(
  player,
  POSITION,
  { current: 100 },
  // 缺少 Position 所需的 x 和 y
);
```

同理：

```typescript
const position = world.get(player, POSITION);
// position 的推导类型是 Position | undefined

const velocity = world.get(player, VELOCITY);
// velocity 的推导类型是 Velocity | undefined
```

如果只写成普通 `symbol`：

```typescript
add<T>(entity: Entity, type: symbol, component: T): void;
```

`type` 与 `component` 之间没有类型联系，编译器无法知道 `POSITION` 必须搭配 `Position` 数据。

如果写成 `component: T`，编译器会同时尝试从 `type` 和 `component` 推导 `T`。在某些调用上下文中，`T` 可能被推导成更宽的类型，导致约束弱于预期。使用 `NoInfer<T>` 可以明确指定：

```text
组件 token 决定 T
组件数据只接受检查，不参与决定 T
```

`NoInfer<T>` 也只存在于编译期，生成 JavaScript 时会被擦除。

#### 4.1.8 as ComponentType<Position> 做了什么

```typescript
const POSITION =
  Symbol("Position") as ComponentType<Position>;
```

运行时执行的只有：

```javascript
const POSITION = Symbol("Position");
```

`as ComponentType<Position>` 是类型断言，它只告诉编译器：

```text
请把这个 Symbol 当作 Position 的组件 token。
```

它不会：

- 修改 Symbol 的运行时结构。
- 创建 `__componentType` 字段。
- 自动验证传入数据。
- 在运行时保存 `Position` 类型定义。

类型断言可以被写错：

```typescript
const WRONG =
  Symbol("Position") as ComponentType<Velocity>;
```

编译器会相信这个声明。因此应通过统一工厂创建 token，减少手写断言：

```typescript
function createComponentType<T>(
  name: string,
): ComponentType<T> {
  return Symbol(name) as ComponentType<T>;
}

const POSITION = createComponentType<Position>("Position");
const VELOCITY = createComponentType<Velocity>("Velocity");
```

#### 4.1.9 编译期安全不等于运行时验证

当前设计提供的是：

```text
编译期：
POSITION token 与 Position 类型关联

运行时：
POSITION Symbol 与 Position Store 关联
```

如果组件数据来自不可信输入，需要给 token 增加运行时校验器：

```typescript
type ComponentDescriptor<T> = Readonly<{
  id: symbol;
  name: string;
  validate: (value: unknown) => value is T;
}>;

const POSITION: ComponentDescriptor<Position> = {
  id: Symbol("Position"),
  name: "Position",
  validate(value): value is Position {
    if (typeof value !== "object" || value === null) {
      return false;
    }

    const candidate = value as Record<string, unknown>;

    return (
      typeof candidate.x === "number" &&
      typeof candidate.y === "number"
    );
  },
};
```

这时：

- 泛型 `T` 继续提供编译期类型推导。
- `id` 提供运行时唯一 Map 键。
- `validate` 提供运行时数据检查。

### 4.2 World 与组件存储

```typescript
class World {
  private readonly slots: EntitySlot[] = [];
  private readonly freeIndices: number[] = [];

  // 外层 Map：组件类型 -> 该类型的组件存储
  // 内层 Map：Entity index -> 组件数据
  private readonly stores = new Map<symbol, Map<number, unknown>>();

  createEntity(): Entity {
    const reusedIndex = this.freeIndices.pop();

    if (reusedIndex !== undefined) {
      const slot = this.slots[reusedIndex];
      slot.alive = true;

      return {
        index: reusedIndex,
        generation: slot.generation,
      };
    }

    const index = this.slots.length;
    this.slots.push({ generation: 0, alive: true });

    return { index, generation: 0 };
  }

  isAlive(entity: Entity): boolean {
    const slot = this.slots[entity.index];

    return (
      slot !== undefined &&
      slot.alive &&
      slot.generation === entity.generation
    );
  }

  destroyEntity(entity: Entity): void {
    this.assertAlive(entity);

    // 删除该 Entity 在所有组件存储中的数据。
    for (const store of this.stores.values()) {
      store.delete(entity.index);
    }

    const slot = this.slots[entity.index];
    slot.alive = false;
    slot.generation += 1;
    this.freeIndices.push(entity.index);
  }

  private assertAlive(entity: Entity): void {
    if (!this.isAlive(entity)) {
      throw new Error(
        `Entity (${entity.index}, ${entity.generation}) is not alive`,
      );
    }
  }

  private getOrCreateStore<T>(
    type: ComponentType<T>,
  ): Map<number, T> {
    let store = this.stores.get(type);

    if (store === undefined) {
      store = new Map<number, unknown>();
      this.stores.set(type, store);
    }

    return store as Map<number, T>;
  }
}
```

此时的数据结构是：

```text
World
├── slots
│   └── Entity index -> { generation, alive }
└── stores
    ├── POSITION -> Map<Entity index, Position>
    └── VELOCITY -> Map<Entity index, Velocity>
```

### 4.3 添加、判断和获取组件

在 `World` 类中加入以下方法：

```typescript
add<T>(
  entity: Entity,
  type: ComponentType<T>,
  component: NoInfer<T>,
): void {
  this.assertAlive(entity);
  this.getOrCreateStore(type).set(entity.index, component);
}

has<T>(entity: Entity, type: ComponentType<T>): boolean {
  return (
    this.isAlive(entity) &&
    (this.stores.get(type)?.has(entity.index) ?? false)
  );
}

get<T>(entity: Entity, type: ComponentType<T>): T | undefined {
  if (!this.isAlive(entity)) {
    return undefined;
  }

  return this.stores.get(type)?.get(entity.index) as T | undefined;
}

remove<T>(entity: Entity, type: ComponentType<T>): boolean {
  this.assertAlive(entity);
  return this.stores.get(type)?.delete(entity.index) ?? false;
}
```

调用过程：

```typescript
const world = new World();
const player = world.createEntity();

world.add(player, POSITION, { x: 0, y: 0 });
world.add(player, VELOCITY, { x: 2, y: 3 });

console.log(world.has(player, POSITION)); // true
console.log(world.has(player, VELOCITY)); // true

const position = world.get(player, POSITION);
console.log(position); // { x: 0, y: 0 }

world.remove(player, VELOCITY);
console.log(world.has(player, VELOCITY)); // false
```

### 4.4 查询同时具有两个组件的实体

继续在 `World` 类中加入：

```typescript
*query2<A, B>(
  typeA: ComponentType<A>,
  typeB: ComponentType<B>,
): IterableIterator<readonly [Entity, A, B]> {
  const storeA = this.stores.get(typeA) as
    | Map<number, A>
    | undefined;
  const storeB = this.stores.get(typeB) as
    | Map<number, B>
    | undefined;

  if (storeA === undefined || storeB === undefined) {
    return;
  }

  // 遍历较小的存储，减少成员检查次数。
  const indices =
    storeA.size <= storeB.size
      ? storeA.keys()
      : storeB.keys();

  for (const index of indices) {
    const slot = this.slots[index];

    if (
      slot === undefined ||
      !slot.alive ||
      !storeA.has(index) ||
      !storeB.has(index)
    ) {
      continue;
    }

    const entity: Entity = {
      index,
      generation: slot.generation,
    };

    yield [
      entity,
      storeA.get(index) as A,
      storeB.get(index) as B,
    ] as const;
  }
}
```

`query2(POSITION, VELOCITY)` 的返回值包含：

```text
[Entity, Position, Velocity]
```

Entity 用于标识当前处理对象，两个组件值用于直接执行系统逻辑。

## 5. MovementSystem 示例

```typescript
function runMovementSystem(
  world: World,
  deltaTime: number,
): void {
  for (const [entity, position, velocity] of world.query2(
    POSITION,
    VELOCITY,
  )) {
    position.x += velocity.x * deltaTime;
    position.y += velocity.y * deltaTime;

    console.log(
      `Moved entity ${entity.index} to (${position.x}, ${position.y})`,
    );
  }
}
```

使用示例：

```typescript
const world = new World();

const player = world.createEntity();
world.add(player, POSITION, { x: 0, y: 0 });
world.add(player, VELOCITY, { x: 2, y: 3 });

const tree = world.createEntity();
world.add(tree, POSITION, { x: 10, y: 20 });

runMovementSystem(world, 0.5);
```

输出：

```text
Moved entity 0 to (1, 1.5)
```

`tree` 没有 `Velocity`，因此不会进入 `MovementSystem` 的查询结果。

## 6. 销毁与 index 复用

```typescript
world.destroyEntity(player);

console.log(world.has(player, POSITION)); // false

const enemy = world.createEntity();

console.log(enemy.index === player.index); // 可能为 true
console.log(enemy.generation);             // 比 player 大 1
console.log(world.isAlive(player));         // false
console.log(world.isAlive(enemy));          // true
```

销毁实体时必须：

1. 从全部组件存储中删除该 index。
2. 将槽位标记为不可用。
3. 增加 generation。
4. 将 index 放入空闲列表等待复用。

这样组件查询和 Entity 生命周期才能保持一致。

## 7. 生产级 ECS 如何优化

上述 `Map` 实现适合解释语义，但生产级 ECS 通常使用更紧凑的数据结构：

| 实现 | has/get | 多组件查询 | 特点 |
|---|---|---|---|
| Map | 哈希查找 | Entity ID 交集 | 简单直观 |
| Sparse Set | 稀疏数组定位 | 遍历最小 dense 数组 | 单组件增删高效 |
| Archetype | 定位实体所在原型 | 直接遍历匹配原型 | 多组件批处理更连续 |

三者的共同逻辑都是：

```text
Entity 提供身份
Component Storage 保存数据
Entity index 关联身份与数据
Query 找出满足组件条件的 Entity
System 处理 Query 返回的组件数据
```

[上一章：最小示例](./05-minimal-example.md) | [返回目录](./README.md) | [下一章：高性能存储设计](./07-high-performance-storage.md)
