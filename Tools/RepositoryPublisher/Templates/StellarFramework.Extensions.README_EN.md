# StellarFramework.Extensions

Add optional Algorithms, World, Flow, and HotUpdate capabilities to a Unity project that already uses StellarFramework. This is an **Assets source extension release**, not a standalone Unity project.

Release: {{RELEASE_VERSION}}
Source: StellarFramework.Dev@{{SOURCE_COMMIT}}

## Start with General

Install the matching [StellarFramework General release](https://github.com/StarrDream/StellarFramework) first. Extensions use General public Kit contracts. Downloading this repository alone, opening it as a Unity project, or skipping required UPM packages leaves the project incomplete.

## Two ways to install

### Option A: Add extension source to an existing project

1. Confirm the project contains the required General Kits. This release lists them in `requiredGeneralProfileIds` in `RELEASE-MANIFEST.json`.
2. Merge the files under this repository's `Assets` folder into your game's `Assets` folder. Merge directories; do not delete or overwrite existing project files. Preserve all `.meta` files.
3. Merge the packages in `requiredUpm` from `RELEASE-MANIFEST.json` into your project's `Packages/manifest.json`. Update an existing package key instead of adding a duplicate JSON key. Do not guess package versions.
4. Return to Unity and wait for Package Manager resolution and compilation.
5. Read the selected extension guide and run your project's validation.

Use this route when you have already decided which Extensions Kits your project needs and are comfortable merging Unity assets.

### Option B: Select one Kit and export it

1. Clone the [StellarFramework General](https://github.com/StarrDream/StellarFramework) Unity project, or download its ZIP from GitHub and extract it. Open the project root in Unity.
2. Merge this repository's `Assets` files into the General project's `Assets` folder, preserving `.meta` files.
3. Add the packages from this release's `requiredUpm` to the General project's `Packages/manifest.json`, then wait for Unity to compile.
4. Open **StellarFramework → Tools Hub → Export** in the General project. Its catalog already describes extension profiles; after adding the source files you can choose one profile or its dependency closure.
5. Import the generated `.unitypackage` into your game project. For HotUpdate, follow the HybridCLR / YooAsset guides to configure the target-platform tools and content server as well.

If you need only one extension Kit, select its profile. The exporter declares framework dependencies; check the profile and guide for UPM requirements.

## Choose by goal

| Extension area | Capabilities | Start reading |
| --- | --- | --- |
| **Algorithms** | GridKit, SpatialKit, PathKit, SimulationKit, and explicit adapters | The individual guides under `Assets/StellarFramework/FrameworkDoc/02-Kits` |
| **World** | WorldKit, WorldGenKit, PlacementKit, Streaming, and World Framework tools | `Assets/StellarFramework/FrameworkDoc/06-WorldFramework` and the WorldKit / WorldGenKit guides |
| **Flow** | FlowKit Core, Unity integration, Tools Hub graph editor, and validation | `Assets/StellarFramework/FrameworkDoc/02-Kits/FlowKit` |
| **HotUpdate** | HybridCLRKit, HybridCLR tools, and HotUpdate Publisher | `Assets/StellarFramework/FrameworkDoc/02-Kits/HybridCLRKit` |

Extension Kits can be selected independently; extension domains do not create implicit hard dependencies. An adapter may still require General Kits and UPM packages documented for that adapter.

## HotUpdate requirements

HybridCLR code updates require matching Runtime code, build artifacts, and content versions. Importing a DLL by itself is not a release process:

- Build HotUpdate code, AOT metadata, Manifest, and YooAsset content for the same target platform and version.
- Android and other IL2CPP targets need HybridCLR artifacts matching the current Player.
- Verify initial install, cold download, and cache reuse after the process restarts.
- Run the target-platform Release Gate from Dev before release. See the [Dev Android verification guide](https://github.com/StarrDream/StellarFramework.Dev/blob/main/Tools/AndroidVerification/README.md) and `Assets/StellarFramework/FrameworkDoc/02-Kits/HybridCLRKit`.

## Source and support

This repository contains the extension Runtime, editor tooling, and extension usage guides needed by the published profiles. Its release manifest records the source commit, included profiles, required General profiles, exact UPM specifications, and validation status.

Make fixes and feature changes in [StellarFramework.Dev](https://github.com/StarrDream/StellarFramework.Dev). The Dev Publisher generates both General and Extensions releases from that source.
