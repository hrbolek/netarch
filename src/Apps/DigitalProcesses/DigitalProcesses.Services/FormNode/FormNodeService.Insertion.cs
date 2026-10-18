using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;
using WebAppArchitecture.DigitalProcesses.Services.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Services.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.AuthorizationLayer;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;

public sealed partial class FormNodeService
{
    public Task<FormNodeLibraryPage> SearchAsync(FormNodeLibraryQuery query, CancellationToken ct = default)
    {
        if (query.offset < 0 || query.limit is < 1 or > 50)
            throw new ArgumentException("Library paging requires offset >= 0 and limit between 1 and 50.");
        if (new[] { query.search, query.configuration, query.renderer_key }.Any(x => x?.Length > 500))
            throw new ArgumentException("Library filters must not exceed 500 characters.");
        var readable = Security.Query<FormNodeModel>(SecurityOperations.Read);
        var types = Security.Query<FormNodeTypeModel>(SecurityOperations.Read);
        var filtered = readable;
        if (query.node_type_id is Guid typeId) filtered = filtered.Where(x => x.node_type_id == typeId);
        if (query.form_version_id is Guid version) filtered = filtered.Where(x => x.form_version_id == version);
        if (!string.IsNullOrWhiteSpace(query.renderer_key))
            filtered = filtered.Where(x => types.Any(t => t.id == x.node_type_id && t.renderer_key == query.renderer_key));
        if (!string.IsNullOrWhiteSpace(query.search))
        {
            var text = query.search.Trim().ToLowerInvariant();
            filtered = filtered.Where(x => x.key.ToLower().Contains(text) ||
                (x.label != null && x.label.ToLower().Contains(text)) ||
                (x.description != null && x.description.ToLower().Contains(text)));
        }
        if (!string.IsNullOrWhiteSpace(query.configuration))
        {
            var pattern = ConfigurationSearchPattern(query.configuration);
            filtered = filtered.Where(x => x.configuration_json != null &&
                Regex.IsMatch(x.configuration_json, pattern, RegexOptions.IgnoreCase));
        }
        var joined = from node in filtered
                     join type in types on node.node_type_id equals type.id
                     select new { node, type };
        var total = joined.Count();
        var rows = joined.OrderBy(x => x.node.label ?? x.node.key).ThenBy(x => x.node.id)
            .Skip(query.offset).Take(query.limit)
            .Select(x => new FormNodeLibraryEntry(FormNodeResponse.FromModel(x.node), x.type.name,
                x.type.renderer_key, readable.Count(child => child.parent_id == x.node.id))).ToArray();
        return Task.FromResult(new FormNodeLibraryPage(rows, total, query.offset, query.limit));
    }

    public IReadOnlyList<FormRendererExample> Examples() =>
        Security.Query<FormNodeTypeModel>(SecurityOperations.Read).OrderBy(x => x.renderer_key).ToArray()
            .Select(type => new FormRendererExample(FormNodeTypeResponse.FromModel(type),
                FormComponentCatalog.IsSupported(type.renderer_key) ? FormComponentCatalog.GetDescription(type.renderer_key) : type.name,
                FormComponentCatalog.IsSupported(type.renderer_key) ? JsonSerializer.Serialize(FormComponentCatalog.GetExample(type.renderer_key)) : "{}"))
            .ToArray();

    public async Task<FormNodeDraft> PrepareAsync(PrepareFormNodeDraftRequest request, CancellationToken ct = default)
    {
        if ((request.source_node_id is null) == (request.node_type_id is null))
            throw new ArgumentException("Choose exactly one source node or renderer type.");
        if (request.source_node_id is Guid id)
        {
            _ = await Security.GetByIdAsync<FormNodeModel>(id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Source node is not readable.");
            var source = (await GetSubtreeAsync(id, ct)).Where(x => request.include_children || x.id == id).ToArray();
            if (source.Length > 400) throw new ArgumentException("A draft supports at most 400 nodes.");
            var map = source.ToDictionary(x => x.id, _ => Guid.NewGuid());
            var copied = source.Select(x => new FormNodeDraftNode(map[x.id],
                x.id == id ? null : x.parent_id is Guid parent && map.TryGetValue(parent, out var mapped) ? mapped :
                    throw new ArgumentException("Source subtree contains an unreadable ancestor."),
                x.key, x.node_type_id, x.label, x.description, x.position, x.min_occurs, x.max_occurs,
                RemapConfiguration(x.configuration_json, map))).ToArray();
            var sourcePaths = StoredKeyPaths(source[0].form_version_id);
            var draft = new FormNodeDraft(map[id], copied, source.Where(x => sourcePaths.ContainsKey(x.id))
                .ToDictionary(x => map[x.id], x => sourcePaths[x.id]));
            return RebaseDraft(draft, Validate(draft, ReadTypes()));
        }
        var example = Examples().SingleOrDefault(x => x.type.id == request.node_type_id)
            ?? throw new KeyNotFoundException("Renderer type is not readable.");
        var root = Guid.NewGuid();
        var key = Regex.Replace(example.type.renderer_key, "[^A-Za-z0-9_]", "_");
        if (key.Length == 0 || !char.IsAsciiLetter(key[0]) && key[0] != '_') key = "node_" + key;
        var exampleNodes = new FormNodeDraftNode[] { new(root, null, key, example.type.id,
            example.type.name, configuration_json: example.configuration_json) };
        return new(root, exampleNodes, DraftKeyPaths(exampleNodes));
    }

    public async Task<FormNodeDraftWorkspaceResponse> WorkspaceAsync(FormNodeDraftWorkspaceRequest request, CancellationToken ct = default)
    {
        var version = await Security.GetByIdAsync<FormVersionModel>(request.target_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Target form version is not readable.");
        var types = ReadTypes();
        var draft = RebaseDraft(request.draft, Validate(request.draft, types));
        var ordered = draft.nodes;
        var selected = request.selected_id ?? request.draft.root_id;
        if (!ordered.Any(x => x.id == selected)) throw new ArgumentException("Selected node is not part of this draft.");
        var now = DateTimeOffset.UtcNow;
        var paths = new Dictionary<Guid, string>();
        var preview = ordered.Select(x =>
        {
            var path = x.parent_id is Guid parent ? MaterializedPath.Child(paths[parent], x.id) : MaterializedPath.Root(x.id);
            paths[x.id] = path;
            return new FormNodeResponse(x.id, version.id, x.parent_id, path, x.key, x.label, x.description,
                x.node_type_id, x.position, x.min_occurs, x.max_occurs, false, x.configuration_json,
                version.rbacobject_id, now, now, Security.CurrentUserId, Security.CurrentUserId);
        }).ToArray();
        return new(version.id, draft, selected, preview, types.Values.Select(FormNodeTypeResponse.FromModel).ToArray(), Examples());
    }

    public async Task<IReadOnlyList<FormNodeResponse>> InsertAsync(InsertFormNodeDraftRequest request, CancellationToken ct = default)
    {
        if (request.position < 0) throw new ArgumentException("Position must be nonnegative.");
        var version = await Security.GetByIdAsync<FormVersionModel>(request.form_version_id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Target version not readable.");
        if (version.is_published) throw new InvalidOperationException("Published form version is immutable.");
        FormNodeModel? parent = null;
        Guid rbac;
        if (request.parent_id is Guid parentId)
        {
            (parent, _) = await CreationScopeGuard.RequireEntityAsync<FormNodeModel>(Security, parentId, "contained", ct);
            if (parent.form_version_id != version.id) throw new ArgumentException("Target parent belongs to another form.");
            var parentType = await Security.GetByIdAsync<FormNodeTypeModel>(parent.node_type_id, SecurityOperations.Read, ct)
                ?? throw new KeyNotFoundException("Target type not readable.");
            if (!parentType.can_have_children) throw new ArgumentException("Target node cannot contain children.");
            rbac = parent.rbacobject_id;
        }
        else
        {
            var (_, scope) = await CreationScopeGuard.RequireEntityAsync<FormVersionModel>(Security, version.id, "contained", ct);
            rbac = scope.RbacObjectId;
        }
        var types = ReadTypes();
        var draft = RebaseDraft(request.draft, Validate(request.draft, types));
        var ordered = draft.nodes;
        var root = ordered[0];
        FormSectionRules.Validate(parent?.id, types[root.node_type_id], root.max_occurs, root.configuration_json,
            ordered.Any(x => x.parent_id == root.id && types[x.node_type_id].renderer_key == "section"));
        FormSectionRules.ValidateParent(parent, types[root.node_type_id]);
        // Hidden siblings must not permit duplicate keys or ambiguous ordering in the stored tree.
        var siblings = Unsecured_Query().Where(x => x.form_version_id == version.id && x.parent_id == request.parent_id).ToArray();
        if (siblings.Any(x => x.key == root.key)) throw new ArgumentException("The target already contains this key. Rename the draft root.");
        if (siblings.Length + ordered.Count > 10000) throw new ArgumentException("Target sibling count exceeds the supported insertion size.");
        var map = ordered.ToDictionary(x => x.id, _ => Guid.NewGuid());
        var targetPrefix = "$.form";
        if (parent is not null)
        {
            if (!StoredKeyPaths(version.id).TryGetValue(parent.id, out var parentKeyPath))
                throw new ArgumentException("Target ancestors must be readable to resolve qualified form references.");
            targetPrefix = parentKeyPath;
        }
        var rootKeyPath = draft.reference_paths![root.id];
        var destinationPaths = draft.reference_paths.Values.Distinct().ToDictionary(x => x,
            x => targetPrefix + "." + root.key + x[rootKeyPath.Length..]);
        var paths = new Dictionary<Guid, string>();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;
        var created = ordered.Select(x =>
        {
            var id = map[x.id];
            var isRoot = x.id == root.id;
            var parentId = isRoot ? request.parent_id : map[x.parent_id!.Value];
            var parentPath = isRoot ? parent?.path : paths[x.parent_id!.Value];
            var path = parentPath is null ? MaterializedPath.Root(id) : MaterializedPath.Child(parentPath, id);
            paths[x.id] = path;
            return new FormNodeModel(id, version.id, parentId, path, x.key, x.label, x.description, x.node_type_id,
                isRoot ? request.position : x.position, x.min_occurs, x.max_occurs, false,
                RemapConfiguration(x.configuration_json, map, destinationPaths), rbac, now, now, actor, actor);
        }).ToArray();
        return await Security.ExecuteInTransactionAsync(async token =>
        {
            foreach (var sibling in siblings.Where(x => x.position >= request.position))
                await Security.StageUpdateAsync(sibling with { position = checked(sibling.position + 1), updated_at = now, updated_by = actor }, token);
            foreach (var item in created) await Security.StageCreateAsync(item, token);
            await Security.SaveChangesAsync(token);
            return created.Select(FormNodeResponse.FromModel).ToArray();
        }, ct);
    }

    private Dictionary<Guid, FormNodeTypeModel> ReadTypes() =>
        Security.Query<FormNodeTypeModel>(SecurityOperations.Read).ToDictionary(x => x.id);

    private static string ConfigurationSearchPattern(string text) =>
        // JSON serializers may encode text as \uXXXX; search decoded characters without loading unpaged rows.
        string.Concat(text.Select(character => "(?:" + string.Join("|",
            new[] { character, char.ToLowerInvariant(character), char.ToUpperInvariant(character) }.Distinct()
                .SelectMany(value => new[]
                {
                    Regex.Escape(value.ToString()),
                    Regex.Escape("\\u" + ((int)value).ToString("x4")),
                    Regex.Escape(JsonSerializer.Serialize(value.ToString())[1..^1])
                }).Distinct()) + ")"));

    private Dictionary<Guid, string> StoredKeyPaths(Guid versionId)
    {
        var readable = Security.Query<FormNodeModel>(SecurityOperations.Read)
            .Where(x => x.form_version_id == versionId).ToDictionary(x => x.id);
        var result = new Dictionary<Guid, string>();
        foreach (var node in readable.Values)
        {
            var keys = new List<string>();
            var visited = new HashSet<Guid>();
            FormNodeModel? current = node;
            while (current is not null)
            {
                if (!visited.Add(current.id)) throw new ArgumentException("Stored form hierarchy contains a cycle.");
                keys.Add(current.key);
                if (current.parent_id is not Guid parentId) break;
                if (!readable.TryGetValue(parentId, out current)) break;
            }
            if (current is not null)
            {
                keys.Reverse();
                result[node.id] = "$.form." + string.Join(".", keys);
            }
        }
        return result;
    }

    private static Dictionary<Guid, string> DraftKeyPaths(IReadOnlyList<FormNodeDraftNode> ordered)
    {
        var paths = new Dictionary<Guid, string>();
        foreach (var node in ordered)
            paths[node.id] = (node.parent_id is Guid parent ? paths[parent] : "$.form") + "." + node.key;
        return paths;
    }

    private static FormNodeDraft RebaseDraft(FormNodeDraft draft, IReadOnlyList<FormNodeDraftNode> ordered)
    {
        var current = DraftKeyPaths(ordered);
        var supplied = draft.reference_paths ?? current;
        if (supplied.Count > 400 || supplied.Any(x =>
            x.Value is null || !x.Value.StartsWith("$.form.", StringComparison.Ordinal) || x.Value.Length > 10000))
            throw new ArgumentException("Draft reference paths must identify existing draft nodes using qualified form selectors.");
        var previous = supplied.Where(x => current.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
        if (previous.GroupBy(x => x.Value).Any(group => group.Select(x => current[x.Key]).Distinct().Count() > 1))
            throw new ArgumentException("Ambiguous qualified reference paths; use distinct node keys.");
        var replacements = previous.GroupBy(x => x.Value).ToDictionary(x => x.Key, x => current[x.First().Key]);
        return new(draft.root_id, ordered.Select(x => x with
        {
            configuration_json = RemapConfiguration(x.configuration_json, new Dictionary<Guid, Guid>(), replacements)
        }).ToArray(), current);
    }

    private static IReadOnlyList<FormNodeDraftNode> Validate(FormNodeDraft draft, IReadOnlyDictionary<Guid, FormNodeTypeModel> types)
    {
        if (draft?.nodes is null || draft.nodes.Count is < 1 or > 400)
            throw new ArgumentException("A draft requires between 1 and 400 nodes.");
        if (draft.nodes.Any(x => x is null || x.id == Guid.Empty) || draft.nodes.Select(x => x.id).Distinct().Count() != draft.nodes.Count)
            throw new ArgumentException("Draft node IDs must be unique, nonempty UUIDs.");
        var byId = draft.nodes.ToDictionary(x => x.id);
        if (!byId.TryGetValue(draft.root_id, out var root) || root.parent_id is not null ||
            draft.nodes.Count(x => x.parent_id is null) != 1)
            throw new ArgumentException("A draft must have exactly one root.");
        var ordered = new List<FormNodeDraftNode>();
        var children = draft.nodes.ToLookup(x => x.parent_id);
        void Visit(FormNodeDraftNode node, int depth)
        {
            if (depth > 20) throw new ArgumentException("Draft maximum depth is 20.");
            if (!types.TryGetValue(node.node_type_id, out var type)) throw new ArgumentException("Draft node type is not readable.");
            if (string.IsNullOrWhiteSpace(node.key) || node.key.Length > 200 ||
                !Regex.IsMatch(node.key, "^[A-Za-z_][A-Za-z0-9_.]*$"))
                throw new ArgumentException("Node key must begin with a letter or underscore and contain only letters, numbers, underscores or dots.");
            if (node.label?.Length > 2000 || node.description?.Length > 8000 || node.configuration_json?.Length > 64000)
                throw new ArgumentException("Node attributes exceed the supported size.");
            if (node.position < 0 || node.min_occurs < 0 || node.max_occurs < 1 || node.max_occurs > 10000 || node.min_occurs > node.max_occurs ||
                (node.max_occurs > 1 && !type.can_repeat))
                throw new ArgumentException("Invalid position or occurrence limits.");
            _ = FormNodeConfiguration.Parse(node.configuration_json);
            var nested = children[node.id].OrderBy(x => x.position).ThenBy(x => x.key).ToArray();
            if (nested.Length > 0 && !type.can_have_children) throw new ArgumentException("A field cannot contain child nodes.");
            if (nested.Select(x => x.key).Distinct(StringComparer.Ordinal).Count() != nested.Length)
                throw new ArgumentException("Sibling keys must be unique.");
            var layout = type.renderer_key == "section" ? FormSectionLayout.Read(node.configuration_json) : "stacked";
            if (layout == "table" && (node.max_occurs <= 1 || nested.Any(x => types.TryGetValue(x.node_type_id, out var t) && t.renderer_key == "section")))
                throw new ArgumentException("A table requires repetition and no child sections.");
            if (layout == "tabs" && node.id != draft.root_id) throw new ArgumentException("Only a view root can use tabs in a draft.");
            ordered.Add(node);
            foreach (var child in nested) Visit(child, depth + 1);
        }
        Visit(root, 0);
        if (ordered.Count != draft.nodes.Count) throw new ArgumentException("Draft contains detached nodes or cycles.");
        return ordered;
    }

    private static string RemapConfiguration(string? configuration, IReadOnlyDictionary<Guid, Guid> map,
        IReadOnlyDictionary<string, string>? keyPaths = null)
    {
        var changedPaths = keyPaths?.Where(x => x.Key != x.Value).ToDictionary(x => x.Key, x => x.Value);
        if (map.Count == 0 && changedPaths is not { Count: > 0 }) return configuration ?? "{}";
        var json = FormNodeConfiguration.Parse(configuration);
        var pathPattern = changedPaths is { Count: > 0 }
            ? string.Join("|", changedPaths.Keys.OrderByDescending(x => x.Length).Select(Regex.Escape)) : null;
        string RemapText(string text)
        {
            if (pathPattern is not null)
            {
                var length = text.Length;
                text = Regex.Replace(text, "(?:" + pathPattern + ")(?=$|[^A-Za-z0-9_])", match =>
                {
                    var replacement = changedPaths![match.Value];
                    length += replacement.Length - match.Length;
                    if (length > 64000) throw new ArgumentException("Rebased configuration exceeds the supported size.");
                    return replacement;
                });
            }
            foreach (var pair in map)
                text = text.Replace(pair.Key.ToString("D"), pair.Value.ToString("D"), StringComparison.OrdinalIgnoreCase)
                    .Replace(pair.Key.ToString("N"), pair.Value.ToString("N"), StringComparison.OrdinalIgnoreCase);
            return text;
        }
        JsonNode? Remap(JsonNode? value)
        {
            if (value is JsonValue scalar && scalar.TryGetValue<string>(out var text))
                return JsonValue.Create(RemapText(text));
            if (value is JsonObject obj)
                return new JsonObject(obj.Select(x => KeyValuePair.Create(RemapText(x.Key), Remap(x.Value))));
            if (value is JsonArray array) return new JsonArray(array.Select(Remap).ToArray());
            return value?.DeepClone();
        }
        var result = Remap(json)!.ToJsonString();
        if (result.Length > 64000) throw new ArgumentException("Rebased configuration exceeds the supported size.");
        return result;
    }
}
