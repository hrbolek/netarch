# Architecture Update 14.1

## Build fix for stale Update 10 service file

Update 10 contained `src/Base/Services/BaseService.cs`. From Update 11 onward the implementation lives in `src/SecurityLayer/BaseService.cs` and the contract remains in `src/Base/Services/IBaseService.cs`.

If a newer archive is unpacked over an existing older workspace, ZIP extraction does not delete files that no longer exist in the archive. The obsolete Update 10 `BaseService.cs` therefore may remain and causes duplicate `IBaseService` declarations and incompatible generic constraints.

Update 14.1 adds an explicit MSBuild guard to `src/Base/Base.csproj`:

```xml
<Compile Remove="Services/BaseService.cs" />
```

The preferred cleanup is still to delete the obsolete file or unpack the archive into a clean directory.

Current canonical files are:

- `src/Base/Services/IBaseService.cs` — the service contract
- `src/SecurityLayer/BaseService.cs` — the security-aware implementation

The obsolete file `src/Base/Services/BaseService.cs` must not be compiled.
