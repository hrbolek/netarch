using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record ProcessDefinitionResponse(
    Guid id,
    string name,
    string? description,
    Guid initiator_role_type_id,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<ProcessDefinitionModel, ProcessDefinitionResponse>, ISecuredEntityDTOModel
{
    public static ProcessDefinitionResponse FromModel(ProcessDefinitionModel model) => new(model.id, model.name, model.description, model.initiator_role_type_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<ProcessDefinitionModel, ProcessDefinitionResponse>> Projection => model => new(model.id, model.name, model.description, model.initiator_role_type_id, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public ProcessDefinitionModel ToModel() => new(id, name, description, initiator_role_type_id, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
