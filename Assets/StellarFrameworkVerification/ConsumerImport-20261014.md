# 消费者导入与 HttpKit 账号边界验证（2026-10-14）

环境：Windows、Unity 2022.3.62f3c1。对应框架发布版本 1.0.4。此次验证覆盖源码导入、安装包内容、Kit 安装器、YooAsset 资源构建与 HttpKit 行为，没有重新运行 Android/Windows Player 或 IL2CPP 热更验收。

## 本次复现的问题

- 原主仓在没有 Library 的新目录中导入失败。YooAsset 2.3.19 的编辑器及 Addressables 1.22.3 的文档示例程序集直接引用 NUnit，原发布依赖未声明该项。
- Kit 安装器按包 ID 顺序逐个安装依赖，导致资源插件先于 NUnit 安装；真实空白项目导入复现了中途编译失败。
- HttpKit 去重只包含 Method、URL 与 Body，图片共享任务及缓存只按 URL 管理，缺少账号与认证头边界。
- Dev 残留 36 份当前配置不使用的热更 DLL，包括 TestRunner、旧 HotUpdateKit 和 MCP 工具程序集；原生成路径表还包含维护者验证内容。

## 修复后验证

| 验证 | 结果 |
| --- | --- |
| General 发布预览首次导入（无 Library） | PASS，Unity 退出码 0 |
| Dev 代码快照首次导入（无 Library） | PASS，Unity 退出码 0 |
| General＋Extensions 组合首次导入（无 Library） | PASS，Unity 退出码 0 |
| 86 个 Kit/组合安装包及嵌入 payload 检查 | PASS；无框架测试代码、测试 DLL 或 Test Runner 安装依赖；路径常量只指向包内资源 |
| HttpKit 单独安装，未安装 UGUI | PASS，实际通过安装器导入并编译 |
| Resources＋AssetBundle 组合安装 | PASS，实际通过安装器导入并编译 |
| Addressables 独立安装 | PASS，先安装 NUnit 1.0.6，未安装 Test Runner |
| YooAsset 加载＋资源更新＋HybridCLR 代码更新组合安装 | PASS，先安装 NUnit 1.0.6，未安装 Test Runner |
| HttpKit＋UGUI 图片绑定安装 | PASS，实际通过安装器导入并编译 |
| HttpKit PlayMode 行为 | 6/6 PASS：Token 隔离、作用域切换、原子账号与 Token 切换、请求头归一化与快照、图片共享及缓存隔离、取消旧账号图片请求 |
| 导出选择与前置依赖顺序测试 | 8/8 PASS |
| 仓库发布器 Python 测试 | 11/11 PASS |
| YooAsset 构建工具独立安装 | PASS；空白工程只包含 ToolsHub.Core 与资源构建程序集，无 ResKit 运行时或 HybridCLR |
| YooAsset 独立工具实际构建 | PASS；Windows 资源版本 1.0.0 与 1.0.1 均生成完整产物，实际加载 Bundle 内的 TextAsset；已有版本拒绝覆盖，采集配置与 StreamingAssets 未改动 |
| 最终 EditMode 回归 | 766/766 PASS，0 failed / 0 skipped；含资源构建工具的 12 项检查 |

前序修复验证执行了 693 项检查，初次 690 通过、3 项失败。其中两项是此前 README 改写后留下的旧文档断言，一项仍要求将维护者 E2E 内容加入公开资源索引；修正后对应测试组通过。本轮扩大到 766 项，首次 761 通过、5 项失败：新快捷入口违反顶层菜单规范，以及四项断言仍匹配发布器旧英文错误文字。快捷入口改到 Tools 菜单，错误断言与中文界面一致，完整重新执行 766 项全部通过。

前序输出使用 Unity 临时目录。本轮可复查的回归、资源工具安装与实际构建结果保留在开发机 `BuildArtifacts/Verification/20261014`，导出矩阵位于 `BuildArtifacts/Verification/ConsumerExport`，均不作为框架资源发布。`validate_unitypackages.py` 检查包内内容；`ConsumerImportGate.cs` 是独立空白工程的命令行安装验证入口；`ConsumerResourceBuildGate.cs` 在安装资源构建工具后执行真实 Bundle 构建与加载。

## 新电脑的 Package Manager 错误

本机查询官方与中国包源，均能找到 `com.unity.test-framework` 1.1.33，下载地址均返回 HTTP 200；修复后 Dev 代码快照的首次导入通过。用户所述另一台电脑的包下载/解析失败尚未在该电脑复测，不能据此判定已经解决其网络或包源问题。

参见开发文档 [新机器包解析](../StellarFramework/FrameworkDoc/09-Development/PackageResolution.md)。诊断脚本可以检查版本和下载地址，或在关闭 Unity 后备份并重建项目锁文件；不修改全局代理、缓存或包源。
