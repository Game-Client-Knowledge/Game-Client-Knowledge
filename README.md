# Game Client Knowledge

面向游戏客户端开发与面试准备的开放知识库。本仓库只保存 Markdown、
图片和示例源码；展示网站位于独立的 `Game-Client-Knowledge-Web` 仓库。

## 内容目录

| 赛道 / 模块 | 内容 |
|---|---|
| [`program/`](./program/README.md) | 游戏客户端程序赛道 |
| [`program/knowledge/`](./program/knowledge/README.md) | 程序八股、语言基础、引擎原理和专题知识 |
| [`program/interviews/`](./program/interviews/README.md) | 程序面经 |
| [`program/examples/`](./program/examples/README.md) | 程序短代码示例 |
| [`program/code/`](./program/code/README.md) | 程序完整代码工程 |
| [`planning/`](./planning/README.md) | 游戏策划赛道 |
| [`planning/knowledge/`](./planning/knowledge/README.md) | 策划八股与基础知识 |
| [`planning/interviews/`](./planning/interviews/README.md) | 策划面经 |
| [`planning/written-tests/`](./planning/written-tests/README.md) | 策划笔试题 |
| [`planning/cases/`](./planning/cases/README.md) | 策划案例拆解 |
| [`planning/templates/`](./planning/templates/README.md) | 策划作品集与文档模板 |

## 最简贡献方式

不需要登记导航、修改网站代码或编写配置文件。

1. 先选择岗位赛道，再在对应内容模块下创建语义清晰的目录。
2. 添加一个带一级标题的 `README.md`。
3. 需要拆章时继续添加 `01-topic.md`、`02-topic.md` 等文件。
4. 使用相对路径引用仓库内的其他文档或资源。

网站在构建时递归扫描目录，以 Markdown 的第一个一级标题作为名称，以
文件名前缀作为默认顺序，并自动生成导航、上下篇和全文搜索索引。

完整约定参见 [CONTRIBUTING.md](./CONTRIBUTING.md)。
