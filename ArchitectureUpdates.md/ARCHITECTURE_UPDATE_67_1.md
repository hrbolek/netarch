# ZArchitecture67.1 — service interface contract visibility

The reported CS1061 diagnostics reference methods already declared in 67. In 67.1, the three version-service interfaces are moved to separate source files, without changing their signatures or implementations, to make project compilation inputs unambiguous. No runtime workflow changes.

To force a clean rebuild of the complete dependency graph from the project root:

```bash
bash tools/verify-digital-processes-contracts.sh
```

This deletes `.build` (the custom artifacts path), restores and builds Host and all referenced projects. The commands must be run where .NET SDK is installed.
