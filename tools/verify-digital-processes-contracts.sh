#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
rm -rf .build
for type in IFormVersionService IWorkflowVersionService IProcessDefinitionVersionService; do
  find src/Apps/DigitalProcesses/DigitalProcesses.Services/Services -name "$type.cs" -print
 done
dotnet restore src/Host/Host.csproj --force
dotnet build src/Host/Host.csproj --no-restore
