"""Tests for the release publisher's source-to-manifest provenance guard."""

from __future__ import annotations

import importlib.util
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


PROJECT_ROOT = Path(__file__).resolve().parents[3]
PUBLISHER_PATH = PROJECT_ROOT / "Tools/RepositoryPublisher/publish_repositories.py"
SPEC = importlib.util.spec_from_file_location("stellar_repository_publisher", PUBLISHER_PATH)
assert SPEC is not None and SPEC.loader is not None
PUBLISHER = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = PUBLISHER
SPEC.loader.exec_module(PUBLISHER)


class ReleaseSourceGuardTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp = tempfile.TemporaryDirectory(prefix="stellar-release-source-guard-")
        self.root = Path(self.temp.name)
        self.git("init", "--quiet")
        self.git("config", "user.email", "publisher-tests@example.invalid")
        self.git("config", "user.name", "Publisher Tests")
        (self.root / "included.txt").write_text("committed release bytes\n", encoding="utf-8")
        (self.root / "unrelated.txt").write_text("committed docs\n", encoding="utf-8")
        self.git("add", "included.txt", "unrelated.txt")
        self.git("commit", "--quiet", "-m", "source snapshot")

    def tearDown(self) -> None:
        self.temp.cleanup()

    def git(self, *args: str) -> str:
        result = subprocess.run(
            ["git", "-C", str(self.root), *args],
            check=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            encoding="utf-8",
        )
        return result.stdout.strip()

    def test_clean_selected_source_matches_head(self) -> None:
        PUBLISHER.require_committed_sources(self.root, ["included.txt"])

    def test_unrelated_worktree_change_does_not_block_release(self) -> None:
        (self.root / "unrelated.txt").write_text("updated docs\n", encoding="utf-8")
        PUBLISHER.require_committed_sources(self.root, ["included.txt"])

    def test_unstaged_release_source_change_is_rejected(self) -> None:
        (self.root / "included.txt").write_text("uncommitted release bytes\n", encoding="utf-8")

        with self.assertRaisesRegex(PUBLISHER.ReleaseError, "included.txt"):
            PUBLISHER.require_committed_sources(self.root, ["included.txt"])

    def test_staged_release_source_change_is_rejected(self) -> None:
        (self.root / "included.txt").write_text("staged release bytes\n", encoding="utf-8")
        self.git("add", "included.txt")

        with self.assertRaisesRegex(PUBLISHER.ReleaseError, "included.txt"):
            PUBLISHER.require_committed_sources(self.root, ["included.txt"])

    def test_staged_release_source_deletion_is_rejected(self) -> None:
        plan = PUBLISHER.ProductPlan(
            product="Test",
            repository="example/test",
            profile_ids=(),
            include_roots=("included.txt",),
            exclude_roots=(),
            required_general_profiles=(),
            domains=(),
        )
        self.git("rm", "--quiet", "included.txt")

        selected_inputs = PUBLISHER.release_inputs(self.root, plan, ["unrelated.txt"])
        self.assertIn("included.txt", selected_inputs)
        with self.assertRaisesRegex(PUBLISHER.ReleaseError, "included.txt"):
            PUBLISHER.require_committed_sources(self.root, selected_inputs)


if __name__ == "__main__":
    unittest.main()
