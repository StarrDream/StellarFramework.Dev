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

    def test_resource_adapters_declare_their_nunit_package_dependency(self) -> None:
        for profile_id in ("reskit.addressables", "reskit.yooasset"):
            with self.subTest(profile_id=profile_id):
                self.assertIn("com.unity.test-framework", self.profiles[profile_id]["requiredUpm"])

    def test_general_manifest_includes_test_framework_but_omits_optional_hybridclr(self) -> None:
        dependencies = self.manifest["dependencies"]
        self.assertEqual(dependencies.get("com.unity.test-framework"), "1.1.33")
        self.assertNotIn("com.code-philosophy.hybridclr", dependencies)

    def test_extensions_release_declares_exact_hybridclr_upm_spec(self) -> None:
        release = PUBLISHER.load_json(PROJECT_ROOT / PUBLISHER.REPOSITORY_CATALOG)
        plan = PUBLISHER.collect_plan(PROJECT_ROOT, self.base, release, "extensions")

        dependencies = PUBLISHER.required_upm_manifest(PROJECT_ROOT, self.base, plan)

        self.assertIn("com.code-philosophy.hybridclr", dependencies)
        self.assertIn("hybridclr_unity.git", dependencies["com.code-philosophy.hybridclr"])
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
            self.assertIn("Release: 1.0.0", generated_readme.read_text(encoding="utf-8"))
            self.assertIn("test-source-commit", generated_readme.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
