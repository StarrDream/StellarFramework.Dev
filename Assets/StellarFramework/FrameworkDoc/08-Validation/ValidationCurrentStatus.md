# StellarFramework 当前验证状态

> 这是面向维护者的“当前状态摘要”。历史阶段证据、Benchmark 明细与旧 Profile 数量保留在 `KitExportValidationMatrix.md`，不要把历史数字继续堆到本页。

## 2026-10-05 ResKit 热更新 Provider 架构

- 按用户决定，本轮架构整理继续留在 `main`；Dev、General、Extensions 共用的框架发布版本为 `1.0.3`，本轮不调整发布版本。
- 热更新运行时按 ResKit 能力拆分：资源加载由 Loader/`ResScope` 提供，内容版本与下载由 `IResContentUpdateProvider` 提供，代码加载由 `IResCodeUpdateProvider` 提供。项目启动层负责按顺序组合。
- 默认组合为 `ResKit.YooAsset`（内容更新与资源 Loader）+ `ResKit.CodeUpdate.HybridCLR`（代码载荷读取、校验和程序集入口）。两项是可独立导出的 ResKit 扩展；ResKit.Core 不引用 YooAsset 或 HybridCLR。
- 独立的 HybridCLRKit 类型转发程序集、静态兼容入口和“自行创建资源 Scope”的隐式 Runner 已移除。HybridCLR API 位于 `StellarFramework.Res.CodeUpdate.HybridCLR` 命名空间，外部调用入口是 ResKit Provider。
- `HotUpdate Publisher` 的 Development 环境默认发布到项目本地 `BuildArtifacts/HotUpdate/Local`，同一台 Windows 电脑通过 `file:///` 地址直接读取；Android 设备无法访问开发机的 Windows 路径，需要可达的 HTTP 地址。Staging/Production 需要显式配置 HTTP(S) 地址，Production 强制 HTTPS。本地文件模式适合首次试跑，不验证 HTTP Range。
- 本轮验证 Demo 使用的应用包版本标签为 `1.0.1`，热更内容包版本为 `1.0.2`；两者是 Player 与热更链路中的独立版本，不代表框架发布版本。Unity `2022.3.62f3c1` 的 `StellarFramework.Tests.FrameworkValidation` EditMode 回归 **695/695 PASS**，0 failed / 0 skipped。
- Android x86_64 IL2CPP Release APK `Builds/AndroidVerification/StellarFramework-HotUpdate-x86_64-release.apk` 已重新构建，MuMu 12（Android 12 / API 32，`127.0.0.1:7555`，720x1280）实测冷启动和强制停止后的缓存重启均 **PASS**。冷启动下载 6 个文件 / 1,934,926 字节，Manifest、DLL SHA256、4 项 AOT metadata 和热更入口均通过；重启命中 15 个缓存文件，下载 0 字节。完整门禁记录：`Tools/AndroidVerification/Results/20261005-055401/pipeline-result.json`、`result.json`。测试内容由本机临时 HTTP 服务经 `adb reverse` 提供，不是公网 CDN。
- 同一 APK 在 MuMu 上手动完成 LocalizationKit **English → 中文 → English** 双向切换，标题、HUD、按钮和包版本标签均随语言更新；截图：`Tools/AndroidVerification/Results/20261005-055401/manual-mumu/tank-arena-en.png`、`tank-arena-zh.png`、`tank-arena-en-after-toggle.png`。UIAdaptationKit 启动日志为 **PASS**（720x1280、Portrait、安全区锚点有效）；该设备返回 `insets=False`，所以本次没有覆盖刘海/挖孔硬件布局。
- Windows x64 IL2CPP Release Player `Builds/WindowsVerification/HotUpdate-v1.0.2-20261005-060246` 已构建并运行 **PASS**：YooAsset 内容更新、Manifest/DLL 校验、4 项 AOT metadata、HybridCLR 热更入口及 Range 中断续传均通过；续传从 262144 字节偏移继续。运行记录：该目录下 `Temp/StellarHotUpdateVerification/runtime-result.json`。Android 与 Windows 构建各 0 errors / 1 non-fatal warning。Android 验证设备是 MuMu 模拟器，不是物理真机；本轮没有真机结果。

## 2026-10-02 Tank Arena Demo 与平台验证

- 唯一用户 Demo 为 `Assets/StellarFramework/Samples/TankArena/Scene/FrameworkDemo.unity`；旧 ArchitectureDemo 场景、资源和专属 PlayMode 测试已移除，ToolsHub 与快速开始入口均指向 Tank Arena。
- 案例源码位于 `Assets/StellarFramework/Samples/TankArena/Runtime`，热更程序集标识为 `HotUpdate`；默认配置位于 `Assets/StreamingAssets/TankArena/demo-config.json`。案例说明见 `Assets/StellarFramework/Samples/TankArena/CaseStudy.md`。场景视觉内容由 Sample 内的 Prefab、材质、贴图和字体资源组成；运行时代码通过实例化 Prefab 组装场景，不以代码创建画面节点或几何体。
- Windows x64 Release HotUpdate Gate：`Builds/WindowsVerification/HotUpdate-v1.0.0-20261002-091604`，StandaloneWindows64 / IL2CPP，YooAsset 包版本 `1.0.0`、6 个 Bundle，Player 构建 0 errors / 1 warning。Player 实测验证 Range 下载中断后以 262144 字节偏移续传、从 ResKit/YooAsset 读取 Manifest、加载 `HotUpdate` 程序集并进入 Tank Arena 战斗。运行记录位于 `C:/Users/Administrator/AppData/LocalLow/DefaultCompany/StellarFramework/Player.log`。
- Android x86_64 Release HotUpdate APK：`Builds/AndroidVerification/StellarFramework-HotUpdate-x86_64-release.apk`，IL2CPP，构建 0 errors / 1 warning。MuMu (`emulator-5556`, Android 12 / API 32, 720x1280) 冷启动下载 6 个文件 / 1,934,936 字节，校验程序集 SHA256 并加载 4 项 AOT metadata；重启命中 15 个本地缓存文件，下载 0 字节。门禁记录位于 `Tools/AndroidVerification/Results/20261002-090714/pipeline-result.json`。
- Android 上 UIAdaptationKit 在冷启动与重启时均记录 `PASS`，Portrait breakpoint 与安全区锚点一致。MuMu 此次未报告安全区 inset；本次设备结果不代表带挖孔或刘海的硬件验证。
- LocalizationKit 在 MuMu 上完成 English → 中文 → English 实机切换，标题、HUD、按钮和包版本标签均随语言变化。截图见 `Tools/AndroidVerification/Results/LocalizationManual-20261002/locale-switch.png` 与 `locale-revert.png`；冷启动、缓存重启及 UIAdaptation 复测记录见同目录 `result.json`。
- 本轮针对快速开始文档、案例资源边界、发布排除规则、ToolsHub 场景入口和热更源码的 7 项 EditMode 回归测试全部通过。Windows 与 Android BuildReport 各记录 1 条 warning；Release Gate 未导出 warning 明细。两平台 Player / APK 运行均完成热更与案例启动验证。

## 2026-09-30 二次审查增量（历史记录，早于当前 Demo 更新）

- ToolsHub、Kit Catalog、asmdef 边界和仓库发布树完成静态复查：84 个原子 Profile ID 唯一，82 个 Stable / 2 个 RC；依赖、源码和文档路径完整，Profile 输出无重复；102 个 asmdef 的内部依赖图无环，Runtime 到 Editor 引用为 0。
- FlowKit 业务骨架生成器的 Assets 路径边界已修复：`AssetsBackup` 等相邻目录不再被误判为 `Assets` 子目录。Unity Editor 与对应 EditMode 测试程序集编译通过；精确回归 `ProjectScaffolderRejectsPathsOutsideAssetsDirectoryBoundary` job `7aa89f32` **1/1 PASS**，Unity Diagnose 为 0 errors / 0 warnings。
- FlowKit 路径边界回归最初因 `<UnsavedScene:0>` 被安全测试门阻止；随后刷新并等待 Unity 空闲后运行并通过（job `7aa89f32`）。整个流程没有保存或丢弃该场景。
- MuMu 普通 Release 样例验证 `Tools/AndroidVerification/Results/20260930-1417-MuMu-ArchitectureDemo/mumu-verification-summary.json`：APK 冷启动与运行稳定 PASS；Localization 中文 → English → 中文 PASS；tablet、widescreen、1024x768 三种几何下的通用 Canvas 视觉 smoke PASS。该旧 APK 未引用 UIAdaptationKit；专项 Android 结果见同日的 `20260930-195337`。
- MuMu Android HotUpdate 最终 Gate `Tools/AndroidVerification/Results/20260930-191703/pipeline-result.json` = **PASS**：HybridCLR Android IL2CPP 产物重新生成；4 项 AOT metadata 全部加载；程序集 SHA256 与 Manifest 一致；冷启动更新下载 6 个文件 / 1,845,419 字节并调用热更入口；强制停止后重启下载 0 字节；清理 PASS。APK 构建 0 errors / 1 warning（`insecureHttpOption=Unavailable`）；Unity Editor Diagnose 为 0 errors / 0 warnings。
- HotUpdate Publisher Consumer E2E `Temp/HotUpdatePublisherLocalE2E/publish-result.json`：Development + LocalFolder 本机发布 `.011` **PASS**；不可变上传与 HTTP 读回完成，11 个发布文件通过 Full Gate，消费者 Android 冷启动/重启与 `.011 → .010 → .011` 回滚均通过。发布目录为 `Temp/HotUpdatePublisherLocalE2E/PublishRoot/hotupdate/Development`，HTTP/CDN 验证只连接本机测试服务；真实 Production S3/CDN endpoint、凭证和生产权限尚未验证。
- UIAdaptationKit 的 `UIKitAdaptationTests` **21/21 PASS**，clean consumer PlayMode smoke **1/1 PASS**（详见 `Assets/docs/chatgptwebmemory.md`）。ArchitectureDemo 现引用 UIAdaptationKit；最终 Android IL2CPP x86_64 构建在 MuMu Android 12/API 32 挖孔 Overlay 专项门禁 `Tools/AndroidVerification/Results/20260930-200311/pipeline-result.json` **PASS**：构建 0 errors / 0 warnings；冷启动与重启存活；两个 Screen Space Canvas 的 `SafeAreaRoot` 锚点都与 Unity 返回的 Safe Area 一致，安全区左侧 inset 为 136 px，Landscape breakpoint 生效。该 MuMu/Unity 组合返回 `Screen.cutouts` 数量 0，因此设备实测证明 Safe Area 路径；精确 Cutout 数据缺失时的策略由 Kit EditMode 测试覆盖。
- 最终 APK 在挖孔 Overlay 下重新实测 Localization 中文 → English → 中文，结果 `Tools/AndroidVerification/Results/20260930-200311/localization-cutout/localization-result.json` **PASS**，三张截图分别证明中文、英文及切回中文状态。该 MuMu 本轮保持 Landscape；Portrait breakpoint 的选择逻辑有自动化测试覆盖，未记录 Portrait 设备运行证据。
- 较早的 Android HotUpdate 尝试 `20260930-121301` 未能完成，已由 `20260930-191703` 的完整 PASS Gate 覆盖；失败尝试没有产生设备验证结果。

## 2026-09-22 验证基线（历史）

### 编译与运行

- Unity Editor compile：**PASS**，0 errors / 0 warnings。
- Console Error：**0**。
- `StellarFramework.Tests.FrameworkValidation` EditMode：**600 / 600 PASS**，0 failed，0 skipped。
- `StellarFramework` PlayMode：**15 / 15 PASS**，0 failed，0 skipped。
- `FrameworkArchitecture_Playable.unity`：可进入 PlayMode，Console **0 Error**。
- ArchitectureDemo scene validation：Missing Reference **0**；仅存在 UIKit Layer 占位/重复名称类 Info，不构成 release blocker。

### Catalog

- 原子 Distribution Profile：**84**。
- Recommended Profile：**5**。
- 原子 kind：
  - `kit-with-dependencies`：38
  - `tooling`：20
  - `kit`：22
  - `single-file`：2
  - `shared-runtime`：1
  - `generated-support`：1
- Runtime tier：
  - Foundation：21
  - Extension：12
  - Adapter：27
  - non-tier：24

- Maturity：
  - Stable：79
  - RC：3
  - Experimental：2

当前非 Stable 原子 Profile：

```text
RC:           HttpKit
RC:           ResKit.Addressables
RC:           ResKit.YooAsset
Experimental: HybridCLRKit
Experimental: HybridCLRKit.Tools
```

因此当前 Recommended Profile 的闭包成熟度为：Localization / ResKit / UIAdaptationKit / UIKit Complete = Stable；Hot Update Full = Experimental。

以上 maturity 数字是 2026-09-22 的历史快照。P6 当前 maturity 状态如下：

## 2026-09-24 P6 成熟度状态

```text
Stable       80
RC            4
Experimental  0
```

当前非 Stable 原子 Profile：

```text
RC:           HttpKit
RC:           ResKit.Addressables
RC:           HybridCLRKit
RC:           HybridCLRKit.Tools
```

`ResKit.YooAsset` 已基于 focused tests、Range/cache Gate、Android Release IL2CPP 内容更新 E2E 和 Hot Update Full clean consumer PASS 升为 Stable。`HybridCLRKit` 与 `HybridCLRKit.Tools` 已基于 Android Release IL2CPP Gate、PlayMode Gate 和 clean consumer PASS 升为 RC；Stable 评估保留到 P7 Windows64 Release IL2CPP Gate 重跑后。`Hot Update Full` 由完整依赖闭包自动派生为 RC。

P4 Android 机器证据：`Tools/AndroidVerification/Results/20260923-204313/pipeline-result.json` 与同目录 `result.json`。P5 clean consumer 机器证据路径及依赖锁见本轮 Agent Plan 与 `Assets/docs/chatgptwebmemory.md`。

P6 focused maturity policy 为 **2/2 PASS**。FrameworkValidation 主 filter **614/614 PASS**，独立 Addressables validation class **3/3 PASS**，合计 **617/617 PASS**，0 failed / 0 skipped；UnitySkills Diagnose 为 compile/update idle、Console 0 errors / 0 warnings。

## 2026-09-24 P7 最终成熟度与回归状态

```text
Stable       82
RC            2
Experimental  0
```

当前非 Stable 原子 Profile：

```text
RC: HttpKit
RC: ResKit.Addressables
```

P7 基于 P4 Android Release IL2CPP HotUpdate E2E、P5 Hot Update Full clean consumer、P7 Windows64 Release IL2CPP Player Gate 和完整回归，将 `HybridCLRKit` 与 `HybridCLRKit.Tools` 从 RC 升为 Stable。`Hot Update Full` maturity 继续由完整依赖闭包自动派生为 Stable，没有手工设置 Recommended Profile 等级。Windows Player 聚合证据：`D:\SF-P7-Win64-20260924\P7Artifacts\Windows64ReleasePlayer-FullBuild-05\P7-Windows64-Release-IL2CPP-GateEvidence.json`；P4 Android 证据：`Tools/AndroidVerification/Results/20260923-204313/pipeline-result.json` 与同目录 `result.json`；P5 clean consumer 证据路径与依赖锁见 Agent Plan 和 `Assets/docs/chatgptwebmemory.md`。

P7 回归证据：UIAdaptationKit / HotUpdate focused suites **44/44 PASS**；maturity、Catalog、Publisher、Standalone Export policies **89/89 PASS**；FrameworkValidation **614/614 PASS**，独立 Addressables policy **3/3 PASS**。`StellarFramework.Tests.PlayMode` 产品测试为 **15/15 PASS**；Unity Test Runner 的 PlayMode fresh discovery 总数为 **17**，其中另外 1 项是 HotUpdate ReleaseGate、1 项是 UnitySkills 自身的 PlayModeRecovery 测试。HotUpdate Release PlayMode Gate fresh discovery 总数 **17**，exact gate **1/1 PASS**。P7 结果 JSON 保存在 `D:\SF-P7-Win64-20260924\P7Artifacts\`。Windows64 Release IL2CPP build 有 0 errors 和 1 条 HybridCLR `CheckSettings` warning：隔离 clean consumer 未配置 source hot-update modules，Gate 使用 P5 已核验 Windows DLL；Runtime 聚合 Gate 的各项断言均 PASS。

Release Gate 故意中断 HTTP Range 下载，以验证恢复流程。Unity Console 中对应的两条错误日志和两条 YooAsset abort 清理警告已归档至 `D:\SF-P7-Win64-20260924\P7Artifacts\P7-Expected-Range-Interruption-Console.log`。Gate 运行后已清空 Console；UnitySkills Diagnose 显示健康、compile/update idle、Console **0 errors / 0 warnings**。未发现 `TOOLING EVIDENCE GAP`。

Recommended Profile：

```text
Localization Complete
ResKit Complete
UIAdaptationKit Complete
UIKit Complete
Hot Update Full
```

Catalog 仍不包含 `samples.*` Profile；用户 Sample 与发布验证职责继续分离。

## 当前发布含义

本页的 PASS 表示当前母工程统一回归基线正常，不等价于“所有 Profile 都已经完成所有目标平台实机发布”。

以下验证仍按能力类型单独判断：

- Android / iOS / HarmonyOS 等目标平台 Player；
- IL2CPP；
- HybridCLR 真机代码热更；
- YooAsset / Addressables 真实远端内容发布；
- 平台 SDK 或第三方插件专项集成。

这些能力的未执行项必须继续保留为 RC / Experimental 或明确 Gate，不能因为母工程统一回归 PASS 就自动升级为 Stable。

## 更新规则

1. 当前结构与统一测试结果只更新本文件。
2. 阶段性 Benchmark、冻结记录、专项验证追加到 `KitExportValidationMatrix.md`。
3. Catalog 增删 Profile 时同步更新本页的 Catalog 摘要。
4. 不回写篡改历史阶段数字；历史证据只追加澄清。
5. Release 前必须重新运行 compile、FrameworkValidation EditMode、PlayMode 与对应 clean-project / Player Gate。
