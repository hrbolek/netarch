# DigitalProcesses: acceptance coverage and outstanding work

Confirmed before this update: **25/25** application tests, including three negative command-atomicity tests.

## Faculty-scoped security scenario (DP16–DP18)

The tests in `DigitalProcessesFacultyScopeTests.cs` seed a *real SQLite database* served
by `IsolatedHost` and exercise the HTTP authorization boundary. The scenario models:

```
Root
├─ Faculty A
│  └─ Administrative proceedings       [officer has functional Role here]
│     └─ Process definition group
│        └─ Request group               [no implicit role inheritance]
│           ├─ Submission v1 (archived) [explicit access binding]
│           └─ Submission v2 (active)   [explicit access binding]
└─ Faculty B
   └─ Administrative proceedings
      └─ Different Request + Submission [no access binding]
```

Bindings deliberately bridge the Request and Submission role-inheritance boundaries.
Membership never grants or revokes a Role. Test data includes published form, workflow,
process versions and persisted snapshot metadata; these tests do **not** execute a
successful Start/Transition command.

## Not yet covered: required before full lifecycle acceptance

1. Create/publish FormVersion from FormNodes via authorized services/endpoints.
2. Create/publish WorkflowVersion and states/transitions.
3. Attach and publish ProcessVersion under an **existing faculty administrative-case RBAC scope**.
   Current `ProcessDefinitionService.CreateTypedAsync` creates the process group under
   a **fixed global ProcessDefinition creation scope**, not a caller-selected faculty case.
   The runtime `RequestService.StartAsync` DOES use the ProcessDefinition's group
   as the parent. Explicit attach/move with authorization requires a product decision
   and likely a dedicated API operation.
4. Actual positive HTTP Start → fill Submission nodes → Transition → archive + new
   Submission, with state-specific access rules for officer, applicant and stranger.
5. Negative tests for editable/archived nodes, Deny affecting reads/writes, transition
   authorization, concurrency and transactional rollback.

The application must **not** be described as fully acceptance-tested until the above
items have working tests. This note intentionally distinguishes regression tests
from unresolved product behavior.

Run: `dotnet test tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcesses.Integration.Tests.csproj -c Debug`.
Expected discovery: 28 tests (25 previous + 3 new), subject to compilation and execution.
