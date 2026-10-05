# ResKit hot-update flow

## Separate the three responsibilities

```text
Application startup
    │ chooses policy, address, retry, UI, and offline behavior
    ├── Content-update provider
    │     checks/prepares a content version and downloads/caches files
    ├── ResKit loader + ResScope
    │     reads assets through the selected backend and releases them
    └── Code-update provider
          reads code payloads through the app-provided scope,
          verifies/loads them, and enters the hot-update code
```

ResKit defines contracts and provider registration. It does not automatically run this sequence or infer the application's startup policy.

## Default framework combination

1. Install/register the YooAsset ResKit loader and YooAsset content-update provider.
2. Ask ResKit for `IResContentUpdateProvider<...>` and update the selected package/content version.
3. Stop or enter the application's offline policy if content preparation fails.
4. After successful update, create a `ResScope` using the intended loader.
5. Ask ResKit for `IResCodeUpdateProvider<...>` and pass that scope to the HybridCLR implementation.
6. Keep the scope alive while its payloads are needed; dispose it according to the operation's ownership contract.
7. Verify the exact Manifest, DLL, AOT metadata, platform build settings, and package version as one release unit.

The default implementation profiles are `reskit.yooasset`, `reskit.contentupdate.yooasset`, and `reskit.codeupdate.hybridclr`. Use the `hotupdate.full` recommended export when the project wants the default complete development package and corresponding tools. Read the current Catalog for dependency and UPM closure.

## Common boundary mistakes

- **YooAsset loader vs. YooAsset content update:** registering the loader makes assets readable. It does not by itself check a remote version or download content.
- **Resource hot update vs. code hot update:** updating bundles does not execute new code. HybridCLR is a separate code provider and has platform/build constraints.
- **Provider selection vs. export/build format:** changing a runtime provider does not create matching manifests, bundles, DLLs, or metadata. Build and publish tools must emit the format that the chosen loader and providers consume.
- **Local path vs. device endpoint:** `file:///` points to the current machine's filesystem. It is useful for Editor/Windows development, but an Android device cannot reach the developer PC's disk. Android needs an endpoint reachable from the device.
- **Local-file pass vs. HTTP/CDN pass:** local file loading does not verify TLS, HTTP Range, interrupted-download resume, DNS, public firewall reachability, or production credentials.
- **`HybridCLRKit` naming:** current architecture puts HybridCLR behind `ResKit.CodeUpdate.HybridCLR`; check the installed package version and source tree before using older guide names.

## Dev source guide locations

When the Dev repository is present, use the matching version of these guides:

- Architecture and provider boundaries: `Assets/StellarFramework/FrameworkDoc/01-Architecture/ResourceAndCodeUpdatePlugins.md`
- ResKit contract and loader usage: `Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/ResKit-统一资源-说明文档-Guide.md`
- HybridCLR code provider: `Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/CodeUpdate/HybridCLR/ResKit-CodeUpdate-HybridCLR-说明文档-Guide.md`
- Consumer startup workflow: `Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/CodeUpdate/HybridCLR/HotUpdate-开发规范-Guide.md`
- Publisher UI and deployment steps: `Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/CodeUpdate/HybridCLR/HotUpdate-Publisher-使用文档-Guide.md`

Always inspect the project's installed API before copying examples, as identifiers and package boundaries can change between releases.
