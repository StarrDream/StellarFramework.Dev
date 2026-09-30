# StellarFramework.Dev

StellarFramework 的 Unity 开发工程，面向框架维护者。Runtime、Editor、Tools Hub、Kit Catalog、验证工具和发布脚本都在这里维护。General 与 Extensions 两个使用者仓由本仓生成。

## 环境要求

- Unity Editor：**2022.3.62f3c1**（工程当前版本）
- Python 3：运行仓库发布器及相关检查
- Android 验证：Android SDK、ADB；模拟器和设备要求见验证指南

## 打开工程

克隆本仓，在 Unity Hub 中添加并打开仓库目录。等待 Package Manager 完成依赖解析后，从菜单 **StellarFramework → Tools Hub** 打开工具中心。Start Here 页面提供工程入口和样例导航。

## 工程结构

| 路径 | 内容 |
| --- | --- |
| <code>Assets/StellarFramework/Runtime/Kits</code> | Runtime Kit 与 Adapter |
| <code>Assets/StellarFramework/Editor</code> | Editor 工具及 Tools Hub 模块 |
| <code>Assets/StellarFramework/FrameworkDoc</code> | 面向使用者的 Kit 与工具文档 |
| <code>Assets/StellarFramework/Samples</code> | 入门样例和专项示例 |
| <code>Assets/StellarFramework/Tests</code> | EditMode、PlayMode 与框架验证测试 |
| <code>Assets/StellarFrameworkVerification</code> | 验证工程和发布门禁 |
| <code>Assets/StellarFramework/KitCatalog</code> | Kit 导出与仓库归属配置 |
| <code>Tools/RepositoryPublisher</code> | General、Extensions 发布器 |
| <code>Tools/AndroidVerification</code> | Android 构建与设备验证脚本 |

## 开发流程

新增或修改能力时，先确定公开接口、依赖方向和 Unity / 平台边界，再实现 Runtime 与所需 Adapter。同步更新 Kit Catalog、使用文档和导出配置，并为新行为补充对应测试。

提交前按改动范围运行 Unity 编译、相关 EditMode / PlayMode 测试、Catalog 与导出验证。涉及 Player、资源加载或热更新的改动，还要在干净消费者工程或目标平台门禁中验证。验证入口见[验证架构](Assets/StellarFrameworkVerification/ValidationArchitecture.md)和[当前验证状态](Assets/StellarFramework/FrameworkDoc/08-Validation/ValidationCurrentStatus.md)。

## 发布使用者仓

本仓是 General 与 Extensions 的唯一源码来源。提交源码和发布模板后，先检查发布计划：

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
~~~

所需门禁通过后，生成本地发布仓：

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --general-target C:\GitProject\StellarFramework --extensions-target C:\GitProject\StellarFramework.Extensions --validation PASS
~~~

发布器会重建目标仓中 <code>.git</code> 以外的内容。运行前确认目标路径和 remote 正确，并保存或清理目标仓中的本地改动。生成后检查两个 <code>RELEASE-MANIFEST.json</code> 的 <code>sourceCommit</code>，运行静态发布树和消费者工程验证，再提交发布仓。

## 使用者入口

只想在项目中使用框架，请从 [StellarFramework General](https://github.com/StarrDream/StellarFramework) 开始。需要 Algorithms、World、Flow 或 HybridCLR 热更新时，再查看 [StellarFramework.Extensions](https://github.com/StarrDream/StellarFramework.Extensions)。用户文档位于 <code>Assets/StellarFramework/FrameworkDoc</code>。