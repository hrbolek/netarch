using System.Text.Json.Nodes;
using WebAppArchitecture.DigitalProcesses.Models;

namespace WebAppArchitecture.DigitalProcesses.Services;

public interface IFormSearchService
{
    Task<AuxiliaryOperationResult> SearchAsync(FormSearchRequest request, CancellationToken ct);
}

public interface IFormComponentCatalogService
{
    AuxiliaryOperationResult GetCatalog();
}

public interface IFormDocumentService
{
    Task<AuxiliaryOperationResult> SchemaAsync(Guid id, CancellationToken ct);
    Task<AuxiliaryOperationResult> DataAsync(Guid id, CancellationToken ct);
    Task<AuxiliaryOperationResult> DataListAsync(Guid id, CancellationToken ct);
    JsonObject Reconstruct(Guid submissionId, Guid formVersionId);
}

public interface IApplicantPolicyService
{
    AuxiliaryOperationResult List(Guid processVersionId, Guid? stateId);
    Task<AuxiliaryOperationResult> SetAsync(Guid processVersionId, SetApplicantVisibilityRequest body, CancellationToken ct);
}

public interface IFormDesignAiService
{
    Task<AuxiliaryOperationResult> DesignAsync(FormDesignRequest request, CancellationToken ct);
    Task<AuxiliaryOperationResult> ApproveAsync(ApproveFormDesignRequest request, CancellationToken ct);
}

public interface IDigitalProcessesManagementService
{
    Task<ApplicantMatrixPageData?> ApplicantMatrixAsync(Guid id, CancellationToken ct);
    Task<FormTemplatesPageData?> TemplatePageAsync(Guid id, CancellationToken ct);
}
