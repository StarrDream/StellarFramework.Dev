#!/usr/bin/env python3
"""Inspect exported installers and their embedded payloads without extracting tar paths."""
from __future__ import annotations

import argparse
import io
import json
import re
import tarfile
from pathlib import Path


def assets(data: bytes) -> dict[str, bytes]:
    result: dict[str, bytes] = {}
    with tarfile.open(fileobj=io.BytesIO(data), mode="r:gz") as archive:
        members = {member.name: member for member in archive.getmembers() if member.isfile()}
        for name, member in members.items():
            if not name.endswith("/pathname"):
                continue
            stream = archive.extractfile(member)
            assert stream is not None
            path = stream.read().decode("utf-8-sig").strip().replace("\\", "/")
            if not path.startswith("Assets/") or ".." in path.split("/"):
                raise ValueError(f"Unexpected Unity asset path: {path}")
            content = archive.extractfile(members[name.removesuffix("pathname") + "asset"])
            assert content is not None
            result[path] = content.read()
    return result


def validate(package: Path) -> dict:
    wrapper = assets(package.read_bytes())
    requests = [json.loads(data.decode("utf-8-sig")) for path, data in wrapper.items()
                if Path(path).name.startswith("__StellarFramework-KitBootstrap-") and path.endswith(".json")]
    if len(requests) != 1:
        raise ValueError(f"Expected one installer request in {package.name}")
    request = requests[0]
    dependencies = {value["packageId"] for value in request["dependencies"]}
    if "com.unity.test-framework" in dependencies:
        raise ValueError(f"Test Runner dependency leaked into {package.name}")
    if dependencies.intersection({"com.tuyoogame.yooasset", "com.unity.addressables"}) and "com.unity.ext.nunit" not in dependencies:
        raise ValueError(f"Missing third-party Editor NUnit dependency in {package.name}")
    ordered = [dependency["packageId"] for dependency in request["dependencies"]]
    for backend in ("com.tuyoogame.yooasset", "com.unity.addressables"):
        if backend in ordered and ordered.index("com.unity.ext.nunit") > ordered.index(backend):
            raise ValueError(f"Editor prerequisite installed after backend: {package.name}: {backend}")
    payload = assets(wrapper[request["payloadAssetPath"]])
    for expected in request["expectedAssetPaths"]:
        if expected not in payload:
            raise ValueError(f"Installer verification path missing: {expected}")
    for path, data in payload.items():
        if any(token in path for token in ("/Tests/", "StellarFrameworkVerification/", "HotUpdatePublisherConsumerE2E/")):
            raise ValueError(f"Development content leaked: {package.name}: {path}")
        if path.endswith(".cs") and re.search(rb'^\s*using\s+(NUnit|UnityEngine\.TestTools|UnityEditor\.TestTools)\b', data, re.MULTILINE):
            raise ValueError(f"Test code leaked: {package.name}: {path}")
        if path.endswith(".dll.bytes") and ("TestRunner" in path or ".Tests" in path or "nunit" in path.lower()):
            raise ValueError(f"Test DLL leaked: {package.name}: {path}")
        if path.endswith("/AssetMap/AssetsMap.cs"):
            for key in re.findall(rb'public const string @?\w+ = "(Assets/[^"\r\n]+)";', data):
                if key.decode("utf-8") not in payload:
                    raise ValueError(f"Unshipped generated key: {package.name}: {key.decode('utf-8')}")
    return {"package": package.name, "status": "PASS", "assets": len(payload), "dependencies": sorted(dependencies)}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--export-report", required=True, type=Path)
    parser.add_argument("--result", required=True, type=Path)
    args = parser.parse_args()
    report = json.loads(args.export_report.read_text(encoding="utf-8-sig"))
    if report["status"] != "PASS":
        raise ValueError("Export matrix did not complete successfully.")
    results = [validate(Path(package["path"])) for package in report["packages"]]
    args.result.parent.mkdir(parents=True, exist_ok=True)
    args.result.write_text(json.dumps({"status": "PASS", "count": len(results), "packages": results}, indent=2) + "\n", encoding="utf-8")
    print(f"PASS: {len(results)} installers and embedded payloads inspected")


if __name__ == "__main__":
    main()
