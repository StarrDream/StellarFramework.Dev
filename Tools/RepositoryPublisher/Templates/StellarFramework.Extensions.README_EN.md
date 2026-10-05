# StellarFramework.Extensions

Extension Kits for use with StellarFramework General. This repository provides Algorithms, World, and Flow capabilities. It is not a standalone Unity project; start with the matching General release.

Release: **{{RELEASE_VERSION}}**

Dev source commit: [{{SOURCE_COMMIT}}](https://github.com/StarrDream/StellarFramework.Dev/commit/{{SOURCE_COMMIT}})

## Framework overview

Extensions adds algorithms, world organization and generation, placement rules, and workflow orchestration. It is distributed as Unity Assets source. Merge it into the General project at the same release version, then use Tools Hub to export the Kits your project needs.

1. Download the matching [StellarFramework General release](https://github.com/StarrDream/StellarFramework/releases) and open it with Unity 2022.3.62f3c1.
2. Merge this repository's `Assets` directory into General's `Assets` directory. Preserve all `.meta` files.
3. In **StellarFramework → Export**, select the extension Kits or Profile and review its dependencies.
4. Import the unitypackage into your Unity project and install the listed dependencies.

## Requirements

- Unity Editor 2022.3.62f3c1
- StellarFramework General at the same release version
- Unity Package Manager access when opening the General project
- Preserve Unity `.meta` files when merging source

Extensions is a source repository, not a UPM package. Profiles and prerequisites are listed in `RELEASE-MANIFEST.json` and the General Kit Catalog.

## Framework concepts

| Concept | Description |
| --- | --- |
| General | Supplies the `StellarFramework.cs` architecture foundation and shared Kits |
| Extensions Kit | Optional capabilities in Algorithms, World, and Flow; import only what the project needs |
| Catalog Profile | Declares export files, required General Profiles, and external UPM dependencies |
| Tools Hub | Configure and export in the General project after merging Extensions source; not part of the game Player |

Preserve `.meta` files to retain Unity asset GUIDs and scene or prefab references.

## Architecture

The MSV architecture comes from `StellarFramework.cs` in the matching General release. Extensions adds optional algorithms and systems; it does not replace the architecture container or require every extension Kit.

~~~mermaid
flowchart LR
    Startup["Game startup"] -->|"Init / lifecycle"| Architecture["Architecture<T><br/>from General"]
    Architecture --> Model["Model<br/>application state"]
    Architecture --> Service["Service<br/>application operations"]
    View["View<br/>StellarView / Unity UI"] -->|"calls"| Service
    Service -->|"reads / updates"| Model
    View -->|"read-only query"| Model
    Extension["Selected Extensions Kits<br/>Algorithms / World / Flow"] -->|"used as needed"| Service
~~~

See the [General MSV architecture guide](https://github.com/StarrDream/StellarFramework.Dev/blob/{{SOURCE_COMMIT}}/Assets/StellarFramework/FrameworkDoc/01-Architecture/Architecture/Architecture-MSV-架构说明文档-Guide.md). Export Profiles resolve Kit prerequisites.

## Kit guide

| Use case | Kit | What it does | Guide |
| --- | --- | --- | --- |
| Algorithms and maps | GridKit | Grid data, geometry, occupancy, and topology | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/GridKit/GridKit-网格系统-说明文档-Guide.md) |
| Algorithms and maps | GridKit Unity Projection | Project grid data into Unity scenes and physics queries | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/GridKitUnityProjection/GridKit-UnityProjectionAdapter-Guide.md) |
| Algorithms and maps | SpatialKit | Spatial indexing and nearby-object queries | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/SpatialKit/SpatialKit-空间索引-说明文档-Guide.md) |
| Algorithms and maps | PathKit | Path search over graphs and grids | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/PathKit/PathKit-路径搜索-说明文档-Guide.md) |
| Algorithms and simulation | SimulationKit | Batched scheduling for many logical objects | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/SimulationKit/SimulationKit-批量模拟调度-说明文档-Guide.md) |
| Worlds and levels | WorldKit | World state, data layers, and chunk lifecycle | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/WorldKit/WorldKit-世界组织系统-说明文档-Guide.md) |
| Worlds and levels | WorldGenKit | Deterministic generation from rules and stages | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/WorldGenKit/WorldGenKit-世界生成系统-说明文档-Guide.md) |
| Worlds and levels | PlacementKit | Validate footprints, slope, water, and connections | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/PlacementKit/PlacementKit-通用放置规则-Guide.md) |
| Worlds and levels | WorldKit Streaming | Chunk loading, unloading, and streaming storage integration | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/WorldKitStreaming/WorldKitStreaming-无限世界流送-Guide.md) |
| Workflows | FlowKit | Flow graph data, execution, Unity integration, and visual tools | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/FlowKit/FlowKit-工作流系统-说明文档-Guide.md) |

## Release links

- [StellarFramework General Releases](https://github.com/StarrDream/StellarFramework/releases): download the matching General version first.
- [StellarFramework.Dev](https://github.com/StarrDream/StellarFramework.Dev): extension source, Catalog, and release templates.
- [RELEASE-MANIFEST.json](RELEASE-MANIFEST.json): extension scope, required General Profiles, and external dependencies.
