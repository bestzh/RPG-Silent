"""使用真实 git commit 验证钩子拦截与安装安全性。"""

from pathlib import Path
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

import check_repository as check


class CommitHookTests(unittest.TestCase):
    def setUp(self):
        environment = patch.dict(os.environ, {"GIT_CONFIG_GLOBAL": os.devnull,
                                               "GIT_CONFIG_NOSYSTEM": "1"})
        environment.start()
        self.addCleanup(environment.stop)
        self.temp = tempfile.TemporaryDirectory(prefix="git hook 中文 ")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        check.git(self.root, "init", "-q")
        check.git(self.root, "config", "user.name", "Hook Test")
        check.git(self.root, "config", "user.email", "test@example.invalid")
        check.git(self.root, "config", "core.autocrlf", "false")
        # Isolate this fixture from any developer-wide hooks configuration.
        check.git(self.root, "config", "--local", "core.hooksPath", ".git/hooks")
        self.tool_dir = self.root / "RPG-Silent/Tools/Git"
        self.tool_dir.mkdir(parents=True)
        source = Path(__file__).parent
        for name in ("check_repository.py", "policy.json", "install_hooks.py"):
            shutil.copyfile(source / name, self.tool_dir / name)
        (self.tool_dir / "hooks").mkdir()
        for name in ("pre-commit", "pre-push"):
            shutil.copyfile(source / "hooks" / name, self.tool_dir / "hooks" / name)
        self.write(".gitignore", "__pycache__/\n*.pyc\n")
        check.git(self.root, "add", "-A")
        check.git(self.root, "-c", "core.hooksPath=", "commit", "-qm", "baseline")
        check.git(self.root, "config", "--unset", "core.hooksPath")
        self.base = check.resolve(self.root, "HEAD")

    def write(self, name, content):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8", newline="\n")

    def install(self, *args):
        return subprocess.run([sys.executable, str(self.tool_dir / "install_hooks.py"), *args],
                              cwd=self.tool_dir, capture_output=True)

    def commit(self):
        return subprocess.run(["git", "commit", "-qm", "change"], cwd=self.root,
                              capture_output=True)

    def test_real_commit_checks_staged_content_and_allows_fixed_commit(self):
        result = self.install()
        self.assertEqual(0, result.returncode, result.stderr)
        self.write("notes.md", "<<<<<<< HEAD\n")
        check.git(self.root, "add", "notes.md")
        self.write("notes.md", "resolved but not staged\n")
        result = self.commit()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("冲突标记".encode(), result.stdout + result.stderr)
        self.assertEqual(self.base, check.resolve(self.root, "HEAD"))
        check.git(self.root, "add", "notes.md")
        result = self.commit()
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertNotEqual(self.base, check.resolve(self.root, "HEAD"))

    def test_missing_meta_blocks_real_commit(self):
        self.assertEqual(0, self.install().returncode)
        self.write("RPG-Silent/Assets/角色.txt", "asset\n")
        check.git(self.root, "add", "-A")
        result = self.commit()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("缺少已跟踪的 .meta".encode(), result.stdout + result.stderr)
        self.assertEqual(self.base, check.resolve(self.root, "HEAD"))

    def test_existing_hook_is_preserved(self):
        path = ".git/hooks/pre-push"
        self.write(path, "#!/bin/sh\nexit 0\n")
        original = (self.root / path).read_bytes()
        self.assertNotEqual(0, self.install().returncode)
        self.assertEqual(original, (self.root / path).read_bytes())
        result = subprocess.run(["git", "config", "--local", "--get", "core.hooksPath"],
                                cwd=self.root, capture_output=True)
        self.assertEqual(1, result.returncode)

    def test_custom_hooks_path_is_preserved(self):
        check.git(self.root, "config", "core.hooksPath", "custom hooks")
        self.assertNotEqual(0, self.install().returncode)
        self.assertEqual(b"custom hooks\n", check.git(self.root, "config", "--get", "core.hooksPath"))

    def test_install_is_repeatable_and_can_be_uninstalled(self):
        self.assertEqual(0, self.install().returncode)
        self.assertEqual(0, self.install().returncode)
        self.assertEqual(0, self.install("--uninstall").returncode)
        self.assertTrue((self.tool_dir / "hooks/pre-commit").is_file())
        check.git(self.root, "config", "core.hooksPath", "custom hooks")
        self.assertNotEqual(0, self.install("--uninstall").returncode)
        self.assertEqual(b"custom hooks\n", check.git(self.root, "config", "--get", "core.hooksPath"))

    def test_missing_python_fails_instead_of_skipping_checks(self):
        self.assertEqual(0, self.install().returncode)
        check.git(self.root, "config", "rpg-silent.hooksPython", str(self.root / "missing-python"))
        self.write("notes.md", "valid\n")
        check.git(self.root, "add", "notes.md")
        result = self.commit()
        self.assertNotEqual(0, result.returncode)
        self.assertIn("未找到 Python".encode(), result.stderr)
        self.assertEqual(self.base, check.resolve(self.root, "HEAD"))

    def test_lfs_push_transfers_objects_for_a_fresh_clone(self):
        self.assertEqual(0, self.install().returncode)
        self.write(".gitattributes", "*.dat filter=lfs diff=lfs merge=lfs -text\n")
        payload = b"first-party LFS transfer fixture\x00" * 128
        (self.root / "fixture.dat").write_bytes(payload)
        check.git(self.root, "add", "-A")
        self.assertEqual(0, self.commit().returncode)
        pointer = check.git(self.root, "show", "HEAD:fixture.dat")
        self.assertTrue(pointer.startswith(b"version https://git-lfs.github.com/spec/v1\n"))
        with tempfile.TemporaryDirectory(prefix="lfs remote ") as directory:
            remote = Path(directory) / "remote.git"
            clone = Path(directory) / "fresh clone"
            check.git(self.root, "init", "--bare", str(remote))
            check.git(self.root, "remote", "add", "origin", remote.as_uri())
            # Only the actual pre-push hook uploads the LFS object to this remote.
            check.git(self.root, "push", "origin", "HEAD:refs/heads/main")
            check.git(self.root, "clone", "--branch", "main", remote.as_uri(), str(clone))
            check.git(clone, "lfs", "install", "--local", "--skip-repo")
            check.git(clone, "lfs", "pull")
            check.git(clone, "lfs", "fsck")
            self.assertEqual(payload, (clone / "fixture.dat").read_bytes())


if __name__ == "__main__":
    unittest.main()
