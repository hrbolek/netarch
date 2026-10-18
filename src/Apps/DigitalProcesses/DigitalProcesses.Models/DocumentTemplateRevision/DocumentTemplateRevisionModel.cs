using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;
/// <summary>An immutable source revision. Rendered documents can reference the exact revision used.</summary>

public sealed class DocumentTemplateRevisionModel : IEntityDBModel
{
    public Guid id { get; set; }
    public Guid template_id { get; set; }
    public int revision { get; set; }
    public byte[] content { get; set; } = Array.Empty<byte>();
    public string content_type { get; set; } = "application/rtf";
    public DateTimeOffset created_at { get; set; }
}
