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
- 消费者包 `HotUpdatePublisherConsumerE2E` 的 `2026.09.30.011` 在 MuMu 冷启动和重启均通过：7 个文件 / 1,846,971 字节；四项 AOT metadata、程序集 SHA256 和入口调用均通过；重启下载 0 字节。
- YooAsset 产物清单中 `HotUpdateBehavior` 地址有效；`.011` 发布、不可变上传及远端回读均通过。
- Publisher 回滚 `.011 → .010 → .011` 均通过，最终本地版本指针为 `.011`。
- 测试证据：Publisher EditMode 129/129；Release History 5/5；Rollback 4/4；RepositoryPublisher Python 单元测试 10/10。

## 发布准备状态

- 临时 E2E 菜单脚手架和生成的 Consumer 中间文件已清理；无关 EditorSettings 已恢复。
- RepositoryPublisher 从 Dev 生成 General（52 个 Profile）与 Extensions（32 个 Profile）。两仓和合并后的 Extensions Consumer 均通过 Release Tree 校验。
- 合并后的 Consumer 在 Unity 2022.3.62f3c1 完成首次导入及脚本编译，C# 编译错误为 0。
- Dev、General、Extensions 的本地提交组成待推送变更；远端推送尚未执行。
