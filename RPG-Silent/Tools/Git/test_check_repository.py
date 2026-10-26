"""用临时真实 Git 仓库验证检查器；不接触游戏工程和存档。"""

from pathlib import Path
import tempfile
import unittest

import check_repository as check


class RepositoryChecks(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        check.git(self.root, "init", "-q")
        check.git(self.root, "config", "user.name", "Repository Test")
        check.git(self.root, "config", "user.email", "test@example.invalid")
        check.git(self.root, "config", "core.autocrlf", "false")
        check.git(self.root, "-c", "core.hooksPath=", "commit", "--allow-empty", "-qm", "baseline")
        self.base = check.resolve(self.root, "HEAD")
        self.policy = check.Policy(4096, 16384, {})

    def write(self, path, data):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data.encode("utf-8") if isinstance(data, str) else data)

    def meta(self, path, folder=False):
        self.write(path + ".meta", "fileFormatVersion: 2\nguid: " + "a" * 32 +
                   ("\nfolderAsset: yes\n" if folder else "\n"))

    def asset(self, path="RPG-Silent/Assets/角色 示例.txt"):
        self.write(path, "hello\n")
        self.meta(path)
        return path

    def stage(self):
        check.git(self.root, "add", "-A")

    def commit(self):
        self.stage()
        check.git(self.root, "-c", "core.hooksPath=", "commit", "-qm", "fixture")
        return check.resolve(self.root, "HEAD")

    def audit(self):
        self.stage()
        return check.audit(self.root, check.snapshot(self.root, self.base),
                           check.snapshot(self.root, None), self.policy)

    def test_valid_asset_pair_with_unicode_and_spaces(self):
        self.asset()
        self.assertEqual([], self.audit())

    def test_missing_asset_meta_is_rejected(self):
        self.write("RPG-Silent/Assets/item.prefab", "asset")
        self.assertTrue(any("缺少" in p for p in self.audit()))

    def test_new_folder_requires_meta(self):
        self.asset("RPG-Silent/Assets/Skills/item.txt")
        self.assertTrue(any("Skills" in p and "缺少" in p for p in self.audit()))
        self.meta("RPG-Silent/Assets/Skills", folder=True)
        self.assertEqual([], self.audit())

    def test_delete_asset_must_delete_meta(self):
        path = self.asset()
        self.base = self.commit()
        (self.root / path).unlink()
        self.assertTrue(any("成对提交删除" in p for p in self.audit()))
        (self.root / (path + ".meta")).unlink()
        self.assertEqual([], self.audit())

    def test_delete_meta_while_asset_remains_is_rejected(self):
        path = self.asset()
        self.base = self.commit()
        (self.root / (path + ".meta")).unlink()
        self.assertTrue(any("缺少" in p for p in self.audit()))

    def test_empty_folder_meta_is_valid(self):
        self.meta("RPG-Silent/Assets/Empty", folder=True)
        self.assertEqual([], self.audit())

    def test_orphan_file_meta_and_invalid_guid_are_rejected(self):
        self.meta("RPG-Silent/Assets/orphan.txt")
        self.assertTrue(any("成对提交删除" in p for p in self.audit()))
        self.asset("RPG-Silent/Assets/orphan.txt")
        self.write("RPG-Silent/Assets/orphan.txt.meta", "guid: invalid\n")
        self.assertTrue(any("GUID" in p for p in self.audit()))

    def test_conflict_markers_are_rejected_but_markdown_rules_are_valid(self):
        self.write("notes.md", "title\n=======\ntext\n")
        self.assertEqual([], self.audit())
        self.write("notes.md", "<<<<<<< HEAD\nours\n=======\ntheirs\n>>>>>>> branch\n")
        self.assertTrue(any("冲突标记" in p for p in self.audit()))

    def test_binary_is_not_scanned_as_conflicted_text(self):
        self.write("texture.bin", b"\0\n<<<<<<< HEAD\n")
        self.assertEqual([], self.audit())

    def test_generated_files_are_rejected(self):
        for path in ("output/game.exe", "RPG-Silent/RPG-Silent.slnx",
                     "RPG-Silent/Library/generated.txt", "RPG-Silent/ComputeCommandBuffer.cs"):
            self.write(path, "generated")
        self.assertEqual(4, len(self.audit()))

    def test_existing_generated_file_can_be_untracked(self):
        path = "RPG-Silent/ComputeCommandBuffer.cs"
        self.write(path, "generated")
        self.base = self.commit()
        check.git(self.root, "rm", "--cached", "--", path)
        issues = check.audit(self.root, check.snapshot(self.root, self.base),
                             check.snapshot(self.root, None), self.policy)
        self.assertEqual([], issues)
        self.assertTrue((self.root / path).is_file())

    def test_addressables_outputs_in_nested_project_are_ignored_and_rejected(self):
        ignore = Path(__file__).resolve().parents[3] / ".gitignore"
        self.write(".gitignore", ignore.read_text(encoding="utf-8"))
        generated = [
            "RPG-Silent/Assets/AddressableAssetsData/Windows/addressables_content_state.bin",
            "RPG-Silent/Assets/AddressableAssetsData/Windows/addressables_content_state.bin.meta",
            "RPG-Silent/Assets/AddressableAssetsData/link.xml",
            "RPG-Silent/Assets/AddressableAssetsData/link.xml.meta",
            "RPG-Silent/Assets/StreamingAssets/aa/settings.json",
        ]
        for path in generated:
            self.write(path, "generated")
            self.assertTrue(check.generated(path), path)
            self.assertEqual(path, check.git(self.root, "check-ignore", "--", path).decode().strip())
        settings = "RPG-Silent/Assets/AddressableAssetsData/AddressableAssetSettings.asset"
        self.asset(settings)
        self.stage()
        self.assertIn(settings, check.snapshot(self.root, None))
        self.assertFalse(check.generated(settings))

    def test_large_file_exception_is_path_specific_and_bounded(self):
        self.policy.exceptions["legacy.bin"] = 8192
        self.write("legacy.bin", b"\0" * 8192)
        self.assertEqual([], self.audit())
        self.write("other.bin", b"\0" * 4097)
        self.assertTrue(any("other.bin" in p for p in self.audit()))
        self.write("legacy.bin", b"\0" * 8193)
        self.assertTrue(any("legacy.bin" in p for p in self.audit()))

    def test_hard_limit_cannot_be_bypassed_by_exception(self):
        self.policy.exceptions["legacy.bin"] = 32768
        self.write("legacy.bin", b"\0" * 16384)
        self.assertTrue(any("legacy.bin" in p for p in self.audit()))

    def test_staged_content_is_checked_instead_of_working_tree(self):
        self.write("code.txt", "<<<<<<< HEAD\n")
        self.stage()
        self.write("code.txt", "resolved but not staged\n")
        issues = check.audit(self.root, check.snapshot(self.root, self.base),
                             check.snapshot(self.root, None), self.policy)
        self.assertTrue(any("冲突标记" in p for p in issues))

    def test_commit_mode_ignores_unstaged_edits(self):
        self.write("code.txt", "clean\n")
        head = self.commit()
        self.write("code.txt", "<<<<<<< HEAD\n")
        count, issues = check.audit_range(self.root, self.base, head, self.policy)
        self.assertEqual(1, count)
        self.assertEqual([], issues)

    def test_intermediate_large_file_is_not_hidden_by_later_deletion(self):
        self.write("large.bin", b"\0" * 4097)
        self.commit()
        (self.root / "large.bin").unlink()
        head = self.commit()
        count, issues = check.audit_range(self.root, self.base, head, self.policy)
        self.assertEqual(2, count)
        self.assertTrue(any("large.bin" in p for p in issues))


if __name__ == "__main__":
    unittest.main()
