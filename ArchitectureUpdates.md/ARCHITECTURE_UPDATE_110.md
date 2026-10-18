# ZArchitecture 110 — compilation fixes

## CS8915: global using order

`src/Apps/DigitalProcesses/DigitalProcesses.Resolvers/GlobalUsings.cs` now declares `global using WebAppArchitecture.Base.Data;` before the ordinary `using WebAppArchitecture.DigitalProcesses.DBLayer;` directive.

## CS9107: ProcessDefinitionService captured constructor dependency

`ProcessDefinitionService` was converted from a primary constructor to an explicit constructor. The `IDBLayer` and `ISecurityProjectionService` dependencies are now held in explicitly declared private readonly fields. The `IDBLayer` dependency is also passed to the secured base constructor, without primary-constructor capture, which removes CS9107. The existing transaction and staged-creation code is unchanged in behavior.

## Validation

Static checks of changed files and archive integrity were performed. .NET build/tests were not run because the .NET SDK is unavailable in this environment.
