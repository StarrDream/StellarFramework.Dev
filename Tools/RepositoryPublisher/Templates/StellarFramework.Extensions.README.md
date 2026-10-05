# StellarFramework.Extensions

与 StellarFramework General 配套使用的扩展 Kit 源码仓，包含 Algorithms、World 和 Flow 能力。该仓不是独立 Unity 工程；使用时先准备与本仓版本一致的 General。

发布版本：**{{RELEASE_VERSION}}**

Dev 源码提交：[{{SOURCE_COMMIT}}](https://github.com/StarrDream/StellarFramework.Dev/commit/{{SOURCE_COMMIT}})

## 框架主体介绍

Extensions 为通用框架补充算法、世界组织与生成、放置规则和流程编排能力。它发布 Unity Assets 源码。将扩展源码合入同版本的 General 工程后，可在 Tools Hub 里选择需要的 Kit 导出。

1. 下载同版本的 [StellarFramework General](https://github.com/StarrDream/StellarFramework/releases)，用 Unity 2022.3.62f3c1 打开。
2. 将本仓 `Assets` 合入 General 工程的 `Assets` 目录，保留所有 `.meta` 文件。
3. 在 **StellarFramework → Export** 选择扩展 Kit 或 Profile，检查依赖后导出。
4. 在自己的 Unity 工程中导入 unitypackage，并按包内清单安装依赖。

## 环境要求

- Unity Editor 2022.3.62f3c1
- 与 Extensions 版本号相同的 StellarFramework General
- 首次打开 General 工程时可访问 Unity Package Manager
- 合并源码时保留 Unity `.meta` 文件

Extensions 是源码仓，不是 UPM 包。导出 Profile 和前置依赖见 `RELEASE-MANIFEST.json` 与 General 的 Kit Catalog。

## 框架概念

| 概念 | 说明 |
| --- | --- |
| General | 提供 `StellarFramework.cs` 架构基础和扩展所需的通用 Kit |
| Extensions Kit | Algorithms、World、Flow 领域的可选能力，不要求全部接入 |
| Catalog Profile | 声明导出文件、General 前置 Profile 和外部 UPM 依赖 |
| Tools Hub | 在合并了 Extensions 的 General 工程里配置和导出，不进入游戏 Player |

合并时保留 `.meta`，以维持 Unity 资源 GUID 和场景、Prefab 引用。

## 架构介绍

MSV 架构由同版本 General 的 `StellarFramework.cs` 提供。Extensions 增加可选算法和系统能力，不替换架构容器，也不要求所有扩展 Kit 一起接入。

~~~mermaid
flowchart LR
    Startup["游戏启动"] -->|"Init / 生命周期"| Architecture["Architecture<T><br/>来自 General"]
    Architecture --> Model["Model<br/>应用状态"]
    Architecture --> Service["Service<br/>应用操作"]
    View["View<br/>StellarView / Unity UI"] -->|"调用"| Service
    Service -->|"读取 / 更新"| Model
    View -->|"只读查询"| Model
    Extension["所选扩展 Kit<br/>Algorithms / World / Flow"] -->|"按需使用"| Service
~~~

[General 的 MSV 架构说明](https://github.com/StarrDream/StellarFramework.Dev/blob/{{SOURCE_COMMIT}}/Assets/StellarFramework/FrameworkDoc/01-Architecture/Architecture/Architecture-MSV-架构说明文档-Guide.md)；扩展依赖由导出 Profile 自动解析。

## Kit 介绍

| Kit | 简介 | 文档 |
| --- | --- | --- |
| GridKit | 网格数据、几何、占用与拓扑 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/GridKit/GridKit-网格系统-说明文档-Guide.md) |
| GridKit Unity Projection | 将网格映射到 Unity 场景与物理查询 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/GridKitUnityProjection/GridKit-UnityProjectionAdapter-Guide.md) |
| SpatialKit | 空间索引与邻近对象查询 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/SpatialKit/SpatialKit-空间索引-说明文档-Guide.md) |
| PathKit | 图和网格上的路径搜索 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/PathKit/PathKit-路径搜索-说明文档-Guide.md) |
| SimulationKit | 大量逻辑对象的分批模拟调度 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/SimulationKit/SimulationKit-批量模拟调度-说明文档-Guide.md) |
| WorldKit | 世界状态、数据层与区块生命周期 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/WorldKit/WorldKit-世界组织系统-说明文档-Guide.md) |
| WorldGenKit | 基于规则与阶段的确定性世界生成 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/WorldGenKit/WorldGenKit-世界生成系统-说明文档-Guide.md) |
| PlacementKit | 占地、坡度、水域和连接条件校验 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/PlacementKit/PlacementKit-通用放置规则-Guide.md) |
| WorldKit Streaming | 区块加载、卸载和流送存储接入 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/WorldKitStreaming/WorldKitStreaming-无限世界流送-Guide.md) |
| FlowKit | 流程图数据、执行、Unity 集成与可视化工具 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/FlowKit/FlowKit-工作流系统-说明文档-Guide.md) |

## 发布链接

- [StellarFramework 主仓](https://github.com/StarrDream/StellarFramework)：提供扩展 Kit 所需的通用框架。
- [StellarFramework.Dev 开发仓](https://github.com/StarrDream/StellarFramework.Dev)：扩展源码与发布工程。
