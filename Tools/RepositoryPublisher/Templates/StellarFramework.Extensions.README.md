# StellarFramework.Extensions

给已在 Unity 项目中使用 StellarFramework 的开发者添加 Algorithms、World、Flow 和 HotUpdate 扩展。本仓是 **Assets 源码扩展发布**，不是可单独打开的 Unity 工程。

发布版本：{{RELEASE_VERSION}}
源码：StellarFramework.Dev@{{SOURCE_COMMIT}}

## 先看这一条

先安装与本版本匹配的 [StellarFramework General](https://github.com/StarrDream/StellarFramework)。Extensions 依赖 General 的公开 Kit Contract；只下载本仓、单独把它作为 Unity 工程打开，或漏装 UPM 包都会导致项目不完整。

## 两种安装方式

### 方式 A：给现有项目直接添加扩展源码

1. 先确认项目已包含 General 中对应的通用 Kit。本次发布要求的通用 Profile 列在本仓 `RELEASE-MANIFEST.json` 的 `requiredGeneralProfileIds`。
2. 将本仓 `Assets` 目录中的内容合并进游戏项目的 `Assets`。不要删除或覆盖项目原有文件；同名目录要合并，并保留所有 `.meta` 文件。
3. 打开本仓的 `RELEASE-MANIFEST.json`，把 `requiredUpm` 中所列包及版本合并进项目的 `Packages/manifest.json`。若包名已经存在，更新对应键值，不要重复添加 JSON 键；不要手抄或猜版本。
4. 回到 Unity，等待 Package Manager 解析依赖并完成编译。
5. 阅读所选扩展的使用文档，再运行你的项目验证。

此方式适合已经决定使用一个或多个 Extensions Kit、并且熟悉 Unity 资源合并的项目。

### 方式 B：先挑一个 Kit，再导出给游戏项目

1. 克隆 [StellarFramework General](https://github.com/StarrDream/StellarFramework) Unity 工程，或从 GitHub 下载 ZIP 并解压，然后用 Unity 打开工程根目录。
2. 把本仓 `Assets` 中的文件合并到 General 工程的 `Assets` 目录，保留 `.meta`。
3. 按上方第 3 步将本仓 `requiredUpm` 中的依赖加入 General 工程的 `Packages/manifest.json`，等待 Unity 编译完成。
4. 在 General 工程中打开 **StellarFramework → Tools Hub → Export**。Catalog 已包含扩展 Kit 的 Profile；合并源码后即可选单一 Profile 或其依赖闭包导出。
5. 将导出的 `.unitypackage` 导入游戏工程。需要热更新时，再依照 HybridCLR / YooAsset 文档配置目标平台工具和内容服务器。

只需要一个扩展 Kit 时，选单一 Profile 即可；导出器负责声明框架内的依赖。是否需要 UPM 包以 Profile 和使用指南为准。

## 按用途选择

| 扩展方向 | 包含能力 | 第一次阅读 |
| --- | --- | --- |
| **Algorithms** | GridKit、SpatialKit、PathKit、SimulationKit，以及明确拆分的 Adapter | `Assets/StellarFramework/FrameworkDoc/02-Kits` 下各 Kit 指南 |
| **World** | WorldKit、WorldGenKit、PlacementKit、Streaming 和 World Framework Tools | `Assets/StellarFramework/FrameworkDoc/06-WorldFramework` 与 WorldKit / WorldGenKit 指南 |
| **Flow** | FlowKit Core、Unity Integration、Tools Hub 图编辑器与校验器 | `Assets/StellarFramework/FrameworkDoc/02-Kits/FlowKit` 下的使用文档与业务编程规范 |
| **HotUpdate** | HybridCLRKit、HybridCLR Tools 与 HotUpdate Publisher | `Assets/StellarFramework/FrameworkDoc/02-Kits/HybridCLRKit` |

各扩展 Kit 可独立选择；不同扩展域之间没有默认硬依赖。选用 Adapter 时，仍需满足它在文档中列出的 General Kit 和 UPM 前置条件。

## 热更新 Kit 特别说明

HybridCLR 代码热更新要同时满足 Runtime、构建产物和内容版本要求，不能只导入一个 DLL 就上线：

- HotUpdate 代码、AOT Metadata、Manifest 和 YooAsset 内容需要使用一致的目标平台与版本。
- Android 等 IL2CPP 目标必须生成与当前 Player 匹配的 HybridCLR 产物。
- 首次安装包、冷下载、进程退出后重启缓存命中都应验证。
- 发布前运行 Dev 提供的目标平台 Release Gate。Android 虚拟设备步骤见 [Dev Android 验证指南](https://github.com/StarrDream/StellarFramework.Dev/blob/main/Tools/AndroidVerification/README.md)，热更新用法见 `Assets/StellarFramework/FrameworkDoc/02-Kits/HybridCLRKit`。

## 文档、来源和反馈

本仓保留发布所需的扩展 Runtime、Editor 工具和扩展使用文档。Release Manifest 给出准确源码提交、发布范围、General Profile 前置条件、UPM 包规格和验证状态。

功能改动与 Bug 修复请在 [StellarFramework.Dev](https://github.com/StarrDream/StellarFramework.Dev) 完成，再由 Dev Publisher 同时生成 General 与 Extensions 发布仓。
