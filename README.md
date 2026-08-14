# 文档中心

## 1. 主题目录

| 主题 | 内容 | 入口 |
|---|---|---|
| ECS 系统 | Entity Component System 的原理、结构、流程与实践 | [开始阅读](./ecs-system/README.md) |
| 游戏客户端面试 | 客户端基础、引擎、渲染、网络、性能和项目设计知识分类 | [查看分类](./game-client-interview/README.md) |
| C++ 基础知识 | 多重继承与 mix-in、RAII、分配器、模板特征，及 C++11/14/17 新特性 | [开始阅读](./cpp-fundamentals/README.md) |
| Knowledge Architect Skill | 系统性知识回复、Markdown 信息架构与全库重编排工作流 | [查看设计](./knowledge-architect-skill/README.md) |

## 2. 文档组织规范

后续主题统一采用以下结构：

```text
<topic>/
├── README.md            # 范围说明、摘要和阅读导航
├── 01-<section>.md      # 基础概念
├── 02-<section>.md      # 核心机制
├── ...                  # 实践、示例或扩展内容
└── example/             # 示例程序目录
    └── <知识点名称>/    # 示例程序文件夹以知识点命名
```

内容编写遵循以下规则：

1. 先给结论和适用范围，再解释原理。
2. 使用分级标题拆分概念，避免大段连续文字。
3. 复杂关系优先使用表格、流程图或代码示例。
4. 每个文件只聚焦一个主题。
5. 主题首页维护阅读顺序和文件链接。
6. 明确区分事实、设计建议、假设和适用边界。

## 3. 提交规范

提交信息使用中文，格式为：

```text
<模块>: <提交内容总结>
```

- 模块：本次修改对应的主题目录名，如 `ecs-system`、`game-client-interview`、`knowledge-architect-skill`；仓库级修改使用 `docs`。
- 提交内容总结：一句话概括本次改动内容。

示例：

```text
ecs-system: 新增组件存储与查询实现章节
game-client-interview: 补充网络同步模块面试题
```

每次提交需在对应模块的 `README.md` 中添加或更新本次涉及文档的目录条目。

## 4. 示例程序规范

- 示例程序统一放在对应主题目录下的 `example/` 目录中。
- 示例程序文件夹以其对应的知识点为名称，如 `example/generation-entity-handle/`。
- 每个示例包内建议包含源码与必要说明（README 或注释）。
