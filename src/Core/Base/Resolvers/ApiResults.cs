using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;

namespace WebAppArchitecture.Base.Endpoints;


// ============================================================
// Response mode
// ============================================================

public static class ApiResults
{
    public static ModelResult<TModel> Model<TModel>(
        TModel model)
    {
        return new ModelResult<TModel>(model);
    }

    public static ModelResult<TModel> Ok<TModel>(
        TModel model)
    {
        return new ModelResult<TModel>(model);
    }

}
