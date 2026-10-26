"""发布门禁回归：使用临时 Git 仓库及明确的模拟构建，不创建真实发布。"""

import json
from pathlib import Path
import subprocess
import tempfile
import unittest
import zipfile

from package_windows import digest, package


class ReleasePackageTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="rpg-release-test-")
        self.addCleanup(self.temp.cleanup)
        self.project = Path(self.temp.name).resolve()
        self.validation = self.project / "Logs/Validation/fixture"
        self.build = self.project / "Builds/Validation/fixture"
        (self.project / "ProjectSettings").mkdir()
        self.validation.mkdir(parents=True)
        self.build.mkdir(parents=True)
        (self.project / ".gitignore").write_text("Logs/\nBuilds/\n")
        (self.project / "ProjectSettings/ProjectSettings.asset").write_text("  bundleVersion: 0.1.0\n")
        (self.project / "ProjectSettings/ProjectVersion.txt").write_text("m_EditorVersion: 6000.0.66f2\n")
        self.git("init", "--quiet")
        self.git("add", ".")
        self.git("-c", "user.name=Release Test", "-c", "user.email=test@example.invalid",
                 "-c", "core.hooksPath=", "-c", "commit.gpgsign=false", "commit", "--quiet", "-m", "fixture")
        self.record = {
            "status": "Passed", "commit": self.git("rev-parse", "HEAD"), "workingTree": [],
            "unityVersion": "6000.0.66f2", "buildOutput": str(self.build),
            "tests": {"total": 27, "passed": 27, "failed": 0, "skipped": 0},
        }
        self.write_json(self.validation / "validation.json", self.record)
        self.write_json(self.validation / "player-startup.json", {
            "seconds": 30, "earlyExit": False, "startMenuLoaded": True, "errorPatternFound": False})
        (self.validation / "editmode.xml").write_text('<test-run total="27" passed="27" result="Passed"/>')
        for relative in ("RPG-Silent.exe", "UnityPlayer.dll", "RPG-Silent_Data/data",
                         "MonoBleedingEdge/mono.dll", "AddressablesState/Windows/state.bin"):
            path = self.build / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"TEST FIXTURE ONLY")
        self.write_json(self.build / "build-report.json", {
            "status": "Succeeded", "errors": 0, "version": "0.1.0", "unityVersion": "6000.0.66f2",
            "output": str(self.build / "RPG-Silent.exe"), "messages": []})
        self.write_checksums()

    def git(self, *args):
        return subprocess.check_output(["git", "-C", str(self.project), *args], encoding="utf-8").strip()

    @staticmethod
    def write_json(path, value):
        path.write_text(json.dumps(value), encoding="utf-8")

    def write_checksums(self):
        self.write_json(self.build / "sha256.json", [
            {"path": p.relative_to(self.build).as_posix(), "sha256": digest(p)}
            for p in self.build.rglob("*") if p.is_file() and p.name != "sha256.json"])

    def test_packages_verified_runtime_and_separate_evidence(self):
        output = package(self.project, self.validation)
        manifest = json.loads((output / "release.json").read_text())
        self.assertEqual(self.record["commit"], manifest["commit"])
        for archive in manifest["archives"]:
            self.assertEqual(archive["sha256"], digest(output / archive["file"]))
        with zipfile.ZipFile(output / "RPG-Silent-v0.1.0-win64.zip") as archive:
            self.assertIn("RPG-Silent.exe", archive.namelist())
            self.assertNotIn("build-report.json", archive.namelist())
            self.assertFalse(any(p.startswith("AddressablesState/") for p in archive.namelist()))
        with zipfile.ZipFile(output / "RPG-Silent-v0.1.0-verification.zip") as archive:
            self.assertIn("AddressablesState/Windows/state.bin", archive.namelist())
        with self.assertRaises(FileExistsError):
            package(self.project, self.validation)

    def test_rejects_tampered_player(self):
        (self.build / "RPG-Silent.exe").write_bytes(b"changed after validation")
        with self.assertRaisesRegex(ValueError, "产物校验失败"):
            package(self.project, self.validation)

    def test_rejects_stale_commit(self):
        self.record["commit"] = "0" * 40
        self.write_json(self.validation / "validation.json", self.record)
        with self.assertRaisesRegex(ValueError, "当前提交"):
            package(self.project, self.validation)

    def test_rejects_dirty_source(self):
        (self.project / "unexpected.txt").write_text("uncommitted")
        with self.assertRaisesRegex(ValueError, "工作区干净"):
            package(self.project, self.validation)

    def test_rejects_unclassified_runtime_file(self):
        (self.build / "new-plugin.dll").write_bytes(b"new runtime dependency")
        self.write_checksums()
        with self.assertRaisesRegex(ValueError, "尚未归类"):
            package(self.project, self.validation)


if __name__ == "__main__":
    unittest.main()
