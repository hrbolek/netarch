#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet clean src/Apps/DigitalProcesses/DigitalProcesses.Models/DigitalProcesses.Models.csproj
dotnet clean src/Apps/DigitalProcesses/DigitalProcesses.Services/DigitalProcesses.Services.csproj
dotnet build src/Apps/DigitalProcesses/DigitalProcesses.Models/DigitalProcesses.Models.csproj --no-incremental
dotnet build src/Apps/DigitalProcesses/DigitalProcesses.Services/DigitalProcesses.Services.csproj --no-incremental
dotnet build src/Host/Host.csproj --no-incremental
