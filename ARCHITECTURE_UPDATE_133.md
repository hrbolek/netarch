# ZArchitecture 133 — upload events JSON

Added in `src/Apps/Events`:

- `EventsJsonImport.cs` for explicit mapping from `systemdata.hk2026.json`'s `events` collection; UUID preservation; parent-first insertion; replay-safe skip-by-UUID; transactional writes; limited error diagnostics.
- `POST /events/EventResponse/import-json` accepts multipart `file` and `rbacObjectId` (100 MB max; request size override), checks `create` on the requested scope before parsing. No public unauthenticated import endpoint.
- Admin `/events/EventResponse/admin` now offers file upload and results/errors.
- `download_systemdata.py` utility downloads the upstream public GitHub JSON with streaming to disk.

**Important:** This execution environment could inspect GitHub's published 47.4 MB file metadata, but not fetch its complete contents; direct container DNS failed and the web text retriever rejects a file larger than 4 MB. The importer maps documented, common legacy names (`id`, `eventtype_id`, `masterevent_id`, `name`, `startdate`, `enddate`) and aliases; it reports incompatible rows without inventing fields. The actual 2026 file MUST be run through the importer to validate field compatibility. The much smaller `systemdata.min.json` from the same repository confirms top-level category arrays and `eventtypes`, but contains no event instances.

The importer creates missing event types ONLY when explicitly present in the same JSON `eventtypes` collection, with original UUIDs and `legacy-{id}` codes. Unknown referenced type UUIDs are **not silently synthesized**. Unrecognized relationships to other domains are intentionally not imported. Default EF model has no facility/lesson/user foreign keys on an Event record.

No .NET SDK available locally, so compilation was not verified.
