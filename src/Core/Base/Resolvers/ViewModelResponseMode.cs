using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace WebAppArchitecture.Base.Endpoints;


// ============================================================
// Response mode
// ============================================================

public enum ViewModelResponseMode
{
    Auto,
    Json,
    Html,
    Fragment
}


// ============================================================
// Default preference
// ============================================================
