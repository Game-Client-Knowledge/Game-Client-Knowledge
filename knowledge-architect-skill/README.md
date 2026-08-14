# Knowledge Architect Skill

## 1. 目标

`knowledge-architect` 用于把复杂问题转化为两个相互配合的交付物：

1. 对话中给出简洁、高信号的结论。
2. 在仓库根目录维护系统化、分层、可索引的 Markdown 知识库。

它适用于：

- 技术概念的系统讲解。
- 机制、运行时和性能原理分析。
- 软件架构设计与方案审查。
- 技术选型、替代方案和权衡分析。
- 对已有知识文档的持续补充与重构。

## 2. 核心工作流

```text
发现现有文档和证据
-> 分类问题
-> 构建知识结构
-> 必要时检索来源
-> 编写或更新文档
-> 扫描全部文档并重新编排
-> 执行机械校验
-> 返回对话摘要和文件链接
```

其中“扫描全部文档并重新编排”是每次实质性回答后的强制步骤，不能只检查本次修改文件。

## 3. 全库重编排

每次完成内容编写后，Skill 会重新审视：

- 根目录 `README.md` 是否覆盖全部主题。
- 每个主题是否存在 `README.md`。
- 阅读顺序是否符合知识前置关系。
- 是否存在重复解释或相互竞争的标题。
- 文件职责是否过大或过度碎片化。
- 前后章节和语义交叉链接是否完整。
- 术语和结论是否在不同文档中保持一致。
- 是否存在无法从根索引到达的孤立文档。

该步骤是语义上的信息架构检查，不等同于简单的死链扫描。

## 4. Skill 结构

> 说明：本文档是 `knowledge-architect` Skill 的设计文档。Skill 的实际文件不存放于本仓库，以下是其规划结构。

```text
knowledge-architect/
├── SKILL.md
├── references/
│   ├── workflow.md
│   ├── information-architecture.md
│   ├── content-patterns.md
│   ├── quality-gates.md
│   ├── source-policy.md
│   └── evaluation-cases.md
├── assets/
│   ├── topic-readme-template.md
│   └── architecture-analysis-template.md
└── scripts/
    └── audit_docs.py
```

各部分职责：

| 文件 | 职责 |
|---|---|
| `SKILL.md` | 触发条件、强制规则和核心执行顺序 |
| `workflow.md` | 更新、创建、拆分和合并的详细判断 |
| `information-architecture.md` | 索引、粒度、命名和全库重编排算法 |
| `content-patterns.md` | 概念、机制、架构、决策和教程模板 |
| `quality-gates.md` | 内容、证据、结构和导航质量门 |
| `source-policy.md` | 来源优先级、引用规则和借鉴记录 |
| `evaluation-cases.md` | 后续修改 Skill 时使用的回归场景 |
| `assets/` | 新主题首页和架构分析起始模板 |
| `audit_docs.py` | Markdown 结构、链接和可达性审计 |

## 5. 审计命令

在设计结构中，审计脚本规划在 `knowledge-architect/scripts/audit_docs.py`，在仓库根目录执行：

```bash
python3 knowledge-architect/scripts/audit_docs.py docs
```

脚本会检查：

- 每篇文档恰好有一个 H1。
- 标题层级没有跳跃。
- 代码围栏完整闭合。
- 相对链接有效。
- 一级主题目录包含 `README.md`。
- 每篇文档都可以从根目录 `README.md` 到达。
- 文档是否缺少入站链接。

脚本负责确定性机械检查；内容是否重复、章节是否应该拆分等语义问题仍由全库重编排步骤判断。

## 6. 借鉴来源

Skill 采用原创实现，并借鉴以下公开实践：

- [Agent Skills Specification](https://agentskills.io/specification)：目录标准、描述字段和渐进式披露。
- [Agent Skills Engineering Guide](https://www.anthropic.com/engineering/equipping-agents-for-the-real-world-with-agent-skills)：核心指令、按需引用和确定性脚本的分层。
- [ADR Author Skill](https://github.com/JayRHa/AgentSkills/tree/main/adr-author)：事实与假设分离、真实备选方案和负面后果。
- [API Docs Writer Skill](https://github.com/JayRHa/AgentSkills/tree/main/api-docs-writer)：先定位事实来源、避免编造、使用验证门。
- [GitHub agents.md research](https://github.blog/ai-and-ml/github-copilot/how-to-write-a-great-agents-md-lessons-from-over-2500-repositories/)：明确职责、命令、边界和示例。
- Marketplace `knowledge-capture` Skill：内容分类、目标位置选择和知识可发现性。

没有直接复制第三方 Skill；这里只提炼其工作流原则。

## 7. 验证结果

设计验证阶段已完成：

```text
Skill frontmatter 校验
Python 语法编译
全部 Skill Markdown 代码围栏检查
现有文档全库扫描
相对链接与根索引可达性检查
```

首次在 ECS 文档集运行审计：

```text
Documents scanned: 11
Errors: 0
Warnings: 0
```

加入本文档后应再次运行审计，以验证新的主题索引关系。

