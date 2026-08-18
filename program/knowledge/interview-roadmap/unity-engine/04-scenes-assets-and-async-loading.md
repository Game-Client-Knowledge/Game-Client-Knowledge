# 场景、资源与异步加载

## 1. Scene 在运行时是什么

Scene 资源保存一组可序列化对象和资源引用。加载 Scene 时，Unity 大致需要：

```text
读取 Scene 数据
-> 解析 GameObject 和 Component
-> 解析依赖资源
-> 创建原生/托管对象
-> 恢复序列化字段和引用
-> 激活对象并进入生命周期
```

运行时可同时存在多个已加载 Scene：

```text
Persistent     管理器、音频、网络
Level_Forest   地形、敌人、关卡逻辑
UI_HUD         HUD 和战斗 UI
Lighting_Day   灯光与烘焙配置
```

这叫 Additive 多场景组织。它适合团队协作、分区加载和职责拆分，但跨 Scene 引用
与卸载顺序必须明确。

## 2. Active Scene 与 Loaded Scene

几个概念不要混在一起：

- **Loaded**：Scene 数据已经加载到内存。
- **Active Scene**：新建无父对象默认归属的 Scene，并影响部分环境设置。
- **Enabled GameObject**：Scene 中具体对象是否激活。
- **Visible**：对象是否最终被摄像机渲染。

设置 Active Scene：

```csharp
using UnityEngine.SceneManagement;

Scene level = SceneManager.GetSceneByName("Level_Forest");
if (level.IsValid() && level.isLoaded)
{
    SceneManager.SetActiveScene(level);
}
```

Active 不等于“只有它运行”。其他已加载 Scene 中的 active GameObject 仍可更新。

## 3. Single 与 Additive 加载

### 3.1 Single

```csharp
SceneManager.LoadScene("Level_Forest", LoadSceneMode.Single);
```

新 Scene 替换当前普通 Scene。标记为 `DontDestroyOnLoad` 的对象可保留。

### 3.2 Additive

```csharp
SceneManager.LoadScene("UI_HUD", LoadSceneMode.Additive);
```

新 Scene 与已有 Scene 同时存在。卸载时：

```csharp
SceneManager.UnloadSceneAsync("UI_HUD");
```

多场景不是免费的平行宇宙。若两个 Scene 都放了 EventSystem、主 Camera 或
AudioListener，加载后它们会非常积极地一起工作，然后 Console 也会积极提醒你。

## 4. 异步加载 Scene

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    public IEnumerator LoadLevel(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(
            sceneName,
            LoadSceneMode.Single
        );

        while (!operation.isDone)
        {
            float normalizedProgress = operation.progress;
            UpdateLoadingBar(normalizedProgress);
            yield return null;
        }
    }

    private void UpdateLoadingBar(float progress)
    {
        // 更新 UI。
    }
}
```

异步意味着把加载工作分阶段调度，降低长时间阻塞风险，不保证完全没有主线程
工作。对象激活、脚本初始化和部分资源上传仍可能产生尖峰。

### 4.1 延迟激活

```csharp
AsyncOperation operation =
    SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

operation.allowSceneActivation = false;

while (operation.progress < 0.9f)
{
    UpdateLoadingBar(operation.progress / 0.9f);
    yield return null;
}

yield return WaitForPlayerConfirmation();
operation.allowSceneActivation = true;
```

当禁止激活时，进度常停在约 `0.9`，`isDone` 也不会变为 true。这里的 0.9 是
Unity 异步 Scene 加载协议中的阶段标记，不代表“全宇宙还有精确 10% 的字节”。

### 4.2 降低激活尖峰

- 减少 `Awake`/`OnEnable`/`Start` 的集中重活。
- 将非关键初始化分帧。
- 对大量对象使用池或分批激活。
- 提前加载 Shader Variant 和关键资源。
- 用 Profiler 区分 IO、反序列化、脚本和 GPU 上传。
- 在目标设备测试，不只在编辑器里看进度条。

## 5. `DontDestroyOnLoad`

```csharp
private void Awake()
{
    DontDestroyOnLoad(gameObject);
}
```

它把对象及其子层级移入特殊的持久 Scene，使其在 Single 切场景时不被销毁。
常用于：

- 游戏总入口。
- 音频管理。
- 网络会话。
- 全局加载界面。

典型单例保护：

```csharp
public sealed class GameRoot : MonoBehaviour
{
    public static GameRoot Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

每个 Scene 都放一个“永久管理器”却没有去重，会让切场景像复印会议主持人：
每个人都觉得自己应该发言。

## 6. Unity 中的“资源”有哪些

常见资源：

- Texture、Sprite、Mesh、AnimationClip、AudioClip。
- Material、Shader、RenderTexture。
- Prefab、Scene、ScriptableObject。
- 字体、视频、配置和本地化数据。
- AssetBundle 或 Addressables 管理的远端内容。

资源与实例不同：

```text
Prefab asset --Instantiate--> GameObject instance
Mesh asset -----------------> MeshFilter 引用
Material asset -------------> Renderer 引用
AudioClip asset ------------> AudioSource 引用
```

销毁 GameObject 实例不一定立即卸载它引用的资源；释放资源也必须确认没有活跃实例
仍在使用。

## 7. 四种常见资源引用方式

### 7.1 Inspector 直接引用

```csharp
[SerializeField] private GameObject enemyPrefab;
```

优点：

- 类型安全。
- 引用关系可视化。
- Unity 自动收集构建依赖。

适合固定依赖。缺点是大型内容系统若所有资源都被入口 Scene 直接引用，可能导致
不必要的加载和依赖膨胀。

### 7.2 `Resources`

放在任意 `Resources/` 目录下：

```csharp
GameObject prefab = Resources.Load<GameObject>("Enemies/Goblin");
GameObject instance = Instantiate(prefab);
```

异步：

```csharp
ResourceRequest request =
    Resources.LoadAsync<GameObject>("Enemies/Goblin");

yield return request;

GameObject prefab = (GameObject)request.asset;
Instantiate(prefab);
```

优点是简单，适合少量启动资源或原型；代价包括：

- 字符串路径缺少编译期检查。
- `Resources` 内容会进入构建。
- 大量资源会让依赖、构建和卸载管理变得模糊。

`Resources` 不是禁止使用，而是不适合假扮完整内容平台。

### 7.3 AssetBundle

AssetBundle 是 Unity 构建出的资源容器，可从本地或网络加载：

```text
下载/读取 Bundle
-> 加载 AssetBundle
-> 从 Bundle 加载资源
-> 实例化或使用资源
-> 释放资源实例和 Bundle
```

需要自行处理：

- Bundle 拆分粒度。
- 依赖和版本。
- 下载、缓存、校验与重试。
- 加载句柄和释放顺序。
- 重复资源和 Shader Variant。

它是底层能力，灵活但工程责任较多。

### 7.4 Addressables

Addressables 在 AssetBundle 等能力之上提供地址、分组、依赖、Catalog、下载和
引用计数式句柄管理。

概念流程：

```text
address / label
-> 定位 Catalog 条目
-> 下载或读取依赖 Bundle
-> 加载目标 Asset
-> 返回 AsyncOperationHandle
-> 使用
-> Release
```

使用 Addressables 需要安装对应 Package。不同版本 API 细节可能变化，以下示例
表达句柄生命周期：

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddressableSpawner : MonoBehaviour
{
    private AsyncOperationHandle<GameObject> prefabHandle;
    private GameObject instance;

    public IEnumerator Spawn(string address)
    {
        prefabHandle =
            Addressables.LoadAssetAsync<GameObject>(address);

        yield return prefabHandle;

        if (prefabHandle.Status != AsyncOperationStatus.Succeeded)
        {
            yield break;
        }

        instance = Instantiate(prefabHandle.Result);
    }

    private void OnDestroy()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        if (prefabHandle.IsValid())
        {
            Addressables.Release(prefabHandle);
        }
    }
}
```

若使用 `Addressables.InstantiateAsync`，通常应配对 `ReleaseInstance`。核心规则
不是死记函数名，而是：

```text
谁持有 handle
-> 谁能创建实例
-> 实例何时销毁
-> handle 何时 Release
```

过早 Release 可能让依赖生命周期失去保障；从不 Release 则会让内存保持增长。

## 8. 资源异步加载不等于异步实例化

```text
异步读取资源
-> 资源可用
-> Instantiate Prefab
-> Awake / OnEnable
-> Start
```

前半段异步并不自动消除后半段的主线程成本。一个包含几千个组件的 Prefab 即使
下载得很优雅，实例化时仍可能在主线程举办一场大型点名。

常见优化：

- 拆分超大 Prefab。
- 分批实例化或激活。
- 使用对象池。
- 将静态配置与运行实例分离。
- 避免每个子对象在 `Awake` 中扫描全场。
- 对可重复内容做增量加载。

## 9. 资源释放链

以 Addressables Prefab 为例：

```text
Addressables Handle
        |
        v
Prefab Asset + dependencies
        |
        v
GameObject instance
```

释放通常要考虑：

1. 停止使用实例的逻辑。
2. 销毁或归还实例。
3. 释放加载句柄。
4. 等待引用计数允许依赖卸载。

`Resources.UnloadUnusedAssets` 会扫描不再被引用的资源，可能很昂贵，不应代替
明确的生命周期管理。GC 负责托管内存，也不会自动理解“这个贴图现在该从显存
离开了”。

## 10. 跨 Scene 引用

编辑器中的直接跨 Scene 引用容易受到加载顺序和卸载影响。常见策略：

- 通过持久服务查找运行时能力。
- 使用稳定 ID，在 Scene 加载后解析。
- Scene 自己注册入口和出口。
- 用 ScriptableObject 事件通道或配置传递资源引用。
- 通过 Addressables 地址加载共享资源。

避免让一个关卡对象长期持有另一个即将卸载 Scene 的对象引用。那像保留一张已经
拆掉的房间门卡，刷得再认真也没有门。

## 11. 一个较完整的关卡切换流程

```text
锁定重复切换
-> 显示加载 UI
-> 保存当前关卡状态
-> 停止或转移跨场景任务
-> 异步加载新 Scene，暂不激活
-> 预加载新关卡关键资源
-> 允许激活
-> 设置 Active Scene
-> 初始化新关卡服务
-> 卸载旧 Scene
-> 释放旧资源 handle
-> 隐藏加载 UI
-> 恢复输入
```

需要处理失败、重复点击、网络断开和应用进后台。只写成功路径的加载器，在真机上
通常会很快收到现实补充的异常分支。

## 12. 本章检查

1. Loaded Scene 与 Active Scene 有何区别？
2. Additive 加载为什么可能出现两个 Camera 或 EventSystem？
3. `allowSceneActivation = false` 时进度为何常停在 0.9？
4. Prefab Asset 和实例的生命周期为何不同？
5. `Resources`、AssetBundle、Addressables 各适合什么规模？
6. Addressables handle 为什么必须明确 Release？
7. 异步加载完成后为什么仍可能在实例化阶段卡顿？
8. `UnloadUnusedAssets` 为什么不能代替资源所有权设计？

[上一章：协程、异步与时间](./03-coroutines-async-and-time.md) |
[返回 Unity 引擎基础](./README.md) |
[下一章：输入、角色移动与摄像机](./05-input-character-movement-and-camera.md)
