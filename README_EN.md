# StellarFramework.Dev

**This repository is for framework developers and maintainers.** It is the complete Unity development project and the only source for user-facing releases. If you only want to use the framework, start with [StellarFramework General](https://github.com/StarrDream/StellarFramework). Add [StellarFramework.Extensions](https://github.com/StarrDream/StellarFramework.Extensions) for Algorithms, World, Flow, or HotUpdate.

## Three repository roles

| Repository | Audience | Purpose |
| --- | --- | --- |
| StellarFramework.Dev | Framework maintainers | Change Runtime / Editor / Tools Hub, run full validation, generate both user releases |
| StellarFramework | Unity users | Open the General project, run the onboarding demo, export selected general Kits |
| StellarFramework.Extensions | Unity users | Add Algorithms / World / Flow / HotUpdate; requires the matching General release |

**Develop and fix features in Dev. Dev Publisher generates the user repositories; do not maintain parallel feature implementations in those repositories.**

## Open the development project

1. Install **Unity 2022.3.62f3c1** through Unity Hub.
2. In Unity Hub, choose **Add → Add project from disk** and select this repository.
3. Wait for asset import and Package Manager resolution. Open **StellarFramework → Tools Hub → Start Here → Quick Start**.
4. Run ArchitectureDemo before making changes to confirm that the project opens correctly in your Unity environment.

Dev contains every Kit source, Tools Hub, distribution catalog, Samples, Tests, and maintainer verification tools. Do not copy release directories back into Dev or maintain the same feature source in multiple repositories.

## Where to work

- Runtime Kits: `Assets/StellarFramework/Runtime/Kits`.
- Editor and Tools Hub: `Assets/StellarFramework/Editor`.
- User Kit guides: `Assets/StellarFramework/FrameworkDoc/02-Kits`.
- Tools Hub guides: `Assets/StellarFramework/FrameworkDoc/04-ToolsHub`.
- User onboarding demo: `Assets/StellarFramework/Samples/ArchitectureDemo`.
- Automated regression tests: `Assets/StellarFramework/Tests`.
- Release verification: `Assets/StellarFrameworkVerification` and `Tools/Verification`.
- Selectable Kit export source of truth: `Assets/StellarFramework/KitCatalog/KitDistributionCatalog.json`.
- General / Extensions ownership: `Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json`.

The current catalog defines 84 selectable distribution Profiles and 5 Recommended Profiles. A Profile may represent a Runtime Kit, adapter, editor tooling, or single-file export. The exporter adds declared framework dependencies. When adding a capability, review its Profile, dependency closure, UPM declarations, user guide, and export validation—not just compilation.

## Completion checklist for a Kit or feature

1. Define its responsibility, public contract, dependency direction, and platform conditions. Foundation / General Runtime must not depend on Extensions.
2. Keep Unity, third-party SDKs, and resource backends behind clear adapters. Follow MSV: Model owns state, Service owns rules, View translates presentation and input.
3. Write a user guide with a minimal usage path, dependencies, failure behavior, setup, and troubleshooting.
4. Register the capability in the catalog and verify that a single Kit Profile can be selected. Add a Recommended Profile only when it has a clear use case.
5. Add useful Editor / Tools Hub workflows without making Runtime depend on Editor assemblies.
6. Add focused EditMode / PlayMode / FrameworkValidation coverage, including failure paths and dependency boundaries.
7. Compile a clean consumer export. Resource, HotUpdate, and platform-specific work also needs its corresponding Player / Android Release Gate.

## Validation sequence

Scale the checks with the change:

1. Confirm Unity compilation and review new Console errors or warnings.
2. Run focused EditMode and FrameworkValidation coverage.
3. Run affected PlayMode tests for async lifetime, scenes, and real runtime behavior.
4. Run Architecture / Catalog / Packaging Policy checks for boundaries and exportability.
5. Compile the release combination in a clean consumer project. Add a Player / Release Gate for build, resource, or HotUpdate changes.

See [Validation Architecture](Assets/StellarFrameworkVerification/ValidationArchitecture.md) and [current validation summary](Assets/StellarFramework/FrameworkDoc/08-Validation/ValidationCurrentStatus.md). Prefer the project-side `stellar_test_run_safe` gate for automated Unity Test Runner work to avoid Refresh / Compile races.

### Android and HotUpdate

The Android Release Gate uses an Android API 35 virtual device to install a Release APK, verify first launch and process restart, and check HotUpdate cold download and cached restart behavior. Prepare Android SDK, the API 35 AVD, and hardware virtualization as described in the [Android verification guide](Tools/AndroidVerification/README.md), then run from the repository root:

~~~powershell
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Test-StellarAndroidEnvironment.ps1
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1
powershell -ExecutionPolicy Bypass -File .\Tools\AndroidVerification\Invoke-StellarAndroidReleaseVerification.ps1 -HotUpdate
~~~

Results and screenshots are written to `Tools/AndroidVerification/Results`. This is an emulator smoke / release check. Real-device GPU, ARM64 native plugins, vendor ROMs, XR / PICO, and hardware sensors still require the corresponding devices.

## Generate the user releases from Dev

Commit selected source and publisher templates first, then preview:

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
~~~

Review General / Extensions profile ownership, file sets, and dependencies. After the required validation passes, generate the local release projects:

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --general-target C:\GitProject\StellarFramework --extensions-target C:\GitProject\StellarFramework.Extensions --validation PASS
~~~

**Publishing recreates all target content except .git.** Verify the target directories and remotes first; do not keep unsaved work in a release repository. After publishing, confirm both `RELEASE-MANIFEST.json` files point to the current Dev HEAD, run consumer / static release validation, then commit General and Extensions. Remote pushes and release tags require their own approval and version decision.

See `Tools/RepositoryPublisher/README.md` for publisher behavior and source-provenance checks.