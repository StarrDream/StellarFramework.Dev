# Repository Publisher

`StellarFramework.Dev` is the only development source of truth. This tool creates the two user-facing repositories from the tracked Dev working tree.

## Dry run

```text
python Tools/RepositoryPublisher/publish_repositories.py --dry-run
```

The dry run validates profile ownership, rejects General -> Extensions dependencies, reports file counts and calculates which General profiles are required by Extensions.

## Staging / release

```text
python Tools/RepositoryPublisher/publish_repositories.py ^
  --general-target <path-to-StellarFramework> ^
  --extensions-target <path-to-StellarFramework.Extensions>
```

The target `.git` directories are preserved. All other target content is replaced by deterministic release output, so the caller must validate the dry run and target remotes first.

After target-specific compile / consumer gates pass, rerun with `--validation PASS` before the release commit.

