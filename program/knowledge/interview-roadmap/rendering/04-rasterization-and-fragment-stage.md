# 光栅化与片元阶段速记

## 从图元到片元

光栅化判断三角形覆盖哪些采样点，并用重心坐标插值 varying。片元是一次候选样本/着色工作，不等于最终像素；它还可能被深度、模板、discard 或覆盖规则淘汰。

透视投影下普通屏幕线性插值会失真，GPU 对属性做透视校正插值；`flat/noperspective` 等限定改变规则。

## Early-Z

若能在昂贵片元着色前确定深度失败，可节省工作。写深度、`discard/clip`、某些副作用或管线状态可能限制提前测试。Depth Prepass 用额外几何/带宽换取减少重片元着色，是否划算依场景和 GPU。

## Fragment Shader 与纹理

片元阶段计算材质、光照和输出，也可能写多 Render Target。主要成本：屏幕覆盖、Overdraw、纹理采样/带宽、复杂 BRDF、分支发散和高精度运算。

- 双线性在单 mip 邻域过滤；三线性再混合两个 mip；各向异性改善斜视表面。
- Mipmap 降低远处采样走样和带宽，代价是约 1/3 额外存储及生成/流送管理。
- UV 寻址有 repeat/clamp/mirror；颜色纹理与数据纹理需正确颜色空间。
- 移动端 tile-based GPU 尤其关注 Overdraw、Render Target 带宽与 load/store。

## 光照最低主线

```text
材质参数 + 几何项(N,V,L,H)
-> BRDF/光源/阴影
-> 直接光 + 间接/环境光
-> HDR 颜色
```

面试不应把“PBR”只答成几张贴图；至少说明能量守恒、漫反射/镜面、粗糙度和金属度如何影响 BRDF。

## 高频追问

1. 片元为何不等于像素？
2. varying 为什么要透视校正？
3. Early-Z 何时失效，Prepass 何时反而更慢？
4. Mipmap 为什么既改善质量又可能提速？
5. 透明 UI/粒子为什么容易产生 Overdraw？
6. GPU 分支何时产生明显成本？

[上一章：几何阶段](./03-gpu-geometry-stage.md) | [下一章：输出与呈现](./05-output-merger-and-presentation.md)
