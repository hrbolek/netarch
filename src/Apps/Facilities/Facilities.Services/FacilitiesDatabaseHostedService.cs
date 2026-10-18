using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Facilities;

/// <summary>
/// Non-destructive Facilities bootstrap. Register after the Events database initializer.
/// The composed DbContext must call ConfigureEvents and ConfigureFacilities.
/// Empty databases are initialized with the composed model; existing PostgreSQL
/// deployments receive only the missing Facilities tables and indexes.
/// </summary>
public sealed class FacilitiesDatabaseHostedService(
    IServiceScopeFactory scopeFactory, ILogger<FacilitiesDatabaseHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        await FacilitiesSchemaBootstrap.EnsureAsync(db, cancellationToken);
        logger.LogInformation("Facilities schema verified.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class FacilitiesSchemaBootstrap
{
    public static async Task EnsureAsync(CoreDbContext db, CancellationToken ct = default)
    {
        if (db.Model.FindEntityType(typeof(FacilityModel)) is null || db.Model.FindEntityType(typeof(Events.EventModel)) is null)
            throw new InvalidOperationException("The composed database model must configure Facilities and Events.");
        if (!db.Database.IsNpgsql())
            throw new NotSupportedException("Facilities schema bootstrap requires PostgreSQL.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(762306240)", ct);
        // Under the bootstrap lock, EnsureCreated safely handles a genuinely empty composed database.
        await db.Database.EnsureCreatedAsync(ct);
        await db.Database.ExecuteSqlRawAsync(PostgresSql, ct);
        await db.Database.ExecuteSqlRawAsync(FacilityFieldsUpgradeSql, ct);
        await db.Database.ExecuteSqlRawAsync(OwnedScopeSql, ct);
        await db.Database.ExecuteSqlRawAsync(MoveOperationSql, ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>Domain-owned catalog identity; provisioning never grants a role this operation.</summary>
    public static readonly Guid MoveOperationId = Guid.Parse("e326f9a1-68bb-4fb8-8d49-45fb36cd55c5");

    private const string MoveOperationSql = """
        DO $$
        BEGIN
          IF EXISTS (SELECT 1 FROM operations WHERE id = 'e326f9a1-68bb-4fb8-8d49-45fb36cd55c5'::uuid AND lower(name) <> 'move') THEN
            RAISE EXCEPTION 'Facilities move operation ID is already assigned to another operation';
          END IF;
          IF NOT EXISTS (SELECT 1 FROM operations WHERE lower(name) = 'move') THEN
            INSERT INTO operations (id, name, created_at, updated_at, created_by, updated_by)
            VALUES ('e326f9a1-68bb-4fb8-8d49-45fb36cd55c5'::uuid, 'move', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP,
                    '00000000-0000-0000-0000-000000000000'::uuid, '00000000-0000-0000-0000-000000000000'::uuid);
          END IF;
        END $$;
        """;

    private const string PostgresSql = """
        CREATE TABLE IF NOT EXISTS facility_types (
          id uuid PRIMARY KEY, rbacobject_id uuid NOT NULL,
          created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL,
          created_by uuid NOT NULL, updated_by uuid NOT NULL,
          "Code" text NOT NULL, "Name" text NOT NULL, "Description" text NULL,
          "Reservable" boolean NOT NULL, "CanContainChildren" boolean NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_facility_types_Code" ON facility_types ("Code");
        CREATE INDEX IF NOT EXISTS "IX_facility_types_rbacobject_id" ON facility_types (rbacobject_id);
        CREATE TABLE IF NOT EXISTS facility_type_allowed_children (
          "ParentTypeId" uuid NOT NULL REFERENCES facility_types(id) ON DELETE RESTRICT,
          "ChildTypeId" uuid NOT NULL REFERENCES facility_types(id) ON DELETE RESTRICT,
          PRIMARY KEY ("ParentTypeId", "ChildTypeId")
        );
        CREATE INDEX IF NOT EXISTS "IX_facility_type_allowed_children_ChildTypeId" ON facility_type_allowed_children ("ChildTypeId");
        CREATE TABLE IF NOT EXISTS facilities (
          id uuid PRIMARY KEY, rbacobject_id uuid NOT NULL,
          created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL,
          created_by uuid NOT NULL, updated_by uuid NOT NULL,
          "ParentId" uuid NULL REFERENCES facilities(id) ON DELETE RESTRICT,
          "FacilityTypeId" uuid NOT NULL REFERENCES facility_types(id) ON DELETE RESTRICT,
          "Code" text NOT NULL, "Name" text NOT NULL, "Description" text NULL,
          "Capacity" integer NULL,
          "IsActive" boolean NOT NULL, path text NOT NULL,
          CONSTRAINT ck_facility_code CHECK (length(trim("Code")) > 0),
          CONSTRAINT ck_facility_capacity CHECK ("Capacity" IS NULL OR "Capacity" >= 0)
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_facilities_rbacobject_id" ON facilities (rbacobject_id);
        CREATE INDEX IF NOT EXISTS "IX_facilities_ParentId" ON facilities ("ParentId");
        CREATE INDEX IF NOT EXISTS "IX_facilities_FacilityTypeId" ON facilities ("FacilityTypeId");
        CREATE INDEX IF NOT EXISTS "IX_facilities_path" ON facilities (path text_pattern_ops);
        CREATE TABLE IF NOT EXISTS facility_reservations (
          id uuid PRIMARY KEY, rbacobject_id uuid NOT NULL,
          created_at timestamptz NOT NULL, updated_at timestamptz NOT NULL,
          created_by uuid NOT NULL, updated_by uuid NOT NULL,
          "FacilityId" uuid NOT NULL REFERENCES facilities(id) ON DELETE RESTRICT,
          "EventId" uuid NOT NULL REFERENCES events(id) ON DELETE RESTRICT,
          "StartsAt" timestamptz NOT NULL, "EndsAt" timestamptz NOT NULL,
          "Status" text NOT NULL, "Note" text NULL, "DecisionReason" text NULL,
          CONSTRAINT ck_facility_reservation_interval CHECK ("StartsAt" < "EndsAt"),
          CONSTRAINT ck_facility_reservation_status CHECK ("Status" IN ('requested','confirmed','rejected','cancelled'))
        );
        CREATE INDEX IF NOT EXISTS "IX_facility_reservations_rbacobject_id" ON facility_reservations (rbacobject_id);
        CREATE INDEX IF NOT EXISTS "IX_facility_reservations_EventId" ON facility_reservations ("EventId");
        CREATE INDEX IF NOT EXISTS "IX_facility_reservations_FacilityId_Status_StartsAt_EndsAt"
          ON facility_reservations ("FacilityId", "Status", "StartsAt", "EndsAt");
        """;

    // Additive upgrade for deployments created before Code/Capacity existed.
    // Legacy rows receive their stable UUID as a deterministic nonempty code.
    // No uniqueness rule is imposed: Code is required, not specified as globally unique.
    private const string FacilityFieldsUpgradeSql = """
        ALTER TABLE facilities ADD COLUMN IF NOT EXISTS "Code" text;
        ALTER TABLE facilities ADD COLUMN IF NOT EXISTS "Capacity" integer NULL;
        UPDATE facilities SET "Code" = id::text WHERE "Code" IS NULL OR length(trim("Code")) = 0;
        ALTER TABLE facilities ALTER COLUMN "Code" SET NOT NULL;
        DO $$
        BEGIN
          IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = 'facilities'::regclass AND conname = 'ck_facility_code') THEN
            ALTER TABLE facilities ADD CONSTRAINT ck_facility_code CHECK (length(trim("Code")) > 0);
          END IF;
          IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = 'facilities'::regclass AND conname = 'ck_facility_capacity') THEN
            ALTER TABLE facilities ADD CONSTRAINT ck_facility_capacity CHECK ("Capacity" IS NULL OR "Capacity" >= 0);
          END IF;
        END $$;
        """;

    // Fail on duplicate/missing scopes rather than silently sharing or reassigning
    // permissions. Dropping/recreating this index transactionally also upgrades an
    // intermediate deployment whose same-named index was not unique.
    private const string OwnedScopeSql = """
        DROP INDEX IF EXISTS "IX_facilities_rbacobject_id";
        CREATE UNIQUE INDEX "IX_facilities_rbacobject_id" ON facilities (rbacobject_id);
        DO $$
        BEGIN
          IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conrelid = 'facilities'::regclass AND conname = 'FK_facilities_groups_rbacobject_id') THEN
            ALTER TABLE facilities ADD CONSTRAINT "FK_facilities_groups_rbacobject_id"
              FOREIGN KEY (rbacobject_id) REFERENCES groups(id) ON DELETE RESTRICT;
          END IF;
        END $$;
        """;
}
