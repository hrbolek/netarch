using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.Models.TemplateFieldBinding;
/// <summary>Stable mapping of a template token to a node of the owning form version.</summary>
public sealed class TemplateFieldBindingModel : IEntityDBModel
{
    public Guid id { get; set; }
    public Guid revision_id { get; set; }
    public string placeholder { get; set; } = string.Empty;
    public Guid form_node_id { get; set; }
}
