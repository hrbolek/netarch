using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.Events;

/// <summary>
/// Non-destructive PostgreSQL schema upgrade for the Events module.
/// EnsureCreated does not add tables when a previous version of the database exists.
/// This bootstrap only creates missing Events tables; it never drops or rewrites core tables.
/// </summary>

public sealed class EventsDatabaseHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<EventsDatabaseHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        if (!db.Database.IsNpgsql())
        {
            // Existing SQLite integration tests use EnsureCreated on a fresh database.
            logger.LogInformation("Events schema bootstrap skipped for non-PostgreSQL provider.");
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Serializes concurrent application instances performing first-run bootstrap.
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(762306125)", cancellationToken);
        await db.Database.ExecuteSqlRawAsync(SchemaSql, cancellationToken);
        await db.Database.ExecuteSqlRawAsync(SeedSql, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Events tables and reference data verified.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS event_types (
          id uuid PRIMARY KEY, rbacobject_id uuid NOT NULL,
          created_at timestamp with time zone NOT NULL, updated_at timestamp with time zone NOT NULL,
          created_by uuid NOT NULL, updated_by uuid NOT NULL,
          "ParentId" uuid NULL, "Code" text NOT NULL, "Name" text NOT NULL,
          "Description" text NULL, "Abbreviation" text NULL, "Color" text NULL,
          "RegistrationMode" text NOT NULL,
          CONSTRAINT "FK_event_types_event_types_ParentId" FOREIGN KEY ("ParentId") REFERENCES event_types(id) ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_event_types_Code" ON event_types("Code");
        CREATE INDEX IF NOT EXISTS "IX_event_types_ParentId" ON event_types("ParentId");

        CREATE TABLE IF NOT EXISTS events (
          id uuid PRIMARY KEY, rbacobject_id uuid NOT NULL,
          created_at timestamp with time zone NOT NULL, updated_at timestamp with time zone NOT NULL,
          created_by uuid NOT NULL, updated_by uuid NOT NULL,
          "EventTypeId" uuid NOT NULL, "ParentId" uuid NULL,
          "Name" text NOT NULL, "Description" text NULL,
          "IsPlannerSegment" boolean NOT NULL,
          "StartsAt" timestamp with time zone NOT NULL,
          "EndsAt" timestamp with time zone NOT NULL,
          CONSTRAINT "FK_events_event_types_EventTypeId" FOREIGN KEY ("EventTypeId") REFERENCES event_types(id) ON DELETE RESTRICT,
          CONSTRAINT "FK_events_events_ParentId" FOREIGN KEY ("ParentId") REFERENCES events(id) ON DELETE RESTRICT
        );
        CREATE INDEX IF NOT EXISTS "IX_events_EventTypeId" ON events("EventTypeId");
        CREATE INDEX IF NOT EXISTS "IX_events_ParentId" ON events("ParentId");
        CREATE INDEX IF NOT EXISTS "IX_events_IsPlannerSegment" ON events("IsPlannerSegment");
        CREATE INDEX IF NOT EXISTS "IX_events_StartsAt_EndsAt" ON events("StartsAt", "EndsAt");

        ALTER TABLE event_types ADD COLUMN IF NOT EXISTS path text NOT NULL DEFAULT '';
        ALTER TABLE events ADD COLUMN IF NOT EXISTS path text NOT NULL DEFAULT '';
        CREATE INDEX IF NOT EXISTS "IX_event_types_path" ON event_types(path text_pattern_ops);
        CREATE INDEX IF NOT EXISTS "IX_events_path" ON events(path text_pattern_ops);
        WITH RECURSIVE t AS (
          SELECT id, '/' || id::text || '/' AS p FROM event_types WHERE "ParentId" IS NULL
          UNION ALL SELECT c.id, t.p || c.id::text || '/' FROM event_types c JOIN t ON c."ParentId" = t.id)
        UPDATE event_types e SET path = t.p FROM t WHERE e.id = t.id AND e.path = '';
        WITH RECURSIVE t AS (
          SELECT id, '/' || id::text || '/' AS p FROM events WHERE "ParentId" IS NULL
          UNION ALL SELECT c.id, t.p || c.id::text || '/' FROM events c JOIN t ON c."ParentId" = t.id)
        UPDATE events e SET path = t.p FROM t WHERE e.id = t.id AND e.path = '';

        CREATE TABLE IF NOT EXISTS event_registration_policies (
          id uuid PRIMARY KEY, rbacobject_id uuid NOT NULL,
          created_at timestamp with time zone NOT NULL, updated_at timestamp with time zone NOT NULL,
          created_by uuid NOT NULL, updated_by uuid NOT NULL,
          "EventId" uuid NOT NULL, "Capacity" integer NULL,
          "OpensAt" timestamp with time zone NULL,
          "ClosesAt" timestamp with time zone NULL,
          CONSTRAINT "FK_event_registration_policies_events_EventId" FOREIGN KEY ("EventId") REFERENCES events(id) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_event_registration_policies_EventId" ON event_registration_policies("EventId");

        CREATE TABLE IF NOT EXISTS event_participation_sources (
          id uuid PRIMARY KEY, "Code" text NOT NULL, "Name" text NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_event_participation_sources_Code" ON event_participation_sources("Code");
        CREATE TABLE IF NOT EXISTS event_participant_statuses (
          id uuid PRIMARY KEY, "Code" text NOT NULL, "Name" text NOT NULL,
          "Description" text NULL, "IsInitial" boolean NOT NULL, "IsTerminal" boolean NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_event_participant_statuses_Code" ON event_participant_statuses("Code");
        CREATE TABLE IF NOT EXISTS event_participant_roles (
          id uuid PRIMARY KEY, "Code" text NOT NULL, "Name" text NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_event_participant_roles_Code" ON event_participant_roles("Code");

        CREATE TABLE IF NOT EXISTS event_participants (
          id uuid PRIMARY KEY, rbacobject_id uuid NOT NULL,
          created_at timestamp with time zone NOT NULL, updated_at timestamp with time zone NOT NULL,
          created_by uuid NOT NULL, updated_by uuid NOT NULL,
          "EventId" uuid NOT NULL, "UserId" uuid NULL, "GroupId" uuid NULL,
          "SourceId" uuid NOT NULL, "StatusId" uuid NOT NULL, "RoleId" uuid NULL,
          "RespondedAt" timestamp with time zone NULL,
          "StatusChangedAt" timestamp with time zone NULL,
          CONSTRAINT ck_event_participant_target CHECK (
              ("UserId" IS NOT NULL AND "GroupId" IS NULL)
              OR ("UserId" IS NULL AND "GroupId" IS NOT NULL)),
          CONSTRAINT "FK_event_participants_events_EventId" FOREIGN KEY ("EventId") REFERENCES events(id) ON DELETE CASCADE,
          CONSTRAINT "FK_event_participants_event_participation_sources_SourceId" FOREIGN KEY ("SourceId") REFERENCES event_participation_sources(id) ON DELETE RESTRICT,
          CONSTRAINT "FK_event_participants_event_participant_statuses_StatusId" FOREIGN KEY ("StatusId") REFERENCES event_participant_statuses(id) ON DELETE RESTRICT,
          CONSTRAINT "FK_event_participants_event_participant_roles_RoleId" FOREIGN KEY ("RoleId") REFERENCES event_participant_roles(id) ON DELETE RESTRICT
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_event_participants_EventId_UserId" ON event_participants("EventId", "UserId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_event_participants_EventId_GroupId" ON event_participants("EventId", "GroupId");
        CREATE INDEX IF NOT EXISTS "IX_event_participants_SourceId" ON event_participants("SourceId");
        CREATE INDEX IF NOT EXISTS "IX_event_participants_StatusId" ON event_participants("StatusId");
        CREATE INDEX IF NOT EXISTS "IX_event_participants_RoleId" ON event_participants("RoleId");
        """;

    private const string SeedSql = """
        INSERT INTO event_participant_statuses (id,"Code","Name","Description","IsInitial","IsTerminal") VALUES
        ('01b96c1d-8389-4267-a859-d8116e1c32f3','invited','Pozván','Uživatel byl pozván na událost',true,false),
        ('7d2ef223-b60e-4e6d-b7d5-5fdc1f8e2ec2','accepted','Přijal','Uživatel potvrdil účast',false,false),
        ('d6a5e9e4-3e47-4c95-a4aa-b194dd2bc3a7','declined','Odmítl','Uživatel odmítl účast',false,true),
        ('d7c38ef9-c7d0-4ff9-a72e-fd1e0b70f387','attended','Účastnil se','Uživatel se účastnil události',false,true),
        ('b0b2df1d-4e67-47c3-9c68-4d25a0e0a01a','excused','Omluven','Uživatel se omluvil z účasti',false,true),
        ('e27be0d6-3774-4378-bafb-8d596741da71','pending_approval','Čeká na schválení',NULL,true,false)
        ON CONFLICT (id) DO NOTHING;
        INSERT INTO event_participant_roles(id,"Code","Name") VALUES
        ('3265a488-bbfa-4c59-946c-7a7b059ee4f0','organizer','Organizátor'),
        ('f4ef4d25-ed76-447d-86ed-4d35346995f4','participant','Účastník')
        ON CONFLICT (id) DO NOTHING;
        INSERT INTO event_participation_sources(id,"Code","Name") VALUES
        ('a9a5aff3-c8cd-4dd9-8248-142d5028bdf2','invitation','Pozvání'),
        ('4c17ed6f-b149-4552-8da0-50f3d8770324','self_registration','Samostatná registrace'),
        ('6a98f445-cc04-4664-bf03-469aa77bc61c','assignment','Přiřazení')
        ON CONFLICT (id) DO NOTHING;
        """;
}
