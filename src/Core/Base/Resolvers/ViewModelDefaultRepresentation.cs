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
/// Typ View Default Representation ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public enum ViewModelDefaultRepresentation
{
    Html,
    Json
}


// ============================================================
// Options
// ============================================================
