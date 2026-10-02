# Demo 索引

## 唯一完整玩法 Demo

| 入口 | 内容 |
| --- | --- |
| `Assets/StellarFramework/Samples/TankArena/Scene/FrameworkDemo.unity` | 可玩坦克生存流程；实际接入 Architecture / BindableKit、FSMKit、ActionKit、EventKit、PoolKit、ConfigKit、SaveKit、SettingsKit、LocalizationKit、UIAdaptationKit 与 LogKit。热更交付另由 Release Gate 覆盖 YooAsset、ResKit 和 HybridCLR。资源修改见 [Tank Arena 案例说明](../../Samples/TankArena/CaseStudy.md)。 |

在 **Unity Editor** 中直接运行该场景，会由仅编辑器代码调用本地 `HotUpdateMain.Main()`。要验证真实热更新包下载与加载，应运行 Android / Windows HotUpdate Release Gate；两条路径不能混为一谈。

## 如何学习其他 Kit

Demo 通过一局游戏展示多 Kit 的协作方式，不承担每个 Kit 的完整 API 教学。使用某个 Kit 前，先读它的 `FrameworkDoc/02-Kits` 指南，确认最小依赖、可选 Adapter、初始化顺序和故障处理；不要用 HUD 面板中的 Kit 名称推断 Sample 已覆盖全部功能。

- API、依赖边界和接入示例：`Assets/StellarFramework/FrameworkDoc/02-Kits`
- 源码组织：各 Kit 对应源码指南
- 行为与工程策略验证：`Assets/StellarFramework/Tests`
- Player、资源包和热更新发布：`Assets/StellarFrameworkVerification`

## 选择路线

1. 从 [快速开始](../00-Overview/快速开始.md) 打开 Tank Arena。
2. 根据项目需要选择对应 Kit Guide，先接入 Core，再按需添加 Unity Adapter、Editor Tools 或资源后端。
3. 需要查看 Demo 实现时阅读 [Tank Arena 源码结构](Samples-源码文档-Guide.md)。
4. 需要验证生产交付时运行对应平台的 Release Gate。

## Kit 使用案例入口

`FrameworkDoc/02-Kits` 中的指南覆盖各 Runtime Profile 的 API、边界和接入步骤，发布 Catalog 的文档闭包由 Kit Catalog policy 检查。以下指南还包含跨 Kit 或具体场景案例：

- [GridKit Unity Projection：Terrain 到基础通行网格](../02-Kits/GridKitUnityProjection/GridKit-UnityProjectionAdapter-Guide.md)
- [LocalizationKit：缺词回退与当前语言查询](../02-Kits/LocalizationKit/LocalizationKit-Guide.md)
- [PlacementKit：塔位验证与多失败原因](../02-Kits/PlacementKit/PlacementKit-通用放置规则-Guide.md)
- [WorldKit Streaming：分级需求、Floating Origin 与 Delta 持久化](../02-Kits/WorldKitStreaming/WorldKitStreaming-无限世界流送-Guide.md)
