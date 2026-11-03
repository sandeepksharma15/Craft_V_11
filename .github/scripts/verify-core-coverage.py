from pathlib import Path
import sys
import xml.etree.ElementTree as ET

project = ET.parse("Source/Core/Craft.Core/Craft.Core.csproj").getroot()
for reference in ("PackageReference", "ProjectReference", "FrameworkReference"):
    if project.findall(f".//{reference}"):
        raise SystemExit(f"Craft.Core must remain dependency-free: found {reference}")

reports = list(Path(sys.argv[1]).rglob("coverage.cobertura*.xml"))
if len(reports) != 1:
    raise SystemExit(f"Expected one Core coverage report, found {len(reports)}")
report = ET.parse(reports[0]).getroot()
classes = report.findall(".//class")
expected = {"Craft.Core.Result", "Craft.Core.Result`1", "Craft.Core.Error", "Craft.Core.PageInfo", "Craft.Core.Page`1"}
reported = {item.attrib["name"] for item in classes}
if not expected <= reported:
    raise SystemExit(f"Missing coverage: {expected - reported}")
for item in classes:
    missing = [line.attrib["number"] for line in item.findall("./lines/line") if int(line.attrib["hits"]) == 0]
    partial = [(line.attrib["number"], line.attrib.get("condition-coverage")) for line in item.findall("./lines/line")
               if line.attrib.get("branch", "").lower() == "true" and not line.attrib.get("condition-coverage", "").startswith("100%")]
    print(f"{item.attrib['name']}: uncovered lines={missing}; partial branches={partial}")
print(f"Lines: {report.attrib['lines-covered']}/{report.attrib['lines-valid']}")
print(f"Branches: {report.attrib['branches-covered']}/{report.attrib['branches-valid']}")
if int(report.attrib["lines-valid"]) == 0 or int(report.attrib["branches-valid"]) == 0:
    raise SystemExit("Coverage is empty")
if float(report.attrib["line-rate"]) != 1 or float(report.attrib["branch-rate"]) != 1:
    raise SystemExit("Core requires 100% line and branch coverage")
