using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormNodeResponse(
    Guid id,
    Guid form_version_id,
    Guid? parent_id,
    string path,
    string key,
    string? label,
    string? description,
    Guid node_type_id,
    int position,
    int min_occurs,
    int max_occurs,
    bool own_security_scope,
    string? configuration_json,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<FormNodeModel, FormNodeResponse>, ISecuredEntityDTOModel
{
    public static FormNodeResponse FromModel(FormNodeModel model) => new(model.id, model.form_version_id, model.parent_id, model.path, model.key, model.label, model.description, model.node_type_id, model.position, model.min_occurs, model.max_occurs, model.own_security_scope, model.configuration_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.label ?? model.key));
    public static Expression<Func<FormNodeModel, FormNodeResponse>> Projection => model => new(model.id, model.form_version_id, model.parent_id, model.path, model.key, model.label, model.description, model.node_type_id, model.position, model.min_occurs, model.max_occurs, model.own_security_scope, model.configuration_json, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, (model.label ?? model.key));
    public FormNodeModel ToModel() => new(id, form_version_id, parent_id, path, key, label, description, node_type_id, position, min_occurs, max_occurs, own_security_scope, configuration_json, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
