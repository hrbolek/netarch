using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace WebAppArchitecture.Base.Endpoints;


// ============================================================
// Response mode
// ============================================================

/// <summary>
/// Typ I Result ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public interface IModelResult
{
    object? Model { get; }
}
