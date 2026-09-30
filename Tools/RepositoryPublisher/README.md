# Repository Publisher

`StellarFramework.Dev` is the only development source of truth. This tool creates the two user-facing repositories from the tracked Dev working tree.

## Dry run

```text
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
```

The dry run validates profile ownership, rejects General -> Extensions dependencies, reports file counts and calculates which General profiles are required by Extensions.

The publisher renders user-facing documentation from the Dev templates under `Templates/`. Both release repositories include Chinese and English READMEs; update the templates here so release documentation stays generated from the same source as the code. The target manifests also retain the source commit and the release's validation status.

## Staging / release

```text
python Tools/RepositoryPublisher/publish_repositories.py ^
  --general-target <path-to-StellarFramework> ^
  --extensions-target <path-to-StellarFramework.Extensions>
```

The target `.git` directories are preserved. All other target content is replaced by deterministic release output, so the caller must validate the dry run and target remotes first.

Before writing to either target, the publisher verifies that every selected source file and every generator input (catalogs, package manifest, publisher code, and templates) matches `HEAD`, including selected files deleted from the current index. It fails closed if one of those release inputs has staged or unstaged changes. Commit the intended change to `StellarFramework.Dev` first so `RELEASE-MANIFEST.json`'s `sourceCommit` identifies the exact bytes used to build the release. Unrelated working-tree changes do not block publication.

After target-specific compile / consumer gates pass, rerun with `--validation PASS` before the release commit.

The source provenance guard has standard-library unit tests:

```text
python -m unittest discover -s Tools/RepositoryPublisher/tests -v
```

## Static release validation

Validate a generated General / Extensions pair with:

```text
python Tools/RepositoryPublisher/validate_release_tree.py ^
  --general-root <general-staging> ^
  --extensions-root <extensions-staging> ^
  --source-commit <dev-sha>
```

For a composed consumer tree (General plus Extensions overlay), add `--composed-root`. The validator rejects unresolved local `StellarFramework.*` asmdef references, missing Unity `.meta` files, extension assemblies in General, maintainer-only payload leakage, and standalone Unity-project metadata in Extensions.

