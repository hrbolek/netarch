#!/usr/bin/env python3
"""Check both advertised formats on the running Host's Swagger document."""
import json
import sys
import urllib.request

url = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8080/swagger/v1/swagger.json"
with urllib.request.urlopen(url, timeout=10) as resp:
    doc = json.load(resp)
path = "/digital-processes/FormDefinitionResponse/list"
content = doc["paths"][path]["get"]["responses"]["200"]["content"]
expected = {"application/json", "text/html"}
missing = expected - set(content)
if missing:
    raise SystemExit(f"FAIL {path}: missing response content types {sorted(missing)}; actual {sorted(content)}")
print(f"OK {path}: {sorted(content)}")
