---
name: stellarframework-unity
description: Use when implementing, integrating, or reviewing a Unity project that uses StellarFramework, especially its MSV architecture, Kit selection, ResKit, YooAsset and HybridCLR hot update, localization, UI adaptation, or ToolsHub. Complements the project's Unity skill with StellarFramework-specific contracts and workflow.
---

# StellarFramework for Unity

This skill gives Unity coding agents framework-specific guidance. It complements the consumer project's Unity skill: follow that skill for Unity Editor, scenes, prefabs, import settings, and Test Runner operations; use this skill to choose and use StellarFramework APIs correctly. Do not let either skill silently replace the other project's conventions.

This folder is portable. To make it available in another Codex Unity project, copy the complete `stellarframework-unity` folder to `<UnityProject>/.agents/skills/stellarframework-unity/`. It is agent guidance, not a Unity runtime package. If the project's Unity skill has its own install location, follow that skill system's discovery convention as well.

## Start from the project that is actually installed

Before changing code:

1. Read `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, relevant `.asmdef` files, and the project's existing startup/architecture code.
2. Determine how StellarFramework is installed: full source, exported `.unitypackage`, or a combination of General and Extensions packages. Look for `RELEASE-MANIFEST.json` and `KitDistributionCatalog.json`. Use the manifest's version, profile IDs, and pinned UPM dependencies as the installed contract.
3. If the manifest or catalog is absent, inspect the installed files and assembly references. Do not assume that every Kit, editor tool, adapter, or provider is present just because it exists in the Dev repository.
4. Check the current catalog maturity before recommending a Kit. Profiles describe export dependencies; they are not proof that a feature is installed or verified on every target platform.
5. Follow the user's requested scope. Do not add a Kit or third-party package just to match a sample project.

## Use the MSV architecture

The framework architecture is MSV: Model, Service, View, managed by `Architecture<T>`.

- Register Models and Services in `InitModules()`. Use the framework lifecycle instead of adding a second global container.
- Model owns application state. Service owns use-case behavior and changes Models. Keep scene objects, Unity UI, and presentation decisions out of Model.
- View reads state through an `IReadOnlyModel` contract and asks a Service to perform an action. A View should not obtain a mutable Model or change state directly.
- Views expose `IReadOnlyArchitecture` and use `GetReadOnlyModel<T>()`; there is no `IView.GetModel<T>()` extension in this framework version. Use `GetModel<T>()` from Services, where the architecture grants mutable Model access.
- A read-only contract must expose read-only members too. For bindable state use `IReadOnlyBindableProperty<T>`; do not expose `BindableProperty<T>`, writable collections, or other mutable objects through the View-facing interface.
- Bind and unbind View observers with the existing `StellarView` lifecycle. Avoid duplicate subscriptions and respect unregister handles.
- The current architecture is MSV. Do not introduce a Command layer or an event/command queue unless the user asks for that architectural change.

Use `Assets/StellarFramework/Samples/TankArena/CaseStudy.md` as a working example when available. It demonstrates the architecture in a real gameplay loop; it does not imply that every installed project needs all of the Kits used there.

## Select Kits and exports from the Catalog

Read `Assets/StellarFramework/KitCatalog/KitDistributionCatalog.json` in the Dev source, or the copy shipped with the installed package, before choosing package profiles. Follow each profile's `requiredProfileIds`, `requiredUpm`, source paths, and maturity. For a consumer project without the Catalog, use its `RELEASE-MANIFEST.json` and installed assembly definitions.

Keep optional capabilities optional. In particular, UIKit, LocalizationKit, UIAdaptationKit, and resource backends are separate choices. ResKit supports independent loader profiles (Resources, AssetBundle, Addressables, YooAsset), and projects may combine loaders when the installed startup code configures the intended backend. The Catalog is the source of truth; do not invent profile IDs or copy dependency lists from memory.

Before changing ToolsHub or package contents, determine whether the task targets a consumer project's installed tools or the Dev source project. Keep editor-only tools out of runtime assemblies and preserve the package export boundary.

## ResKit and hot update boundaries

Treat resource loading, resource-content updating, and code execution as separate capabilities:

- `ResKit.Core` defines resource-loader scopes and provider contracts.
- A selected loader (for example Resources, YooAsset, AssetBundle, or Addressables) loads assets through ResKit and owns its matching release behavior.
- `IResContentUpdateProvider` prepares a content version and downloads/caches resource content. The default implementation is `ResKit.ContentUpdate.YooAsset`.
- `IResCodeUpdateProvider` loads and enters code payloads. The default implementation is `ResKit.CodeUpdate.HybridCLR`.
- The application startup flow explicitly installs providers, updates content when needed, creates the appropriate `ResScope`, and then invokes the code provider. ResKit does not decide the app's prompts, retry policy, offline policy, or startup UI.

Do not refer to the removed standalone `HybridCLRKit` as the current hot-update API. Use the installed ResKit provider contracts and verify exact types/namespaces against the project version. A custom loader, content provider, or code runtime can replace one side independently, but its build/publish format must remain compatible with the other providers.

For a hot-update change, inspect the exact provider, package/platform settings, manifest and payload format, and publisher configuration. A local `file:///` Development source is useful for Editor/Windows smoke tests; an Android device cannot read a developer PC's disk path. Use a device-reachable endpoint for Android. Local-file success does not prove HTTP Range/resume, CDN, TLS, or production deployment.

See [ResKit and hot-update reference](references/reskit-hot-update.md) for the data flow and source-document map.

## Localization, UI, and asset work

- Localization: use the installed LocalizationKit API and project locale data. Test an actual language change and verify the affected text/format parameters update; do not hardcode fallback language or invent translation keys without inspecting the project's tables.
- UI adaptation: use UIAdaptationKit's configured profile, safe-area strategy, breakpoints, and layout variants. Do not replace its setup with device-name checks or one-off pixel offsets without a demonstrated project need.
- UIKit: use the project's registered UI configuration and ResKit loading path. Preserve prefab/asset ownership and loading/release symmetry.
- Sample visuals belong in project assets or prefabs when that is the project's convention. Do not replace authored UI or art with runtime-generated geometry as a shortcut.

Read the installed version's Kit documentation before changing configuration. Dev source documentation commonly lives under `Assets/StellarFramework/FrameworkDoc/02-Kits/`; the relevant entries are Reskit, LocalizationKit, UIAdaptationKit, and UIKit. For ToolsHub, use `FrameworkDoc/04-ToolsHub/`.

## Make and report changes safely

- Prefer the smallest change in the consumer project. Do not edit framework package internals to work around application code unless the user asked to change the framework itself.
- Preserve Unity `.meta` files, GUIDs, asmdef boundaries, and serialized references. Use the project's Unity skill/editor workflow for scene, prefab, asset import, and build operations.
- When changing a Kit or framework source, inspect its existing tests, policy checks, documentation, and export profile boundaries. Do not describe a consumer smoke test as full platform validation.
- Report separately what was inspected, what commands/tests were actually run, and what evidence is only recorded from an earlier run. State platform, backend, build type, and emulator/device where relevant.
- Do not infer “production-ready” from Editor tests or one Player run. Record target platform, IL2CPP/Mono, provider/backend, physical-device status, remote endpoint, and any untested failure/recovery path.

For the framework review snapshot and caveats recorded while this skill was created, see [framework reference](references/framework-reference.md). Re-check the project's current files instead of treating that snapshot as a future release guarantee.
