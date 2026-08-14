# 文档中心

## 1. 主题目录

| 主题 | 内容 | 入口 |
|---|---|---|
| ECS 系统 | Entity Component System 的原理、结构、流程与实践 | [开始阅读](./ecs-system/README.md) |
| 游戏客户端面试 | 客户端基础、引擎、渲染、网络、性能和项目设计知识分类 | [查看分类](./game-client-interview/README.md) |
| Knowledge Architect Skill | 系统性知识回复、Markdown 信息架构与全库重编排工作流 | [查看设计](./knowledge-architect-skill/README.md) |

## 2. 文档组织规范

后续主题统一采用以下结构：

```text
<topic>/
├── README.md            # 范围说明、摘要和阅读导航
├── 01-<section>.md      # 基础概念
├── 02-<section>.md      # 核心机制
└── ...                  # 实践、示例或扩展内容
```

内容编写遵循以下规则：

1. 先给结论和适用范围，再解释原理。
2. 使用分级标题拆分概念，避免大段连续文字。
3. 复杂关系优先使用表格、流程图或代码示例。
4. 每个文件只聚焦一个主题。
5. 主题首页维护阅读顺序和文件链接。
6. 明确区分事实、设计建议、假设和适用边界。
