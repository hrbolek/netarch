namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
public sealed record FormNodeLibraryQuery(
    string? search = null, Guid? node_type_id = null, string? renderer_key = null,
    string? configuration = null, Guid? form_version_id = null, int offset = 0, int limit = 20);
