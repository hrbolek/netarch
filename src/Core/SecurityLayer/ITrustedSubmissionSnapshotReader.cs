using WebAppArchitecture.DBLayer;

namespace WebAppArchitecture.SecurityLayer;

/// <summary>
/// Privileged system operation for cloning an authorized Request across workflow states.
/// NEVER map this interface directly to a user-facing HTTP endpoint.
/// Ordinary reads must continue to use ISecurityLayer.Query(Read).
/// </summary>
public interface ITrustedSubmissionSnapshotReader
{
    SubmissionNodeModel[] ReadCompleteTree(Guid submissionId);
}

public sealed class TrustedSubmissionSnapshotReader(IDBLayer db) : ITrustedSubmissionSnapshotReader
{
    public SubmissionNodeModel[] ReadCompleteTree(Guid submissionId)
        => db.Query<SubmissionNodeModel>()
            .Where(x => x.submission_id == submissionId)
            .OrderBy(x => x.path).ToArray();
}
