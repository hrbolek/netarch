using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormNodeTypeResponse(
    Guid id,
    Guid? parent_type_id,
    string name,
    string renderer_key,
    bool can_have_children,
    bool can_have_value,
    bool can_repeat,
    string? configuration_schema_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<FormNodeTypeModel, FormNodeTypeResponse>, ISecuredEntityDTOModel
{
    public static FormNodeTypeResponse FromModel(FormNodeTypeModel model) => new(model.id, model.parent_type_id, model.name, model.renderer_key, model.can_have_children, model.can_have_value, model.can_repeat, model.configuration_schema_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public static Expression<Func<FormNodeTypeModel, FormNodeTypeResponse>> Projection => model => new(model.id, model.parent_type_id, model.name, model.renderer_key, model.can_have_children, model.can_have_value, model.can_repeat, model.configuration_schema_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, model.name);
    public FormNodeTypeModel ToModel() => new(id, parent_type_id, name, renderer_key, can_have_children, can_have_value, can_repeat, configuration_schema_json, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
