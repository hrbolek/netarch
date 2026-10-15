using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;
using WebAppArchitecture.DigitalProcesses.Models.FormVersion;

namespace WebAppArchitecture.DigitalProcesses.UI.FormNode.Components;

public sealed class FormNodeEditorView(FormDesignerPageResponse model)
{
    public FormDesignerPageResponse Model { get; } = model;
    public string DesignerUrl => Model.root_node_id is Guid root ? $"/digital-processes/FormNodeResponse/editor/{root}" : $"/digital-processes/FormVersionResponse/designer/{Model.designer.form.id}";
    public string NodePath(FormNodeResponse node)
    {
        var nodes = Model.designer.nodes.Concat(Model.ancestor_nodes ?? []).ToDictionary(x => x.id);
        var keys = new List<string>();
        var visited = new HashSet<Guid>();
        FormNodeResponse? current = node;
        while (current is not null && visited.Add(current.id) && keys.Count < 64)
        {
            keys.Add(current.key);
            if (current.parent_id is Guid parentId && !nodes.ContainsKey(parentId))
                return "Cesta není dostupná: nadřazený uzel nelze číst.";
            current = current.parent_id.HasValue && nodes.TryGetValue(current.parent_id.Value, out var parent) ? parent : null;
        }
        keys.Reverse();
        return "$.form." + string.Join(".", keys);
    }

    public string TestUrl => Model.root_node_id is Guid root ? $"/digital-processes/FormNodeResponse/editor/{root}?test=true" : $"/digital-processes/FormVersionResponse/test/{Model.designer.form.id}";
    public IEnumerable<FormNodeResponse> OrderedNodes => OrderTree(Model.designer.nodes, Model.root_node_id);
    private static IEnumerable<FormNodeResponse> OrderTree(IReadOnlyCollection<FormNodeResponse> nodes, Guid? editorRoot)
    {
        var byParent = nodes.ToLookup(n => n.parent_id);
        var visited = new HashSet<Guid>();
        IEnumerable<FormNodeResponse> Visit(Guid? parent)
        {
            foreach (var node in byParent[parent].OrderBy(n => n.position).ThenBy(n => n.key))
            {
                if (!visited.Add(node.id)) continue;
                yield return node;
                foreach (var child in Visit(node.id)) yield return child;
            }
        }
        if (editorRoot is Guid root)
        {
            var viewRoot = nodes.Single(x => x.id == root);
            visited.Add(root);
            yield return viewRoot;
            foreach (var child in Visit(root)) yield return child;
        }
        else
            foreach (var node in Visit(null)) yield return node;
        // Keep orphaned nodes visible instead of silently dropping them.
        foreach (var node in nodes.OrderBy(n => n.position).ThenBy(n => n.key))
            if (visited.Add(node.id)) yield return node;
    }
    public IEnumerable<FormNodeResponse> Roots => Model.designer.nodes.Where(x => Model.root_node_id is Guid root ? x.id == root : x.parent_id == null).OrderBy(x => x.position).ThenBy(x => x.key);
    public bool IsSection(FormNodeResponse node) => Model.designer.node_types.Any(t => t.id == node.node_type_id && t.renderer_key == "section");
    public IEnumerable<FormNodeResponse> Containers
    {
        get
        {
            var typeIds = Model.designer.node_types.Where(x => x.can_have_children).Select(x => x.id).ToHashSet();
            return Model.designer.nodes.Where(x => typeIds.Contains(x.node_type_id)).OrderBy(x => x.path).ThenBy(x => x.position);
        }
    }
    public IEnumerable<FormNodeTypeResponse> OrderedNodeTypes => Model.designer.node_types
        .OrderBy(x => x.renderer_key switch
        {
            "section" => 0,
            "input.text" => 10,
            "input.textarea" => 20,
            "input.number" => 30,
            "input.date" => 40,
            "input.checkbox" => 50,
            "input.select" => 60,
            "input.graphql-search" => 61,
            "action.document" => 72,
            "display.url" => 65,
            "action.import" => 70,
            "action.export" => 71,
            _ => 100
        })
        .ThenBy(x => x.name);

}
