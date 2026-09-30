# StellarFramework.Dev

**本仓只面向框架开发者和维护者。** 这里包含完整 Unity 研发工程，是所有用户发布仓的唯一源码来源。只想使用框架的开发者请从 [General 用户仓](https://github.com/StarrDream/StellarFramework) 开始；需要 Algorithms、World、Flow 或 HotUpdate 时再添加 [Extensions](https://github.com/StarrDream/StellarFramework.Extensions)。

## 三仓分工

| 仓库 | 面向谁 | 用来做什么 |
| --- | --- | --- |
| StellarFramework.Dev | 框架维护者 | 修改 Runtime / Editor / Tools Hub，运行完整验证，生成两个用户发布仓 |
| StellarFramework | Unity 使用者 | 打开完整 General 工程、运行入门 Demo、按需导出一个或多个通用 Kit |
| StellarFramework.Extensions | Unity 使用者 | 添加 Algorithms / World / Flow / HotUpdate 扩展；依赖匹配版本的 General |

**功能只在 Dev 开发和修复。发布仓由 Dev Publisher 生成，不接受平行功能开发。**

## 打开研发工程

1. 使用 Unity Hub 安装 **Unity 2022.3.62f3c1**。
2. 在 Unity Hub 中通过 **Add → Add project from disk** 选择本仓目录。
3. 等待导入和 Package Manager 完成；打开 **StellarFramework → Tools Hub → Start Here → Quick Start**。
4. 运行 ArchitectureDemo，确认自己的 Unity 环境可正常打开项目后再开始改动。

开发工程覆盖全部 Kit 源码、Tools Hub、分发 Catalog、Samples、Tests 和维护者验证工具。不要把 General / Extensions 的发布目录复制回 Dev，也不要在多个仓库维护同一份功能代码。

## 研发时去哪里找

- Runtime Kit：`Assets/StellarFramework/Runtime/Kits`。
- Editor 与 Tools Hub：`Assets/StellarFramework/Editor`。
- 用户用法文档：`Assets/StellarFramework/FrameworkDoc/02-Kits`。
- Tools Hub 文档：`Assets/StellarFramework/FrameworkDoc/04-ToolsHub`。
- 用户入门 Demo：`Assets/StellarFramework/Samples/ArchitectureDemo`。
- 自动化回归：`Assets/StellarFramework/Tests`。
- 发布前验证：`Assets/StellarFrameworkVerification` 与 `Tools/Verification`。
- Kit 可选导出源事实：`Assets/StellarFramework/KitCatalog/KitDistributionCatalog.json`。
- General / Extensions 仓库归属：`Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json`。

Catalog 当前定义 84 个可选择分发 Profile 和 5 个 Recommended Profile。Profile 可以对应一个 Runtime Kit、Adapter、Editor 工具或单文件导出；导出器会根据声明补齐框架内依赖。添加或调整能力时要同步检查 Profile、依赖闭包、UPM 声明、文档和导出验证，不能只“让源码能编译”。

## 新 Kit / 功能的完成标准

1. 先明确职责、公开 Contract、依赖方向和平台条件；Foundation / General Runtime 不反向依赖 Extensions。
2. Runtime 与 Unity、第三方 SDK、资源后端之间使用清楚的 Adapter 边界。遵循 MSV：Model 持有状态，Service 承担规则，View 转换表现与输入。
3. 为使用者写入门文档，明确最小代码路径、依赖、失败行为、配置步骤和排错方式。
4. 把能力登记到 Catalog，确认单 Kit Profile 能独立选择；只在有清晰价值时增加 Recommended Profile。
5. 提供必要的 Editor / Tools Hub 工作流，但不要让 Runtime Kit 依赖 Editor 程序集。
6. 为新行为加有针对性的 EditMode / PlayMode / FrameworkValidation 覆盖；覆盖失败路径和依赖边界。
7. 在干净消费者组合中验证导出物可编译。资源加载、热更新、平台相关能力还要运行对应 Player / Android Release Gate。

## 验证顺序

按改动范围从窄到宽：

1. Unity Console：编译通过，检查新增错误和警告。
2. 运行受影响的 EditMode / FrameworkValidation。
3. 运行受影响的 PlayMode，重点检查异步生命周期、场景和真实运行行为。
4. 执行 Architecture / Catalog / Packaging Policy，验证可导出性及边界。
5. 对发布组合运行 Clean Consumer Compile；对构建、资源或热更功能再跑 Player / Release Gate。

测试入口和验证范围见 [Validation Architecture](Assets/StellarFrameworkVerification/ValidationArchitecture.md) 与 [当前验证摘要](Assets/StellarFramework/FrameworkDoc/08-Validation/ValidationCurrentStatus.md)。Unity Test Runner 自动化优先使用项目里的 `stellar_test_run_safe`，避免 Refresh / Compile 和 Test Runner 并发造成误报。

### Android 与 HotUpdate

Android Release Gate 使用 Android API 35 虚拟设备，验证 Release APK 安装、首次启动、进程重启，以及 HotUpdate Profile 的冷下载与缓存命中。先按 [Android 验证指南](Tools/AndroidVerification/README.md)准备 Android SDK、API 35 AVD 和硬件虚拟化，再从本仓根目录运行：

~~~powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Test-StellarAndroidEnvironment.ps1
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1 -HotUpdate
~~~

结果和截图保存在 `Tools/AndroidVerification/Results`。该 Gate 是模拟器 smoke / release 检查；真实设备 GPU、ARM64 原生插件、厂商 ROM、XR / PICO 和硬件传感器仍要在相应设备上单独验证。

## 从 Dev 生成发布仓

发布前先提交所有选中源码和生成模板，然后预览：

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
~~~

确认 General / Extensions 的 Profile 归属、文件集合和依赖正确，验证状态为 PASS 后生成本地发布工程：

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --general-target C:\GitProject\StellarFramework --extensions-target C:\GitProject\StellarFramework.Extensions --validation PASS
~~~

**发布命令会重建目标仓除 .git 外的内容。** 先确认目标目录和 origin 指向正确仓库；不要把未保存的本地修改放在发布仓里。发布后检查两个 `RELEASE-MANIFEST.json` 的 sourceCommit 与当前 Dev HEAD 一致，运行 Consumer / 静态发布验证，再提交 General 和 Extensions。上传远端或发布 Tag 前应单独完成所需审批和版本决策。

生成逻辑、参数和来源保护见 `Tools/RepositoryPublisher/README.md`。