# 内容贡献指南

## 1. 基本原则

内容仓库采用约定驱动结构。第一层是岗位赛道，第二层是内容模块。
目录表达分类，Markdown 一级标题表达展示名称，文件名表达阅读顺序。
只有需要 IDE 式阅读的完整代码工程使用 `code-project.json`。

## 2. 目录层级

```text
<track>/
└── <module>/
    └── <topic>/
        ├── README.md
        ├── 01-<chapter>.md
        └── 02-<chapter>.md
```

当前内置赛道为 `program/` 和 `planning/`。程序赛道包含 `knowledge/`、
`interviews/`、`examples/`、`code/`；策划赛道包含 `knowledge/`、
`interviews/`、`written-tests/`、`cases/`、`templates/`。

新增知识体系时，只需在对应赛道模块下创建主题文件夹并添加 Markdown。
`README.md` 用于说明范围和推荐阅读顺序；内容很短时也可以只保留该文件。

## 3. 八股与专题

### 3.1 在已有专题下新增子专题

例如，在 C++ 专题下新增“多态”专题，推荐结构为：

```text
program/
└── knowledge/
    └── cpp/
        ├── README.md
        ├── 01-cpp98.md
        └── polymorphism/
            ├── README.md
            ├── 01-runtime-polymorphism.md
            ├── 02-vtable-and-dispatch.md
            └── 03-static-polymorphism.md
```

其中：

- `polymorphism/README.md` 是子专题入口，一级标题写 `# C++ 多态`。
- 只有一篇内容时，可以只创建 `README.md`。
- 内容较多时，按阅读顺序添加 `01-`、`02-` 等章节文件。
- 目录和文件名使用小写 ASCII 与连字符，中文展示名称写在 Markdown 标题中。
- 建议在父专题 `program/knowledge/cpp/README.md` 中增加子专题链接，方便直接在仓库中阅读。

网站会自动生成以下页面，不需要修改网站配置：

```text
/program/knowledge/cpp/polymorphism/
/program/knowledge/cpp/polymorphism/01-runtime-polymorphism/
/program/knowledge/cpp/polymorphism/02-vtable-and-dispatch/
/program/knowledge/cpp/polymorphism/03-static-polymorphism/
```

若只需要配套短示例，不要把源码放在知识文档目录中。短示例统一放在：

```text
program/
└── examples/
    └── cpp/
        └── polymorphism/
            ├── README.md
            └── main.cpp
```

然后在 `program/knowledge/cpp/polymorphism/README.md` 中使用相对链接引用示例。
需要文件树、全文搜索和跨文件跳转的完整工程放在 `program/code/`。

## 4. 面经

```text
<track>/
└── interviews/
    └── <company>/
        └── <event-or-position>/
            ├── README.md
            ├── 01-first-round.md
            └── 01-first-round-answers.md
```

原始题目与参考答案分文件保存，便于读者先模拟作答再复盘。公司和批次目录
使用稳定的 ASCII 名称，中文信息写在 Markdown 标题中。

## 5. 短代码示例

```text
program/
└── examples/
    └── <domain>/
        └── <example>/
            ├── README.md
            └── <source files>
```

每个示例必须包含 `README.md`，说明目标、运行环境、命令和预期结果。
`program/examples/` 适合单文件或少量文件、可按普通页面阅读的示例。

## 6. 完整代码工程

```text
program/
└── code/
    └── <domain>/
        └── <project>/
            ├── code-project.json
            ├── README.md
            └── <source tree>
```

完整工程由独立代码处理器生成 IDE 式工作区，支持文件树、标签页、浏览器内
全文搜索、符号大纲、定义跳转和引用查找。工程必须遵循
[代码工程接入规范](./program/code/project-convention.md)。

`bin/`、`obj/`、`build/`、依赖缓存和编辑器索引不得提交。

## 7. Markdown 约定

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

## 8. 提交信息

提交信息使用 `<范围>: <摘要>`，例如：

```text
program/knowledge: 新增 C++20 协程专题
program/interviews: 新增某公司客户端二面记录
program/examples: 补充帧同步预测示例
program/code: 新增 Render Graph 阅读工程
planning/cases: 新增系统设计案例拆解
```
