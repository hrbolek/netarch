using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
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
        if (node is JsonValue value && value.TryGetValue<string>(out var text)) return text;
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
                    case Literal literal:
                        output.Append(literal.Text);
                        break;
                    case Value value:
                        var text = value.Key == "FILLEDID" ? submissionId.ToString()
                            : value.Key == "CLASSNAME" ? "Submission"
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
                var kind = types.TryGetValue(node.node_type_id, out var type) ? type.renderer_key : "";
                if (kind.StartsWith("action.", StringComparison.OrdinalIgnoreCase)) continue;
                var children = nodes.Where(x => x.parent_id == node.id).ToArray();
                if (children.Length > 0 || (types.TryGetValue(node.node_type_id, out var nodeType) && nodeType.can_have_children))
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
