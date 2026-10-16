# ArchitectureInspector

Compact standalone .NET 8 solution: declarative type attributes + Roslyn-based source audit.

```bash
dotnet restore ArchitectureInspector.slnx
dotnet test ArchitectureInspector.slnx
dotnet run --project src/Architecture.Auditor -- ../../ZArchitecture.sln report.json
```

The CLI accepts an `.sln`, `.slnx`, or `.csproj` and writes a JSON array of findings (or stdout). Exit code 1 means at least one error, 0 means no errors, 2 means invalid arguments. The tool uses MSBuildWorkspace, so a compatible .NET SDK and restored project dependencies are required.

Rules in the compact starter:
- `ARCH001` warning: conventional `*Model`, `*Response`, `*Service`, `*Resolvers` class without an annotation.
- `ARCH002` error: annotated role conflicts with conventional class suffix.
- `ARCH101` error: annotated Resolver method exposes an annotated Entity in return type (including generics such as `Task<UserModel>`).
- `ARCH102` error: annotated Resolver method receives an annotated Entity parameter.

Known limitations: these initial rules are deliberately shallow. They do not validate assembly boundaries, DI, method bodies, OpenAPI, indirect calls, or unannotated model exposure; `*Model` suffix is only a naming heuristic. Audit only assemblies/projects under review; warnings are expected until the existing codebase is annotated. No production code depends on the auditor.
