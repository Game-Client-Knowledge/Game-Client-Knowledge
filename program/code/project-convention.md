# 代码工程接入规范

## 1. 适用范围

`code/` 用于需要按完整工程阅读的源码，例如客户端框架、渲染模块、ECS、
网络同步或可运行游戏逻辑。只有几十行、可在一页读完的示例继续放在
`examples/`。

每个代码工程必须是独立目录，并包含：

```text
code/<domain>/<project>/
├── code-project.json
├── README.md
└── <source tree>
```

## 2. 项目清单

`code-project.json` 是代码处理器唯一读取的项目配置：

```json
{
  "schemaVersion": 1,
  "id": "render-graph-demo",
  "title": "Render Graph 示例",
  "description": "资源生命周期和 Pass 调度示例。",
  "language": "cpp",
  "entry": "src/main.cpp",
  "readme": "README.md",
  "includeExtensions": [".cpp", ".h", ".md", ".json"],
  "exclude": ["build", "vendor"],
  "readingOrder": [
    "src/RenderGraph.h",
    "src/RenderGraph.cpp",
    "src/main.cpp"
  ]
}
```

字段约定：

| 字段 | 要求 |
| --- | --- |
| `schemaVersion` | 当前固定为 `1` |
| `id` | 全仓库唯一的小写 kebab-case 标识 |
| `title` | 工作区展示名称 |
| `description` | 一句话说明工程目标 |
| `language` | 工程主语言，例如 `csharp`、`cpp` |
| `entry` | 默认打开的入口文件，必须被文件规则收录 |
| `readme` | 工程说明文件，推荐为 `README.md` |
| `includeExtensions` | 允许发布和搜索的文本扩展名 |
| `exclude` | 在默认排除目录之外追加忽略目录 |
| `readingOrder` | 推荐阅读文件顺序 |

所有路径均相对于 `code-project.json` 所在目录，不允许绝对路径、反斜杠
或 `..`。

## 3. 必须排除的内容

处理器默认排除以下目录：

```text
.git/
.idea/
.vs/
.vscode/
bin/
build/
dist/
node_modules/
obj/
packages/
```

禁止提交编译产物、依赖缓存、编辑器索引、密钥、用户配置和二进制包。
第三方源码只有在阅读目标本身依赖其实现时才可以收录，并应在 `exclude`
与 README 中解释边界。

单个文本文件不得超过 1 MB；单个工程最多发布 2,000 个文件、16 MB 文本。
超过边界时应拆分工程或只保留与阅读目标相关的源码。

## 4. README 要求

工程 README 至少说明：

1. 代码目标和不覆盖的范围。
2. 构建工具、语言版本和依赖。
3. 构建、运行和测试命令。
4. 推荐阅读顺序。
5. 目录结构和核心数据流。
6. 教学性简化与生产边界。

## 5. 浏览器处理

网站只静态发布清单允许的文本文件。浏览器按需加载当前文件，并在空闲时：

- 缓存项目文本。
- 使用 Tree-sitter WASM 解析 C# 或 C++。
- 建立类、结构体、接口、方法、属性和字段索引。
- 提供项目关键词搜索、定义跳转和引用列表。

代码解析和查询不调用编辑器后端，也不占用运行时服务器 CPU。

## 6. 提交前检查

1. 入口、README 和推荐阅读文件均存在。
2. `bin/obj/build` 等目录没有被追踪。
3. 新工程 ID 不与已有工程重复。
4. README 中的命令可以在干净环境执行。
5. 源码文件使用 UTF-8 文本格式。
6. 提交信息使用 `code: <摘要>`。
