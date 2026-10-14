using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record FormSearchRequest(
    Guid NodeId, string Pattern, IReadOnlyList<FormSearchValue>? Values = null);
public sealed record FormSearchValue(string Name, JsonElement Value);
public sealed record SetApplicantVisibilityRequest(Guid StateId, Guid FormNodeId, bool CanRead);
public sealed record FormDesignRequest(Guid FormVersionId, string Instruction);
public sealed record ApproveFormDesignRequest(Guid FormVersionId, JsonElement Schema);

public sealed record ApplicantMatrixPageData(
    ProcessDefinitionVersionResponse Model, bool MayEdit,
    WorkflowStateResponse[] States, FormNodeResponse[] Nodes);
public sealed record FormTemplatesPageData(FormVersionResponse Model, bool MayEdit);
