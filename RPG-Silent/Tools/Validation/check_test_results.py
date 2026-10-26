"""Fail CI when Unity produced no first-party tests, skipped tests, or failures."""
import sys
from pathlib import Path
import xml.etree.ElementTree as ET


def main():
    if len(sys.argv) != 2:
        raise SystemExit("Usage: check_test_results.py <artifact directory>")
    cases = []
    for path in Path(sys.argv[1]).rglob("*.xml"):
        root = ET.parse(path).getroot()
        cases.extend(case for case in root.iter("test-case")
                     if case.get("fullname", "").startswith("RPGSilent.Tests."))
    failed = [case for case in cases if case.get("result") != "Passed"]
    if not cases or failed:
        for case in failed:
            print(f"{case.get('result')}: {case.get('fullname')}", file=sys.stderr)
        raise SystemExit(f"Unity validation failed: {len(cases)} first-party tests, {len(failed)} not passed")
    print(f"Unity validation passed: {len(cases)} first-party tests")


if __name__ == "__main__":
    main()
