DP26: direct IService calls.
Unzip in /workspace root: unzip -o ZArchitecture78_DP26_service_direct_patch.zip -d /workspace
Execute: dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug --filter FullyQualifiedName~DP26_
This bypasses HTTP transport only, not SecurityLayer. It uses a synthetic actor HttpContext. Existing application policy errors will still fail.
