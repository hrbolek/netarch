using WebAppArchitecture.DBLayer;
using WebAppArchitecture.DigitalProcesses.DBLayer;

namespace WebAppArchitecture.DigitalProcesses.Security;

/// <summary>
/// Privileged system operation for cloning an authorized Request across workflow states.
/// NEVER map this interface directly to a user-facing HTTP endpoint.
/// Ordinary reads must continue to use ISecurityLayer.Query(Read).
/// </summary>
/// <summary>
/// Typ I Trusted Submission Snapshot Reader ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface ITrustedSubmissionSnapshotReader
{
    SubmissionNodeModel[] ReadCompleteTree(Guid submissionId);
}

/// <summary>
/// Typ Trusted Submission Snapshot Reader ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class TrustedSubmissionSnapshotReader(IDBLayer db) : ITrustedSubmissionSnapshotReader
{
    /// <summary>
    /// Načítá nebo prezentuje entity pro aktuálního uživatele; výsledek předává kontraktu HTTP nebo Razor stránce. Volání: Query; pořadí a parametry viz implementace.
    /// </summary>
    public SubmissionNodeModel[] ReadCompleteTree(Guid submissionId)
        => db.Query<SubmissionNodeModel>()
            .Where(x => x.submission_id == submissionId)
            .OrderBy(x => x.path).ToArray();
}
