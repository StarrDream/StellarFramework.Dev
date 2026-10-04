# Hot Update Verification Payload

`Assets/GameHotUpdate` contains files prepared for the repository's Android and Windows hot-update verification pipeline. It is not a second game sample and should not be edited as the Tank Arena source of truth.

## Tank Arena sample source

The playable framework demo and its hot-update assembly source are maintained under:

```text
Assets/StellarFramework/Samples/TankArena/Runtime/
```

The assembly name remains `HotUpdate`; `HotUpdateMain.Main()` is the runtime entry point. Android and Windows verification builders compile that source, generate the platform-specific YooAsset package, and prepare the matching Player payload.

## Generated / prepared directories

- `Code/` — compiled hot-update DLL payloads consumed by package preparation.
- `Manifest/` — YooAsset package manifest and version metadata.
- `Metadata/` — AOT assembly metadata prepared for HybridCLR.

Treat these directories as build outputs. Regenerate them through **Tools > StellarFramework > Verification** before verifying a source change. The Editor's direct-scene preview uses the local `HotUpdate` assembly and does not exercise package download or remote delivery.

## Documentation

- Playable demo and controls: `Assets/StellarFramework/Samples/TankArena/README.md`
- HybridCLR setup and project integration: `Assets/StellarFramework/FrameworkDoc/02-Kits/Reskit/CodeUpdate/HybridCLR`
- Android / Windows release gates: `Assets/StellarFrameworkVerification`
- Android device automation: `Tools/AndroidVerification`
