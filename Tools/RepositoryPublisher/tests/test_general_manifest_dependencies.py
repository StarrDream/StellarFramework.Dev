"""Tests for third-party UPM dependencies in the General release manifest."""

from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[3]
PUBLISHER_PATH = PROJECT_ROOT / "Tools/RepositoryPublisher/publish_repositories.py"
SPEC = importlib.util.spec_from_file_location("stellar_manifest_publisher", PUBLISHER_PATH)
assert SPEC is not None and SPEC.loader is not None
PUBLISHER = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = PUBLISHER
SPEC.loader.exec_module(PUBLISHER)


class GeneralManifestDependencyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.base = PUBLISHER.load_json(PROJECT_ROOT / PUBLISHER.BASE_CATALOG)
        release = PUBLISHER.load_json(PROJECT_ROOT / PUBLISHER.REPOSITORY_CATALOG)
        plan = PUBLISHER.collect_plan(PROJECT_ROOT, cls.base, release, "general")
        cls.manifest = PUBLISHER.build_general_manifest(PROJECT_ROOT, cls.base, plan)
        cls.profiles = {str(item["id"]): item for item in cls.base["profiles"]}

    def test_resource_adapters_do_not_pull_test_runner_into_runtime_projects(self) -> None:
        for profile_id in (
            "reskit.resources",
            "reskit.assetbundle",
            "reskit.addressables",
            "reskit.yooasset",
            "reskit.contentupdate.yooasset",
        ):
            with self.subTest(profile_id=profile_id):
                self.assertNotIn("com.unity.test-framework", self.profiles[profile_id]["requiredUpm"])

    def test_general_manifest_includes_hybridclr_without_test_runner_dependency(self) -> None:
        dependencies = self.manifest["dependencies"]
        self.assertNotIn("com.unity.test-framework", dependencies)
        self.assertEqual(
            dependencies.get("com.code-philosophy.hybridclr"),
            "https://github.com/focus-creative-games/hybridclr_unity.git#4feac30cb2e105992986c737f7f54992b8300e1a",
        )

    def test_extensions_release_does_not_depend_on_hybridclr(self) -> None:
        release = PUBLISHER.load_json(PROJECT_ROOT / PUBLISHER.REPOSITORY_CATALOG)
        plan = PUBLISHER.collect_plan(PROJECT_ROOT, self.base, release, "extensions")

        dependencies = PUBLISHER.required_upm_manifest(PROJECT_ROOT, self.base, plan)

        self.assertNotIn("com.code-philosophy.hybridclr", dependencies)
        self.assertIn("com.cysharp.unitask", dependencies)

    def test_extensions_release_generates_english_readme_from_dev_template(self) -> None:
        release = PUBLISHER.load_json(PROJECT_ROOT / PUBLISHER.REPOSITORY_CATALOG)
        plan = PUBLISHER.collect_plan(PROJECT_ROOT, self.base, release, "extensions")
        inputs = PUBLISHER.release_inputs(PROJECT_ROOT, plan, [])

        self.assertIn(str(PUBLISHER.EXTENSIONS_README_EN_TEMPLATE), inputs)
        with tempfile.TemporaryDirectory(prefix="stellar-extensions-readme-") as target_directory:
            final_files = PUBLISHER.write_product_files(
                PROJECT_ROOT,
                Path(target_directory),
                self.base,
                release,
                plan,
                "test-source-commit",
                "PASS",
            )
            generated_readme = Path(target_directory) / "README_EN.md"

            self.assertIn("README_EN.md", final_files)
            self.assertTrue(generated_readme.is_file())
            release_version = PUBLISHER.load_json(
                PROJECT_ROOT / PUBLISHER.REPOSITORY_CATALOG
            )["releaseVersion"]
            self.assertIn(f"Release: **{release_version}**", generated_readme.read_text(encoding="utf-8"))
            self.assertIn("test-source-commit", generated_readme.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
