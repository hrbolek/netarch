using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebAppArchitecture.Base.Security;

namespace WebAppArchitecture.DBLayer;

public static class SystemDataImporter
{
    public const string DefaultSource =
        "https://raw.githubusercontent.com/hrbolek/_uois/latest/systemdata.hk2026.json";

    public static async Task<SystemDataImportResult> ImportAsync(
        ApplicationDbContext db,
        string source,
        CancellationToken cancellationToken = default)
    {
        await using var stream = await OpenSourceAsync(source, cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("System data JSON root must be an object.");

        var now = DateTimeOffset.UtcNow;
        var actor = RbacWellKnown.BootstrapAdminUserId;

        // Read source groups first; they are needed both for Group paths and for
        // deriving a useful parent tree for GroupType, because the source file
        // does not expose parent_group_type_id directly.
        var sourceGroups = ReadArray(root, "groups")
            .Select(ReadSourceGroup)
            .Where(x => x is not null)
            .Cast<SourceGroup>()
            .ToDictionary(x => x.Id);

        var sourceTypes = ReadArray(root, "grouptypes")
            .Select(ReadSourceGroupType)
            .Where(x => x is not null)
            .Cast<SourceGroupType>()
            .ToDictionary(x => x.Id);

        var inferredTypeParents = InferGroupTypeParents(sourceGroups.Values);
        var typePathCache = new Dictionary<Guid, string>();
        foreach (var type in sourceTypes.Values)
        {
            if (await db.GroupTypes.AnyAsync(x => x.id == type.Id, cancellationToken))
                continue;

            var parentId = inferredTypeParents.TryGetValue(type.Id, out var inferred)
                ? inferred
                : RbacWellKnown.RootGroupTypeId;

            if (parentId == type.Id || !sourceTypes.ContainsKey(parentId) && parentId != RbacWellKnown.RootGroupTypeId)
                parentId = RbacWellKnown.RootGroupTypeId;

            var path = BuildGroupTypePath(type.Id, sourceTypes, inferredTypeParents, typePathCache);
            db.GroupTypes.Add(new GroupTypeModel(
                type.Id,
                type.Name,
                parentId,
                path,
                type.RbacObjectId ?? RbacWellKnown.RootGroupId,
                type.CreatedAt ?? now,
                type.UpdatedAt ?? type.CreatedAt ?? now,
                actor,
                actor));
        }
        await db.SaveChangesAsync(cancellationToken);

        var groupPathCache = new Dictionary<Guid, string>();
        foreach (var group in sourceGroups.Values)
        {
            if (await db.Groups.AnyAsync(x => x.id == group.Id, cancellationToken))
                continue;

            // Every source root is attached below the technical application root.
            var masterId = group.MasterGroupId ?? RbacWellKnown.RootGroupId;
            var path = BuildGroupPath(group.Id, sourceGroups, groupPathCache);
            db.Groups.Add(new GroupModel(
                group.Id,
                group.Name,
                group.GroupTypeId,
                masterId,
                path,
                true,
                group.RbacObjectId ?? group.Id,
                group.CreatedAt ?? now,
                group.UpdatedAt ?? group.CreatedAt ?? now,
                actor,
                actor));
        }
        await db.SaveChangesAsync(cancellationToken);

        var userCount = 0;
        foreach (var element in ReadArray(root, "users"))
        {
            if (!TryGuid(element, "id", out var id) || await db.Users.AnyAsync(x => x.id == id, cancellationToken))
                continue;

            var name = String(element, "name")
                ?? JoinName(String(element, "firstname"), String(element, "familyname"))
                ?? JoinName(String(element, "firstname"), String(element, "surname"))
                ?? id.ToString();
            var email = String(element, "email");
            var surname = String(element, "surname") ?? String(element, "familyname");
            var rbacobjectId = GuidValue(element, "rbacobject_id") ?? RbacWellKnown.RootGroupId;
            var created = Date(element, "created") ?? now;
            var updated = Date(element, "lastchange") ?? created;
            db.Users.Add(new UserModel(
                id, name, email, surname, rbacobjectId, created, updated, actor, actor));
            userCount++;
        }
        await db.SaveChangesAsync(cancellationToken);

        var roleTypeCount = 0;
        foreach (var element in ReadArray(root, "roletypes"))
        {
            if (!TryGuid(element, "id", out var id) || await db.RoleTypes.AnyAsync(x => x.id == id, cancellationToken))
                continue;

            var name = String(element, "name") ?? id.ToString();
            var created = Date(element, "created") ?? now;
            var updated = Date(element, "lastchange") ?? created;
            db.RoleTypes.Add(new RoleTypeModel(
                id, name, GuidValue(element, "rbacobject_id") ?? RbacWellKnown.RootGroupId,
                created, updated, actor, actor));
            roleTypeCount++;
        }
        await db.SaveChangesAsync(cancellationToken);

        var roleCount = 0;
        foreach (var element in ReadArray(root, "roles"))
        {
            if (!TryGuid(element, "id", out var id)
                || !TryGuid(element, "group_id", out var groupId)
                || !TryGuid(element, "user_id", out var userId)
                || !TryGuid(element, "roletype_id", out var roleTypeId)
                || await db.Roles.AnyAsync(x => x.id == id, cancellationToken))
                continue;

            if (!sourceGroups.ContainsKey(groupId)
                || !await db.Users.AnyAsync(x => x.id == userId, cancellationToken)
                || !await db.RoleTypes.AnyAsync(x => x.id == roleTypeId, cancellationToken))
                continue;

            var created = Date(element, "created") ?? now;
            var updated = Date(element, "lastchange") ?? created;
            var validFrom = Date(element, "startdate") ?? Date(element, "valid_from") ?? created;
            var validTo = Date(element, "enddate") ?? Date(element, "valid_to");
            var createdBy = GuidValue(element, "createdby") ?? actor;
            var updatedBy = GuidValue(element, "changedby") ?? createdBy;

            db.Roles.Add(new RoleModel(
                id,
                groupId,
                userId,
                roleTypeId,
                false,
                validFrom,
                validTo,
                null,
                GuidValue(element, "rbacobject_id") ?? groupId,
                created,
                updated,
                createdBy,
                updatedBy));
            roleCount++;
        }
        await db.SaveChangesAsync(cancellationToken);

        var membershipCount = 0;
        foreach (var section in new[] { "memberships", "groupmemberships" })
        {
            foreach (var element in ReadArray(root, section))
            {
                if (!TryGuid(element, "id", out var id)
                    || !TryGuid(element, "group_id", out var groupId)
                    || !TryGuid(element, "user_id", out var userId)
                    || await db.Memberships.AnyAsync(x => x.id == id, cancellationToken))
                    continue;

                var created = Date(element, "created") ?? now;
                var updated = Date(element, "lastchange") ?? created;
                var validFrom = Date(element, "startdate") ?? Date(element, "valid_from") ?? created;
                var validTo = Date(element, "enddate") ?? Date(element, "valid_to");
                db.Memberships.Add(new MembershipModel(
                    id, groupId, userId, validFrom, validTo,
                    GuidValue(element, "rbacobject_id") ?? groupId,
                    created, updated,
                    GuidValue(element, "createdby") ?? actor,
                    GuidValue(element, "changedby") ?? actor));
                membershipCount++;
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        return new SystemDataImportResult(
            userCount,
            sourceGroups.Count,
            sourceTypes.Count,
            roleCount,
            roleTypeCount,
            membershipCount);
    }

    private static async Task<Stream> OpenSourceAsync(string source, CancellationToken cancellationToken)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            // Keep response/client alive together with returned stream.
            return new OwnedHttpStream(input, response, client);
        }

        return File.OpenRead(source);
    }

    private static IEnumerable<JsonElement> ReadArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
            yield break;
        foreach (var item in array.EnumerateArray())
            yield return item;
    }

    private static SourceGroup? ReadSourceGroup(JsonElement e)
    {
        if (!TryGuid(e, "id", out var id) || !TryGuid(e, "grouptype_id", out var typeId))
            return null;
        return new SourceGroup(
            id,
            String(e, "name") ?? id.ToString(),
            GuidValue(e, "mastergroup_id"),
            typeId,
            GuidValue(e, "rbacobject_id"),
            Date(e, "created"),
            Date(e, "lastchange"));
    }

    private static SourceGroupType? ReadSourceGroupType(JsonElement e)
    {
        if (!TryGuid(e, "id", out var id))
            return null;
        return new SourceGroupType(
            id,
            String(e, "name") ?? id.ToString(),
            GuidValue(e, "rbacobject_id"),
            Date(e, "created"),
            Date(e, "lastchange"));
    }

    private static Dictionary<Guid, Guid> InferGroupTypeParents(IEnumerable<SourceGroup> groups)
    {
        var byId = groups.ToDictionary(x => x.Id);
        var candidates = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var child in byId.Values)
        {
            if (child.MasterGroupId is not Guid parentGroupId || !byId.TryGetValue(parentGroupId, out var parent))
                continue;
            if (child.GroupTypeId == parent.GroupTypeId)
                continue;
            if (!candidates.TryGetValue(child.GroupTypeId, out var values))
                candidates[child.GroupTypeId] = values = new HashSet<Guid>();
            values.Add(parent.GroupTypeId);
        }

        return candidates
            .Where(x => x.Value.Count == 1)
            .ToDictionary(x => x.Key, x => x.Value.Single());
    }

    private static string BuildGroupPath(Guid id, IReadOnlyDictionary<Guid, SourceGroup> groups, IDictionary<Guid, string> cache)
    {
        if (cache.TryGetValue(id, out var cached))
            return cached;
        var visited = new HashSet<Guid>();
        var chain = new Stack<Guid>();
        var current = id;
        while (groups.TryGetValue(current, out var group) && visited.Add(current))
        {
            chain.Push(current);
            if (group.MasterGroupId is not Guid parent || !groups.ContainsKey(parent))
                break;
            current = parent;
        }
        var path = $"/{RbacWellKnown.RootGroupId:D}/" + string.Join('/', chain.Select(x => x.ToString("D"))) + "/";
        cache[id] = path;
        return path;
    }

    private static string BuildGroupTypePath(
        Guid id,
        IReadOnlyDictionary<Guid, SourceGroupType> types,
        IReadOnlyDictionary<Guid, Guid> parents,
        IDictionary<Guid, string> cache)
    {
        if (cache.TryGetValue(id, out var cached))
            return cached;
        var visited = new HashSet<Guid>();
        var chain = new Stack<Guid>();
        var current = id;
        while (types.ContainsKey(current) && visited.Add(current))
        {
            chain.Push(current);
            if (!parents.TryGetValue(current, out var parent) || !types.ContainsKey(parent))
                break;
            current = parent;
        }
        var path = $"/{RbacWellKnown.RootGroupTypeId:D}/" + string.Join('/', chain.Select(x => x.ToString("D"))) + "/";
        cache[id] = path;
        return path;
    }

    private static bool TryGuid(JsonElement e, string name, out Guid value)
    {
        value = Guid.Empty;
        return e.TryGetProperty(name, out var p)
            && p.ValueKind == JsonValueKind.String
            && Guid.TryParse(p.GetString(), out value);
    }

    private static Guid? GuidValue(JsonElement e, string name)
        => TryGuid(e, name, out var value) ? value : null;

    private static string? String(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static DateTimeOffset? Date(JsonElement e, string name)
    {
        var value = String(e, name);
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out var dto))
            return dto;
        return null;
    }

    private static string? JoinName(string? first, string? last)
    {
        var value = string.Join(' ', new[] { first, last }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed record SourceGroup(
        Guid Id, string Name, Guid? MasterGroupId, Guid GroupTypeId, Guid? RbacObjectId,
        DateTimeOffset? CreatedAt, DateTimeOffset? UpdatedAt);

    private sealed record SourceGroupType(
        Guid Id, string Name, Guid? RbacObjectId, DateTimeOffset? CreatedAt, DateTimeOffset? UpdatedAt);

    private sealed class OwnedHttpStream(Stream inner, HttpResponseMessage response, HttpClient client) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);
        protected override void Dispose(bool disposing)
        {
            if (disposing) { inner.Dispose(); response.Dispose(); client.Dispose(); }
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            response.Dispose(); client.Dispose();
            await base.DisposeAsync();
        }
    }
}
