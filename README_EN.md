# StellarFramework.Dev

The complete development workspace and single source of truth for StellarFramework.

The user-facing repositories are `StarrDream/StellarFramework` (General) and `StarrDream/StellarFramework.Extensions` (Algorithms / World / Flow / HotUpdate). Features and fixes are developed here first, validated, and then published to those repositories.

## Development model

- MSV: Model owns state, Service owns business rules and state transitions, View owns presentation and input translation.
- Adapter isolation keeps Unity, third-party SDKs, storage, networking and platform details out of Core.
- Foundation cannot depend on Extension; General Runtime cannot depend on Extensions Runtime.
- Failures remain explicit. Broad catch/fallback paths must not hide real failures.
- Hot paths prioritize low allocation and avoid unnecessary LINQ or reflection scans.

Architecture rules: `Assets/StellarFramework/FrameworkDoc/01-Architecture/KitArchitectureGuide.md`.

## Repository publishing

`KitDistributionCatalog.json` remains the Kit/dependency/export source of truth. `RepositoryReleaseCatalog.json` assigns those profiles to General or an Extensions domain. `Tools/RepositoryPublisher/publish_repositories.py` performs dry-run validation and deterministic repository projection.

```text
StellarFramework.Dev
        |\
        | \----> StellarFramework.Extensions
        |
        +------> StellarFramework
```

Run `python Tools/RepositoryPublisher/publish_repositories.py --dry-run` before any release cutover.

## Verification

Use compile, focused EditMode, FrameworkValidation, PlayMode, architecture/catalog/packaging policy, and capability-specific Clean Consumer / Player / Release gates. Automated Unity Test Runner work should prefer the project-side `stellar_test_run_safe` gate.

Validation architecture: `Assets/StellarFrameworkVerification/ValidationArchitecture.md`.
