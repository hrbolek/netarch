#!/usr/bin/env python3
"""Fetch public 2026 systemdata from GitHub with a streaming download."""
import urllib.request
from pathlib import Path
url='https://raw.githubusercontent.com/hrbolek/_uois/latest/systemdata.hk2026.json'
dest=Path(__file__).with_name('systemdata.hk2026.json')
with urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'EventsImporter/1.0'}),timeout=45) as src, dest.open('wb') as out:
    while chunk:=src.read(1024*1024): out.write(chunk)
print(f'{dest}: {dest.stat().st_size} bytes')
