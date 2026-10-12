using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;

namespace WebAppArchitecture.DigitalProcesses.Services;

/// <summary>Temporary, server-owned preview state. Never used as a durable Submission.</summary>
public sealed class PreviewFormInstanceStore(IMemoryCache cache)
{
    private readonly ConcurrentDictionary<Guid, object> _locks = new();

    public T WithInstance<T>(Guid id, Guid versionId, Func<PreviewFormInstance, T> handler)
    {
        var gate = _locks.GetOrAdd(id, static _ => new object());
        lock (gate)
        {
            var entry = cache.Get<PreviewFormInstance>(id);
            if (entry is null)
            {
                entry = new PreviewFormInstance(versionId);
                cache.Set(id, entry, new MemoryCacheEntryOptions {
                    SlidingExpiration = TimeSpan.FromMinutes(40),
                });
            }
            if (entry.VersionId != versionId)
                throw new InvalidOperationException("Preview instance belongs to another form version.");
            return handler(entry);
        }
    }
}

/// <summary>
/// Typ Preview Form Instance ve vrstvě Apps; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public sealed class PreviewFormInstance(Guid versionId)
{
    public Guid VersionId { get; } = versionId;
    public Dictionary<string, JsonNode?> Values { get; } = new(StringComparer.Ordinal);
    public long Revision { get; set; }
    // Indexed instances are persisted separately from immutable form node definitions.
}
