# StellarFramework.Dev

StellarFramework 的开发、验证和发布源仓。框架 Runtime、Editor 工具、Tools Hub、Kit Catalog、样例、测试及发布脚本都在这里维护。General 和 Extensions 使用者仓由本仓生成；不要在下游仓直接维护框架源码。

当前发布版本：**1.0.1**。版本规则见 [VERSIONING.md](VERSIONING.md)。

## 开发环境

- Unity Editor：**2022.3.62f3c1**
- Python 3：运行仓库发布器和静态检查
- Android 验证：Android SDK、ADB，以及可用的 Android 设备或模拟器

在 Unity Hub 中添加本仓目录并打开工程。等待 Package Manager 完成解析和 Unity 导入后，从菜单 **StellarFramework → Tools Hub** 查看工程入口。

## 目录结构

| 路径 | 用途 |
| --- | --- |
| `Assets/StellarFramework/Runtime/Kits` | Runtime Kit、Adapter 与公共接口 |
| `Assets/StellarFramework/Editor` | Tools Hub、Kit 工具及发布器 |
| `Assets/StellarFramework/KitCatalog` | Kit 依赖、导出和仓库归属配置 |
| `Assets/StellarFramework/FrameworkDoc` | 面向使用者的 Kit 指南，以及面向维护者的架构和验证文档 |
| `Assets/StellarFramework/Samples` | Tank Arena 框架示例 |
| `Assets/StellarFramework/Tests` | EditMode、PlayMode 和架构策略测试 |
| `Assets/StellarFrameworkVerification` | Player、热更新及发布验证门禁 |
| `Tools/RepositoryPublisher` | 生成 General 与 Extensions 使用者仓 |
| `Tools/AndroidVerification` | Android 构建与设备自动化 |

## 修改框架

1. 先确认 Kit 的公开 API、依赖方向和 Runtime/Editor 边界。
2. 实现功能并更新 `KitDistributionCatalog.json` 中的依赖、UPM 要求、导出路径和成熟度。
3. 更新随包交付的 Kit 指南、Tools Hub 帮助和示例说明。
4. 为依赖边界、导出闭包和运行行为补充测试。
5. 按改动范围运行 Unity 编译、EditMode/PlayMode 测试和发布验证。验证入口见[验证架构](Assets/StellarFrameworkVerification/ValidationArchitecture.md)及[当前验证状态](Assets/StellarFramework/FrameworkDoc/08-Validation/ValidationCurrentStatus.md)。

ResKit、资源后端和热更新 Provider 的组合边界见[资源与代码更新扩展设计](Assets/StellarFramework/FrameworkDoc/01-Architecture/ResourceAndCodeUpdatePlugins.md)。Kit 的依赖分层和导出规则见[Kit 架构指南](Assets/StellarFramework/FrameworkDoc/01-Architecture/KitArchitectureGuide.md)。

General 发布通用能力，例如 `TimeKit` 和 `LocalizationKit`；Algorithms、World 与 Flow 能力（包括 `GridKit`、`SpatialKit`、`SimulationKit`、`PathKit`）位于 Extensions。具体归属由 Kit Catalog 管理。

## 发布 General 与 Extensions

Dev 是三个仓库的唯一源码来源。先把准备发布的源代码、Catalog 和 README 提交到 Dev，再运行发布器；它会将当前 Dev 提交号写入下游的 `RELEASE-MANIFEST.json`。

先检查发布计划：

```powershell
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
```

确认 General 和 Extensions 目标路径正确，且目标仓没有需要保留的本地文件后，生成使用者仓：

```powershell
python Tools/RepositoryPublisher/publish_repositories.py `
  --general-target C:\GitProject\StellarFramework `
  --extensions-target C:\GitProject\StellarFramework.Extensions `
  --validation PASS
```

发布器会重建两个目标目录中的发布内容，并保留各自的 `.git`。运行前请备份或提交目标仓里的本地改动。只有对应验证实际通过后，才将 `--validation` 设为 `PASS`。生成后检查文件差异、Kit Catalog、UPM 清单和两个 `RELEASE-MANIFEST.json`，然后分别提交 General 与 Extensions。

## 发布仓定位

- [StellarFramework](https://github.com/StarrDream/StellarFramework)：面向 Unity 项目使用者的通用框架仓。
- [StellarFramework.Extensions](https://github.com/StarrDream/StellarFramework.Extensions)：面向已使用 General 的项目，提供 Algorithms、World 和 Flow 扩展 Kit。

使用者文档位于 `Assets/StellarFramework/FrameworkDoc`；版本号必须按 [VERSIONING.md](VERSIONING.md) 在三个仓库中保持一致。
