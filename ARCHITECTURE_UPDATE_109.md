# ZArchitecture 109 — build diagnostics cleanup

## CS0246 in ProcessRequestProvisioner

Added missing `using WebAppArchitecture.SecurityLayer;` after moving the provisioner from Core to DigitalProcesses. The application project already references `SecurityLayer.csproj`. No dependency from Core to DigitalProcesses was introduced.

## CS8714 in FormDataContextBuilder

Replaced `Dictionary<Guid?, FormNodeResponse[]>` with `ILookup<Guid?, FormNodeResponse>` built by `ToLookup()`. Nullable parent identifiers, including `null` for root nodes, are supported without violating dictionary key constraints. Sorting by position/key and recursion/cycle checks remain.

## CS0618 in legacy DTO-specific services

Removed the `[Obsolete]` attribute from the **still needed compatibility adapter** `SecuredBaseService<TDBModel,TDTOModel>`. This eliminates the listed warning without weakening authorization or changing 21 existing service contracts. **This is warning cleanup, not full migration:** these services and their resolvers still depend on DTO-specific `IBaseService<TDBModel,TDTOModel>` methods. The target architecture remains `SecuredBaseService<TDBModel>` with per-method DTO projections. Removing the adapter properly requires coordinated interface, DI and resolver migration.

## Verification

Performed static text checks and ZIP integrity verification. .NET SDK not available in the execution environment; compilation and PostgreSQL integration tests remain to be run.
