using System.Linq.Expressions;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormVersionResponse(
    Guid id,
    Guid form_definition_id,
    int version,
    bool is_published,
    DateTimeOffset? published_at,
    Guid rbacobject_id,
    DateTimeOffset created_at,
    DateTimeOffset updated_at,
    Guid created_by,
    Guid updated_by,
    string visual_label = ""
) : IEntityDTOModel<FormVersionModel, FormVersionResponse>, ISecuredEntityDTOModel
{
    public static FormVersionResponse FromModel(FormVersionModel model) => new(model.id, model.form_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, $"Form version {model.version}");
    public static Expression<Func<FormVersionModel, FormVersionResponse>> Projection => model => new(model.id, model.form_definition_id, model.version, model.is_published, model.published_at, model.rbacobject_id, model.created_at, model.updated_at, model.created_by, model.updated_by, "Form version");
    public FormVersionModel ToModel() => new(id, form_definition_id, version, is_published, published_at, rbacobject_id, created_at, updated_at, created_by, updated_by);
}
