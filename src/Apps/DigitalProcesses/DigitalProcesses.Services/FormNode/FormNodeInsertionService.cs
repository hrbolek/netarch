using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;



public interface IFormNodeInsertionService
{
    Task<FormNodeLibraryPage> SearchAsync(FormNodeLibraryQuery query, CancellationToken ct = default);
    IReadOnlyList<FormRendererExample> Examples();
    Task<FormNodeDraft> PrepareAsync(PrepareFormNodeDraftRequest request, CancellationToken ct = default);
    Task<FormNodeDraftWorkspaceResponse> WorkspaceAsync(FormNodeDraftWorkspaceRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<FormNodeResponse>> InsertAsync(InsertFormNodeDraftRequest request, CancellationToken ct = default);
}

public sealed class FormNodeInsertionService(IFormNodeService nodes) : IFormNodeInsertionService
{
    public Task<FormNodeLibraryPage> SearchAsync(FormNodeLibraryQuery query, CancellationToken ct = default) => nodes.SearchAsync(query, ct);
    public IReadOnlyList<FormRendererExample> Examples() => nodes.Examples();
    public Task<FormNodeDraft> PrepareAsync(PrepareFormNodeDraftRequest request, CancellationToken ct = default) => nodes.PrepareAsync(request, ct);
    public Task<FormNodeDraftWorkspaceResponse> WorkspaceAsync(FormNodeDraftWorkspaceRequest request, CancellationToken ct = default) => nodes.WorkspaceAsync(request, ct);
    public Task<IReadOnlyList<FormNodeResponse>> InsertAsync(InsertFormNodeDraftRequest request, CancellationToken ct = default) => nodes.InsertAsync(request, ct);
}
