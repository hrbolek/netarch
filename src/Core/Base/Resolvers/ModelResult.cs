using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace WebAppArchitecture.Base.Endpoints;


// ============================================================
// Response mode
// ============================================================

public sealed class ModelResult<TModel>
    : IResult, IModelResult
{
    public TModel Value { get; }

    object? IModelResult.Model => Value;


    public ModelResult(TModel value)
    {
        Value = value;
    }


    public Task ExecuteAsync(
        HttpContext httpContext
    )
    {
        return Results
            .Ok(Value)
            .ExecuteAsync(httpContext);
    }
}
