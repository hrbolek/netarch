using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>Persistent RTF templates bound to a form version; all operations check the actor's RBAC permissions.</summary>
public static class RtfDocumentEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var root = "/digital-processes";
        endpoints.MapGet(root + "/FormVersionResponse/rtf-templates/{id:guid}", List)
            .WithTags("DigitalProcesses.Rtf").WithName("FormRtfTemplateList")
            .WithDescription("Lists stored RTF templates associated with this form version, subject to RBAC Read.");
        endpoints.MapPost(root + "/FormVersionResponse/rtf-templates/{id:guid}", Upload)
            .WithTags("DigitalProcesses.Rtf").WithName("FormRtfTemplateUpload")
            .Accepts<byte[]>("application/rtf")
            .WithDescription("Stores an RTF template. Send raw application/rtf bytes; optional ?name=... . Requires Update on the form version.");
        endpoints.MapPost(root + "/FormVersionResponse/rtf-template/generate/{id:guid}", Generate)
            .WithTags("DigitalProcesses.Rtf").WithName("FormRtfTemplateGenerate")
            .WithDescription("Generates and persists a starter RTF template from saved FormNodes, including nested objects and repeated sections. Requires Update.");
        endpoints.MapGet(root + "/RtfTemplate/{id:guid}", Download)
            .WithTags("DigitalProcesses.Rtf").WithName("RtfTemplateDownload")
            .WithDescription("Downloads the source RTF template, subject to form-version Read.");
        endpoints.MapDelete(root + "/RtfTemplate/{id:guid}", Delete)
            .WithTags("DigitalProcesses.Rtf").WithName("RtfTemplateDelete")
            .WithDescription("Deletes a stored template. Requires form-version Update.");
        endpoints.MapGet(root + "/RtfTemplate/{id:guid}/submission/{submissionId:guid}", Render)
            .WithTags("DigitalProcesses.Rtf").WithName("SubmissionRenderRtf")
            .WithDescription("Downloads an RTF document rendered using this template and a readable Submission of the same form version. Reads values from SubmissionNodes.");
    }

    private static async Task<IResult> List(Guid id, ISecurityLayer security, RtfTemplateStore store, CancellationToken ct)
    {
        if (await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Read, ct) is null) return Results.NotFound();
        return Results.Json(store.List(id));
    }

    private static async Task<IResult> Upload(Guid id, HttpRequest request, ISecurityLayer security, RtfTemplateStore store, CancellationToken ct)
    {
        if (await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Update, ct) is null) return Results.NotFound();
        if (!(request.ContentType?.StartsWith("application/rtf", StringComparison.OrdinalIgnoreCase) ?? false))
            return Results.Problem("Send raw RTF with Content-Type: application/rtf", statusCode: 415);
        using var ms = new MemoryStream();
        await request.Body.CopyToAsync(ms, ct);
        if (ms.Length > 5_000_000) return Results.BadRequest(new { error = "RTF template exceeds 5 MB." });
        if (!RtfTemplateEngine.IsRtf(ms.ToArray())) return Results.BadRequest(new { error = "Payload is not an RTF document." });
        if (!RtfTemplateEngine.IsValidTemplate(ms.ToArray(), out var validation)) return Results.BadRequest(new { error = validation });
        var name = request.Query["name"].ToString();
        var item = store.Save(id, string.IsNullOrWhiteSpace(name) ? "RTF template" : name, ms.ToArray());
        return Results.Created($"/digital-processes/RtfTemplate/{item.Id}", item);
    }

    private static async Task<IResult> Generate(Guid id, ISecurityLayer security, RtfTemplateStore store, CancellationToken ct)
    {
        if (await security.GetByIdAsync<FormVersionModel>(id, SecurityOperations.Update, ct) is null) return Results.NotFound();
        var definitions = security.Query<FormNodeModel>(SecurityOperations.Read).Where(n => n.form_version_id == id).ToArray();
        var typeIds = definitions.Select(n => n.node_type_id).Distinct().ToArray();
        var types = security.Query<FormNodeTypeModel>(SecurityOperations.Read)
            .Where(t => typeIds.Contains(t.id)).ToDictionary(t => t.id);
        var doc = RtfTemplateEngine.Generate(definitions, types);
        var template = store.Save(id, "Generated from schema", Encoding.ASCII.GetBytes(doc));
        return Results.Created($"/digital-processes/RtfTemplate/{template.Id}", template);
    }

    private static async Task<IResult> Download(Guid id, ISecurityLayer security, RtfTemplateStore store, CancellationToken ct)
    {
        var item = store.Find(id);
        if (item is null || await security.GetByIdAsync<FormVersionModel>(item.FormVersionId, SecurityOperations.Read, ct) is null) return Results.NotFound();
        return Results.File(store.Read(id), "application/rtf", "template.rtf");
    }

    private static async Task<IResult> Delete(Guid id, ISecurityLayer security, RtfTemplateStore store, CancellationToken ct)
    {
        var item = store.Find(id);
        if (item is null || await security.GetByIdAsync<FormVersionModel>(item.FormVersionId, SecurityOperations.Update, ct) is null) return Results.NotFound();
        store.Delete(id);
        return Results.NoContent();
    }

    private static async Task<IResult> Render(Guid id, Guid submissionId, ISecurityLayer security, RtfTemplateStore store, CancellationToken ct)
    {
        var template = store.Find(id);
        if (template is null || await security.GetByIdAsync<FormVersionModel>(template.FormVersionId, SecurityOperations.Read, ct) is null) return Results.NotFound();
        var submission = await security.GetByIdAsync<SubmissionModel>(submissionId, SecurityOperations.Read, ct);
        if (submission is null || submission.form_version_id != template.FormVersionId) return Results.NotFound();
        // Reconstruct() uses security-filtered SubmissionNode and FormNode queries.
        var data = FormDocumentEndpoints.Reconstruct(submission, security);
        try
        {
            var document = RtfTemplateEngine.Render(store.Read(id), data, submissionId);
            return Results.File(document, "application/rtf", $"submission-{submissionId:N}.rtf");
        }
        catch (FormatException ex) { return Results.BadRequest(new { error = ex.Message }); }
    }
}

public sealed record RtfTemplateInfo(Guid Id, Guid FormVersionId, string Name, DateTimeOffset CreatedAt);

/// <summary>PostgreSQL-backed template store. Scoped to the request DbContext.</summary>
public sealed class RtfTemplateStore(ApplicationDbContext db)
{
    public IReadOnlyList<RtfTemplateInfo> List(Guid version) => db.DocumentTemplates.AsNoTracking()
        .Where(x => x.form_version_id == version).OrderBy(x => x.created_at)
        .Select(x => new RtfTemplateInfo(x.id, x.form_version_id, x.name, x.created_at)).ToArray();

    public RtfTemplateInfo? Find(Guid id) => db.DocumentTemplates.AsNoTracking()
        .Where(x => x.id == id)
        .Select(x => new RtfTemplateInfo(x.id, x.form_version_id, x.name, x.created_at))
        .SingleOrDefault();

    public RtfTemplateInfo Save(Guid version, string name, byte[] bytes)
    {
        var entity = new DocumentTemplateModel
        {
            id = Guid.NewGuid(), form_version_id = version,
            name = name[..Math.Min(name.Length, 200)],
            content_type = "application/rtf", content = bytes,
            created_at = DateTimeOffset.UtcNow
        };
        db.DocumentTemplates.Add(entity);
        db.DocumentTemplateRevisions.Add(new DocumentTemplateRevisionModel
        {
            id = Guid.NewGuid(), template_id = entity.id, revision = 1,
            content = bytes.ToArray(), content_type = "application/rtf",
            created_at = entity.created_at
        });
        db.SaveChanges();
        return new RtfTemplateInfo(entity.id, version, entity.name, entity.created_at);
    }

    public byte[] Read(Guid id) => db.DocumentTemplates.AsNoTracking()
        .Where(x => x.id == id).Select(x => x.content).Single();

    public void Delete(Guid id)
    {
        var entity = db.DocumentTemplates.SingleOrDefault(x => x.id == id);
        if (entity is null) return;
        db.DocumentTemplates.Remove(entity);
        db.SaveChanges();
    }
}

public static class RtfTemplateEngine
{
    private static readonly Regex Token = new(@"\[\[(CLASSNAME|FILLEDID|[WFI]:[^\]\r\n]+|E)\]\]", RegexOptions.Compiled);
    private abstract record Part;
    private sealed record Literal(string Text) : Part;
    private sealed record Value(string Key) : Part;
    private sealed record Block(string Key, bool Repeated, List<Part> Children) : Part;

    public static bool IsRtf(byte[] data) => data.Length >= 5 && Encoding.Latin1.GetString(data, 0, 5) == @"{\rtf";
    public static bool IsValidTemplate(byte[] data, out string? message)
    {
        try { Parse(Encoding.Latin1.GetString(data)); message = null; return true; }
        catch (FormatException ex) { message = ex.Message; return false; }
    }
    private static List<Part> Parse(string source)
    {
        var result = new List<Part>();
        var stack = new Stack<List<Part>>();
        var current = result;
        var position = 0;
        foreach (Match match in Token.Matches(source))
        {
            if (match.Index > position) current.Add(new Literal(source[position..match.Index]));
            var token = match.Groups[1].Value;
            if (token.StartsWith("I:", StringComparison.Ordinal)) current.Add(new Value(token[2..]));
            else if (token is "CLASSNAME" or "FILLEDID") current.Add(new Value(token));
            else if (token.StartsWith("W:", StringComparison.Ordinal) || token.StartsWith("F:", StringComparison.Ordinal))
            {
                var children = new List<Part>();
                current.Add(new Block(token[2..], token[0] == 'F', children));
                stack.Push(current);
                current = children;
            }
            else if (token == "E")
            {
                if (stack.Count == 0) throw new FormatException("Unmatched [[E]] in RTF template.");
                current = stack.Pop();
            }
            position = match.Index + match.Length;
        }
        if (stack.Count != 0) throw new FormatException("Unclosed [[W:...]] or [[F:...]] block in RTF template.");
        if (position < source.Length) current.Add(new Literal(source[position..]));
        return result;
    }
    private static string EscapeValue(string value)
    {
        var sb = new StringBuilder();
        foreach (var ch in value)
        {
            if (ch is '\\' or '{' or '}') { sb.Append('\\').Append(ch); continue; }
            if (ch == '\n') { sb.Append(@"\line "); continue; }
            if (ch == '\r') continue;
            if (ch <= 127) sb.Append(ch);
            else sb.Append(@"\u").Append(unchecked((short)ch)).Append('?');
        }
        return sb.ToString();
    }
    private static string Scalar(JsonNode? node)
    {
        if (node is null) return "";
        if (node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var s)) return s;
        }
        return node.ToJsonString();
    }
    public static byte[] Render(byte[] template, JsonObject form, Guid submissionId)
    {
        var parts = Parse(Encoding.Latin1.GetString(template));
        var output = new StringBuilder();
        void AppendParts(IEnumerable<Part> items, JsonNode? scope)
        {
            foreach (var part in items)
            {
                switch (part)
                {
                    case Literal literal: output.Append(literal.Text); break;
                    case Value value:
                        var text = value.Key == "FILLEDID" ? submissionId.ToString() : value.Key == "CLASSNAME" ? "Submission"
                            : Scalar((scope as JsonObject)?[value.Key]);
                        output.Append(EscapeValue(text));
                        break;
                    case Block block:
                        var child = (scope as JsonObject)?[block.Key];
                        if (block.Repeated)
                        {
                            if (child is JsonArray array)
                                foreach (var element in array) AppendParts(block.Children, element);
                            else if (child is not null) AppendParts(block.Children, child);
                        }
                        else if (child is JsonArray many)
                        {
                            foreach (var element in many) AppendParts(block.Children, element);
                        }
                        else if (child is not null) AppendParts(block.Children, child);
                        break;
                }
            }
        }
        AppendParts(parts, form);
        return Encoding.Latin1.GetBytes(output.ToString());
    }
    public static string Generate(FormNodeModel[] nodes, Dictionary<Guid, FormNodeTypeModel> types)
    {
        var sb = new StringBuilder(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Arial;}}\uc1\pard\f0\fs22 ");
        void Emit(Guid? parent)
        {
            foreach (var node in nodes.Where(x => x.parent_id == parent && !string.IsNullOrWhiteSpace(x.key)).OrderBy(x => x.position))
            {
                var kind = types.TryGetValue(node.node_type_id, out var t) ? t.renderer_key : "";
                if (kind.StartsWith("action.", StringComparison.OrdinalIgnoreCase)) continue;
                var children = nodes.Where(x => x.parent_id == node.id).ToArray();
                if (children.Length > 0 || (types.TryGetValue(node.node_type_id, out var nt) && nt.can_have_children))
                {
                    sb.Append("[[").Append(node.max_occurs > 1 ? "F:" : "W:").Append(node.key).Append("]] ");
                    sb.Append(@"\par\b ").Append(EscapeValue(node.label ?? node.key)).Append(@"\b0\par ");
                    Emit(node.id);
                    sb.Append("[[E]] ");
                }
                else
                {
                    sb.Append(EscapeValue(node.label ?? node.key)).Append(@":\tab [[I:").Append(node.key).Append(@"]]\par ");
                }
            }
        }
        Emit(null);
        sb.Append('}');
        return sb.ToString();
    }
}
