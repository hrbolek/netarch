using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.AuthorizationLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Events;
using WebAppArchitecture.Facilities;
using WebAppArchitecture.Tests.Shared;
using Xunit;

namespace Facilities.Integration.Tests;

public sealed class FacilitiesPostgresFixture : IAsyncLifetime
{
    private readonly PostgresTestDatabase database = new();
    private readonly ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    public Guid RootId { get; } = Guid.NewGuid();
    public Guid RootTypeId { get; } = Guid.NewGuid();
    public Guid AdministratorId { get; } = Guid.NewGuid();
    public DateTimeOffset Now => DateTimeOffset.UtcNow;

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await FacilitiesSchemaBootstrap.EnsureAsync(context);
        var now = Now;
        context.GroupTypes.Add(new GroupTypeModel(RootTypeId, "Facilities test organization", null,
            $"/{RootTypeId:D}/", RootId, now, now, AdministratorId, AdministratorId));
        context.Groups.Add(new GroupModel(RootId, "Facilities test root", RootTypeId, null,
            $"/{RootId:D}/", true, RootId, now, now, AdministratorId, AdministratorId));
        context.Users.Add(new UserModel(AdministratorId, "Facilities test administrator",
            null, null, RootId, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        services.Dispose();
        database.Dispose();
        return Task.CompletedTask;
    }

    public FacilitiesTestContext CreateContext()
        => new(new DbContextOptionsBuilder<FacilitiesTestContext>()
            .UseNpgsql(database.ConnectionString).Options);

    public ActorServices ForActor(Guid actorId) => new(CreateContext(), services, actorId);

    public async Task<Guid> CreateActorAsync(string name)
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.Users.Add(new UserModel(id, name, null, null, RootId, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> CreateScopeAsync(string name, bool inheritRoles = true)
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var root = await context.Groups.SingleAsync(x => x.id == RootId);
        var now = Now;
        context.Groups.Add(new GroupModel(id, name, root.group_type_id, root.id,
            root.path + $"{id:D}/", inheritRoles, id, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
        return id;
    }

    public async Task GrantAsync(Guid actorId, Guid scopeId, params string[] operations)
    {
        if (operations.Length == 0) throw new ArgumentException("At least one operation is required.", nameof(operations));
        await using var context = CreateContext();
        var now = Now;
        var roleTypeId = Guid.NewGuid();
        context.RoleTypes.Add(new RoleTypeModel(roleTypeId, $"Facilities test {roleTypeId:N}",
            RootId, now, now, AdministratorId, AdministratorId));

        foreach (var operation in operations.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existingOperation = await context.Operations.SingleOrDefaultAsync(x => x.name == operation);
            var operationId = existingOperation?.id
                ?? (RbacOperationIds.All.TryGetValue(operation, out var wellKnownId) ? wellKnownId : Guid.NewGuid());
            if (existingOperation is null)
                context.Operations.Add(new OperationModel(operationId, operation, now, now, AdministratorId, AdministratorId));
            context.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleTypeId, operationId));
        }

        context.Roles.Add(new RoleModel(Guid.NewGuid(), scopeId, actorId, roleTypeId, false,
            now.AddDays(-1), null, null, scopeId, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
    }

    public async Task<Guid> GrantMoveAsync(Guid actorId, Guid scopeId)
    {
        var roleTypeId = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.RoleTypes.Add(new RoleTypeModel(roleTypeId, $"Facilities move {roleTypeId:N}",
            RootId, now, now, AdministratorId, AdministratorId));
        context.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleTypeId,
            FacilitiesSchemaBootstrap.MoveOperationId));
        context.Roles.Add(new RoleModel(Guid.NewGuid(), scopeId, actorId, roleTypeId, false,
            now.AddDays(-1), null, null, scopeId, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
        return roleTypeId;
    }

    public async Task<Guid> CreateRoleTypeAsync(string name, params string[] operations)
    {
        if (operations.Length == 0) throw new ArgumentException("At least one operation is required.", nameof(operations));
        await using var context = CreateContext();
        var now = Now;
        var roleTypeId = Guid.NewGuid();
        context.RoleTypes.Add(new RoleTypeModel(roleTypeId, name, RootId,
            now, now, AdministratorId, AdministratorId));
        foreach (var operation in operations.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existingOperation = await context.Operations.SingleOrDefaultAsync(x => x.name == operation);
            var operationId = existingOperation?.id
                ?? (RbacOperationIds.All.TryGetValue(operation, out var wellKnownId) ? wellKnownId : Guid.NewGuid());
            if (existingOperation is null)
                context.Operations.Add(new OperationModel(operationId, operation, now, now,
                    AdministratorId, AdministratorId));
            context.RoleTypeOperations.Add(new RoleTypeOperationModel(Guid.NewGuid(), roleTypeId, operationId));
        }
        await context.SaveChangesAsync();
        return roleTypeId;
    }

    public async Task<Guid> AssignRoleAsync(Guid roleTypeId, Guid actorId, Guid scopeId)
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.Roles.Add(new RoleModel(id, scopeId, actorId, roleTypeId, false,
            now.AddDays(-1), null, null, scopeId, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> AddMembershipAsync(Guid actorId, Guid scopeId)
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.Memberships.Add(new MembershipModel(id, scopeId, actorId,
            now.AddDays(-1), null, scopeId, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> AddDenyRuleAsync(Guid roleTypeId, Guid scopeId, bool includeDescendants = true)
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.RbacDenyRules.Add(new RbacDenyRuleModel(id, scopeId, roleTypeId,
            includeDescendants, scopeId, now, now, AdministratorId, AdministratorId));
        await context.SaveChangesAsync();
        return id;
    }

    public async Task RebuildAccessAsync()
    {
        await using var context = CreateContext();
        var db = new DBLayer(context, services);
        await new SecurityProjectionService(db).RebuildAllAsync();
    }

    public async Task<bool> IsAllowedAsync(Guid actorId, Guid scopeId, string operation)
    {
        await using var context = CreateContext();
        var db = new DBLayer(context, services);
        return await new RbacAuthorizationService(db).IsAllowedAsync(actorId, scopeId, operation);
    }

    public async Task<bool> HasDirectGrantAsync(Guid actorId, Guid scopeId, string operation)
    {
        await using var context = CreateContext();
        return await (
            from role in context.Roles
            join roleOperation in context.RoleTypeOperations
                on role.role_type_id equals roleOperation.role_type_id
            join op in context.Operations on roleOperation.operation_id equals op.id
            where role.user_id == actorId && role.group_id == scopeId && op.name == operation
            select role.id).AnyAsync();
    }

    public async Task<Guid> SeedEventAsync(Guid eventScopeId, string name = "Facilities test event")
    {
        var eventId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.Set<EventTypeModel>().Add(new EventTypeModel
        {
            id = typeId,
            Code = $"FAC-{typeId:N}",
            Name = "Facilities test event type",
            rbacobject_id = eventScopeId,
            path = $"/{typeId:D}/",
            created_at = now,
            updated_at = now,
            created_by = AdministratorId,
            updated_by = AdministratorId
        });
        context.Set<EventModel>().Add(new EventModel
        {
            id = eventId,
            Name = name,
            EventTypeId = typeId,
            StartsAt = now.AddDays(-1),
            EndsAt = now.AddDays(30),
            rbacobject_id = eventScopeId,
            path = $"/{eventId:D}/",
            created_at = now,
            updated_at = now,
            created_by = AdministratorId,
            updated_by = AdministratorId
        });
        await context.SaveChangesAsync();
        return eventId;
    }

    public async Task<Guid> SeedFacilityTypeAsync(Guid typeScopeId, bool reservable = true)
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.Set<FacilityTypeModel>().Add(new FacilityTypeModel
        {
            id = id,
            Code = $"FAC-{id:N}",
            Name = "Facilities test type",
            Reservable = reservable,
            CanContainChildren = true,
            rbacobject_id = typeScopeId,
            created_at = now,
            updated_at = now,
            created_by = AdministratorId,
            updated_by = AdministratorId
        });
        await context.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> SeedFacilityAsync(Guid facilityScopeId, Guid typeId, string name = "Facilities test room")
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.Set<FacilityModel>().Add(new FacilityModel
        {
            id = id,
            Code = $"FAC-{id:N}",
            Name = name,
            FacilityTypeId = typeId,
            IsActive = true,
            rbacobject_id = facilityScopeId,
            path = $"/{id:D}/",
            created_at = now,
            updated_at = now,
            created_by = AdministratorId,
            updated_by = AdministratorId
        });
        await context.SaveChangesAsync();
        return id;
    }

    public async Task<Guid> SeedReservationAsync(Guid facilityId, Guid eventId, Guid reservationScopeId,
        DateTimeOffset startsAt, DateTimeOffset endsAt, string status = ReservationStatuses.Requested)
    {
        var id = Guid.NewGuid();
        await using var context = CreateContext();
        var now = Now;
        context.Set<ReservationModel>().Add(new ReservationModel
        {
            id = id,
            FacilityId = facilityId,
            EventId = eventId,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = status,
            rbacobject_id = reservationScopeId,
            created_at = now,
            updated_at = now,
            created_by = AdministratorId,
            updated_by = AdministratorId
        });
        await context.SaveChangesAsync();
        return id;
    }

    public async Task<ReservationModel> ReadReservationAsync(Guid id)
    {
        await using var context = CreateContext();
        return await context.Set<ReservationModel>().AsNoTracking().SingleAsync(x => x.id == id);
    }

    public async Task<FacilityModel> ReadFacilityAsync(Guid id)
    {
        await using var context = CreateContext();
        return await context.Set<FacilityModel>().AsNoTracking().SingleAsync(x => x.id == id);
    }

    public async Task<IReadOnlyList<FacilityModel>> ReadFacilitiesAsync(params Guid[] ids)
    {
        await using var context = CreateContext();
        return await context.Set<FacilityModel>().AsNoTracking().Where(x => ids.Contains(x.id)).ToListAsync();
    }

    public async Task<IReadOnlyList<GroupModel>> ReadGroupsAsync(params Guid[] ids)
    {
        await using var context = CreateContext();
        return await context.Groups.AsNoTracking().Where(x => ids.Contains(x.id)).ToListAsync();
    }

    public async Task<int> CountGroupsAsync()
    {
        await using var context = CreateContext();
        return await context.Groups.CountAsync();
    }

    public async Task<IReadOnlyList<FacilityModel>> ReadFacilitySubtreeAsync(string path)
    {
        await using var context = CreateContext();
        return await context.Set<FacilityModel>().AsNoTracking()
            .Where(x => x.path.StartsWith(path)).OrderBy(x => x.path).ToListAsync();
    }

    public sealed class ActorServices : IDisposable
    {
        private readonly FacilitiesTestContext context;

        internal ActorServices(FacilitiesTestContext context, IServiceProvider services, Guid actorId)
        {
            this.context = context;
            var db = new DBLayer(context, services);
            var http = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("oid", actorId.ToString())], "FacilitiesIntegrationTest"))
            };
            var security = new AuthorizationLayer(db, new RbacAuthorizationService(db),
                new HeaderOrClaimActorProvider(), new HttpContextAccessor { HttpContext = http });
            FacilityTypes = new FacilityTypeService(security, db, context);
            Facilities = new FacilityService(security, db, context,
                new FacilityPermissionTree(security, db, context, new SecurityProjectionService(db)));
            var eventTypes = new EventTypeService(security, db);
            var events = new EventService(security, db, context, eventTypes);
            Reservations = new ReservationService(security, db, context, events);
        }

        public FacilityTypeService FacilityTypes { get; }
        public FacilityService Facilities { get; }
        public ReservationService Reservations { get; }
        public void Dispose() => context.Dispose();
    }
}

public sealed class FacilitiesTestContext(DbContextOptions<FacilitiesTestContext> options) : CoreDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureEvents();
        modelBuilder.ConfigureFacilities();
    }
}
