#!/usr/bin/env python3
"""Deterministic repository publisher for StellarFramework.Dev.

The development repository is the only source of truth. This tool projects the
tracked working tree into the two user repositories without moving source inside
the Dev project. It intentionally keeps repository ownership separate from the
existing per-Kit unitypackage catalog.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Mapping, Sequence, Set, Tuple


BASE_CATALOG = Path("Assets/StellarFramework/KitCatalog/KitDistributionCatalog.json")
REPOSITORY_CATALOG = Path("Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json")
GENERAL_README_TEMPLATE = Path("Tools/RepositoryPublisher/Templates/StellarFramework.README.md")
GENERAL_README_EN_TEMPLATE = Path("Tools/RepositoryPublisher/Templates/StellarFramework.README_EN.md")
EXTENSIONS_README_TEMPLATE = Path("Tools/RepositoryPublisher/Templates/StellarFramework.Extensions.README.md")
GENERAL_GITIGNORE_TEMPLATE = Path("Tools/RepositoryPublisher/Templates/StellarFramework.gitignore")
EXTENSIONS_GITIGNORE_TEMPLATE = Path("Tools/RepositoryPublisher/Templates/StellarFramework.Extensions.gitignore")


class ReleaseError(RuntimeError):
    pass


@dataclass(frozen=True)
class ProductPlan:
    product: str
    repository: str
    profile_ids: Tuple[str, ...]
    include_roots: Tuple[str, ...]
    exclude_roots: Tuple[str, ...]
    required_general_profiles: Tuple[str, ...]
    domains: Tuple[str, ...]


def run_git(source_root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(source_root), *args],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if result.returncode != 0:
        raise ReleaseError(f"git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout.strip()


def load_json(path: Path) -> dict:
    try:
        with path.open("r", encoding="utf-8-sig") as stream:
            return json.load(stream)
    except (OSError, json.JSONDecodeError) as exc:
        raise ReleaseError(f"Unable to read JSON {path}: {exc}") from exc


def norm(path: str) -> str:
    return path.replace("\\", "/").strip("/")


def is_inside(path: str, root: str) -> bool:
    path = norm(path)
    root = norm(root)
    return path == root or path.startswith(root + "/")


def is_excluded(path: str, roots: Iterable[str]) -> bool:
    path = norm(path)
    for root in roots:
        root = norm(root)
        if is_inside(path, root) or path == root + ".meta":
            return True
    return False


def tracked_files(source_root: Path) -> Tuple[str, ...]:
    raw = subprocess.run(
        ["git", "-C", str(source_root), "ls-files", "-z"],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if raw.returncode != 0:
        raise ReleaseError(raw.stderr.decode("utf-8", "replace"))
    return tuple(
        item.decode("utf-8", "surrogateescape")
        for item in raw.stdout.split(b"\0")
        if item
    )


def profile_roots(profile: Mapping[str, object]) -> List[str]:
    roots: List[str] = []
    for key in ("sourcePaths", "documentationPaths"):
        for value in profile.get(key, []) or []:
            roots.append(str(value))
    source_root = profile.get("sourceRoot")
    if source_root:
        roots.append(str(source_root))
    return roots


def add_root_and_meta(selected: Set[str], all_files: Sequence[str], root: str) -> None:
    root = norm(root)
    for path in all_files:
        if is_inside(path, root) or path == root + ".meta":
            selected.add(path)


def resolve_dependency_closure(profile_ids: Iterable[str], profiles: Mapping[str, dict]) -> Set[str]:
    resolved: Set[str] = set()
    visiting: Set[str] = set()

    def visit(profile_id: str) -> None:
        if profile_id in resolved:
            return
        if profile_id in visiting:
            raise ReleaseError(f"Circular distribution dependency at {profile_id}")
        if profile_id not in profiles:
            raise ReleaseError(f"Unknown distribution profile {profile_id}")
        visiting.add(profile_id)
        for dependency in profiles[profile_id].get("requiredProfileIds", []) or []:
            visit(str(dependency))
        visiting.remove(profile_id)
        resolved.add(profile_id)

    for item in profile_ids:
        visit(item)
    return resolved


def validate_catalogs(base: dict, release: dict) -> Tuple[Dict[str, dict], Set[str], Dict[str, str]]:
    if release.get("schemaVersion") != 1:
        raise ReleaseError("Repository release catalog schemaVersion must be 1.")
    profiles = {str(item["id"]): item for item in base.get("profiles", [])}
    if not profiles:
        raise ReleaseError("Base distribution catalog contains no profiles.")

    owner: Dict[str, str] = {}
    general_ids = {str(item) for item in release["general"].get("profileIds", [])}
    for profile_id in general_ids:
        owner[profile_id] = "general"

    for domain in release["extensions"].get("domains", []):
        domain_id = str(domain["id"])
        for raw_profile_id in domain.get("profileIds", []):
            profile_id = str(raw_profile_id)
            if profile_id in owner:
                raise ReleaseError(f"Profile {profile_id} is assigned more than once.")
            owner[profile_id] = f"extensions.{domain_id}"

    missing = sorted(set(profiles) - set(owner))
    unknown = sorted(set(owner) - set(profiles))
    if missing or unknown:
        raise ReleaseError(f"Repository ownership mismatch. missing={missing}, unknown={unknown}")

    for profile_id in sorted(general_ids):
        closure = resolve_dependency_closure([profile_id], profiles)
        illegal = sorted(item for item in closure if owner[item].startswith("extensions."))
        if illegal:
            raise ReleaseError(
                f"General profile {profile_id} depends on extension profiles: {', '.join(illegal)}"
            )

    return profiles, general_ids, owner


def collect_plan(source_root: Path, base: dict, release: dict, product: str) -> ProductPlan:
    profiles, general_ids, owner = validate_catalogs(base, release)
    if product == "general":
        config = release["general"]
        profile_ids = tuple(str(item) for item in config.get("profileIds", []))
        include_roots = list(config.get("extraIncludePaths", []) or [])
        for profile_id in profile_ids:
            include_roots.extend(profile_roots(profiles[profile_id]))
        return ProductPlan(
            product="StellarFramework",
            repository=str(config["repository"]),
            profile_ids=tuple(sorted(profile_ids)),
            include_roots=tuple(sorted(set(map(norm, include_roots)))),
            exclude_roots=tuple(sorted(set(map(norm, config.get("excludePaths", []) or [])))),
            required_general_profiles=(),
            domains=(),
        )

    if product != "extensions":
        raise ReleaseError(f"Unknown product {product}")

    config = release["extensions"]
    profile_ids: List[str] = []
    include_roots = list(config.get("extraIncludePaths", []) or [])
    domains: List[str] = []
    required_general: Set[str] = set()
    for domain in config.get("domains", []):
        domain_id = str(domain["id"])
        domains.append(domain_id)
        domain_profiles = [str(item) for item in domain.get("profileIds", [])]
        profile_ids.extend(domain_profiles)
        include_roots.extend(domain.get("extraIncludePaths", []) or [])
        for profile_id in domain_profiles:
            include_roots.extend(profile_roots(profiles[profile_id]))
        closure = resolve_dependency_closure(domain_profiles, profiles)
        required_general.update(item for item in closure if item in general_ids)

    return ProductPlan(
        product="StellarFramework.Extensions",
        repository=str(config["repository"]),
        profile_ids=tuple(sorted(set(profile_ids))),
        include_roots=tuple(sorted(set(map(norm, include_roots)))),
        exclude_roots=tuple(sorted(set(map(norm, config.get("excludePaths", []) or [])))),
        required_general_profiles=tuple(sorted(required_general)),
        domains=tuple(domains),
    )


def collect_files(source_root: Path, plan: ProductPlan) -> Tuple[str, ...]:
    all_files = tracked_files(source_root)
    selected: Set[str] = set()
    for root in plan.include_roots:
        add_root_and_meta(selected, all_files, root)
    selected = {path for path in selected if not is_excluded(path, plan.exclude_roots)}
    return tuple(sorted(selected))


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def render_template(path: Path, values: Mapping[str, str]) -> str:
    text = path.read_text(encoding="utf-8")
    for key, value in values.items():
        text = text.replace("{{" + key + "}}", value)
    return text


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content.replace("\r\n", "\n"), encoding="utf-8", newline="\n")


def clean_target(target: Path) -> None:
    target.mkdir(parents=True, exist_ok=True)
    for child in target.iterdir():
        if child.name == ".git":
            continue
        if child.is_dir() and not child.is_symlink():
            shutil.rmtree(child)
        else:
            child.unlink()


def copy_files(source_root: Path, target: Path, files: Sequence[str]) -> None:
    for relative in files:
        source = source_root / relative
        destination = target / relative
        if not source.exists():
            raise ReleaseError(f"Selected source does not exist: {relative}")
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, destination)


def build_general_manifest(source_root: Path, base: dict, plan: ProductPlan) -> dict:
    profiles = {str(item["id"]): item for item in base["profiles"]}
    required_upm: Set[str] = set()
    for profile_id in plan.profile_ids:
        required_upm.update(str(item) for item in profiles[profile_id].get("requiredUpm", []) or [])

    source_manifest = load_json(source_root / "Packages/manifest.json")
    source_dependencies = source_manifest.get("dependencies", {})
    dependencies = {
        package_id: version
        for package_id, version in source_dependencies.items()
        if package_id.startswith("com.unity.modules.") or package_id in required_upm
    }
    missing = sorted(required_upm - set(dependencies))
    if missing:
        raise ReleaseError(f"General package manifest is missing UPM dependencies: {missing}")
    return {"dependencies": dict(sorted(dependencies.items()))}


def write_release_manifest(
    target: Path,
    plan: ProductPlan,
    source_repository: str,
    source_commit: str,
    release_version: str,
    validation: str,
    files: Sequence[str],
) -> None:
    manifest = {
        "schemaVersion": 1,
        "product": plan.product,
        "version": release_version,
        "repository": plan.repository,
        "sourceRepository": source_repository,
        "sourceCommit": source_commit,
        "profile": "General" if plan.product == "StellarFramework" else "Extensions",
        "domains": list(plan.domains),
        "profileIds": list(plan.profile_ids),
        "requiredGeneralProfileIds": list(plan.required_general_profiles),
        "validation": validation,
        "fileCount": len(files),
    }
    write_text(target / "RELEASE-MANIFEST.json", json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")


def write_product_files(
    source_root: Path,
    target: Path,
    base: dict,
    release: dict,
    plan: ProductPlan,
    source_commit: str,
    validation: str,
) -> Tuple[str, ...]:
    files = collect_files(source_root, plan)
    clean_target(target)
    copy_files(source_root, target, files)

    values = {
        "SOURCE_COMMIT": source_commit,
        "RELEASE_VERSION": str(release["releaseVersion"]),
    }
    if plan.product == "StellarFramework":
        write_text(target / "README.md", render_template(source_root / GENERAL_README_TEMPLATE, values))
        write_text(target / "README_EN.md", render_template(source_root / GENERAL_README_EN_TEMPLATE, values))
        write_text(target / ".gitignore", (source_root / GENERAL_GITIGNORE_TEMPLATE).read_text(encoding="utf-8"))
        package_manifest = build_general_manifest(source_root, base, plan)
        write_text(target / "Packages/manifest.json", json.dumps(package_manifest, indent=2) + "\n")
    else:
        write_text(target / "README.md", render_template(source_root / EXTENSIONS_README_TEMPLATE, values))
        write_text(target / ".gitignore", (source_root / EXTENSIONS_GITIGNORE_TEMPLATE).read_text(encoding="utf-8"))

    final_files = list(files) + ["README.md", ".gitignore", "RELEASE-MANIFEST.json"]
    if plan.product == "StellarFramework":
        final_files.extend(["README_EN.md", "Packages/manifest.json"])
    write_release_manifest(
        target,
        plan,
        str(release["sourceRepository"]),
        source_commit,
        str(release["releaseVersion"]),
        validation,
        tuple(sorted(set(final_files))),
    )
    return tuple(sorted(set(final_files)))


def verify_target_remote(target: Path, expected_repository: str) -> None:
    if not (target / ".git").exists():
        return
    remote = run_git(target, "remote", "get-url", "origin")
    expected_suffix = expected_repository.replace("\\", "/") + ".git"
    normalized = remote.replace("\\", "/")
    if not normalized.endswith(expected_suffix) and not normalized.endswith(expected_repository.replace("\\", "/")):
        raise ReleaseError(f"Target remote mismatch: expected {expected_repository}, got {remote}")


def verify_no_extension_assemblies_in_general(target: Path, extension_profiles: Iterable[str], base: dict) -> None:
    profiles = {str(item["id"]): item for item in base["profiles"]}
    forbidden_roots: Set[str] = set()
    for profile_id in extension_profiles:
        forbidden_roots.update(profile_roots(profiles[profile_id]))
    existing = []
    for root in forbidden_roots:
        root_path = target / norm(root)
        if root_path.exists():
            existing.append(norm(root))
    if existing:
        raise ReleaseError(f"General release leaked extension roots: {sorted(existing)}")


def dry_run_summary(source_root: Path, base: dict, release: dict) -> dict:
    general = collect_plan(source_root, base, release, "general")
    extensions = collect_plan(source_root, base, release, "extensions")
    general_files = collect_files(source_root, general)
    extension_files = collect_files(source_root, extensions)
    overlap = sorted(set(general_files) & set(extension_files))
    allowed_overlap = {
        "Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json",
        "Assets/StellarFramework/KitCatalog/RepositoryReleaseCatalog.json.meta",
    }
    unexpected_overlap = sorted(set(overlap) - allowed_overlap)
    if unexpected_overlap:
        raise ReleaseError(f"General/Extensions source overlap is not allowed: {unexpected_overlap[:20]}")
    return {
        "generalProfiles": len(general.profile_ids),
        "extensionProfiles": len(extensions.profile_ids),
        "generalFiles": len(general_files),
        "extensionFiles": len(extension_files),
        "sharedMetadataFiles": sorted(set(overlap)),
        "requiredGeneralProfilesForExtensions": list(extensions.required_general_profiles),
        "domains": list(extensions.domains),
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source-root", default=".")
    parser.add_argument("--general-target")
    parser.add_argument("--extensions-target")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--validation", default="UNVERIFIED")
    args = parser.parse_args()

    source_root = Path(args.source_root).resolve()
    base = load_json(source_root / BASE_CATALOG)
    release = load_json(source_root / REPOSITORY_CATALOG)
    source_commit = run_git(source_root, "rev-parse", "HEAD")
    summary = dry_run_summary(source_root, base, release)

    if args.dry_run or (not args.general_target and not args.extensions_target):
        print(json.dumps({"sourceCommit": source_commit, **summary}, ensure_ascii=False, indent=2))
        return 0

    general_plan = collect_plan(source_root, base, release, "general")
    extension_plan = collect_plan(source_root, base, release, "extensions")

    if args.general_target:
        target = Path(args.general_target).resolve()
        verify_target_remote(target, general_plan.repository)
        write_product_files(source_root, target, base, release, general_plan, source_commit, args.validation)
        verify_no_extension_assemblies_in_general(target, extension_plan.profile_ids, base)
    if args.extensions_target:
        target = Path(args.extensions_target).resolve()
        verify_target_remote(target, extension_plan.repository)
        write_product_files(source_root, target, base, release, extension_plan, source_commit, args.validation)

    print(json.dumps({"sourceCommit": source_commit, **summary}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ReleaseError as exc:
        print(f"REPOSITORY RELEASE ERROR: {exc}", file=sys.stderr)
        raise SystemExit(2)
