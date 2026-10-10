namespace WebAppArchitecture.DBLayer;

/// <summary>An immutable source revision. Rendered documents can reference the exact revision used.</summary>
public sealed class DocumentTemplateRevisionModel
{
    public Guid id { get; set; }
    public Guid template_id { get; set; }
    public int revision { get; set; }
    public byte[] content { get; set; } = Array.Empty<byte>();
    public string content_type { get; set; } = "application/rtf";
    public DateTimeOffset created_at { get; set; }
}

/// <summary>Stable mapping of a template token to a node of the owning form version.</summary>
public sealed class TemplateFieldBindingModel
{
    public Guid id { get; set; }
    public Guid revision_id { get; set; }
    public string placeholder { get; set; } = string.Empty;
    public Guid form_node_id { get; set; }
}
