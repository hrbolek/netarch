using System.Text;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

public static class RbacOperations
{
    public const string Create = "create";
    public const string Read = "read";
    public const string Update = "update";
    public const string Delete = "delete";
    public const string List = "list";
    public const string Vector = "vector";
    public const string ManageGroups = "manage_groups";
    public const string ManageMemberships = "manage_memberships";
    public const string ManageRoles = "manage_roles";
    public const string ManageSecurityObjects = "manage_security_objects";
}

public static class RbacWellKnown
{
    public static readonly Guid RootGroupId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid RootSecurityObjectId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    public static readonly Guid BootstrapAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}

public sealed record FakeEntraSeedUser(
    Guid id,
    string Name,
    string Email,
    string RootRoleType
);

public static class FakeEntraSeedUsers
{
    public static readonly FakeEntraSeedUser JohnNewbie = new(
        Guid.Parse("97c80a87-8206-4845-98df-2af25ed33916"),
        "John Newbie",
        "john.newbie@world.com",
        "Rector");

    public static readonly FakeEntraSeedUser PlannerAdmin = new(
        Guid.Parse("b06d811c-6945-4c74-bbc7-0222537d2e13"),
        "Planner Admin",
        "planner.admin@world.com",
        "Administrator");

    public static readonly FakeEntraSeedUser Auditor = new(
        Guid.Parse("7c03699a-f3c2-48b5-854f-8302c47145c9"),
        "Auditor",
        "auditor@world.com",
        "Dean");

    public static IReadOnlyList<FakeEntraSeedUser> All { get; } =
    [
        JohnNewbie,
        PlannerAdmin,
        Auditor
    ];
}

public interface ICurrentActorProvider
{
    Guid? TryGetActorUserId(HttpContext httpContext);
}

public sealed class HeaderOrClaimActorProvider : ICurrentActorProvider
{
    public Guid? TryGetActorUserId(HttpContext httpContext)
    {
        if (TryParseClaim(httpContext.User, "oid", out var oid))
            return oid;

        if (TryParseClaim(httpContext.User, "sub", out var sub))
            return sub;

        if (TryParseClaim(httpContext.User, ClaimTypes.NameIdentifier, out var nid))
            return nid;

        if (TryParseValue(httpContext.Request.Headers["x-ms-client-principal-id"], out var easyAuthHeaderActor))
            return easyAuthHeaderActor;

        if (TryParseEasyAuthPrincipalActor(httpContext.Request.Headers["x-ms-client-principal"], out var easyAuthPrincipalActor))
            return easyAuthPrincipalActor;

        if (TryParseValue(httpContext.Request.Headers["X-Actor-UserId"], out var headerActor))
            return headerActor;

        if (TryParseValue(httpContext.Request.Query["actorUserId"], out var queryActor))
            return queryActor;

        return null;
    }

    private static bool TryParseClaim(ClaimsPrincipal principal, string claimType, out Guid userId)
    {
        userId = Guid.Empty;
        var value = principal.FindFirstValue(claimType);
        return TryParseValue(value, out userId);
    }

    private static bool TryParseValue(string? value, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Guid.TryParse(value, out userId);
    }

    private static bool TryParseEasyAuthPrincipalActor(string? encodedPrincipal, out Guid userId)
    {
        userId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(encodedPrincipal))
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
            var payload = JsonSerializer.Deserialize<EasyAuthPrincipalPayload>(json, EasyAuthPrincipalJsonOptions);
            if (payload?.claims is null)
                return false;

            foreach (var claim in payload.claims)
            {
                if (claim is null || !string.Equals(claim.typ, "oid", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryParseValue(claim.val, out userId))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static readonly JsonSerializerOptions EasyAuthPrincipalJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

public static class EasyEntraHeaderPrincipalExtensions
{
    public static IApplicationBuilder UseEasyEntraHeaderPrincipal(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (httpContext, next) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true &&
                TryBuildPrincipal(httpContext.Request.Headers, out var principal))
            {
                httpContext.User = principal;
            }

            await next();
        });
    }

    private static bool TryBuildPrincipal(IHeaderDictionary headers, out ClaimsPrincipal principal)
    {
        principal = new ClaimsPrincipal(new ClaimsIdentity());
        var claims = new List<Claim>();


        if (TryDecodeEasyAuthPrincipal(headers["x-ms-client-principal"], claims) ||
            TryBuildFallbackClaims(headers, claims))
        {
            principal = new ClaimsPrincipal(
                new ClaimsIdentity(claims, "EasyEntraProxy", ClaimTypes.Name, ClaimTypes.Role));
            return true;
        }

        return false;
    }

    private static bool TryDecodeEasyAuthPrincipal(string? encodedPrincipal, List<Claim> claims)
    {
        if (string.IsNullOrWhiteSpace(encodedPrincipal))
            return false;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedPrincipal));
            var payload = JsonSerializer.Deserialize<EasyAuthPrincipalPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload?.claims is null || payload.claims.Count == 0)
                return false;

            foreach (var claim in payload.claims)
            {
                if (claim is null || string.IsNullOrWhiteSpace(claim.typ) || string.IsNullOrWhiteSpace(claim.val))
                    continue;

                AddClaimMappings(claims, claim.typ, claim.val);
            }

            return claims.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryBuildFallbackClaims(IHeaderDictionary headers, List<Claim> claims)
    {
        var principalId = headers["x-ms-client-principal-id"].ToString();
        var principalName = headers["x-ms-client-principal-name"].ToString();

        if (string.IsNullOrWhiteSpace(principalId) && string.IsNullOrWhiteSpace(principalName))
            return false;

        if (!string.IsNullOrWhiteSpace(principalId))
        {
            AddClaimMappings(claims, "oid", principalId);
            AddClaimMappings(claims, "sub", principalId);
        }

        if (!string.IsNullOrWhiteSpace(principalName))
        {
            AddClaimMappings(claims, "name", principalName);
            AddClaimMappings(claims, "preferred_username", principalName);
            AddClaimMappings(claims, "email", principalName);
        }

        return claims.Count > 0;
    }

    private static void AddClaimMappings(List<Claim> claims, string claimType, string claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimType) || string.IsNullOrWhiteSpace(claimValue))
            return;

        claims.Add(new Claim(claimType, claimValue));

        if (string.Equals(claimType, "roles", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var role in claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                claims.Add(new Claim(ClaimTypes.Role, role));

            return;
        }

        if (string.Equals(claimType, "scp", StringComparison.OrdinalIgnoreCase))
        {
            var scopes = claimValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var scope in scopes)
            {
                claims.Add(new Claim("scp", scope));
                claims.Add(new Claim("scope", scope));
            }

            return;
        }

        if (string.Equals(claimType, "oid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(claimType, "sub", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, claimValue));
            return;
        }

        if (string.Equals(claimType, "name", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Name, claimValue));
            return;
        }

        if (string.Equals(claimType, "email", StringComparison.OrdinalIgnoreCase))
            claims.Add(new Claim(ClaimTypes.Email, claimValue));
    }
}

file sealed record EasyAuthPrincipalPayload(List<EasyAuthPrincipalClaim>? claims);
file sealed record EasyAuthPrincipalClaim(string? typ, string? val);

public interface IRbacAuthorizationService
{
    Task<bool> IsAllowedAsync(
        Guid actorUserId,
        Guid rbacObjectId,
        string operation,
        CancellationToken cancellationToken = default);

    Task<bool> CanAssignDeputyRoleAsync(
        Guid actorUserId,
        Guid groupId,
        string roleType,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken = default);
}

public interface IRbacAdminService
{
    Task<RbacGroupRecord> CreateGroupAsync(
        CreateGroupRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<RbacMembershipRecord> AddMembershipAsync(
        AddMembershipRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<RbacRoleRecord> AssignRoleAsync(
        AssignRoleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<RbacSecurityObjectRecord> CreateSecurityObjectAsync(
        CreateSecurityObjectRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task UpsertRoleTypeAsync(
        UpsertRoleTypeRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task UpsertRoleTypeOperationAsync(
        UpsertRoleTypeOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record RbacGroupRecord(
    Guid id,
    string Name,
    Guid? master_group_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
);

public sealed record RbacMembershipRecord(
    Guid id,
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
);

public sealed record RbacRoleRecord(
    Guid id,
    Guid group_id,
    Guid user_id,
    string role_type,
    bool is_deputy,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to,
    Guid? delegated_by_role_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
);

public sealed record RbacSecurityObjectRecord(
    Guid id,
    string Name,
    Guid? group_id,
    Guid? parent_security_object_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by
);

public sealed record CreateGroupRequest(
    string Name,
    Guid? master_group_id = null
);

public sealed record AddMembershipRequest(
    Guid group_id,
    Guid user_id,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null
);

public sealed record AssignRoleRequest(
    Guid group_id,
    Guid user_id,
    string role_type,
    DateTimeOffset valid_from,
    DateTimeOffset? valid_to = null,
    bool is_deputy = false
);

public sealed record CreateSecurityObjectRequest(
    string Name,
    Guid? group_id,
    Guid? parent_security_object_id = null
);

public sealed record UpsertRoleTypeRequest(
    string role_type
);

public sealed record UpsertRoleTypeOperationRequest(
    string role_type,
    string operation
);

public sealed class SqliteRbacService
    : IRbacAuthorizationService, IRbacAdminService
{
    private readonly string _connectionString;

    public SqliteRbacService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string must not be empty.", nameof(connectionString));

        _connectionString = connectionString;
        EnsureDatabase();
    }

    public async Task<bool> IsAllowedAsync(
        Guid actorUserId,
        Guid rbacObjectId,
        string operation,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
            return false;

        var normalizedOperation = NormalizeToken(operation);
        var now = DateTimeOffset.UtcNow;

        var securityAncestors = await GetSecurityObjectAncestorsAsync(
            rbacObjectId,
            cancellationToken);

        foreach (var securityObject in securityAncestors)
        {
            if (securityObject.group_id is null)
                continue;

            var groupLineage = await GetGroupLineageAsync(
                securityObject.group_id.Value,
                cancellationToken);

            foreach (var groupId in groupLineage)
            {
                var membershipActive = await HasActiveMembershipAsync(
                    actorUserId,
                    groupId,
                    now,
                    cancellationToken);

                if (!membershipActive)
                    continue;

                var activeRoles = await GetActiveRolesAsync(
                    actorUserId,
                    groupId,
                    now,
                    cancellationToken);

                foreach (var role in activeRoles)
                {
                    var allowed = await RoleTypeAllowsOperationAsync(
                        role.role_type,
                        normalizedOperation,
                        cancellationToken);

                    if (allowed)
                        return true;
                }
            }
        }

        return false;
    }

    public async Task<bool> CanAssignDeputyRoleAsync(
        Guid actorUserId,
        Guid groupId,
        string roleType,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        CancellationToken cancellationToken = default)
    {
        var normalizedRoleType = NormalizeRoleType(roleType);
        var actorRoles = await GetActiveRolesAsync(
            actorUserId,
            groupId,
            validFrom,
            cancellationToken);

        foreach (var actorRole in actorRoles)
        {
            if (actorRole.is_deputy)
                continue;

            if (!string.Equals(
                    NormalizeRoleType(actorRole.role_type),
                    normalizedRoleType,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsWithin(validFrom, validTo, actorRole.valid_from, actorRole.valid_to))
                continue;

            return true;
        }

        return false;
    }

    public async Task<RbacGroupRecord> CreateGroupAsync(
        CreateGroupRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var allowed = await IsAllowedAsync(
            actorUserId,
            RbacWellKnown.RootSecurityObjectId,
            RbacOperations.ManageGroups,
            cancellationToken);

        if (!allowed)
            throw new InvalidOperationException("Actor is not allowed to create groups.");

        var now = DateTimeOffset.UtcNow;
        var model = new RbacGroupRecord(
            Guid.NewGuid(),
            request.Name.Trim(),
            request.master_group_id,
            now,
            now,
            actorUserId,
            actorUserId);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_groups
                (id, name, master_group_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $name, $master_group_id, $created_at, $updated_at, $created_by, $updated_by);
            """;

        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$name", model.Name);
        command.Parameters.AddWithValue("$master_group_id", model.master_group_id?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$created_at", ToIso(model.created_at));
        command.Parameters.AddWithValue("$updated_at", ToIso(model.updated_at));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        return model;
    }

    public async Task<RbacMembershipRecord> AddMembershipAsync(
        AddMembershipRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.valid_from, request.valid_to);

        var allowed = await IsAllowedAsync(
            actorUserId,
            RbacWellKnown.RootSecurityObjectId,
            RbacOperations.ManageMemberships,
            cancellationToken);

        if (!allowed)
            throw new InvalidOperationException("Actor is not allowed to manage memberships.");

        var now = DateTimeOffset.UtcNow;
        var model = new RbacMembershipRecord(
            Guid.NewGuid(),
            request.group_id,
            request.user_id,
            request.valid_from,
            request.valid_to,
            now,
            now,
            actorUserId,
            actorUserId);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_group_memberships
                (id, group_id, user_id, valid_from, valid_to, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $group_id, $user_id, $valid_from, $valid_to, $created_at, $updated_at, $created_by, $updated_by);
            """;

        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$group_id", model.group_id.ToString());
        command.Parameters.AddWithValue("$user_id", model.user_id.ToString());
        command.Parameters.AddWithValue("$valid_from", ToIso(model.valid_from));
        command.Parameters.AddWithValue("$valid_to", model.valid_to is null ? (object)DBNull.Value : ToIso(model.valid_to.Value));
        command.Parameters.AddWithValue("$created_at", ToIso(model.created_at));
        command.Parameters.AddWithValue("$updated_at", ToIso(model.updated_at));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        return model;
    }

    public async Task<RbacRoleRecord> AssignRoleAsync(
        AssignRoleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.role_type))
            throw new InvalidOperationException("Role type is required.");

        ValidatePeriod(request.valid_from, request.valid_to);

        var normalizedRoleType = NormalizeRoleType(request.role_type);
        var now = DateTimeOffset.UtcNow;

        if (request.is_deputy)
        {
            var canAssignDeputy = await CanAssignDeputyRoleAsync(
                actorUserId,
                request.group_id,
                normalizedRoleType,
                request.valid_from,
                request.valid_to,
                cancellationToken);

            if (!canAssignDeputy)
            {
                throw new InvalidOperationException(
                    "Deputy role can be assigned only by non-deputy holder of the same role type in the same group and only within assigner's validity period.");
            }
        }
        else
        {
            var allowed = await IsAllowedAsync(
                actorUserId,
                RbacWellKnown.RootSecurityObjectId,
                RbacOperations.ManageRoles,
                cancellationToken);

            if (!allowed)
                throw new InvalidOperationException("Actor is not allowed to assign roles.");
        }

        var model = new RbacRoleRecord(
            Guid.NewGuid(),
            request.group_id,
            request.user_id,
            normalizedRoleType,
            request.is_deputy,
            request.valid_from,
            request.valid_to,
            delegated_by_role_id: null,
            created_at: now,
            updated_at: now,
            created_by: actorUserId,
            updated_by: actorUserId);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_group_roles
                (id, group_id, user_id, role_type, is_deputy, valid_from, valid_to, delegated_by_role_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $group_id, $user_id, $role_type, $is_deputy, $valid_from, $valid_to, $delegated_by_role_id, $created_at, $updated_at, $created_by, $updated_by);
            """;

        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$group_id", model.group_id.ToString());
        command.Parameters.AddWithValue("$user_id", model.user_id.ToString());
        command.Parameters.AddWithValue("$role_type", model.role_type);
        command.Parameters.AddWithValue("$is_deputy", model.is_deputy ? 1 : 0);
        command.Parameters.AddWithValue("$valid_from", ToIso(model.valid_from));
        command.Parameters.AddWithValue("$valid_to", model.valid_to is null ? (object)DBNull.Value : ToIso(model.valid_to.Value));
        command.Parameters.AddWithValue("$delegated_by_role_id", model.delegated_by_role_id?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$created_at", ToIso(model.created_at));
        command.Parameters.AddWithValue("$updated_at", ToIso(model.updated_at));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        return model;
    }

    public async Task<RbacSecurityObjectRecord> CreateSecurityObjectAsync(
        CreateSecurityObjectRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Security object name is required.");

        var targetParent = request.parent_security_object_id ?? RbacWellKnown.RootSecurityObjectId;

        var allowed = await IsAllowedAsync(
            actorUserId,
            targetParent,
            RbacOperations.ManageSecurityObjects,
            cancellationToken);

        if (!allowed)
            throw new InvalidOperationException("Actor is not allowed to create security objects.");

        var now = DateTimeOffset.UtcNow;
        var model = new RbacSecurityObjectRecord(
            id: Guid.NewGuid(),
            Name: request.Name.Trim(),
            group_id: request.group_id,
            parent_security_object_id: request.parent_security_object_id,
            created_at: now,
            updated_at: now,
            created_by: actorUserId,
            updated_by: actorUserId);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO rbac_security_objects
                (id, name, group_id, parent_security_object_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $name, $group_id, $parent_security_object_id, $created_at, $updated_at, $created_by, $updated_by);
            """;

        command.Parameters.AddWithValue("$id", model.id.ToString());
        command.Parameters.AddWithValue("$name", model.Name);
        command.Parameters.AddWithValue("$group_id", model.group_id?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$parent_security_object_id", model.parent_security_object_id?.ToString() ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$created_at", ToIso(model.created_at));
        command.Parameters.AddWithValue("$updated_at", ToIso(model.updated_at));
        command.Parameters.AddWithValue("$created_by", model.created_by.ToString());
        command.Parameters.AddWithValue("$updated_by", model.updated_by.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
        return model;
    }

    public async Task UpsertRoleTypeAsync(
        UpsertRoleTypeRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var roleType = NormalizeRoleType(request.role_type);
        if (string.IsNullOrWhiteSpace(roleType))
            throw new InvalidOperationException("Role type is required.");

        var allowed = await IsAllowedAsync(
            actorUserId,
            RbacWellKnown.RootSecurityObjectId,
            RbacOperations.ManageRoles,
            cancellationToken);

        if (!allowed)
            throw new InvalidOperationException("Actor is not allowed to manage role types.");

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO rbac_role_types (name)
            VALUES ($name);
            """;
        command.Parameters.AddWithValue("$name", roleType);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertRoleTypeOperationAsync(
        UpsertRoleTypeOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var roleType = NormalizeRoleType(request.role_type);
        var operation = NormalizeToken(request.operation);

        if (string.IsNullOrWhiteSpace(roleType))
            throw new InvalidOperationException("Role type is required.");

        if (string.IsNullOrWhiteSpace(operation))
            throw new InvalidOperationException("Operation is required.");

        var allowed = await IsAllowedAsync(
            actorUserId,
            RbacWellKnown.RootSecurityObjectId,
            RbacOperations.ManageRoles,
            cancellationToken);

        if (!allowed)
            throw new InvalidOperationException("Actor is not allowed to manage role operations.");

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var roleTypeCommand = connection.CreateCommand();
        roleTypeCommand.CommandText = """
            INSERT OR IGNORE INTO rbac_role_types (name)
            VALUES ($name);
            """;
        roleTypeCommand.Parameters.AddWithValue("$name", roleType);
        await roleTypeCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR IGNORE INTO rbac_role_type_operations (role_type, operation)
            VALUES ($role_type, $operation);
            """;
        command.Parameters.AddWithValue("$role_type", roleType);
        command.Parameters.AddWithValue("$operation", operation);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<RbacSecurityObjectRecord>> GetSecurityObjectAncestorsAsync(
        Guid rbacObjectId,
        CancellationToken cancellationToken)
    {
        var result = new List<RbacSecurityObjectRecord>();
        var visited = new HashSet<Guid>();
        var current = rbacObjectId;

        while (visited.Add(current))
        {
            var model = await GetSecurityObjectByIdAsync(current, cancellationToken);
            if (model is null)
                break;

            result.Add(model);

            if (model.parent_security_object_id is null)
                break;

            current = model.parent_security_object_id.Value;
        }

        return result;
    }

    private async Task<RbacSecurityObjectRecord?> GetSecurityObjectByIdAsync(
        Guid securityObjectId,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, name, group_id, parent_security_object_id, created_at, updated_at, created_by, updated_by
            FROM rbac_security_objects
            WHERE id = $id
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$id", securityObjectId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return ReadSecurityObject(reader);
    }

    private async Task<IReadOnlyCollection<Guid>> GetGroupLineageAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var result = new List<Guid>();
        var visited = new HashSet<Guid>();
        var current = groupId;

        while (visited.Add(current))
        {
            result.Add(current);

            var parent = await GetMasterGroupIdAsync(current, cancellationToken);
            if (parent is null)
                break;

            current = parent.Value;
        }

        return result;
    }

    private async Task<Guid?> GetMasterGroupIdAsync(Guid groupId, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT master_group_id
            FROM rbac_groups
            WHERE id = $id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$id", groupId.ToString());

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return TryReadNullableGuid(scalar);
    }

    private async Task<bool> HasActiveMembershipAsync(
        Guid userId,
        Guid groupId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM rbac_group_memberships
            WHERE user_id = $user_id
              AND group_id = $group_id
              AND valid_from <= $now
              AND (valid_to IS NULL OR valid_to >= $now)
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$user_id", userId.ToString());
        command.Parameters.AddWithValue("$group_id", groupId.ToString());
        command.Parameters.AddWithValue("$now", ToIso(now));

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null;
    }

    private async Task<IReadOnlyCollection<RbacRoleRecord>> GetActiveRolesAsync(
        Guid userId,
        Guid groupId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, group_id, user_id, role_type, is_deputy, valid_from, valid_to, delegated_by_role_id, created_at, updated_at, created_by, updated_by
            FROM rbac_group_roles
            WHERE user_id = $user_id
              AND group_id = $group_id
              AND valid_from <= $now
              AND (valid_to IS NULL OR valid_to >= $now);
            """;

        command.Parameters.AddWithValue("$user_id", userId.ToString());
        command.Parameters.AddWithValue("$group_id", groupId.ToString());
        command.Parameters.AddWithValue("$now", ToIso(now));

        var result = new List<RbacRoleRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadRole(reader));
        }

        return result;
    }

    private async Task<bool> RoleTypeAllowsOperationAsync(
        string roleType,
        string operation,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT 1
            FROM rbac_role_type_operations
            WHERE role_type = $role_type
              AND operation = $operation
            LIMIT 1;
            """;

        command.Parameters.AddWithValue("$role_type", NormalizeRoleType(roleType));
        command.Parameters.AddWithValue("$operation", NormalizeToken(operation));

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null;
    }

    private SqliteConnection CreateConnection()
        => new(_connectionString);

    private void EnsureDatabase()
    {
        EnsureDataDirectoryExists();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS rbac_groups (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                master_group_id TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS rbac_group_memberships (
                id TEXT PRIMARY KEY,
                group_id TEXT NOT NULL,
                user_id TEXT NOT NULL,
                valid_from TEXT NOT NULL,
                valid_to TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS rbac_group_roles (
                id TEXT PRIMARY KEY,
                group_id TEXT NOT NULL,
                user_id TEXT NOT NULL,
                role_type TEXT NOT NULL,
                is_deputy INTEGER NOT NULL,
                valid_from TEXT NOT NULL,
                valid_to TEXT NULL,
                delegated_by_role_id TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS rbac_role_types (
                name TEXT PRIMARY KEY
            );

            CREATE TABLE IF NOT EXISTS rbac_role_type_operations (
                role_type TEXT NOT NULL,
                operation TEXT NOT NULL,
                PRIMARY KEY (role_type, operation)
            );

            CREATE TABLE IF NOT EXISTS rbac_security_objects (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                group_id TEXT NULL,
                parent_security_object_id TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                created_by TEXT NOT NULL,
                updated_by TEXT NOT NULL
            );
            """;

        command.ExecuteNonQuery();
        SeedDefaults(connection);
    }

    private void SeedDefaults(SqliteConnection connection)
    {
        var now = DateTimeOffset.UtcNow;

        using (var rootGroupCommand = connection.CreateCommand())
        {
            rootGroupCommand.CommandText = """
                INSERT OR IGNORE INTO rbac_groups
                    (id, name, master_group_id, created_at, updated_at, created_by, updated_by)
                VALUES
                    ($id, $name, NULL, $created_at, $updated_at, $created_by, $updated_by);
                """;

            rootGroupCommand.Parameters.AddWithValue("$id", RbacWellKnown.RootGroupId.ToString());
            rootGroupCommand.Parameters.AddWithValue("$name", "ROOT");
            rootGroupCommand.Parameters.AddWithValue("$created_at", ToIso(now));
            rootGroupCommand.Parameters.AddWithValue("$updated_at", ToIso(now));
            rootGroupCommand.Parameters.AddWithValue("$created_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            rootGroupCommand.Parameters.AddWithValue("$updated_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            rootGroupCommand.ExecuteNonQuery();
        }

        using (var rootSecurityCommand = connection.CreateCommand())
        {
            rootSecurityCommand.CommandText = """
                INSERT OR IGNORE INTO rbac_security_objects
                    (id, name, group_id, parent_security_object_id, created_at, updated_at, created_by, updated_by)
                VALUES
                    ($id, $name, $group_id, NULL, $created_at, $updated_at, $created_by, $updated_by);
                """;

            rootSecurityCommand.Parameters.AddWithValue("$id", RbacWellKnown.RootSecurityObjectId.ToString());
            rootSecurityCommand.Parameters.AddWithValue("$name", "ROOT_SECURITY_OBJECT");
            rootSecurityCommand.Parameters.AddWithValue("$group_id", RbacWellKnown.RootGroupId.ToString());
            rootSecurityCommand.Parameters.AddWithValue("$created_at", ToIso(now));
            rootSecurityCommand.Parameters.AddWithValue("$updated_at", ToIso(now));
            rootSecurityCommand.Parameters.AddWithValue("$created_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            rootSecurityCommand.Parameters.AddWithValue("$updated_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            rootSecurityCommand.ExecuteNonQuery();
        }

        SeedRoleTypeIfMissing(connection, "Administrator", new[]
        {
            RbacOperations.Create,
            RbacOperations.Read,
            RbacOperations.Update,
            RbacOperations.Delete,
            RbacOperations.List,
            RbacOperations.Vector,
            RbacOperations.ManageGroups,
            RbacOperations.ManageMemberships,
            RbacOperations.ManageRoles,
            RbacOperations.ManageSecurityObjects
        });

        SeedRoleTypeIfMissing(connection, "Rector", new[]
        {
            RbacOperations.Read,
            RbacOperations.List,
            RbacOperations.Vector
        });

        SeedRoleTypeIfMissing(connection, "Dean", new[]
        {
            RbacOperations.Read,
            RbacOperations.List,
            RbacOperations.Vector
        });

        SeedRoleTypeIfMissing(connection, "Study Administrator", new[]
        {
            RbacOperations.Create,
            RbacOperations.Read,
            RbacOperations.Update,
            RbacOperations.List,
            RbacOperations.Vector
        });

        using (var membershipSeed = connection.CreateCommand())
        {
            membershipSeed.CommandText = """
                INSERT OR IGNORE INTO rbac_group_memberships
                    (id, group_id, user_id, valid_from, valid_to, created_at, updated_at, created_by, updated_by)
                VALUES
                    ($id, $group_id, $user_id, $valid_from, NULL, $created_at, $updated_at, $created_by, $updated_by);
                """;

            membershipSeed.Parameters.AddWithValue("$id", Guid.Parse("30000000-0000-0000-0000-000000000001").ToString());
            membershipSeed.Parameters.AddWithValue("$group_id", RbacWellKnown.RootGroupId.ToString());
            membershipSeed.Parameters.AddWithValue("$user_id", RbacWellKnown.BootstrapAdminUserId.ToString());
            membershipSeed.Parameters.AddWithValue("$valid_from", ToIso(now));
            membershipSeed.Parameters.AddWithValue("$created_at", ToIso(now));
            membershipSeed.Parameters.AddWithValue("$updated_at", ToIso(now));
            membershipSeed.Parameters.AddWithValue("$created_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            membershipSeed.Parameters.AddWithValue("$updated_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            membershipSeed.ExecuteNonQuery();
        }

        for (var index = 0; index < FakeEntraSeedUsers.All.Count; index++)
        {
            var user = FakeEntraSeedUsers.All[index];
            SeedRootMembership(
                connection,
                Guid.Parse($"30000000-0000-0000-0000-{(index + 2).ToString("D12")}"),
                user.id,
                now,
                RbacWellKnown.BootstrapAdminUserId);
        }

        using (var roleSeed = connection.CreateCommand())
        {
            roleSeed.CommandText = """
                INSERT OR IGNORE INTO rbac_group_roles
                    (id, group_id, user_id, role_type, is_deputy, valid_from, valid_to, delegated_by_role_id, created_at, updated_at, created_by, updated_by)
                VALUES
                    ($id, $group_id, $user_id, $role_type, 0, $valid_from, NULL, NULL, $created_at, $updated_at, $created_by, $updated_by);
                """;

            roleSeed.Parameters.AddWithValue("$id", Guid.Parse("40000000-0000-0000-0000-000000000001").ToString());
            roleSeed.Parameters.AddWithValue("$group_id", RbacWellKnown.RootGroupId.ToString());
            roleSeed.Parameters.AddWithValue("$user_id", RbacWellKnown.BootstrapAdminUserId.ToString());
            roleSeed.Parameters.AddWithValue("$role_type", NormalizeRoleType("Administrator"));
            roleSeed.Parameters.AddWithValue("$valid_from", ToIso(now));
            roleSeed.Parameters.AddWithValue("$created_at", ToIso(now));
            roleSeed.Parameters.AddWithValue("$updated_at", ToIso(now));
            roleSeed.Parameters.AddWithValue("$created_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            roleSeed.Parameters.AddWithValue("$updated_by", RbacWellKnown.BootstrapAdminUserId.ToString());
            roleSeed.ExecuteNonQuery();
        }

        for (var index = 0; index < FakeEntraSeedUsers.All.Count; index++)
        {
            var user = FakeEntraSeedUsers.All[index];
            SeedRootRole(
                connection,
                Guid.Parse($"40000000-0000-0000-0000-{(index + 2).ToString("D12")}"),
                user.id,
                user.RootRoleType,
                now,
                RbacWellKnown.BootstrapAdminUserId);
        }
    }

    private static void SeedRootMembership(
        SqliteConnection connection,
        Guid membershipId,
        Guid userId,
        DateTimeOffset now,
        Guid actorUserId)
    {
        using var membershipSeed = connection.CreateCommand();
        membershipSeed.CommandText = """
            INSERT OR IGNORE INTO rbac_group_memberships
                (id, group_id, user_id, valid_from, valid_to, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $group_id, $user_id, $valid_from, NULL, $created_at, $updated_at, $created_by, $updated_by);
            """;

        membershipSeed.Parameters.AddWithValue("$id", membershipId.ToString());
        membershipSeed.Parameters.AddWithValue("$group_id", RbacWellKnown.RootGroupId.ToString());
        membershipSeed.Parameters.AddWithValue("$user_id", userId.ToString());
        membershipSeed.Parameters.AddWithValue("$valid_from", ToIso(now));
        membershipSeed.Parameters.AddWithValue("$created_at", ToIso(now));
        membershipSeed.Parameters.AddWithValue("$updated_at", ToIso(now));
        membershipSeed.Parameters.AddWithValue("$created_by", actorUserId.ToString());
        membershipSeed.Parameters.AddWithValue("$updated_by", actorUserId.ToString());
        membershipSeed.ExecuteNonQuery();
    }

    private static void SeedRootRole(
        SqliteConnection connection,
        Guid roleId,
        Guid userId,
        string roleType,
        DateTimeOffset now,
        Guid actorUserId)
    {
        using var roleSeed = connection.CreateCommand();
        roleSeed.CommandText = """
            INSERT OR IGNORE INTO rbac_group_roles
                (id, group_id, user_id, role_type, is_deputy, valid_from, valid_to, delegated_by_role_id, created_at, updated_at, created_by, updated_by)
            VALUES
                ($id, $group_id, $user_id, $role_type, 0, $valid_from, NULL, NULL, $created_at, $updated_at, $created_by, $updated_by);
            """;

        roleSeed.Parameters.AddWithValue("$id", roleId.ToString());
        roleSeed.Parameters.AddWithValue("$group_id", RbacWellKnown.RootGroupId.ToString());
        roleSeed.Parameters.AddWithValue("$user_id", userId.ToString());
        roleSeed.Parameters.AddWithValue("$role_type", NormalizeRoleType(roleType));
        roleSeed.Parameters.AddWithValue("$valid_from", ToIso(now));
        roleSeed.Parameters.AddWithValue("$created_at", ToIso(now));
        roleSeed.Parameters.AddWithValue("$updated_at", ToIso(now));
        roleSeed.Parameters.AddWithValue("$created_by", actorUserId.ToString());
        roleSeed.Parameters.AddWithValue("$updated_by", actorUserId.ToString());
        roleSeed.ExecuteNonQuery();
    }

    private static void SeedRoleTypeIfMissing(SqliteConnection connection, string roleType, IEnumerable<string> operations)
    {
        var normalizedRoleType = NormalizeRoleType(roleType);

        using (var roleTypeCommand = connection.CreateCommand())
        {
            roleTypeCommand.CommandText = """
                INSERT OR IGNORE INTO rbac_role_types (name)
                VALUES ($name);
                """;
            roleTypeCommand.Parameters.AddWithValue("$name", normalizedRoleType);
            roleTypeCommand.ExecuteNonQuery();
        }

        foreach (var operation in operations)
        {
            using var opCommand = connection.CreateCommand();
            opCommand.CommandText = """
                INSERT OR IGNORE INTO rbac_role_type_operations (role_type, operation)
                VALUES ($role_type, $operation);
                """;
            opCommand.Parameters.AddWithValue("$role_type", normalizedRoleType);
            opCommand.Parameters.AddWithValue("$operation", NormalizeToken(operation));
            opCommand.ExecuteNonQuery();
        }
    }

    private void EnsureDataDirectoryExists()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource))
            return;

        if (string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
            return;

        var directory = Path.GetDirectoryName(dataSource);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
    }

    private static void ValidatePeriod(DateTimeOffset validFrom, DateTimeOffset? validTo)
    {
        if (validTo is null)
            return;

        if (validTo.Value < validFrom)
            throw new InvalidOperationException("valid_to must be greater than or equal to valid_from.");
    }

    private static bool IsWithin(
        DateTimeOffset requestedFrom,
        DateTimeOffset? requestedTo,
        DateTimeOffset assignerFrom,
        DateTimeOffset? assignerTo)
    {
        if (requestedFrom < assignerFrom)
            return false;

        if (assignerTo is null)
            return true;

        if (requestedTo is null)
            return false;

        return requestedTo.Value <= assignerTo.Value;
    }

    private static string NormalizeRoleType(string roleType)
        => string.Join(' ', roleType.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizeToken(string token)
        => token.Trim().ToLowerInvariant();

    private static string ToIso(DateTimeOffset value)
        => value.ToString("O");

    private static DateTimeOffset ParseIso(string value)
        => DateTimeOffset.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);

    private static Guid? TryReadNullableGuid(object? value)
    {
        if (value is null || value is DBNull)
            return null;

        var text = Convert.ToString(value);
        return Guid.TryParse(text, out var guid)
            ? guid
            : null;
    }

    private static RbacRoleRecord ReadRole(SqliteDataReader reader)
    {
        return new RbacRoleRecord(
            id: Guid.Parse(reader.GetString(0)),
            group_id: Guid.Parse(reader.GetString(1)),
            user_id: Guid.Parse(reader.GetString(2)),
            role_type: reader.GetString(3),
            is_deputy: reader.GetInt64(4) != 0,
            valid_from: ParseIso(reader.GetString(5)),
            valid_to: reader.IsDBNull(6) ? null : ParseIso(reader.GetString(6)),
            delegated_by_role_id: reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7)),
            created_at: ParseIso(reader.GetString(8)),
            updated_at: ParseIso(reader.GetString(9)),
            created_by: Guid.Parse(reader.GetString(10)),
            updated_by: Guid.Parse(reader.GetString(11))
        );
    }

    private static RbacSecurityObjectRecord ReadSecurityObject(SqliteDataReader reader)
    {
        return new RbacSecurityObjectRecord(
            id: Guid.Parse(reader.GetString(0)),
            Name: reader.GetString(1),
            group_id: reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
            parent_security_object_id: reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
            created_at: ParseIso(reader.GetString(4)),
            updated_at: ParseIso(reader.GetString(5)),
            created_by: Guid.Parse(reader.GetString(6)),
            updated_by: Guid.Parse(reader.GetString(7))
        );
    }
}

public static class RbacServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteRbac(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<SqliteRbacService>(_ => new SqliteRbacService(connectionString));
        services.AddSingleton<IRbacAuthorizationService>(sp => sp.GetRequiredService<SqliteRbacService>());
        services.AddSingleton<IRbacAdminService>(sp => sp.GetRequiredService<SqliteRbacService>());
        services.AddSingleton<ICurrentActorProvider, HeaderOrClaimActorProvider>();

        return services;
    }
}

public static class RbacEndpointExtensions
{
    public static IEndpointRouteBuilder MapRbacEndpoints(this IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("RBAC");

        group.MapGet("/health", () => Results.Ok(new
        {
            status = "ok",
            root_group_id = RbacWellKnown.RootGroupId,
            root_security_object_id = RbacWellKnown.RootSecurityObjectId,
            bootstrap_admin_user_id = RbacWellKnown.BootstrapAdminUserId,
            actor_header = "X-Actor-UserId"
        }));

        group.MapPost("/group/create", async (
            CreateGroupRequest request,
            HttpContext httpContext,
            IRbacAdminService adminService,
            ICurrentActorProvider actorProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = actorProvider.TryGetActorUserId(httpContext);
            if (actor is null)
                return Results.Unauthorized();

            try
            {
                var result = await adminService.CreateGroupAsync(request, actor.Value, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.Forbid();
            }
        });

        group.MapPost("/group/member/add", async (
            AddMembershipRequest request,
            HttpContext httpContext,
            IRbacAdminService adminService,
            ICurrentActorProvider actorProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = actorProvider.TryGetActorUserId(httpContext);
            if (actor is null)
                return Results.Unauthorized();

            try
            {
                var result = await adminService.AddMembershipAsync(request, actor.Value, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException)
            {
                return Results.Forbid();
            }
        });

        group.MapPost("/group/role/assign", async (
            AssignRoleRequest request,
            HttpContext httpContext,
            IRbacAdminService adminService,
            ICurrentActorProvider actorProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = actorProvider.TryGetActorUserId(httpContext);
            if (actor is null)
                return Results.Unauthorized();

            try
            {
                var result = await adminService.AssignRoleAsync(request, actor.Value, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/security-object/create", async (
            CreateSecurityObjectRequest request,
            HttpContext httpContext,
            IRbacAdminService adminService,
            ICurrentActorProvider actorProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = actorProvider.TryGetActorUserId(httpContext);
            if (actor is null)
                return Results.Unauthorized();

            try
            {
                var result = await adminService.CreateSecurityObjectAsync(request, actor.Value, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/role-type/upsert", async (
            UpsertRoleTypeRequest request,
            HttpContext httpContext,
            IRbacAdminService adminService,
            ICurrentActorProvider actorProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = actorProvider.TryGetActorUserId(httpContext);
            if (actor is null)
                return Results.Unauthorized();

            try
            {
                await adminService.UpsertRoleTypeAsync(request, actor.Value, cancellationToken);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/role-type/operation/upsert", async (
            UpsertRoleTypeOperationRequest request,
            HttpContext httpContext,
            IRbacAdminService adminService,
            ICurrentActorProvider actorProvider,
            CancellationToken cancellationToken) =>
        {
            var actor = actorProvider.TryGetActorUserId(httpContext);
            if (actor is null)
                return Results.Unauthorized();

            try
            {
                await adminService.UpsertRoleTypeOperationAsync(request, actor.Value, cancellationToken);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/access/can", async (
            Guid userId,
            Guid rbacId,
            string operation,
            IRbacAuthorizationService authService,
            CancellationToken cancellationToken) =>
        {
            var allowed = await authService.IsAllowedAsync(
                userId,
                rbacId,
                operation,
                cancellationToken);

            return Results.Ok(new
            {
                user_id = userId,
                rbac_id = rbacId,
                operation,
                allowed
            });
        });

        return endpoints;
    }
}
