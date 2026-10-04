# StellarFramework.Extensions

Algorithms、World 和 Flow 的 StellarFramework 扩展 Kit 源码仓。本仓提供可与 General 组合的 Unity `Assets` 内容；它不是独立 Unity 工程，也不是 UPM 包。使用前请准备与本仓发布版本相同的 [StellarFramework General](https://github.com/StarrDream/StellarFramework)。

发布版本：**{{RELEASE_VERSION}}**

源码提交：[{{SOURCE_COMMIT}}](https://github.com/StarrDream/StellarFramework.Dev/commit/{{SOURCE_COMMIT}})

## 框架与扩展概览

StellarFramework 按“基础运行时 → 功能 Kit → Adapter / Provider → Unity 或第三方实现”分层。General 提供通用基础服务和常用 Kit；Extensions 通过声明依赖复用 General 能力，并可按需单独导出。Tools Hub 在 Editor 中负责配置和导出，不进入游戏 Player。

Extensions 发布的是与对应版本 General 配套的 Unity `Assets` 源码，不是独立工程。第一次使用前，请准备与本仓版本一致的 General，并保留合并文件的 `.meta`。

| 扩展领域 | 主要 Kit 与用途 |
| --- | --- |
| Algorithms | GridKit（网格与拓扑）、SpatialKit（空间查询）、PathKit（寻路）、SimulationKit（模拟能力） |
| World | WorldKit（世界状态）、WorldGenKit（程序化生成）、PlacementKit（放置规则）、Streaming（分块加载与存档集成） |
| Flow | FlowKit（流程图数据与执行）、Unity 集成、可视化编辑器与校验工具 |

各 Kit 的 Unity 适配器、Editor 工具和依赖按 Catalog 中的 Profile 拆分，导出时可选择需要的组合。

## 快速开始：从 General 导出扩展 Kit

这是把少量扩展能力加入现有游戏项目的推荐路径：

1. 下载与本仓版本一致的 General，使用 Unity 2022.3.62f3c1 打开并等待依赖解析完成。
2. 将本仓的 `Assets` 内容合并到 General 工程的 `Assets` 目录，保留所有 `.meta` 文件。
3. 按 `RELEASE-MANIFEST.json` 中的 `requiredUpm`，将缺少的包及对应版本加入 General 工程的 `Packages/manifest.json`。等待 Unity 完成编译。
4. 从 **StellarFramework → Export** 选择要使用的扩展 Profile，检查依赖摘要并导出。
5. 在游戏工程中导入导出的 `.unitypackage`，按包内提示完成 UPM 依赖安装。

导出器会根据 General 的 Kit Catalog 补齐框架依赖。不要把合入扩展源码后的 General 工作目录直接当成新发布仓；下游仓应始终由 Dev 发布器生成。

## 直接合入已有 Unity 工程

如果项目需要直接使用源码，先在 `RELEASE-MANIFEST.json` 中核对该版本要求的 General Profile 和 UPM 规格。确认项目已具备这些 General 能力后，将本仓 `Assets` 内容合入项目，保留 `.meta` 文件并安装所需 UPM 包，再按对应 Kit 指南配置。此方式会带入本仓的扩展源码；只需要少量 Kit 时，优先使用上一节的导出流程。

## 扩展范围

| 领域 | Kit |
| --- | --- |
| Algorithms | GridKit、SpatialKit、SimulationKit、PathKit 及其适配器 |
| World | WorldKit、WorldGenKit、PlacementKit、Streaming 与 World Framework Tools |
| Flow | FlowKit Core、Unity 集成、可视化编辑器与校验工具 |

各 Kit 指南位于 `Assets/StellarFramework/FrameworkDoc/02-Kits`；World Framework 文档位于 `Assets/StellarFramework/FrameworkDoc/06-WorldFramework`。`RELEASE-MANIFEST.json` 记录该发布的 Dev 源码提交、扩展域、General 前置 Profile 和 UPM 规格。

扩展源码及发布规则由 [StellarFramework.Dev](https://github.com/StarrDream/StellarFramework.Dev) 维护。使用者仓中的源码问题和功能修改请回到 Dev 工程处理。
