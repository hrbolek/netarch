#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
rm -rf .build
for type in IFormVersionService IWorkflowVersionService IProcessDefinitionVersionService; do
  find src/Apps/DigitalProcesses/DigitalProcesses.Services/Services -name "$type.cs" -print
 done
dotnet restore src/Apps/Host/Host.csproj --force
dotnet build src/Apps/Host/Host.csproj --no-restore
