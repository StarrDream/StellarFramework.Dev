# StellarFramework.Dev

StellarFramework 1.0.3 开发源仓

## 框架主体介绍

本仓是 StellarFramework 的开发、验证和发布工程，维护 Runtime、Editor、Tools Hub、Kit Catalog、文档、案例及发布脚本。General 与 Extensions 两个使用者仓由本仓生成；框架源码和发布模板以本仓为准。

在 Unity Hub 中打开工程，等待资源与依赖导入完成后，从菜单 **StellarFramework → Tools Hub** 进入编辑器工具。Tank Arena 是跨 Kit 案例。发布给项目使用时，可在 Export 中选择单个 Kit、组合 Profile 或资源后端，导出 unitypackage。

## 环境要求

- Unity Editor 2022.3.62f3c1（工程指定版本）
- Python 3：运行仓库发布器和静态检查
- Unity Package Manager：首次打开时解析 Packages/manifest.json
- Android SDK 与 ADB：仅执行 Android 构建和设备验证时需要

## 框架概念

| 概念 | 作用 |
| --- | --- |
| Runtime | 提供架构入口和运行时能力；项目按需选择功能 Kit |
| Kit | 按职责组织的框架能力；Catalog 声明导出文件、依赖、外部包和成熟度 |
| Adapter / Provider | 对接 Unity 或第三方后端；例如 ResKit 的 Addressables 加载适配器、YooAsset 内容更新 Provider、HybridCLR 代码更新 Provider |
| Profile | 一组可导出的 Kit 与适配器配置，可单独选择或组合 |
| Tools Hub | Unity Editor 内的配置、诊断、构建和导出入口，不进入游戏 Player |

框架按 MSV 组织 Model、Service、View。资源加载、资源内容更新和代码更新由 ResKit 的可选实现提供，项目可以分别选择和组合。

## 架构介绍

核心架构入口位于 `Assets/StellarFramework/Runtime/Core/Architecture/StellarFramework.cs`。`Architecture<T>` 管理 Model 与 Service 的注册、初始化、查询和销毁；View 通过只读架构契约读取状态并调用 Service。Service 负责应用操作并通过架构访问 Model。需要状态变更通知时，可以组合 BindableKit。

~~~mermaid
flowchart LR
    Startup["游戏启动"] -->|"Init / 生命周期"| Architecture["Architecture<T><br/>注册、查询、生命周期"]
    Architecture -->|"注册 / 初始化"| Model["Model<br/>应用状态与数据"]
    Architecture -->|"注册 / 初始化"| Service["Service<br/>应用操作与业务流程"]
    View["View<br/>StellarView / Unity UI"] -->|"交互：调用"| Service
    Service -->|"读取 / 更新"| Model
    View -->|"只读查询"| Model
    Model -. "可选：BindableKit 状态通知" .-> View
~~~

[MSV 架构说明](Assets/StellarFramework/FrameworkDoc/01-Architecture/Architecture/Architecture-MSV-架构说明文档-Guide.md) · [架构源码文档](Assets/StellarFramework/FrameworkDoc/01-Architecture/Architecture/Architecture-MSV-架构源码文档-Guide.md)

## Kit 介绍

下表按使用场景概述主要 Kit。每个 Kit 的导出边界、依赖和成熟度以 Kit Catalog 为准；Algorithms、World、Flow 扩展见 [Extensions](https://github.com/StarrDream/StellarFramework.Extensions)。

| 适用场景 | Kit | 简介 | 文档 |
| --- | --- | --- | --- |
| 基础与流程 | LogKit | 分类日志与运行时诊断 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/LogKit/LogKit-PerformanceKit-说明文档-Guide.md) |
| 基础与流程 | EventKit | 类型化事件发布与订阅 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/EventKit/EventKit-事件系统-说明文档-Guide.md) |
| 基础与流程 | BindableKit | 可观察数据和变化通知 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/BindableKit/BindableKit-数据绑定-说明文档-Guide.md) |
| 基础与流程 | ActionKit | 组合与执行可复用动作 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/ActionKit/ActionKit-动作系统-说明文档-Guide.md) |
| 基础与流程 | FSMKit | 状态转换与状态机生命周期 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/FSMKit/FSMKit-状态机-说明文档-Guide.md) |
| 基础与流程 | TimeKit | 统一时间和计时控制 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/TimeKit/TimeKit-时间系统-说明文档-Guide.md) |
| 基础与流程 | PoolKit | 对象复用与分配管理 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/PoolKit/PoolKit-对象池-说明文档-Guide.md) |
| 基础与流程 | SingletonKit | 注册单例对象及其生命周期管理 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/SingletonKit/SingletonKit-单例系统-说明文档-Guide.md) |
| 数据与配置 | ConfigKit | 项目配置加载和访问 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/ConfigKit/ConfigKit-配置系统-说明文档-Guide.md) |
| 数据与配置 | SaveKit | 存档读写和序列化适配 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/SaveKit/SaveKit-存档系统-说明文档-Guide.md) |
| 数据与配置 | SettingsKit | 玩家设置与存储适配 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/SettingsKit/SettingsKit-设置系统-说明文档-Guide.md) |
| 资源与更新 | ResKit | 统一资源加载与释放入口 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/ResKit-统一资源-说明文档-Guide.md) |
| 资源与更新 | Resources / AssetBundle / Addressables (AA) | 可选资源加载后端，可单独导出或组合 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/ResKit-统一资源-说明文档-Guide.md) |
| 资源与更新 | YooAsset | 经 ResKit 加载资源；内容更新 Provider 管版本、下载和缓存 | [资源说明](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/ResKit-统一资源-说明文档-Guide.md) |
| 资源与更新 | HybridCLR | ResKit 可选代码更新 Provider，负责加载和执行热更程序集 | [使用说明](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/CodeUpdate/HybridCLR/ResKit-CodeUpdate-HybridCLR-说明文档-Guide.md) |
| UI 与表现 | UIKit | UI 面板生命周期及打开、关闭管理 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/UIKit/UIKit-界面系统-说明文档-Guide.md) |
| UI 与表现 | UIAdaptationKit | 安全区、屏幕规格和布局适配 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/UIAdaptationKit/UIAdaptationKit-说明文档-Guide.md) |
| UI 与表现 | LocalizationKit | 语言数据、切换与 UGUI/TMP 本地化绑定 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/LocalizationKit/LocalizationKit-Guide.md) |
| UI 与表现 | AudioKit | 音效、音乐播放及资源接入 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/AudioKit/AudioKit-音频系统-说明文档-Guide.md) |
| 网络 | HttpKit | 异步 HTTP 请求与响应处理 | [文档](Assets/StellarFramework/FrameworkDoc/02-Kits/HttpKit/HttpKit-网络请求-说明文档-Guide.md) |

## 发布链接

- [StellarFramework 使用者仓](https://github.com/StarrDream/StellarFramework)：通用框架、Tank Arena 案例与 Kit 导出工程。
- [StellarFramework.Extensions 使用者仓](https://github.com/StarrDream/StellarFramework.Extensions)：Algorithms、World、Flow 扩展 Kit。
- [版本策略](VERSIONING.md)：版本号规则及三仓同步要求。
- [仓库发布器说明](Tools/RepositoryPublisher/README.md)：从 Dev 生成两个使用者仓。
