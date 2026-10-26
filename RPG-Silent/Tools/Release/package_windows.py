#!/usr/bin/env python3
"""仅从当前干净提交的完整验证记录创建 Windows 发布包，不自动创建或推送标签。"""

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import zipfile


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def digest(path):
    result = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            result.update(block)
    return result.hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def package(project, validation):
    validation = validation.resolve()
    require(validation.is_relative_to(project / "Logs/Validation"), "验证记录必须位于本工程 Logs/Validation。")
    git = lambda *args: subprocess.check_output(
        ["git", "-C", str(project), *args], encoding="utf-8").strip()
    require(not git("status", "--porcelain"), "发布要求当前工作区干净，请先提交并验证最终版本。")
    head = git("rev-parse", "HEAD")
    record = read_json(validation / "validation.json")
    require(record["status"] == "Passed" and record["commit"] == head and not record["workingTree"],
            "验证必须来自当前提交的干净工作区。")
    tests = record["tests"]
    require(tests["total"] > 0 and tests["passed"] == tests["total"] and
            tests["failed"] == 0 and tests["skipped"] == 0, "所有测试必须实际执行且通过。")
    startup = read_json(validation / "player-startup.json")
    require(startup["startMenuLoaded"] and not startup["earlyExit"] and
            not startup["errorPatternFound"] and startup["seconds"] >= 30, "缺少通过的独立 Player 启动检查。")
    build_dir = Path(record["buildOutput"]).resolve()
    require(build_dir.is_relative_to(project / "Builds/Validation"), "构建目录必须属于本工程的验证产物。")
    build = read_json(build_dir / "build-report.json")
    require(build["status"] == "Succeeded" and build["errors"] == 0, "构建必须成功且错误数为 0。")
    require(Path(build["output"]).resolve() == build_dir / "RPG-Silent.exe", "构建报告的程序路径不匹配。")
    version = build["version"]
    require(re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", version), "版本号需要使用语义化版本格式。")
    settings = (project / "ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8")
    expected = re.search(r"^  bundleVersion: (.+)$", settings, re.MULTILINE)
    require(expected is not None and expected[1].strip() == version, "构建版本与 PlayerSettings 不一致。")
    expected_unity = re.search(r"^m_EditorVersion: (.+)$",
                              (project / "ProjectSettings/ProjectVersion.txt").read_text(), re.MULTILINE)[1].strip()
    require(record["unityVersion"] == build["unityVersion"] == expected_unity, "Unity 版本不匹配。")

    checksums = read_json(build_dir / "sha256.json")
    actual_files = {p.relative_to(build_dir).as_posix() for p in build_dir.rglob("*")
                    if p.is_file() and p.name != "sha256.json"}
    require(actual_files == {item["path"] for item in checksums}, "构建文件集合与验证时不同。")
    for item in checksums:
        path = (build_dir / item["path"]).resolve()
        require(path.is_relative_to(build_dir) and digest(path) == item["sha256"].lower(),
                "产物校验失败：" + item["path"])

    runtime_roots = {"RPG-Silent.exe", "UnityPlayer.dll", "UnityCrashHandler64.exe",
                     "RPG-Silent_Data", "MonoBleedingEdge", "D3D12"}
    known_roots = runtime_roots | {"AddressablesState", "build-report.json"}
    require({p.split("/", 1)[0] for p in actual_files} <= known_roots,
            "构建包含尚未归类的文件，请先确认它是否需要进入运行包。")
    require((build_dir / "RPG-Silent.exe").is_file() and (build_dir / "RPG-Silent_Data").is_dir(),
            "缺少 Windows 运行文件。")
    require((build_dir / "UnityPlayer.dll").is_file() and (build_dir / "MonoBleedingEdge").is_dir(),
            "缺少 Windows Mono 运行依赖。")
    destination = project / "Builds/Releases" / ("v" + version)
    destination.mkdir(parents=True, exist_ok=False)
    archive = destination / ("RPG-Silent-v" + version + "-win64.zip")
    with zipfile.ZipFile(archive, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as output:
        for relative in sorted(actual_files):
            if relative.split("/", 1)[0] in runtime_roots:
                output.write(build_dir / relative, relative)
    verification = destination / ("RPG-Silent-v" + version + "-verification.zip")
    with zipfile.ZipFile(verification, "x", compression=zipfile.ZIP_DEFLATED) as output:
        for name in ("validation.json", "editmode.xml", "player-startup.json"):
            output.write(validation / name, name)
        for name in ("build-report.json", "sha256.json"):
            output.write(build_dir / name, name)
        for path in sorted((build_dir / "AddressablesState").rglob("*")):
            if path.is_file():
                output.write(path, path.relative_to(build_dir).as_posix())
    manifest = {"version": version, "tag": "v" + version, "commit": head,
                "tree": git("rev-parse", "HEAD^{tree}"), "unityVersion": expected_unity,
                "createdUtc": datetime.now(timezone.utc).isoformat(), "validationRun": validation.name,
                "tests": tests, "buildWarnings": build["messages"], "startup": startup,
                "archives": [{"file": p.name, "bytes": p.stat().st_size, "sha256": digest(p)}
                             for p in (archive, verification)]}
    (destination / "release.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    (destination / "SHA256SUMS.txt").write_text(
        "".join(digest(p) + "  " + p.name + "\n" for p in (archive, verification, destination / "release.json")),
        encoding="utf-8")
    return destination


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="backslashreplace")
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("validation", type=Path, help="Logs/Validation 下的完整验证目录")
    args = parser.parse_args()
    try:
        print(package(Path(__file__).resolve().parents[2], args.validation))
    except (OSError, ValueError, KeyError, subprocess.CalledProcessError) as error:
        print("发布包未生成：" + str(error), file=sys.stderr)
        sys.exit(1)
