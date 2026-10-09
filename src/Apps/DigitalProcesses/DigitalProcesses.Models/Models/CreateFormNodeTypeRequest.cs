namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record CreateFormNodeTypeRequest(
    string name,
    string renderer_key,
    bool can_have_children,
    bool can_have_value,
    bool can_repeat,
    Guid? parent_type_id = null,
    string? configuration_schema_json = null);
