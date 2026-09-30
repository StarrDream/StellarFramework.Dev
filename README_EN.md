# StellarFramework.Dev

The Unity development project for StellarFramework maintainers. Runtime, Editor, Tools Hub, the Kit catalog, validation tools, and release scripts are maintained here. The General and Extensions user repositories are generated from this source.

## Requirements

- Unity Editor **2022.3.62f3c1** (the project version)
- Python 3 for the repository publisher and related checks
- Android SDK and ADB for Android verification; see the verification guide for supported emulators and devices

## Open the project

Clone this repository, add its directory in Unity Hub, and open it. After Package Manager resolves the project dependencies, open **StellarFramework → Tools Hub**. The Start Here page links to the project entry points and sample.

## Project layout

| Path | Contents |
| --- | --- |
| <code>Assets/StellarFramework/Runtime/Kits</code> | Runtime Kits and adapters |
| <code>Assets/StellarFramework/Editor</code> | Editor tools and Tools Hub modules |
| <code>Assets/StellarFramework/FrameworkDoc</code> | User guides for Kits and tools |
| <code>Assets/StellarFramework/Samples</code> | Onboarding and focused samples |
| <code>Assets/StellarFramework/Tests</code> | EditMode, PlayMode, and framework validation tests |
| <code>Assets/StellarFrameworkVerification</code> | Validation project and release gates |
| <code>Assets/StellarFramework/KitCatalog</code> | Kit export and repository ownership data |
| <code>Tools/RepositoryPublisher</code> | General and Extensions publisher |
| <code>Tools/AndroidVerification</code> | Android build and device verification scripts |

## Development workflow

Before changing a capability, define its public API, dependency direction, and Unity or platform boundaries. Update the Kit catalog, user guide, and export configuration along with the Runtime code and adapters. Add focused tests for the behavior being changed.

Before committing, run Unity compilation and the relevant EditMode / PlayMode, catalog, and export checks. Changes involving a Player, resource loading, or HotUpdate also need a clean consumer build or the relevant target-platform gate. See the [validation architecture](Assets/StellarFrameworkVerification/ValidationArchitecture.md) and [current validation status](Assets/StellarFramework/FrameworkDoc/08-Validation/ValidationCurrentStatus.md).

## Publish the user repositories

This is the only source for General and Extensions. Commit the intended source and publisher templates, then review the release plan:

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
~~~

After the required gates pass, generate the local release repositories:

~~~powershell
python Tools/RepositoryPublisher/publish_repositories.py --general-target C:\GitProject\StellarFramework --extensions-target C:\GitProject\StellarFramework.Extensions --validation PASS
~~~

The publisher replaces all target content except <code>.git</code>. Check the target paths and remotes first, and save or clear local changes in those repositories. After generation, verify each <code>RELEASE-MANIFEST.json</code> source commit, run the static release-tree and consumer-project checks, then commit the release repositories.

## For framework users

Start with [StellarFramework General](https://github.com/StarrDream/StellarFramework). Add [StellarFramework.Extensions](https://github.com/StarrDream/StellarFramework.Extensions) for Algorithms, World, Flow, or HybridCLR HotUpdate. User documentation is under <code>Assets/StellarFramework/FrameworkDoc</code>.