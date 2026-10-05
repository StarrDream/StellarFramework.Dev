# StellarFramework.Dev

Development source for StellarFramework 1.0.3.

## Framework overview

This is the development, validation, and release project for StellarFramework. It maintains the Runtime, Editor, Tools Hub, Kit Catalog, documentation, samples, and release scripts. The General and Extensions user repositories are generated from this project; framework source and release templates belong here.

Open the project in Unity Hub and select **StellarFramework → Tools Hub** after Unity finishes importing packages and assets. Tank Arena is the cross-Kit case study. For a game project, use Export to select an individual Kit, a Profile, or a combination of resource backends and create a unitypackage.

## Requirements

- Unity Editor 2022.3.62f3c1, the project version
- Python 3 for the repository publisher and static checks
- Unity Package Manager access on first open to resolve Packages/manifest.json
- Android SDK and ADB for Android builds and device verification

The Android toolchain is needed only for Android verification.

## Framework concepts

| Concept | Purpose |
| --- | --- |
| Runtime | Architecture entry point and shared runtime capabilities; add Kits as needed |
| Kit | A focused capability whose export files, dependencies, external packages, and maturity are recorded in the Kit Catalog |
| Adapter / Provider | Connects a Kit to a Unity or third-party backend, such as Addressables loading or YooAsset/HybridCLR updates through ResKit |
| Profile | An exportable selection of Kits and adapters; export individually or combine Profiles |
| Tools Hub | Editor setup, diagnostics, builds, and exports; not a game Player component |

The framework uses MSV to organize Model, Service, and View. Resource loading, content updates, and code updates have separate selectable implementations.

## Architecture

The core architecture entry point is `Assets/StellarFramework/Runtime/Core/Architecture/StellarFramework.cs`. `Architecture<T>` registers Models and Services and manages their initialization, lookup, and disposal. A View reads state through the read-only architecture contract and calls a Service. A Service performs application operations and updates Models. The architecture defines module responsibilities and access boundaries.

~~~mermaid
flowchart LR
    Startup["Game startup"] -->|"Init / lifecycle"| Architecture["Architecture<T><br/>registration, lookup, lifecycle"]
    Architecture -->|"register / initialize"| Model["Model<br/>application state and data"]
    Architecture -->|"register / initialize"| Service["Service<br/>application operations and flow"]
    View["View<br/>StellarView / Unity UI"] -->|"interaction: call"| Service
    Service -->|"read / update"| Model
    View -->|"read-only query"| Model
~~~

[MSV architecture guide](Assets/StellarFramework/FrameworkDoc/01-Architecture/Architecture/Architecture-MSV-架构说明文档-Guide.md) · [architecture source guide](Assets/StellarFramework/FrameworkDoc/01-Architecture/Architecture/Architecture-MSV-架构源码文档-Guide.md)

## Kit guide

This table summarizes the main Kits by use case. Export boundaries, dependencies, and maturity are defined by the Kit Catalog. Algorithms, World, and Flow extensions are listed in [Extensions](https://github.com/StarrDream/StellarFramework.Extensions).

| Kit | What it does | Guide |
| --- | --- | --- |
| LogKit | Categorized logging and runtime diagnostics | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/LogKit/LogKit-PerformanceKit-说明文档-Guide.md) |
| EventKit | Publish and subscribe to typed events | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/EventKit/EventKit-事件系统-说明文档-Guide.md) |
| BindableKit | Observable data and change notifications | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/BindableKit/BindableKit-数据绑定-说明文档-Guide.md) |
| ActionKit | Compose and run reusable actions | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/ActionKit/ActionKit-动作系统-说明文档-Guide.md) |
| FSMKit | State transitions and lifecycle | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/FSMKit/FSMKit-状态机-说明文档-Guide.md) |
| TimeKit | Shared time and timer control | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/TimeKit/TimeKit-时间系统-说明文档-Guide.md) |
| PoolKit | Object reuse and allocation management | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/PoolKit/PoolKit-对象池-说明文档-Guide.md) |
| SingletonKit | Register singleton objects and manage their lifecycle | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/SingletonKit/SingletonKit-单例系统-说明文档-Guide.md) |
| ConfigKit | Load and access project configuration | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/ConfigKit/ConfigKit-配置系统-说明文档-Guide.md) |
| SaveKit | Save data read/write and serialization adapters | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/SaveKit/SaveKit-存档系统-说明文档-Guide.md) |
| SettingsKit | Player settings and storage adapters | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/SettingsKit/SettingsKit-设置系统-说明文档-Guide.md) |
| ResKit | Shared resource load and release entry point | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/ResKit-统一资源-说明文档-Guide.md) |
| Resources / AssetBundle / Addressables (AA) | Optional loading backends; export independently or combine them | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/ResKit-统一资源-说明文档-Guide.md) |
| YooAsset | Loads through ResKit; its content update Provider manages versions, downloads, and cache | [Resource guide](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/ResKit-统一资源-说明文档-Guide.md) |
| HybridCLR | Optional ResKit code update Provider for hot-update assemblies | [Usage guide](Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/CodeUpdate/HybridCLR/ResKit-CodeUpdate-HybridCLR-说明文档-Guide.md) |
| UIKit | UI panel lifecycle, open, and close management | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/UIKit/UIKit-界面系统-说明文档-Guide.md) |
| UIAdaptationKit | Safe areas, screen sizes, and layout adaptation | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/UIAdaptationKit/UIAdaptationKit-说明文档-Guide.md) |
| LocalizationKit | Language data, switching, and UGUI/TMP binding | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/LocalizationKit/LocalizationKit-Guide.md) |
| AudioKit | Sound and music playback with resource integration | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/AudioKit/AudioKit-音频系统-说明文档-Guide.md) |
| HttpKit | Asynchronous HTTP requests and response handling | [Guide](Assets/StellarFramework/FrameworkDoc/02-Kits/HttpKit/HttpKit-网络请求-说明文档-Guide.md) |

## Release links

- [StellarFramework](https://github.com/StarrDream/StellarFramework): the framework and sample project for Unity users.
- [StellarFramework.Extensions](https://github.com/StarrDream/StellarFramework.Extensions): extension Kits for Algorithms, World, and Flow.
