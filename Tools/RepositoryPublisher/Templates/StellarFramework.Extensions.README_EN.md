# StellarFramework.Extensions

Source assets for the Algorithms, World, Flow, and HybridCLR HotUpdate capabilities in StellarFramework. This repository contains Unity <code>Assets</code> content; it is not a standalone Unity project or a UPM package.

Release: {{RELEASE_VERSION}}

Source commit: [{{SOURCE_COMMIT}}](https://github.com/StarrDream/StellarFramework.Dev/commit/{{SOURCE_COMMIT}})

Install the matching [StellarFramework General](https://github.com/StarrDream/StellarFramework) release first. The General profiles and UPM packages required by this release are listed as <code>requiredGeneralProfileIds</code> and <code>requiredUpm</code> in <code>RELEASE-MANIFEST.json</code>.

## Recommended: export selected Kits from General

1. Clone and open the matching General project.
2. Merge this repository's <code>Assets</code> content into the General project's <code>Assets</code> directory. Preserve all <code>.meta</code> files.
3. Merge the package specifications in <code>requiredUpm</code> into <code>Packages/manifest.json</code>; use the versions in the manifest.
4. Wait for Unity to resolve dependencies and compile.
5. Open **StellarFramework → Export**, select the extension profiles you need, and export them.

This workflow is suited to teams that want to add only selected extension Kits to a game project. The General catalog resolves framework profile dependencies.

## Add source to an existing Unity project

Confirm that the project contains the profiles listed in <code>requiredGeneralProfileIds</code>. Merge this repository's <code>Assets</code> content, add the packages listed in <code>requiredUpm</code>, preserve <code>.meta</code> files, and wait for Unity to compile. Configure and verify each Kit using its guide.

## Extension areas

| Area | Contents |
| --- | --- |
| Algorithms | GridKit, SpatialKit, PathKit, SimulationKit, and related adapters |
| World | WorldKit, WorldGenKit, PlacementKit, Streaming, and World Framework tools |
| Flow | FlowKit Core, Unity integration, graph editor, and validation tools |
| HotUpdate | HybridCLRKit, build tools, and HotUpdate Publisher |

## HotUpdate

The code hot-update workflow combines HybridCLR with **YooAsset**. The Addressables resource profiles are separate and are not part of this hot-update path. A target project must build the Player, HybridCLR artifacts, AOT metadata, YooAsset manifest, and content for the same platform and release version. Run the release gates for cold start, cached restart, and rollback before shipping.

The Publisher supports local-folder and S3-compatible targets. Production endpoints, credentials, TLS, and permissions are project-specific; run a Dry Run and release verification against the target environment before deployment. See <code>Assets/StellarFramework/FrameworkDoc/02-Kits/HybridCLRKit</code>.

## Guides and release contents

Kit guides are under <code>Assets/StellarFramework/FrameworkDoc/02-Kits</code>; World Framework guides are under <code>Assets/StellarFramework/FrameworkDoc/06-WorldFramework</code>. <code>RELEASE-MANIFEST.json</code> records the source commit, profiles, required General profiles, and exact UPM specifications.

This repository is generated from [StellarFramework.Dev](https://github.com/StarrDream/StellarFramework.Dev). Make source changes in the Dev project.