using WebAppArchitecture.DigitalProcesses.Models.Shared;
using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.Shared;
internal static class AuxiliaryResultExtensions
{
    public static IResult ToHttpResult(this AuxiliaryOperationResult result)
        => result.IsProblem
            ? Results.Problem(statusCode: result.StatusCode, title: result.Title, detail: result.Detail)
            : result.StatusCode switch
            {
                200 => Results.Json(result.Value),
                204 => Results.NoContent(),
                400 => Results.BadRequest(result.Value),
                404 => Results.NotFound(),
                409 => Results.Conflict(result.Value),
                _ => Results.Json(result.Value, statusCode: result.StatusCode)
            };
}
