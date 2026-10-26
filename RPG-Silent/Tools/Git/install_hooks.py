#!/usr/bin/env python3
"""为当前克隆安装提交检查；保留已有的第三方钩子配置。"""

import argparse
from pathlib import Path
import subprocess
import sys

HOOKS_PATH = "RPG-Silent/Tools/Git/hooks"
PYTHON_KEY = "rpg-silent.hooksPython"


def git(root, *args, missing_ok=False):
    result = subprocess.run(["git", "-C", str(root), *args],
                            capture_output=True, text=True, encoding="utf-8")
    if result.returncode and not (missing_ok and result.returncode == 1):
        raise RuntimeError(result.stderr.strip() or "Git 配置操作失败")
    return result.stdout.strip() if result.returncode == 0 else None


def configure(root, uninstall=False):
    configured = git(root, "config", "--get", "core.hooksPath", missing_ok=True)
    local = git(root, "config", "--local", "--get", "core.hooksPath", missing_ok=True)
    if uninstall:
        if local != HOOKS_PATH:
            raise RuntimeError("当前克隆没有启用本工具的钩子，未修改其他配置。")
        git(root, "config", "--local", "--unset-all", "core.hooksPath")
        git(root, "config", "--local", "--unset-all", PYTHON_KEY, missing_ok=True)
        print("已移除本工具的本地配置，仓库中的钩子文件保留。")
        return

    if configured is not None and configured != HOOKS_PATH:
        raise RuntimeError("已有 core.hooksPath 配置，未覆盖；请先协调现有钩子：" + configured)
    if configured is None:
        hook_dir = Path(git(root, "rev-parse", "--git-path", "hooks"))
        if not hook_dir.is_absolute():
            hook_dir = root / hook_dir
        existing = [p.name for p in hook_dir.iterdir()
                    if p.is_file() and not p.name.endswith(".sample")] if hook_dir.exists() else []
        if existing:
            raise RuntimeError("已有本地钩子，未停用或覆盖：" + ", ".join(sorted(existing)))

    hooks = [root / HOOKS_PATH / name for name in ("pre-commit", "pre-push")]
    if any(not hook.is_file() for hook in hooks) or not (root / "RPG-Silent/Tools/Git/check_repository.py").is_file():
        raise RuntimeError("缺少提交/推送钩子或检查器，请完整检出 Tools/Git 后重试。")
    for hook in hooks:
        if b"\r" in hook.read_bytes():
            raise RuntimeError(hook.name + " 必须使用 LF 换行，请按 .gitattributes 重新检出。")
    git(root, "lfs", "version")
    # Configure filters only: our tracked pre-push forwards to Git LFS, so
    # `git lfs install` must not replace the repository's shared hook files.
    git(root, "lfs", "install", "--local", "--skip-repo")
    for hook in hooks:
        hook.chmod(hook.stat().st_mode | 0o111)
    git(root, "config", "--local", PYTHON_KEY, Path(sys.executable).resolve().as_posix())
    git(root, "config", "--local", "core.hooksPath", HOOKS_PATH)
    print("已启用提交前检查；每次 git commit 自动验证实际暂存区。")
    print("已启用 Git LFS 本地过滤器和推送前对象上传。")
    print("Python：" + sys.executable)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--uninstall", action="store_true", help="移除本工具的本地配置")
    args = parser.parse_args()
    if sys.version_info < (3, 10):
        print("需要 Python 3.10 或更新版本。", file=sys.stderr)
        return 1
    try:
        root = Path(git(Path(__file__).resolve().parent, "rev-parse", "--show-toplevel"))
        configure(root, args.uninstall)
        return 0
    except (OSError, RuntimeError) as error:
        print("安装未完成：" + str(error), file=sys.stderr)
        return 1


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="backslashreplace")
    sys.exit(main())
