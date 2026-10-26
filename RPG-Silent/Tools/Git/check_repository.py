#!/usr/bin/env python3
"""检查 Git 快照中的仓库卫生；仅使用 Python 标准库，不修改文件。"""

from __future__ import annotations

import argparse
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys
from dataclasses import dataclass

PROJECT = "RPG-Silent/"
ASSETS = PROJECT + "Assets/"
MIB = 1024 * 1024
GENERATED_SCRIPTS = {
    "ComputeCommandBuffer.cs", "IBaseCommandBuffer.cs", "IComputeCommandBuffer.cs",
    "IRasterCommandBuffer.cs", "IUnsafeCommandBuffer.cs", "RasterCommandBuffer.cs",
    "UnsafeCommandBuffer.cs",
}
BINARY_SUFFIXES = {
    ".png", ".jpg", ".jpeg", ".gif", ".tga", ".psd", ".tif", ".tiff",
    ".fbx", ".blend", ".wav", ".mp3", ".ogg", ".mp4", ".dll", ".exe",
    ".ttf", ".otf", ".zip", ".7z", ".pdf", ".unitypackage",
}
CONFLICT = re.compile(rb"^(?:<{7}|>{7}|\|{7})(?:[ \t].*)?\r?$", re.MULTILINE)
GUID = re.compile(rb"^guid: [0-9a-fA-F]{32}\r?$", re.MULTILINE)
FOLDER_META = re.compile(rb"^folderAsset: yes\r?$", re.MULTILINE)


def git(root: Path, *args: str, data: bytes | None = None) -> bytes:
    process = subprocess.run(
        ["git", "-C", str(root), *args], input=data, capture_output=True, check=False
    )
    if process.returncode:
        raise RuntimeError(process.stderr.decode("utf-8", "replace").strip())
    return process.stdout


@dataclass(frozen=True)
class Entry:
    mode: str
    oid: str


@dataclass
class Policy:
    default_limit: int
    hard_limit: int
    exceptions: dict[str, int]

    @classmethod
    def load(cls, path: Path) -> Policy:
        document = json.loads(path.read_text(encoding="utf-8"))
        default = int(document["max_file_mib"]) * MIB
        hard = int(document["hard_limit_mib"]) * MIB
        if not 0 < default < hard <= 100 * MIB:
            raise ValueError("大文件阈值必须满足 0 < 默认值 < 硬上限 <= 100 MiB")
        exceptions = {}
        for name, rule in document["existing_large_files"].items():
            limit = int(rule["max_mib"]) * MIB
            if not default <= limit < hard or not rule.get("reason"):
                raise ValueError("大文件例外必须说明原因，且低于硬上限：" + name)
            exceptions[name] = limit
        return cls(default, hard, exceptions)


def snapshot(root: Path, revision: str | None) -> dict[str, Entry]:
    if revision == "":
        return {}
    result = {}
    if revision is None:
        records = git(root, "ls-files", "--stage", "-z")
    else:
        records = git(root, "ls-tree", "-r", "-z", revision)
    for record in records.split(b"\0"):
        if not record:
            continue
        header, name = record.split(b"\t", 1)
        fields = header.decode("ascii").split()
        path = name.decode("utf-8", "surrogateescape")
        if revision is None:
            mode, oid, stage = fields
            if stage != "0":
                raise ValueError("暂存区仍有未解决的合并冲突：" + path)
        else:
            mode, _kind, oid = fields
        result[path] = Entry(mode, oid)
    return result


def generated(path: str) -> bool:
    lower = path.lower()
    if lower.startswith("output/") or lower.endswith((".slnx", ".csproj", ".sln")):
        return True
    if not path.startswith(PROJECT):
        return False
    relative = path[len(PROJECT):]
    if relative in GENERATED_SCRIPTS:
        return True
    asset_path = relative.lower()
    if asset_path.startswith("assets/addressableassetsdata/"):
        return asset_path.endswith(("/addressables_content_state.bin", "/addressables_content_state.bin.meta")) or asset_path in {
            "assets/addressableassetsdata/link.xml", "assets/addressableassetsdata/link.xml.meta",
        }
    if asset_path == "assets/streamingassets/aa.meta" or asset_path.startswith("assets/streamingassets/aa/"):
        return True
    return relative.split("/", 1)[0].lower() in {
        "library", "temp", "obj", "logs", "usersettings", "build", "builds",
        "memorycaptures", "recordings",
    }


def audit(root: Path, before: dict[str, Entry], after: dict[str, Entry],
          policy: Policy) -> list[str]:
    changed = sorted(path for path in before.keys() | after.keys()
                     if before.get(path) != after.get(path))
    problems = []
    cache = {}

    def content(path: str) -> bytes:
        oid = after[path].oid
        if oid not in cache:
            cache[oid] = git(root, "cat-file", "blob", oid)
        return cache[oid]

    blob_ids = sorted({after[p].oid for p in changed
                       if p in after and after[p].mode != "160000"})
    sizes = {}
    if blob_ids:
        output = git(root, "cat-file", "--batch-check=%(objectname) %(objectsize)",
                     data=("\n".join(blob_ids) + "\n").encode("ascii"))
        for line in output.decode("ascii").splitlines():
            oid, size = line.split()
            sizes[oid] = int(size)

    for path in changed:
        if path not in after:
            continue
        if generated(path):
            problems.append("请停止跟踪构建产物或生成文件：" + path)
        if after[path].mode == "160000":
            continue
        size = sizes[after[path].oid]
        limit = policy.exceptions.get(path, policy.default_limit)
        if size > limit or size >= policy.hard_limit:
            problems.append(f"文件大小 {size / MIB:.2f} MiB 超出上限 {limit / MIB:g} MiB：{path}")
            continue
        if PurePosixPath(path).suffix.lower() in BINARY_SUFFIXES:
            continue
        data = content(path)
        if b"\0" not in data[:8192] and CONFLICT.search(data):
            problems.append("文件含合并冲突标记：" + path)
        # Do not retain large scene/asset blobs after scanning them.
        if not path.endswith(".meta"):
            cache.pop(after[path].oid, None)

    directories = set()
    for path in after:
        directories.update(str(p) for p in PurePosixPath(path).parents if str(p) != ".")
    affected = set()
    for path in changed:
        if path.startswith(ASSETS):
            affected.add(path[:-5] if path.endswith(".meta") else path)
            affected.update(str(p) for p in PurePosixPath(path).parents
                            if str(p).startswith(ASSETS))
    for path in sorted(affected):
        meta = path + ".meta"
        exists = path in after or path in directories
        if exists and meta not in after:
            problems.append("Unity 资源或目录缺少已跟踪的 .meta：" + path)
        elif meta in after:
            data = content(meta)
            if not GUID.search(data):
                problems.append(".meta 缺少有效 GUID：" + meta)
            if not exists and not FOLDER_META.search(data):
                problems.append(".meta 对应资源已不存在，请成对提交删除：" + meta)
    return problems


def resolve(root: Path, ref: str) -> str:
    return git(root, "rev-parse", "--verify", "--end-of-options", ref + "^{commit}").decode().strip()


def audit_range(root: Path, base: str, head: str, policy: Policy) -> tuple[int, list[str]]:
    commits = git(root, "rev-list", "--reverse", base + ".." + head).decode().splitlines()
    problems = []
    for commit in commits:
        parents = git(root, "rev-list", "--parents", "-n", "1", commit).decode().split()
        before = snapshot(root, parents[1] if len(parents) > 1 else "")
        for problem in audit(root, before, snapshot(root, commit), policy):
            problems.append(commit[:8] + " " + problem)
    return len(commits), problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--staged", action="store_true", help="检查暂存区（默认）")
    mode.add_argument("--head", help="检查提交范围，包括中间提交")
    parser.add_argument("--base", help="比较基线；提交模式默认检查 HEAD 的最后一个提交")
    args = parser.parse_args()
    try:
        root = Path(git(Path.cwd(), "rev-parse", "--show-toplevel").decode().strip())
        policy = Policy.load(Path(__file__).with_name("policy.json"))
        if args.head:
            head = resolve(root, args.head)
            if args.base and set(args.base) != {"0"}:
                requested_base = resolve(root, args.base)
                base = git(root, "merge-base", requested_base, head).decode().strip()
            else:
                parents = git(root, "rev-list", "--parents", "-n", "1", head).decode().split()
                if len(parents) < 2:
                    problems = audit(root, {}, snapshot(root, head), policy)
                    count = 1
                    base = None
                else:
                    base = parents[1]
            if base is not None:
                count, problems = audit_range(root, base, head, policy)
            scope = f"{count} 个提交"
        else:
            try:
                base = resolve(root, args.base or "HEAD")
            except RuntimeError:
                if args.base:
                    raise
                base = ""
            problems = audit(root, snapshot(root, base), snapshot(root, None), policy)
            scope = "暂存区"
        for problem in problems:
            print("错误：" + problem)
        print(f"{scope}：{'检查失败' if problems else '检查通过'}（{len(problems)} 个问题）")
        return 1 if problems else 0
    except (RuntimeError, ValueError, OSError) as error:
        print("无法完成仓库检查：" + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="backslashreplace")
        sys.stderr.reconfigure(encoding="utf-8", errors="backslashreplace")
    sys.exit(main())
