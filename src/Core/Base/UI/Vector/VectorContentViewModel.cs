using Microsoft.AspNetCore.Components;
using WebAppArchitecture.Base.Data;
using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.Base.UI.Vector;

public sealed record VectorContentViewModel(
    string Title,
    string EntityType,
    IReadOnlyCollection<IEntityDTOModel> Items,
    Type? ItemComponentType = null,
    IReadOnlyCollection<VectorColumnDefinition>? Columns = null,
    string App = "architecture",
    Type? ItemLabelComponentType = null,
    VectorPageInfo? PageInfo = null,
    string? TargetId = null);
