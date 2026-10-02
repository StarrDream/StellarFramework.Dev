# Samples / Demo 使用说明

## Demo 是什么

仓库只维护一个完整、可运行的框架 Demo：**Tank Arena**。它用一局小型坦克生存游戏，把框架的架构、状态更新、事件、配置、存档、设置、本地化和屏幕适配放进可交互流程。

Demo 适合初次认识 StellarFramework，也适合观察多个 Kit 如何在真实业务中协作。它不是每个 Kit 的 API 清单，也不替代 Kit 指南、自动化回归或平台发布门禁。

案例玩法流程、Kit 连接关系和 Prefab 修改方法见 [Tank Arena 案例说明](../../Samples/TankArena/CaseStudy.md)。

## 打开与游玩

1. 在 Unity 中打开 `Assets/StellarFramework/Samples/TankArena/Scene/FrameworkDemo.unity`。
2. 等待资产导入与脚本编译完成后进入 Play Mode。
3. 用左侧控制区移动车体，用右侧控制区独立瞄准炮塔。选择自动或手动开火，击退逐渐增强的敌军并收集修理核心。
4. 暂停时可以恢复、查看 **SYSTEMS** 面板或重新开始。装甲归零后查看得分与击毁数，再次出击；最高分和累计战绩会保存在本机。

Unity Editor 预览调用当前工程编译的本地 `HotUpdate` 程序集。它没有远端下载过程。需要验证热更时，运行 Android 或 Windows HotUpdate Release Gate。

## 展示范围

实际玩法使用 Architecture / BindableKit、FSMKit、ActionKit、EventKit、PoolKit、ConfigKit、SaveKit、SettingsKit、LocalizationKit、UIAdaptationKit 和 LogKit。Release Gate 另行覆盖 ResKit、YooAsset 与 HybridCLR 的热更交付链路。

没有由这局玩法直接调用的 Kit，不应仅因为出现在文字列表中就算作 Demo 覆盖。逐 Kit 的 API、适用范围、依赖和排错步骤由 `FrameworkDoc/02-Kits` 对应指南维护。

## 验证职责

- Demo：帮助开发者理解与试用完整业务流程。
- `Assets/StellarFramework/Tests`：验证 Kit 行为、边界、文档策略与性能约束。
- `Assets/StellarFrameworkVerification`：验证 Player、资源包和代码热更等发布链路。

三者互相补充，不以 Demo 游玩结果替代自动化测试或平台门禁。

## 相关文档

- [Demo 索引](Samples_Index.md)
- [Samples 源码说明](Samples-源码文档-Guide.md)
- [快速开始](../00-Overview/快速开始.md)
