using WebAppArchitecture.DigitalProcesses.DBLayer;
using System.Text.Json;
using WebAppArchitecture.Base.Security;
using WebAppArchitecture.Base.Services;
using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.Models;
using WebAppArchitecture.SecurityLayer;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>
/// Služba Submission Service. Zapouzdřuje doménové operace a koordinuje kontrolu přístupu, validaci a transakční práci s uložištěm.
/// </summary>
public sealed class SubmissionService(ISecurityLayer security, IDBLayer unsecuredDb) : SecuredBaseService<SubmissionModel>(security, unsecuredDb),ISubmissionService
{
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: GetByIdAsync, Query; pořadí a parametry viz implementace.
    /// </summary>
    public async Task<SubmissionTreeResponse> GetTreeAsync(Guid submissionId,CancellationToken ct=default){var s=await Security.GetByIdAsync<SubmissionModel>(submissionId,SecurityOperations.Read,ct)??throw new KeyNotFoundException();var nodes=Security.Query<SubmissionNodeModel>(SecurityOperations.Read).Where(x=>x.submission_id==submissionId).OrderBy(x=>x.path).ThenBy(x=>x.position).Select(SubmissionNodeResponse.Projection).ToArray();return new(SubmissionResponse.FromModel(s),nodes);}
}
