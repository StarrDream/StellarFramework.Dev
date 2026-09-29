# StellarFramework.Dev

StellarFramework 的完整研发母仓与唯一 Source of Truth。

> 面向使用者的发布仓是 `StarrDream/StellarFramework` 与 `StarrDream/StellarFramework.Extensions`。不要在两个用户仓平行开发功能；修复和新能力必须先进入本仓，通过验证后再发布。

## 仓库职责

本仓保留完整 Runtime / Editor / ToolsHub / Export / Samples / Tests / FrameworkVerification / Android Verification / HotUpdate Publisher / Development Plans。

```text
StellarFramework.Dev
        |\
        | \----> StellarFramework.Extensions   Algorithms / World / Flow / HotUpdate
        |
        +------> StellarFramework              General user release
```

发布边界由以下两个机器可读 Catalog 共同决定：

- `Assets/StellarFramework/KitCatalog/KitDistributionCatalog.json`：Kit、依赖、导出事实源。
- `Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json`：General / Extensions 域归属。

双仓输出由 `Tools/RepositoryPublisher/publish_repositories.py` 生成，支持 Dry Run、依赖边界检查和 Release Manifest。

## 开发原则

- MSV：Model 管状态，Service 管业务规则与状态变化，View 管表现和输入转换。
- Adapter Isolation：Unity / 第三方 SDK / 存储 / 网络 / 平台实现不污染 Core。
- Foundation 不能依赖 Extension；General Runtime 不能依赖 Extensions Runtime。
- 失败必须显式，不允许吞异常或通过 catch-all fallback 伪装成功。
- 热路径优先低 GC，避免不必要 LINQ、反射扫描和隐式分配。
- Dev 中保持完整工程，不使用 submodule、重复源码或跨仓相对路径依赖来模拟发布边界。

详细规则见 `Assets/StellarFramework/FrameworkDoc/01-Architecture/KitArchitectureGuide.md`。

## 主要能力

General：`TimeKit`、`LocalizationKit`、UIKit、UIAdaptationKit、ResKit、SaveKit、ConfigKit、SettingsKit、AudioKit、EventKit、PoolKit、SingletonKit、FSMKit、ActionKit、HttpKit 等。

Extensions：`GridKit`、`SpatialKit`、`SimulationKit`、`PathKit`、WorldKit、WorldGenKit、PlacementKit、FlowKit、HybridCLRKit 等。

## 验证

研发变更至少按影响范围执行：

```text
Compile
-> Focused EditMode
-> FrameworkValidation
-> PlayMode
-> Architecture / Catalog / Packaging Policy
-> Clean Consumer / Player / Release Gate（按能力）
```

自动 Test Runner 优先使用项目侧 `stellar_test_run_safe`，避免 Refresh / Compile 与 EditMode Test Runner 的历史竞态。

验证架构：`Assets/StellarFrameworkVerification/ValidationArchitecture.md`。
当前验证摘要：`Assets/StellarFramework/FrameworkDoc/08-Validation/ValidationCurrentStatus.md`。

## 三仓发布

先执行：

```text
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
```

Dry Run 会保证：

- 每个 Distribution Profile 恰好属于一个发布目标。
- General 不依赖任何 Extension Profile。
- Extensions 显式记录所需 General Profile。
- 两个用户仓不会静默共享 Runtime 源码。

随后按 `Tools/RepositoryPublisher/README.md` 生成用户仓，并分别执行 Clean Consumer / Release Gate。

## 贡献与问题修复

用户仓发现问题时，在本仓修复、验证、再重新发布。用户仓中的 Release Manifest 必须能追溯到本仓 Source SHA。
