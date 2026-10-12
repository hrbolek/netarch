using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

/// <summary>
/// Výstupní DTO entity Process Definition Version response. Odděluje datový model od kontraktu API a Razor pohledů; vytváří se mapováním doménového/databázového modelu.
/// </summary>
public sealed record ProcessDefinitionVersionResponse(
    Guid id,
    Guid process_definition_id,
    Guid form_version_id,
    Guid workflow_version_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = "",
    Guid? target_group_type_id = null
) : IEntityDTOModel<ProcessDefinitionVersionModel, ProcessDefinitionVersionResponse>, ISecuredEntityDTOModel
{
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static ProcessDefinitionVersionResponse FromModel(ProcessDefinitionVersionModel model) => new(model.id, model.process_definition_id, model.form_version_id, model.workflow_version_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Process version {model.version}", model.target_group_type_id);
    public static Expression<Func<ProcessDefinitionVersionModel, ProcessDefinitionVersionResponse>> Projection => model => new(model.id, model.process_definition_id, model.form_version_id, model.workflow_version_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Process version", model.target_group_type_id);
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public ProcessDefinitionVersionModel ToModel() => new(id, process_definition_id, form_version_id, workflow_version_id, version, is_published, published_at, rbacobject_id, created_at, updated_at, created_by, updated_by) { target_group_type_id = target_group_type_id };
}
