"""Require complete line and branch coverage for every Craft exception source file."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

reports = list(Path(sys.argv[1]).rglob("coverage.cobertura*.xml"))
if len(reports) != 1:
    raise SystemExit(f"Expected one exception coverage report, found {len(reports)}")

report = ET.parse(reports[0]).getroot()
source = Path(__file__).resolve().parents[2] / "Source/Core/Craft.Domain/Exceptions"
expected = set()
for path in source.rglob("*.cs"):
    namespace = re.search(r"^namespace\s+([\w.]+)", path.read_text(), re.MULTILINE)
    if namespace is None:
        raise SystemExit(f"Missing namespace in {path}")
    expected.add(f"{namespace[1]}.{path.stem}")
classes = {item.attrib["name"]: item for item in report.findall(".//class")}
if not expected or not expected <= classes.keys():
    raise SystemExit(f"Missing exception coverage: {expected - classes.keys()}")

failures = []
for name in sorted(expected):
    item = classes[name]
    missing = [line.attrib["number"] for line in item.findall("./lines/line") if int(line.attrib["hits"]) == 0]
    partial = [(line.attrib["number"], line.attrib.get("condition-coverage"))
        for line in item.findall("./lines/line")
        if line.attrib.get("branch", "").lower() == "true" and not line.attrib.get("condition-coverage", "").startswith("100%")]
    if float(item.attrib["line-rate"]) != 1 or float(item.attrib["branch-rate"]) != 1:
        failures.append(f"{name}: uncovered lines={missing}, partial branches={partial}")

print(f"Exception lines: {report.attrib['lines-covered']}/{report.attrib['lines-valid']}")
print(f"Exception branches: {report.attrib['branches-covered']}/{report.attrib['branches-valid']}")
if int(report.attrib["lines-valid"]) == 0 or int(report.attrib["branches-valid"]) == 0:
    raise SystemExit("Exception coverage is empty")
if failures:
    raise SystemExit("\n".join(failures))
