# PostgreSQL EffectiveAccess full rebuild fix

Changed: src/Core/SecurityLayer/SecurityProjectionService.cs

Full rebuild now uses SQL ExecuteDeleteAsync through IDBLayer.DeleteWhereAsync instead of loading/tracking and deleting existing rows. Deletion and insertion remain in the surrounding transaction.

Extract the archive directly at /workspace; no Python scripts required.

NOTE: The reported unique-key conflict has not been reproduced here. This addresses a plausible ordering/tracking cause, not a proven unique root cause. If duplicate keys persist, examine concurrent rebuilds and the actual conflicting composite key.
