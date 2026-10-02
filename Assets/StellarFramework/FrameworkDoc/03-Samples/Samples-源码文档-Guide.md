# Tank Arena 源码结构

## 入口与程序集

Tank Arena 是仓库唯一完整可运行的 Framework Demo。场景入口位于：

```text
Assets/StellarFramework/Samples/TankArena/Scene/FrameworkDemo.unity
```

玩法源码与 `HotUpdate.asmdef` 一同放在 `Assets/StellarFramework/Samples/TankArena/Runtime`。程序集名保持为 `HotUpdate`，让 HybridCLR 导出、YooAsset 收集器和现有发布门禁继续使用同一程序集标识。`HotUpdateMain.Main()` 是发布包入口；`Editor/FrameworkDemoPlayBootstrap.cs` 仅在编辑器直接运行该场景时从本地程序集调用入口。

## 主要文件

| 文件 | 职责 |
| --- | --- |
| `Runtime/HotUpdateMain.cs` | HotUpdate 程序集入口，创建并启动 Tank Arena。 |
| `Runtime/TankArenaArchitecture.cs` | 注册架构、Model、Service 与本地化服务；承载战局数据和 FSM 状态。 |
| `Runtime/TankArenaGame.cs` | 加载 Sample Prefab，绑定 HUD 与操控输入，接入配置、存档、设置、事件和对象池。 |
| `Runtime/TankArenaLocalizationService.cs` | 提供简体中文 / 英文目录及语言切换。 |
| `Editor/FrameworkDemoPlayBootstrap.cs` | Unity Editor 本地预览适配器；不参与 Player 热更加载。 |
| `Resources/TankArena/Prefabs/` | 游戏主机、战场、HUD、敌方坦克、维修核心、弹体和特效 Prefab。 |
| `Resources/TankArena/Generated/` | HUD 图形与材质，以及 UIAdaptationProfile。 |
| `Assets/StreamingAssets/TankArena/demo-config.json` | 示例玩法的默认平衡配置。 |

画面对象都从 Sample/Resources 中的 Prefab 加载；`TankArenaGame` 不在运行时创建场地、模型、材质、Sprite 或 HUD 节点。UIAdaptationKit 读取 Sample 内的 Profile 并配置安全区和方向适配；此 Sample 不把 HUD 误称为 UIKit 面板。要学习 UIKit，应阅读对应 Kit Guide 与 UIKit 自带工具说明。

## 代码数据流

~~~text
TankArenaGame (View / input)
  ├─ TankArenaService ──> TankArenaModel
  │                       ├─ BindableProperty -> HUD refresh
  │                       └─ FSM -> Active / Paused / GameOver
  ├─ GlobalTypeEvent -> live Systems event feed
  ├─ ConfigKit -> StreamingAssets defaults / local override
  ├─ SaveKit -> persistent match profile
  ├─ SettingsKit -> saved screen-shake preference
  └─ UIAdaptationKit -> safe-area and orientation-aware layout
~~~

Projectile 与 ArenaEffect 的纯 C# 数据记录由 PoolKit 复用。其画面对象从 `Projectile.prefab` 和 `ShockRing.prefab` 实例化并按战斗生命周期销毁；本案例不宣称 PoolKit 自动池化 Unity GameObject。

## 热更路径

Android / Windows Release Gate 使用 YooAsset 构建和加载包，通过 ResKit 读取 Manifest、热更 DLL 与 AOT metadata，再由 HybridCLR 调用 `HotUpdateMain.Main()`。Demo 的 Editor 本地预览不走这条下载路径，不计作热更新证据。具体产物目录和命令以 HybridCLRKit 指南及 `Assets/StellarFrameworkVerification` 为准。

## 分发与维护

- Samples 是仓库内教学资产，不是 `KitDistributionCatalog` Profile，也不进入正式 Runtime 包。
- 发布 Catalog 将 `Samples/TankArena` 与 `StreamingAssets/TankArena` 一并列为框架仓库示例内容。
- Kit 的行为回归位于 `Assets/StellarFramework/Tests`，Player 与热更发布验证位于 `Assets/StellarFrameworkVerification`。
- 若调整玩法程序集路径，需同步 HybridCLRKit 文档、源策略、包发布策略和 Android / Windows 门禁；更改程序集名或热更输出布局前先更新全部消费者。
