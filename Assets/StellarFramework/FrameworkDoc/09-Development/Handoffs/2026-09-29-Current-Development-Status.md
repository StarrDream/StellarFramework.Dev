# StellarFramework 当前开发状态

日期：2026-09-30

## 仓库职责

- `StellarFramework.Dev` 是唯一开发源，只面向开发者。
- `StellarFramework` 与 `StellarFramework.Extensions` 是面向使用者的发布仓库，只由 RepositoryPublisher 从 Dev 生成。
- 发布仓库不作为开发入口。

## 已完成的本轮闭环

- ToolsHub、Kit、样例、Catalog/Manifest 和三仓 README 已完成本轮整理；Dev README 面向开发者，两个发布仓 README 面向初学者和使用者。
- HotUpdate Publisher 支持按 `HotUpdateSettings.AotMetadataKeys` 选择 AOT metadata；YooAsset 推荐业务 Package 使用 Addressable，并在发布前检查该配置。
- 修复 Release History 时间排序：时间以 ISO-8601 UTC 字符串持久化；旧记录从不可变 `Activated` 事件恢复创建顺序。
- Android BaseRelease 与 IL2CPP x86_64 Full Gate 已在 MuMu 完成。
- YooAsset Android 热更验证包 `StellarHotUpdateVerification/verification-v1` 在 MuMu 冷启动和重启均通过：冷启动下载 6 个文件 / 1,845,419 字节；四项 AOT metadata、程序集 SHA256 和入口调用均通过；重启下载 0 字节。它与 Publisher 消费者包分开记录。
- Publisher ToolsHub Build、Dry Run 与 Build & Publish 已连接到标准 Workflow Assembly。`.011` 的 Development E2E 使用 LocalFolder 本机目录与 loopback HTTP/CDN，11 个发布文件的不可变上传、远端读回和 Android Full Gate 均通过。
- `.011 → .010 → .011` 回滚均通过，最终本机测试目录中的 PackageVersion 指针为 `.011`。上述 E2E 不代表外部 Production S3/CDN 已验证；Production endpoint、凭证、TLS 域名和发布权限仍需由部署项目配置并执行 Dry Run。
- 测试证据：Publisher EditMode 129/129；Release History 5/5；Rollback 4/4；RepositoryPublisher Python 单元测试 10/10。

## 发布准备状态

- 临时 E2E 菜单脚手架和生成的 Consumer 中间文件已清理；无关 EditorSettings 已恢复。
- RepositoryPublisher 在前一轮从 Dev 生成 General（52 个 Profile）与 Extensions（32 个 Profile）；本轮最终发布树将在 Dev 变更提交后重新生成并校验。
- 前一轮合并后的 Consumer 在 Unity 2022.3.62f3c1 首次导入成功、C# 编译错误为 0；本轮会对最终提交重新生成的树执行干净导入。
- 当前任务目标是完成三仓本地提交与最终验证，随后即可推送；远端推送尚未执行。

### 2026-09-30 鈥?Android 热更 Gate 最终复验

- 修复 `HotUpdateRuntimeVerificationBootstrap.TryRun` 的平台条件编译：Android Player 只编译 Intent 热更启动分支，非 Android Player 编译本地配置文件启动分支，消除 Android 的 CS0162 不可达代码警告。
- MuMu `127.0.0.1:16416`（Android 12 / API 32）上的最终 Gate：`Tools/AndroidVerification/Results/20260930-191703/pipeline-result.json` = PASS，HybridCLR Android IL2CPP 预处理、Release APK 构建、产品验证与清理均通过。四项 AOT metadata 加载成功，程序集 SHA256 与 Manifest 相符，热更入口已调用。
- 冷启动下载 6 个文件 / 1,845,419 字节；强制停止后重启下载 0 字节。APK 构建 0 errors / 1 warning；唯一警告对应 `insecureHttpOption=Unavailable` 的设置反射能力。Unity Editor 最终诊断 0 errors / 0 warnings。
- 首次复验因 Unity Bee 缓存仍引用已清理的临时 E2E 脚本而在 APK 前失败；刷新 AssetDatabase 后重跑完整 Gate 成功。失败尝试没有产生设备验证结果。

### 2026-09-30 UI 适配与本地化最终 Android 复验

- ArchitectureDemo 现在运行时配置 UIAdaptationKit，并在屏幕几何更新时记录 SafeAreaRoot 锚点验证结果；MuMu Android 12/API 32 挖孔 Overlay 最终 Gate `Tools/AndroidVerification/Results/20260930-200311/pipeline-result.json` PASS，横屏 Safe Area 左 inset 136 px，两个 Canvas 锚点验证均 PASS，构建 0 errors / 0 warnings。
- 同一最终 APK 的中文 → English → 中文切换截图和结果保存在 `Tools/AndroidVerification/Results/20260930-200311/localization-cutout/`，本地化 PASS，0 fatal/Unity error。该设备本轮只验证 Landscape；Portrait 未做真机运行验证。
