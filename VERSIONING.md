# Versioning

StellarFramework Dev, General, and Extensions use one shared framework release version. The current release is **1.0.2**.

## Version rules

Versions use the `MAJOR.MINOR.PATCH` format:

- **Patch** (`1.0.0` → `1.0.1`): small fixes and compatible maintenance updates.
- **Minor** (`1.0.0` → `1.1.0`): larger additions or improvements that stay on the same major-version line.
- **Major** (`1.x` → `2.0.0`): an architecture upgrade or a release that starts a new major-version line. Create a separate `2.0.0` branch for that work; keep the existing major-version line available for maintenance.

When increasing a version component, reset the components to its right to zero. Apply each framework release version consistently to all three repositories.

## Release synchronization

The Dev repository is the source of truth for the shared release version. Update `releaseVersion` in `Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json`, then use the repository publisher to regenerate General and Extensions. Before committing, confirm the version in both consumer repositories' `README.md` and `RELEASE-MANIFEST.json` matches the catalog.

The Unity application bundle version and the Tank Arena verification hot-update package version should match the framework release when preparing a coordinated release. Rebuild generated hot-update assets and packages after changing their source version constants.

Unity/package dependency versions and serialization or manifest schema versions have their own compatibility rules; they are not changed solely to match the framework release number.

## 版本规则

StellarFramework Dev、General 和 Extensions 三个仓库共用一个框架发布版本，当前版本为 **1.0.2**。

版本采用 `主版本.次版本.修订号` 格式：

- **修订号**（`1.0.0` → `1.0.1`）：小型修复和兼容性维护更新。
- **次版本号**（`1.0.0` → `1.1.0`）：同一主版本线上的较大功能增加或改进。
- **主版本号**（`1.x` → `2.0.0`）：架构升级或开启新的主版本线。为此创建独立的 `2.0.0` 分支，旧主版本线继续用于维护。

提升某一级版本号时，其右侧的版本号归零。每次框架发布都要在三个仓库中使用同一个版本号。

## 发布版本同步

Dev 仓库是共享发布版本的来源。更新 `Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json` 中的 `releaseVersion`，然后使用仓库发布器重新生成 General 和 Extensions。提交前，确认两个使用者仓库的 `README.md` 与 `RELEASE-MANIFEST.json` 版本和 Catalog 一致。

准备一次完整版本发布时，Unity 应用 Bundle 版本和 Tank Arena 验证用热更包版本也应与框架发布版本一致。修改相应源码版本常量后，重新生成热更资源和资源包。

Unity/Kit 依赖版本以及序列化或 Manifest 格式版本各自遵循兼容规则；它们不会仅为匹配框架发布版本而改动。
