using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba Form Definition Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class FormDefinitionService(ISecurityLayer security, ISecurityProjectionService securityProjection, IDBLayer unsecuredDb)
    : SecuredBaseService<FormDefinitionModel>(security, unsecuredDb), IFormDefinitionService
{
    public Task<bool> CanCreateAsync(CancellationToken ct = default)
        => Security.IsAllowedAsync(RbacWellKnown.FormDefinitionCreationScopeId, SecurityOperations.Create, ct);

    /// <summary>
    /// Zpracovává vytvoření entity nebo jejího vstupního formuláře; validační, autorizační a transakční pravidla ponechává ve volaných službách. Volání: CurrentUserId, ExecuteInTransactionAsync; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<FormDefinitionResponse> CreateTypedAsync(CreateFormDefinitionRequest request, CancellationToken ct = default)
    {
        var (catalog, _) = await CreationScopeGuard.RequireEntityAsync<GroupModel>(
            Security, RbacWellKnown.FormDefinitionCreationScopeId, "typed", ct);
        var schemaNodes = request.children is JsonElement children
            ? ReadSchemaNodes(children, Security.Query<FormNodeTypeModel>(SecurityOperations.Read).ToArray())
            : null;

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Security.CurrentUserId;

        return await Security.ExecuteInTransactionAsync(async token =>
        {
            await CreationScopeGuard.StageOwnedGroupAsync(
                Security, id, $"Form definition: {request.name}", catalog, true, now, actor, token);

            var model = new FormDefinitionModel(
                id, request.name, request.description, id,
                now, now, actor, actor);
            var created = await Security.StageCreateAsync(model, token);

            // A form definition is never left without an editable version.
            // Version 1 is created atomically with the definition and shares its RBAC scope.
            var firstVersion = new FormVersionModel(
                Guid.NewGuid(), id, 1, false, null, id,
                now, now, actor, actor);
            await Security.StageCreateAsync(firstVersion, token);
            if (schemaNodes is null)
            {
                await DefaultVersionContent.StageFormAsync(unsecuredDb, firstVersion.id, id, now, actor, token);
            }
            else
            {
                await StageSchemaNodesAsync(schemaNodes, firstVersion.id, id, now, actor, token);
            }

            await Security.SaveChangesAsync(token);
            await securityProjection.SynchronizeRbacObjectAsync(id, token);
            return FormDefinitionResponse.FromModel(created);
        }, ct);
    }

    internal static IReadOnlyList<SchemaNode> ReadSchemaNodes(JsonElement children, IReadOnlyCollection<FormNodeTypeModel> types)
    {
        if (children.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Form definition children must be an object.");

        var typesByRenderer = types
            .GroupBy(type => type.renderer_key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var count = 0;

        IReadOnlyList<SchemaNode> ReadChildren(JsonElement items, int depth)
        {
            if (depth > 20)
                throw new ArgumentException("Form tree exceeds the maximum depth of 20.");

            var result = new List<SchemaNode>();
            var siblingKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in items.EnumerateObject())
            {
                if (++count > 400)
                    throw new ArgumentException("Form tree exceeds the maximum of 400 nodes.");
                if (!System.Text.RegularExpressions.Regex.IsMatch(property.Name, "^[A-Za-z_][A-Za-z0-9_]*$"))
                    throw new ArgumentException($"Invalid FormNode key: {property.Name}");
                if (!siblingKeys.Add(property.Name))
                    throw new ArgumentException($"Duplicate FormNode key: {property.Name}");

                var node = property.Value;
                if (node.ValueKind != JsonValueKind.Object
                    || !node.TryGetProperty("renderer_key", out var rendererElement)
                    || rendererElement.ValueKind != JsonValueKind.String)
                    throw new ArgumentException($"Missing renderer_key for {property.Name}.");

                var renderer = rendererElement.GetString()!;
                if (!typesByRenderer.TryGetValue(renderer, out var type))
                    throw new ArgumentException($"Unavailable FormNode renderer: {renderer}");

                var nested = node.TryGetProperty("children", out var childElement)
                    ? childElement
                    : default;
                if (nested.ValueKind != JsonValueKind.Undefined && nested.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException($"Children for {property.Name} must be an object.");
                if (nested.ValueKind == JsonValueKind.Object && nested.EnumerateObject().Any() && !type.can_have_children)
                    throw new ArgumentException($"Component {renderer} cannot have children.");

                var position = ReadInt(node, "position", result.Count);
                var minOccurs = ReadInt(node, "min_occurs", 1);
                var maxOccurs = ReadInt(node, "max_occurs", 1);
                if (position < 0 || minOccurs < 0 || maxOccurs < 1 || minOccurs > maxOccurs)
                    throw new ArgumentException($"Invalid position or occurrence limits for {property.Name}.");
                if (maxOccurs > 1 && !type.can_repeat)
                    throw new ArgumentException($"Component {renderer} cannot repeat.");

                var label = ReadOptionalString(node, "label") ?? property.Name;
                var description = ReadOptionalString(node, "description");
                var configuration = "{}";
                if (node.TryGetProperty("configuration", out var config))
                {
                    if (config.ValueKind != JsonValueKind.Object)
                        throw new ArgumentException($"Configuration for {property.Name} must be an object.");
                    configuration = config.GetRawText();
                }

                var childNodes = nested.ValueKind == JsonValueKind.Object
                    ? ReadChildren(nested, depth + 1) : Array.Empty<SchemaNode>();
                FormSectionRules.Validate(depth == 0 ? null : Guid.Empty, type, maxOccurs, configuration,
                    childNodes.Any(child => types.Any(t => t.id == child.NodeTypeId && t.renderer_key == "section")));
                result.Add(new SchemaNode(
                    property.Name,
                    label,
                    description,
                    type.id,
                    position,
                    minOccurs,
                    maxOccurs,
                    configuration,
                    childNodes));
            }
            return result;
        }

        return ReadChildren(children, 0);
    }

    private async Task StageSchemaNodesAsync(
        IReadOnlyList<SchemaNode> nodes,
        Guid formVersionId,
        Guid rbacObjectId,
        DateTimeOffset now,
        Guid actor,
        CancellationToken ct)
    {
        async Task StageChildren(IReadOnlyList<SchemaNode> children, Guid? parentId, string? parentPath)
        {
            foreach (var node in children)
            {
                var id = Guid.NewGuid();
                var path = parentPath is null ? MaterializedPath.Root(id) : MaterializedPath.Child(parentPath, id);
                await unsecuredDb.StageCreateAsync(new FormNodeModel(
                    id, formVersionId, parentId, path, node.Key, node.Label, node.Description,
                    node.NodeTypeId, node.Position, node.MinOccurs, node.MaxOccurs, false,
                    node.ConfigurationJson, rbacObjectId, now, now, actor, actor), ct);
                await StageChildren(node.Children, id, path);
            }
        }

        await StageChildren(nodes, null, null);
    }

    private static int ReadInt(JsonElement node, string property, int defaultValue)
    {
        if (!node.TryGetProperty(property, out var value))
            return defaultValue;
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
            throw new ArgumentException($"{property} must be an integer.");
        return result;
    }

    private static string? ReadOptionalString(JsonElement node, string property)
    {
        if (!node.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        if (value.ValueKind != JsonValueKind.String)
            throw new ArgumentException($"{property} must be a string.");
        return value.GetString();
    }

    internal sealed record SchemaNode(
        string Key,
        string? Label,
        string? Description,
        Guid NodeTypeId,
        int Position,
        int MinOccurs,
        int MaxOccurs,
        string ConfigurationJson,
        IReadOnlyList<SchemaNode> Children);

    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: GetByIdAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<FormDefinitionDetailResponse> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var definition = await Security.GetByIdAsync<FormDefinitionModel>(id, SecurityOperations.Read, ct)
            ?? throw new KeyNotFoundException("Form definition not found or not readable.");

        var versions = await Security.Query<FormVersionModel>(SecurityOperations.Read)
            .Where(x => x.form_definition_id == id)
            .OrderByDescending(x => x.version)
            .Select(FormVersionResponse.Projection)
            .ToArrayAsync(ct);

        return new FormDefinitionDetailResponse(FormDefinitionResponse.FromModel(definition), versions);
    }
}
