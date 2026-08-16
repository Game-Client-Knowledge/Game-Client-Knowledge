# 内容贡献指南

## 1. 基本原则

内容仓库采用约定驱动结构。新增内容不需要 frontmatter、清单文件或网站配置。
目录表达分类，Markdown 一级标题表达展示名称，文件名表达阅读顺序。

## 2. 八股与专题

```text
knowledge/
└── <topic>/
    ├── README.md
    ├── 01-<chapter>.md
    └── 02-<chapter>.md
```

新增知识体系时，只需在 `knowledge/` 下创建主题文件夹并添加 Markdown。
`README.md` 用于说明范围和推荐阅读顺序；内容很短时也可以只保留该文件。

## 3. 面经

```text
interviews/
└── <company>/
    └── <event-or-position>/
        ├── README.md
        ├── 01-first-round.md
        └── 01-first-round-answers.md
```

原始题目与参考答案分文件保存，便于读者先模拟作答再复盘。公司和批次目录
使用稳定的 ASCII 名称，中文信息写在 Markdown 标题中。

## 4. 代码示例

```text
examples/
└── <domain>/
    └── <example>/
        ├── README.md
        └── <source files>
```

每个示例必须包含 `README.md`，说明目标、运行环境、命令和预期结果。源码可按
语言和工程惯例自由组织，网站会自动展示说明、文件树和可识别的文本源码。

## 5. Markdown 约定

1. 每个 Markdown 文件只使用一个一级标题。
2. 标题层级逐级递进，不从二级标题直接跳到四级标题。
3. 仓库内链接使用相对路径，外部资料使用完整 HTTPS URL。
4. 图片放在内容所在目录的 `assets/` 子目录中，并使用相对路径引用。
5. 代码块标注语言；流程关系可使用 Mermaid。
6. 文件名推荐使用小写 ASCII 和连字符，章节使用两位数字前缀排序。

frontmatter 是可选项。网站默认从正文推导标题和摘要；只有需要覆盖自动结果时才
添加以下字段：

```yaml
---
title: 自定义标题
description: 一句话摘要
order: 10
---
```

## 6. 提交信息

提交信息使用 `<范围>: <摘要>`，例如：

```text
knowledge: 新增 C++20 协程专题
interviews: 新增某公司客户端二面记录
examples: 补充帧同步预测示例
```
