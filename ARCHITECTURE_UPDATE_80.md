# ARCHITECTURE_UPDATE_80

## PostgreSQL migration repair

Version 79 mistakenly created `dp_document_template_revisions` before defining its
referenced `dp_document_templates` base table. PostgreSQL raised SQLSTATE 42P01.

Version 80 adds `migrations/ARCHITECTURE_80_postgresql.sql`, which creates the base
table first, then revision and binding tables, indexes, and idempotent revision-1 backfill.
It does not drop or overwrite existing content. All changes concern schema only;
application source remains the same as version 79.

Stop Host and back up postgres_development. Run the new migration against database `data`
on host `postgres_development`:

```bash
PGPASSWORD=example psql -h postgres_development -U postgres -d data \
  -v ON_ERROR_STOP=1 -1 -f migrations/ARCHITECTURE_80_postgresql.sql
```

Restart Host after success. Do NOT run the old 79 migration again.
Test databases created by EF EnsureCreated should already contain the complete schema.
Compile / integration tests for 80 have not been run in this environment.
