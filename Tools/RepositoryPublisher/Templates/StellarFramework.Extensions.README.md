# StellarFramework.Extensions

StellarFramework 的高级扩展发布仓。

> 本仓不是独立 Unity 工程，也不是第二个研发母仓。它必须与 `StarrDream/StellarFramework` 的 General Contract 组合使用；正式修改只进入 `StarrDream/StellarFramework.Dev`。

当前发布版本：`{{RELEASE_VERSION}}`  
来源：`StellarFramework.Dev@{{SOURCE_COMMIT}}`

## 扩展域

### Algorithms

GridKit、SpatialKit、PathKit、SimulationKit，以及显式 Adapter。

### World

WorldKit、WorldKit.Streaming、WorldGenKit、PlacementKit、World Framework Tools 与相关表现/存档 Adapter。

### Flow

FlowKit Core、Unity Integration、Graph Editor / Validator。

### HotUpdate

HybridCLRKit、HybridCLR Tools 与 HotUpdate Publisher。

## 使用方式

Extensions 保留 Dev 中原始 `Assets/StellarFramework/...` 路径和 `.meta` GUID，因此不会为了仓库分类制造 namespace、asmdef 或序列化 Breaking Change。

推荐流程：

1. 先使用 `StarrDream/StellarFramework`。
2. 从本仓选取需要的扩展域并覆盖/合并到项目的 `Assets` 目录，必须保留 `.meta`。
3. 组合后可继续使用 General 仓中的 `StellarFramework -> Export` 生成单 Kit 或组合 unitypackage。

不要单独把本仓作为 Unity 工程打开；它有意不携带 `ProjectSettings` / `Packages`，从而避免在缺少 General Contract 时产生误导性的编译红错。

## 依赖方向

```text
General Runtime   -X-> Extension Runtime
Extension Runtime ---> General Public Contract
```

扩展域之间也不默认建立硬耦合。需要组合时使用显式 Adapter。

## 开发与反馈

本仓只接收由 Dev Publisher 生成的发布提交。发现问题时应在 `StarrDream/StellarFramework.Dev` 修复并重新发布，避免三仓漂移。

