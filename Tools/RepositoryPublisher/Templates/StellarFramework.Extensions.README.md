# StellarFramework.Extensions

为 StellarFramework 增加 Algorithms、World、Flow 和 HybridCLR HotUpdate 能力的源码仓。本仓只发布 Unity <code>Assets</code> 内容，不是独立 Unity 工程或 UPM 包。

发布版本：{{RELEASE_VERSION}}

源码提交：[{{SOURCE_COMMIT}}](https://github.com/StarrDream/StellarFramework.Dev/commit/{{SOURCE_COMMIT}})

使用本仓前，先安装相同发布版本的 [StellarFramework General](https://github.com/StarrDream/StellarFramework)。本仓要求的 General Profile 和 UPM 包分别列在 <code>RELEASE-MANIFEST.json</code> 的 <code>requiredGeneralProfileIds</code> 与 <code>requiredUpm</code> 中。

## 推荐方式：从 General 导出所需 Kit

1. 克隆并打开匹配版本的 General 工程。
2. 将本仓 <code>Assets</code> 内容合并到 General 工程的 <code>Assets</code> 目录，保留全部 <code>.meta</code> 文件。
3. 将 <code>requiredUpm</code> 中的包规格合并到 <code>Packages/manifest.json</code>，使用清单中列出的版本。
4. 等待 Unity 完成依赖解析和编译。
5. 从菜单 **StellarFramework → Export** 选择需要的扩展 Profile 并导出。

该方式适合希望只把一个或几个扩展 Kit 放进游戏项目的团队。Profile 和依赖闭包由 General Catalog 管理。

## 直接添加到已有 Unity 项目

确认项目已包含 <code>requiredGeneralProfileIds</code> 指定的通用 Profile，然后合并本仓 <code>Assets</code> 内容并配置 <code>requiredUpm</code> 中的 UPM 包。保留 <code>.meta</code> 文件，等待 Unity 编译，再按对应 Kit 文档配置和验证项目。

## 扩展目录

| 领域 | 内容 |
| --- | --- |
| Algorithms | GridKit、SpatialKit、PathKit、SimulationKit 及相关 Adapter |
| World | WorldKit、WorldGenKit、PlacementKit、Streaming、World Framework Tools |
| Flow | FlowKit Core、Unity 集成、图编辑器与校验工具 |
| HotUpdate | HybridCLRKit、构建工具和 HotUpdate Publisher |

## HotUpdate

本仓代码热更新流程由 HybridCLR 与 **YooAsset** 协作完成；Addressables 资源 Profile 不属于这条热更新路径。目标项目需要为相同平台和版本构建 Player、HybridCLR 产物、AOT Metadata、YooAsset Manifest 与内容包，并按发布门禁验证冷启动、缓存重启和回滚。

Publisher 可使用本机目录或 S3 兼容发布目标。线上 endpoint、凭证、TLS 和权限由项目配置；上线前应在目标环境完成 Dry Run 和发布验证。操作步骤见 <code>Assets/StellarFramework/FrameworkDoc/02-Kits/HybridCLRKit</code>。

## 文档与版本

各 Kit 的用法见 <code>Assets/StellarFramework/FrameworkDoc/02-Kits</code>；World Framework 文档见 <code>Assets/StellarFramework/FrameworkDoc/06-WorldFramework</code>。<code>RELEASE-MANIFEST.json</code> 记录源码提交、Profile、General 前置条件和精确 UPM 包规格。

本仓由 [StellarFramework.Dev](https://github.com/StarrDream/StellarFramework.Dev) 生成。源码问题和功能改动请在 Dev 工程中处理。