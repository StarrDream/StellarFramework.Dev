# ResourcePublisher — YooAsset 资源 CDN 管理

入口：`StellarFramework → Tools Hub → 资源管理 → CDN 资源发布`，或 `Tools/StellarFramework/资源构建/CDN 资源发布`。

独立模块 `reskit.resource.publisher.tools`，仅依赖 `toolshub.core`，**不依赖 HybridCLR**。复用旧 HotUpdatePublisher 已有的 LocalFolder/S3 不可变文件、环境配置、凭证与校验实现；原代码热更流水线暂保留，不做破坏性改写。

## 使用步骤

1. 在 YooAsset 资源构建工具中生成一个 Package 的完整版本产物，选择该 Package 版本目录（如 `Bundles/iOS/GamePackage/1.0.0`）。
2. 设定发布根目录名（例如 `kimiMathfBundles`）、目标平台、Package 名称、待发布版本。
3. 切换 Development / Staging / Production 环境，配置主备 CDN 基础 URL；工具自动生成 `<根目录>/<平台>/<Package>/` 相对路径。所有这些路径均为配置，不写入游戏运行时常量。
4. 选择发布目标：本地文件夹/挂载目录，或 S3 兼容存储。S3 凭证从 `STELLAR_RESOURCE_<PROFILE>` 环境变量读取，绝不保存密钥。
5. 先执行“预览差异（不上传）”；已有不可变对象内容不同立即阻止上传；没有变化的对象复用。
6. 在确认界面执行“上传并发布版本”。全部不可变文件上传并校验长度和 SHA-256 后，才以 CAS 方式更新该 Package 的 `.version` 指针。
7. 可通过“CDN 快速抽检”检查版本与代表性文件，或“CDN 全量 SHA-256 校验”读取全部远端文件字节核对；**快速模式不算全量验证**。

## 限制和安全

- 本模块不负责玩家客户端初始化、下载、更新调度、19 Package 一键批量构建或 StreamingAssets 部署，均由业务适配层按需接入。
- HTTP(S) CDN 地址用于 GET 只读检查；自动发布仅支持本地文件夹或 S3 兼容存储，不把任意 HTTP URL 当作上传 API。
- 只接受包含 YooAsset `.version`、对应版本的 `.bytes`、`.hash` 和 Bundle 文件的构建产物；跳过缓存、`.meta` 及构建中临时文件。
- 正式环境要求 HTTPS。主备 CDN 仅在显式检查时逐一验证。
- 版本指针本身是可变文件，其余资源不可覆盖；一个 Release 的远端指针切换失败应当按失败处理。
- 发布配置存在当前电脑的 EditorPrefs，供开发者独立切换环境；运行时远端配置不由本编辑器自动植入 Player。
- 暂未加入自动的跨版本历史回滚窗口；可复用的底层发布目标已提供 `RollbackAsync`，正式回滚界面需要在发布历史索引功能完成后启用。
