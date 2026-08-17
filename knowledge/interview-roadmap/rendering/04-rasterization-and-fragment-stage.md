# 光栅化与片元阶段

## 1. 从三角形到候选像素

几何阶段得到屏幕空间三角形后，光栅化器判断它覆盖哪些采样点，并为这些位置
生成片元。

```text
屏幕像素网格

o---o---o---o---o
|   |  /|   |   |
o---o-/ o---o---o
|  /|###|###|   |
o-/ o###o###o---o
/   |###| / |   |
o---o---o/--o---o

# 表示三角形覆盖的采样区域
```

光栅化不是把三角形变成一张永久位图，而是为当前 Draw Call 产生后续阶段要处理
的片元候选。

## 2. 片元不完全等于像素

- **像素 Pixel**：最终图像网格中的一个位置。
- **采样点 Sample**：判断覆盖、深度或抗锯齿的采样位置。
- **片元 Fragment**：某个图元为某个采样位置产生的候选数据。

多个三角形可能在同一个像素位置产生多个片元，最后只有通过深度、模板等测试并
完成混合的结果留在颜色缓冲中。开启 MSAA 时，一个像素还可能包含多个覆盖和
深度采样。

所以 Fragment Shader 常被叫作 Pixel Shader，但“执行一次就等于最终得到一个
屏幕像素”并不严格。

## 3. 覆盖判断与重心坐标

光栅化器可以使用边函数判断采样点是否在三角形内部，并得到重心坐标：

```text
P = alpha * A + beta * B + gamma * C
alpha + beta + gamma = 1
```

`alpha`、`beta`、`gamma` 表示点 `P` 相对三个顶点的权重。它们不仅能判断点
是否位于三角形内，也用于插值 UV、颜色、法线等顶点输出。

## 4. 属性插值

假设三个顶点的 UV 分别为：

```text
A.uv = (0, 0)
B.uv = (1, 0)
C.uv = (0, 1)
```

光栅化器会为三角形内部每个片元插值得到 UV，Fragment Shader 再用该 UV 采样
纹理。

透视投影下不能简单在屏幕空间线性插值所有属性，否则倾斜表面的纹理会扭曲。
GPU 通常执行透视正确插值，直觉上会把属性与裁剪空间 `w` 的关系纳入计算。

可在 Shader 中为特定数据选择不同插值方式，例如不插值的实例 ID 或平坦法线，
具体语法随着色语言变化。

## 5. Early-Z

如果硬件能在执行复杂 Fragment Shader 前判断片元一定会被深度测试拒绝，就能
提前丢弃它，这通常称为 Early-Z。

```text
片元到达
-> 提前深度测试失败
-> 不执行昂贵的 Fragment Shader
```

Early-Z 可能受以下行为限制：

- Shader 修改深度。
- 使用 `discard`/`clip` 丢弃片元。
- 某些混合、排序或硬件条件。
- Shader 具有影响执行顺序判断的副作用。

具体 GPU 可能采用 Early、Late 或层次化深度等策略。面试时不要把 Early-Z
描述成所有设备上永远固定的位置。

## 6. Fragment Shader 做什么

Fragment Shader 接收插值数据和绑定资源，计算当前片元输出。

简化 HLSL 风格代码：

```hlsl
struct PSInput
{
    float4 positionCS : SV_POSITION;
    float3 normalWS   : TEXCOORD0;
    float2 uv         : TEXCOORD1;
};

float4 PSMain(PSInput input) : SV_TARGET
{
    float3 baseColor = BaseColorTexture.Sample(
        LinearWrapSampler,
        input.uv
    ).rgb;

    float3 normal = normalize(input.normalWS);
    float nDotL = saturate(dot(normal, -LightDirection));
    float3 lighting = AmbientColor + LightColor * nDotL;

    return float4(baseColor * lighting, 1.0);
}
```

这个例子完成：

1. 使用 UV 采样基础颜色纹理。
2. 使用法线和光线方向计算简单漫反射。
3. 加入环境光。
4. 输出最终候选颜色。

真实材质可能还会采样法线、粗糙度、金属度、阴影和环境纹理，并执行 PBR 计算。

## 7. 纹理采样

### 7.1 过滤

| 方式 | 特点 |
|---|---|
| Nearest | 取最近 Texel，清晰硬朗但容易像素化 |
| Bilinear | 在同一级 Mipmap 邻近 Texel 间插值 |
| Trilinear | 在两级 Mipmap 的双线性结果间再插值 |
| Anisotropic | 改善斜视表面的纹理清晰度，成本更高 |

Texel 是纹理中的元素，Pixel 是屏幕图像中的元素。一个屏幕像素可能读取多个
Texel，一个 Texel 也可能影响多个屏幕像素。

### 7.2 Mipmap

Mipmap 保存逐级缩小的纹理：

```text
1024x1024
-> 512x512
-> 256x256
-> ...
-> 1x1
```

远处物体一个像素可能覆盖原纹理很大区域。如果仍从最高分辨率随机取样，会产生
闪烁和缓存低效。Mipmap 选择与屏幕覆盖更接近的层级，减少走样并改善访问局部性。

完整 Mipmap 链额外占用约三分之一纹理存储，而不是免费赠品。

### 7.3 UV 与寻址

UV 通常使用归一化坐标描述纹理位置。超出 `[0, 1]` 后可：

- Repeat：重复平铺。
- Clamp：夹到边缘。
- Mirror：镜像重复。
- Border：返回边界颜色。

## 8. 光照的最低限度直觉

最简单漫反射常使用：

```text
brightness = max(0, dot(normal, lightDirection))
```

法线越朝向光线，点积越大，表面越亮。完整光照还会考虑：

- 观察方向。
- 表面粗糙度和金属度。
- 光源距离和衰减。
- 阴影遮挡。
- 环境光和间接光。
- 颜色空间与曝光。

Shader 的工作不是“把模型涂上纹理”这么简单，它在近似求解光如何与表面和
观察方向交互。

## 9. Overdraw

同一像素被多个片元反复处理称为 Overdraw：

```text
背景
-> 不透明墙
-> 半透明烟雾
-> 半透明火焰
-> UI 特效
-> 最终像素
```

即使最终只显示一个颜色，前面可能已经执行多次 Fragment Shader 和混合。
Overdraw 在高分辨率、复杂 Shader、大面积粒子和移动端尤其昂贵。

常见优化：

- 不透明物体大致从前向后绘制。
- 缩小粒子实际覆盖区域。
- 减少无意义的透明层。
- 简化高 Overdraw 材质。
- 谨慎使用深度预处理或 Alpha Test。
- 使用平台工具查看 Overdraw 热区。

## 10. 分支、执行组与 `discard`

GPU 通常以一组相邻线程共同执行 Shader。若同组线程走不同分支，硬件可能需要
分别执行两条路径并屏蔽不参与的线程，降低利用率。

这不代表 Shader 中绝对不能写 `if`：

- 分支条件在大区域内一致时可能很高效。
- 编译器可能将简单分支改写为选择。
- 跳过的工作足够昂贵时，动态分支仍可能划算。

`discard` 可用于栅栏、树叶等镂空材质，但可能影响 Early-Z、产生边缘走样并
增加 Overdraw。它不是用来给性能问题按删除键的。

## 11. 本章检查

1. Fragment 和最终 Pixel 为什么不是一一对应？
2. 重心坐标如何用于属性插值？
3. 为什么透视下 UV 需要透视正确插值？
4. Mipmap 如何减少远处纹理闪烁？
5. 透明粒子为什么容易造成严重 Overdraw？

[上一章：GPU 几何阶段](./03-gpu-geometry-stage.md) |
[下一章：输出合并与画面呈现](./05-output-merger-and-presentation.md)
