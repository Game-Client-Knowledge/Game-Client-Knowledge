# Game Client Knowledge

面向游戏客户端开发与面试准备的开放知识库。本仓库只保存 Markdown、
图片和示例源码；展示网站位于独立的 `Game-Client-Knowledge-Web` 仓库。

## 内容目录

| 目录 | 内容 |
|---|---|
| [`knowledge/`](./knowledge/README.md) | 八股、语言基础、引擎原理和专题知识 |
| [`interviews/`](./interviews/README.md) | 按公司、批次和岗位整理的真实面经 |
| [`examples/`](./examples/README.md) | 与知识点或面经配套的可运行代码 |

## 最简贡献方式

不需要登记导航、修改网站代码或编写配置文件。

1. 在对应内容类型下创建语义清晰的目录。
2. 添加一个带一级标题的 `README.md`。
3. 需要拆章时继续添加 `01-topic.md`、`02-topic.md` 等文件。
4. 使用相对路径引用仓库内的其他文档或资源。

网站在构建时递归扫描目录，以 Markdown 的第一个一级标题作为名称，以
文件名前缀作为默认顺序，并自动生成导航、上下篇和全文搜索索引。

完整约定参见 [CONTRIBUTING.md](./CONTRIBUTING.md)。
