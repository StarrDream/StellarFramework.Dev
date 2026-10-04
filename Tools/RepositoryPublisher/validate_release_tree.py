#!/usr/bin/env python3
"""Static release-tree validation for StellarFramework repository projections."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Set, Tuple


FORBIDDEN_GENERAL_ASSEMBLY_TOKENS = (
    "StellarFramework.GridKit",
    "StellarFramework.PathKit",
    "StellarFramework.SpatialKit",
    "StellarFramework.SimulationKit",
    "StellarFramework.WorldKit",
    "StellarFramework.WorldGenKit",
    "StellarFramework.PlacementKit",
    "StellarFramework.FlowKit",
    "StellarFramework.ToolsHub.WorldFramework",
)

FORBIDDEN_EXTENSIONS_ASSEMBLY_TOKENS = (
    "StellarFramework.HybridCLRKit",
    "StellarFramework.ToolsHub.HybridCLRKit",
    "StellarFramework.ToolsHub.HotUpdatePublisher",
)

MAINTAINER_ONLY_GENERAL_PATHS = (
    "Assets/docs",
    "Assets/StellarFramework/Tests",
    "Assets/StellarFrameworkVerification",
    "Assets/StellarFramework/Editor/Verification",
    "Assets/StellarFramework/Editor/StellarToolsHub/Modules/DevTools",
    "Assets/StellarFramework/FrameworkDoc/09-Development",
    "Assets/GameHotUpdate",
    "Assets/HotUpdatePublisherConsumerE2E",
    "Tools/AndroidVerification",
)


class ValidationError(RuntimeError):
    pass


def load_json(path: Path) -> dict:
    try:
        with path.open("r", encoding="utf-8-sig") as stream:
            return json.load(stream)
    except (OSError, json.JSONDecodeError) as exc:
        raise ValidationError(f"Unable to read JSON {path}: {exc}") from exc


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValidationError(message)


def iter_asmdefs(root: Path) -> Iterable[Path]:
    assets = root / "Assets"
    if not assets.exists():
        return ()
    return assets.rglob("*.asmdef")


def read_asmdefs(root: Path) -> Dict[str, Tuple[Path, Sequence[str]]]:
    result: Dict[str, Tuple[Path, Sequence[str]]] = {}
    for path in iter_asmdefs(root):
        document = load_json(path)
        name = str(document.get("name", "")).strip()
        require(bool(name), f"Assembly definition has no name: {path}")
        require(name not in result, f"Duplicate asmdef name '{name}': {result.get(name)} and {path}")
        references = tuple(str(item) for item in (document.get("references", []) or []))
        result[name] = (path, references)
    return result


def local_stellar_reference(reference: str) -> bool:
    return not reference.startswith("GUID:") and (
        reference == "StellarFramework" or reference.startswith("StellarFramework.")
    )


def validate_local_asmdef_closure(root: Path) -> int:
    assemblies = read_asmdefs(root)
    unresolved: List[str] = []
    for assembly_name, (path, references) in assemblies.items():
        for reference in references:
            if local_stellar_reference(reference) and reference not in assemblies:
                unresolved.append(f"{assembly_name} -> {reference} ({path})")
    require(not unresolved, "Unresolved local StellarFramework asmdef references: " + "; ".join(unresolved))
    return len(assemblies)


def validate_meta_completeness(root: Path) -> Tuple[int, int]:
    assets = root / "Assets"
    if not assets.exists():
        return 0, 0
    missing: List[str] = []
    file_count = 0
    directory_count = 0
    for path in assets.rglob("*"):
        if path.name.endswith(".meta"):
            continue
        relative = path.relative_to(root).as_posix()
        if path.is_dir():
            directory_count += 1
        else:
            file_count += 1
        meta = Path(str(path) + ".meta")
        if not meta.exists():
            missing.append(relative)
    require(not missing, "Assets missing .meta files: " + ", ".join(missing[:30]))
    return file_count, directory_count


def validate_release_manifest(root: Path, product: str, source_commit: str | None) -> dict:
    path = root / "RELEASE-MANIFEST.json"
    require(path.exists(), f"Release manifest missing: {path}")
    manifest = load_json(path)
    require(manifest.get("schemaVersion") == 1, f"Unsupported release manifest schema in {path}")
    require(manifest.get("product") == product, f"Unexpected release product in {path}")
    require(manifest.get("sourceRepository") == "StarrDream/StellarFramework.Dev",
            f"Unexpected sourceRepository in {path}")
    if source_commit:
        require(manifest.get("sourceCommit") == source_commit,
                f"Release manifest does not match requested source commit {source_commit}")
    return manifest


def validate_required_upm(manifest: dict) -> Dict[str, str]:
    dependencies = manifest.get("requiredUpm")
    require(isinstance(dependencies, dict), "Release manifest must declare exact requiredUpm package specs.")
    require(
        all(isinstance(name, str) and name.strip() and isinstance(spec, str) and spec.strip()
            for name, spec in dependencies.items()),
        "Release manifest requiredUpm must map package ids to non-empty package specs.",
    )
    return dependencies


def validate_general(root: Path, source_commit: str | None) -> dict:
    require(root.exists(), f"General root not found: {root}")
    manifest = validate_release_manifest(root, "StellarFramework", source_commit)
    for relative in MAINTAINER_ONLY_GENERAL_PATHS:
        require(not (root / relative).exists(), f"Maintainer-only path leaked into General: {relative}")

    packages = load_json(root / "Packages/manifest.json")
    dependency_specs = packages.get("dependencies") or {}
    dependencies = set(dependency_specs)
    required_upm = validate_required_upm(manifest)
    profile_ids = set(manifest.get("profileIds") or [])
    require({"hybridclrkit", "hybridclrkit.tools"}.issubset(profile_ids),
            "General release must include HybridCLRKit and its Tools profile.")
    require("com.code-philosophy.hybridclr" in required_upm,
            "General release must declare the HybridCLR UPM dependency.")
    require(set(required_upm).issubset(dependencies),
            "General RELEASE-MANIFEST requiredUpm packages are missing from Packages/manifest.json.")
    require(all(dependency_specs.get(package_id) == spec for package_id, spec in required_upm.items()),
            "General RELEASE-MANIFEST requiredUpm specs differ from Packages/manifest.json.")
    require("com.besty.unity-skills" not in dependencies, "UnitySkills leaked into General Packages manifest.")
    for relative in (
        "Assets/StellarFramework/Runtime/Kits/HybridCLRKit/StellarFramework.HybridCLRKit.asmdef",
        "Assets/StellarFramework/Editor/StellarToolsHub/Modules/HybridCLRKit",
        "Assets/StellarFramework/Editor/StellarToolsHub/Modules/HotUpdatePublisher",
        "ProjectSettings/HybridCLRSettings.asset",
    ):
        require((root / relative).exists(), f"HybridCLR core release content missing from General: {relative}")

    assemblies = read_asmdefs(root)
    forbidden = sorted(
        name for name in assemblies
        if any(token in name for token in FORBIDDEN_GENERAL_ASSEMBLY_TOKENS)
    )
    require(not forbidden, "Extension assemblies leaked into General: " + ", ".join(forbidden))
    assembly_count = validate_local_asmdef_closure(root)
    asset_files, asset_dirs = validate_meta_completeness(root)
    return {
        "profile": manifest.get("profile"),
        "assemblies": assembly_count,
        "assetFiles": asset_files,
        "assetDirectories": asset_dirs,
        "packageDependencies": len(dependencies),
    }


def validate_extensions(root: Path, source_commit: str | None) -> dict:
    require(root.exists(), f"Extensions root not found: {root}")
    manifest = validate_release_manifest(root, "StellarFramework.Extensions", source_commit)
    require(not (root / "Packages").exists(), "Extensions must not be published as a standalone Unity project.")
    require(not (root / "ProjectSettings").exists(),
            "Extensions must not carry standalone ProjectSettings.")
    for relative in (
        "Assets/StellarFramework/Tests",
        "Assets/StellarFrameworkVerification",
        "Assets/GameHotUpdate",
        "Assets/HotUpdatePublisherConsumerE2E",
    ):
        require(not (root / relative).exists(), f"Maintainer-only path leaked into Extensions: {relative}")
    require("hotupdate" not in (manifest.get("domains") or []),
            "HotUpdate must not be published as an Extensions domain.")
    require(not ({"hybridclrkit", "hybridclrkit.tools"} & set(manifest.get("profileIds") or [])),
            "HybridCLR profiles must not be published in Extensions.")
    assemblies = read_asmdefs(root)
    require(bool(assemblies), "Extensions contains no asmdefs.")
    forbidden = sorted(
        name for name in assemblies
        if any(token in name for token in FORBIDDEN_EXTENSIONS_ASSEMBLY_TOKENS)
    )
    require(not forbidden, "HotUpdate assemblies leaked into Extensions: " + ", ".join(forbidden))
    asset_files, asset_dirs = validate_meta_completeness(root)
    required_upm = validate_required_upm(manifest)
    require(bool(required_upm), "Extensions release manifest must declare UPM dependencies.")
    require("com.code-philosophy.hybridclr" not in required_upm,
            "HybridCLR UPM dependency must not be declared by Extensions.")
    required_general = manifest.get("requiredGeneralProfileIds") or []
    require(bool(required_general), "Extensions manifest must declare General dependencies.")
    return {
        "assemblies": len(assemblies),
        "assetFiles": asset_files,
        "assetDirectories": asset_dirs,
        "domains": manifest.get("domains") or [],
        "requiredGeneralProfiles": required_general,
        "requiredUpm": required_upm,
    }


def validate_composed(root: Path) -> dict:
    require(root.exists(), f"Composed root not found: {root}")
    assembly_count = validate_local_asmdef_closure(root)
    asset_files, asset_dirs = validate_meta_completeness(root)
    return {
        "assemblies": assembly_count,
        "assetFiles": asset_files,
        "assetDirectories": asset_dirs,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--general-root")
    parser.add_argument("--extensions-root")
    parser.add_argument("--composed-root")
    parser.add_argument("--source-commit")
    args = parser.parse_args()
    require(args.general_root or args.extensions_root or args.composed_root,
            "At least one release root must be supplied.")

    report = {}
    if args.general_root:
        report["general"] = validate_general(Path(args.general_root).resolve(), args.source_commit)
    if args.extensions_root:
        report["extensions"] = validate_extensions(Path(args.extensions_root).resolve(), args.source_commit)
    if args.composed_root:
        report["composed"] = validate_composed(Path(args.composed_root).resolve())

    print(json.dumps({"status": "PASS", **report}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ValidationError as exc:
        print(f"RELEASE TREE VALIDATION FAILED: {exc}", file=sys.stderr)
        raise SystemExit(2)
