# StellarFramework project reference

This is a short orientation note for agents using the skill. The consuming Unity project and its installed release remain authoritative.

## Framework layout

- `StellarFramework.cs`: base MSV architecture (`Architecture<T>`, Model, Service, View).
- `Assets/StellarFramework/Runtime/Kits/`: runtime Kit source and adapters.
- `Assets/StellarFramework/Editor/StellarToolsHub/`: editor tooling; keep it outside runtime assemblies and player exports.
- `Assets/StellarFramework/KitCatalog/KitDistributionCatalog.json`: Kit/Profile IDs, dependency closure, export paths, UPM requirements, and maturity.
- `Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json`: which profiles and extra files are assembled into General and Extensions releases.
- `Assets/StellarFramework/FrameworkDoc/`: maintainer and consumer guides. The framework concepts and API docs are more reliable than an old sample snippet.
- `Assets/StellarFramework/Samples/TankArena/`: the user-facing end-to-end sample. `CaseStudy.md` explains the scenario and which Kits it actually uses.
- `Assets/StellarFramework/Tests/` and `Assets/StellarFrameworkVerification/`: maintainer verification, not consumer runtime dependencies.

## MSV access contract

`Architecture<T>` registers modules during initialization. `Service` receives the mutable Model contract through `GetModel<T>()`. A `StellarView` exposes `IReadOnlyArchitecture`; it can retrieve a View-facing interface that inherits `IReadOnlyModel`, then call a Service for behavior.

Views use `GetReadOnlyModel<T>()`; mutable `GetModel<T>()` access is provided to Services. The current `StellarFramework.cs` source uses Unity's logging API directly and has no framework Kit dependency.

A read-only interface only protects the members it exposes: do not leak writable collections or mutable nested objects through a View-facing contract.

## ResKit profile examples

Use Catalog entries, not these examples alone, to build the final dependency closure:

| Need | Typical profile IDs |
| --- | --- |
| Resources backend | `reskit.resources` |
| AssetBundle backend | `reskit.assetbundle` |
| Addressables backend | `reskit.addressables` |
| YooAsset loader | `reskit.yooasset` |
| YooAsset content update | `reskit.contentupdate.yooasset` |
| HybridCLR code update provider | `reskit.codeupdate.hybridclr` |
| Complete default hot-update package and tools | `hotupdate.full` recommended profile |

Combining resource loading with resource hot update does not imply code hot update. Combining a loader with a provider is valid only when the provider's package layout/version data is readable by that loader.

## Review snapshot: 2026-10-05

The Dev Catalog contained 86 atomic profiles: 84 Stable and 2 RC (`httpkit`, `reskit.addressables`). This is a maturity classification, not proof that every target platform has been tested. Verify the current Catalog before making a release decision.

`ProjectSettings/ProjectVersion.txt` identifies this Dev checkout as Unity `2022.3.62f3c1`; the latest validation record used that editor version.

The latest recorded hot-update evidence was a Windows x64 IL2CPP Release Player run and an Android x86_64 IL2CPP APK run on MuMu Android 12/API 32. The record reports content download, cache restart, DLL/AOT metadata checks, and HybridCLR entry passing. It explicitly says the Android device was an emulator, not a physical phone, and the 2026-10-05 run did not report hardware cutout insets. Earlier 2026-09-30 MuMu overlay verification exists, but it does not replace physical-device testing.

The validation record also states that a local test HTTP endpoint was used, not a public CDN. The General and Extensions release manifests present in this checkout point to the same Dev source commit and framework release version `1.0.3`, but each manifest labels its package validation `UNVERIFIED`. The validation record's application package label (`1.0.1`) and hot-update content version (`1.0.2`) are separate from the framework release. Keep the release-evidence caveat until package-level validation is recorded.

Do not copy these snapshot counts or results into a future project's status report. Re-open the current Catalog, manifests, and validation records first.
