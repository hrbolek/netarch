# DigitalProcesses: domain StartProcess and trusted provisioning

- `start_process` is a dedicated RBAC operation. It is distinct from generic `create` permission on a faculty Group.
- Start first verifies the published ProcessDefinitionVersion, its target group type, group validity, and actor `start_process` on the target.
- `ProcessRequestProvisioner.BeginStartAsync` revalidates `start_process` under the database transaction, checks actual target group and request-group parent path, and creates the request group.
- `IProcessRequestWriteScope` stages only explicitly whitelisted request-owned model types and scopes. Generic `SecurityLayer.StageCreateAsync` remains unchanged.
- Transition uses `BeginTransitionAsync` only after checking Update on the persisted request group.
- `SecurityProjection.SynchronizeGroupSubtreeAsync` runs once after staged inserts are saved, still inside the transaction.
- The operation catalog seeds `start_process` with deterministic ID ending `0011`. Assign this operation explicitly to role types permitted to start specific process versions/targets; authorization still controls the **target**, so process-specific restrictions may require an additional policy.
- DP26 fixture assigns `start_process` to its applicant role.

## Validation

`dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug`

The .NET SDK is not installed in the artifact assembly environment. Compilation and test success have not been verified here.
