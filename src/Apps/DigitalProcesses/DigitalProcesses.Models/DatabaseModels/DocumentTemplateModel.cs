using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.DBLayer;

/// <summary>Version-owned RTF template stored transactionally in PostgreSQL.</summary>
public sealed class DocumentTemplateModel
{
    public Guid id { get; set; }
    public Guid form_version_id { get; set; }
    public string name { get; set; } = string.Empty;
    public string content_type { get; set; } = "application/rtf";
    public byte[] content { get; set; } = Array.Empty<byte>();
    public DateTimeOffset created_at { get; set; }
}
