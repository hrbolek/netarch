using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace WebAppArchitecture.DBLayer;

public interface IDatabaseDumpService
{
    Task<bool> LoadIfExistsAsync(string path, CancellationToken cancellationToken = default);
    Task SaveAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// JSON snapshot of the physical SQLite tables. This intentionally works below the
/// entity layer so the dump remains independent of record constructors and navigation
/// properties. The schema itself is still created by EF Core before restore.
/// </summary>
