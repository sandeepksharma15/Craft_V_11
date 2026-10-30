from pathlib import Path
import sys
import xml.etree.ElementTree as ET

reports = list(Path(sys.argv[1]).rglob("coverage.cobertura*.xml"))
if len(reports) != 1:
    raise SystemExit(f"Expected one CryptKey coverage report, found {len(reports)}")
report = ET.parse(reports[0]).getroot()
classes = report.findall(".//class")
expected = {"Craft.CryptKey.Encoding.HashKeys", "Craft.CryptKey.Encoding.KeyPayload", "Craft.CryptKey.Configuration.HashKeyOptions",
            "Craft.CryptKey.Configuration.HashKeyOptionsValidator", "Craft.CryptKey.Extensions.KeyTypeExtensions", "Craft.CryptKey.Extensions.ServiceCollectionExtensions"}
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
    raise SystemExit("CryptKey requires 100% line and branch coverage")
