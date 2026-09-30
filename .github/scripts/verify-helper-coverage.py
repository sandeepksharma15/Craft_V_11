"""Verify the union of real Linux and Windows helper coverage without excluding code paths."""
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

root = Path(sys.argv[1])
cobertura = sorted(root.rglob("coverage.cobertura.xml"))
opencover = sorted(root.rglob("coverage.opencover.xml"))
if len(cobertura) != 2 or len(opencover) != 2:
    raise SystemExit(f"Expected two OS reports of each format, found {len(cobertura)} and {len(opencover)}")

lines = {}
branches = {}
branch_sets = []
for path in cobertura:
    for cls in ET.parse(path).findall(".//class"):
        name = cls.attrib["name"]
        if not name.startswith("Craft.Utilities.Helpers."):
            continue
        for point in cls.findall("./lines/line"):
            key = (name, point.attrib["number"])
            lines[key] = lines.get(key, False) or int(point.attrib["hits"]) > 0

for path in opencover:
    branch_set = set()
    for cls in ET.parse(path).findall(".//Class"):
        name = cls.findtext("FullName")
        if not name or not name.startswith("Craft.Utilities.Helpers."):
            continue
        for method in cls.findall("./Methods/Method"):
            method_name = method.findtext("Name")
            for point in method.findall("./BranchPoints/BranchPoint"):
                key = (name, method_name, point.attrib.get("sl"), point.attrib["offset"],
                    point.attrib["path"], point.attrib.get("offsetend"))
                branch_set.add(key)
                branches[key] = branches.get(key, False) or int(point.attrib["vc"]) > 0
    branch_sets.append(branch_set)

if not lines or not branches or branch_sets[0] != branch_sets[1]:
    raise SystemExit("Missing coverage or different branch universes between OS builds")

missing_lines = [key for key, covered in lines.items() if not covered]
missing_branches = [key for key, covered in branches.items() if not covered]
print(f"Helpers combined lines: {len(lines) - len(missing_lines)}/{len(lines)}")
print(f"Helpers combined branches: {len(branches) - len(missing_branches)}/{len(branches)}")
for key in missing_lines:
    print(f"Uncovered line: {key}")
for key in missing_branches:
    print(f"Uncovered branch: {key}")
if missing_lines or missing_branches:
    raise SystemExit("Helpers require 100% combined Linux/Windows line and branch coverage")
