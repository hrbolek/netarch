using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using System.Text.Json;
using WebAppArchitecture.Base.Filtering;
using WebAppArchitecture.Base.Services;

namespace WebAppArchitecture.Base.Resolvers;

/// <summary>
/// Typ Vector Query Parser ve vrstvě Core; dokumentuje kontrakt a roli typu v architektuře.
/// </summary>
public static class VectorQueryParser
{
    public const int DefaultTake = 25;
    public const int MaxTake = 200;
    public static bool TryParse<TModel>(
        HttpContext httpContext,
        Expression<Func<TModel, bool>> mandatoryPredicate,
        out Expression<Func<TModel, bool>> predicate,
        out VectorQueryOptions options,
        out string? error)
    {
        predicate = mandatoryPredicate;
        options = VectorQueryOptions.Empty;
        error = null;

        var where = httpContext.Request.Query["where"].ToString();
        if (!string.IsNullOrWhiteSpace(where))
        {
            try
            {
                var filter = JsonSerializer.Deserialize<Dictionary<string, object?>>(where)
                    ?? new Dictionary<string, object?>();
                predicate = And(mandatoryPredicate, HasuraFilterExpression.Build<TModel>(filter));
            }
            catch (JsonException ex)
            {
                error = $"The 'where' query parameter must be a valid JSON object. {ex.Message}";
                return false;
            }
            catch (HasuraFilterException ex)
            {
                error = $"Invalid Hasura-like filter expression. {ex.Message}";
                return false;
            }
        }

        int? take = DefaultTake;
        var takeText = httpContext.Request.Query["take"].ToString();
        if (!string.IsNullOrWhiteSpace(takeText))
        {
            if (!int.TryParse(takeText, out var parsedTake) || parsedTake < 1 || parsedTake > MaxTake)
            {
                error = $"The 'take' query parameter must be an integer between 1 and {MaxTake}.";
                return false;
            }
            take = parsedTake;
        }

        int? skip = null;
        var skipText = httpContext.Request.Query["skip"].ToString();
        if (!string.IsNullOrWhiteSpace(skipText))
        {
            if (!int.TryParse(skipText, out var parsedSkip) || parsedSkip < 0)
            {
                error = "The 'skip' query parameter must be a non-negative integer.";
                return false;
            }
            skip = parsedSkip;
        }

        IReadOnlyCollection<VectorOrderBy>? orderBy = null;
        var orderByText = httpContext.Request.Query["order_by"].ToString();
        if (!string.IsNullOrWhiteSpace(orderByText))
        {
            try
            {
                using var document = JsonDocument.Parse(orderByText);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    error = "The 'order_by' query parameter must be a JSON object.";
                    return false;
                }

                var parsed = new List<VectorOrderBy>();
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    var modelProperty = typeof(TModel).GetProperty(property.Name,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.IgnoreCase);
                    if (modelProperty is null)
                    {
                        error = $"Unknown order_by property '{property.Name}'.";
                        return false;
                    }
                    if (Attribute.IsDefined(modelProperty, typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute)))
                    {
                        error = $"The order_by property '{property.Name}' is not a persisted field and cannot be ordered in the database query.";
                        return false;
                    }

                    var direction = property.Value.GetString();
                    if (!string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
                    {
                        error = $"order_by direction for '{property.Name}' must be 'asc' or 'desc'.";
                        return false;
                    }

                    parsed.Add(new VectorOrderBy(property.Name,
                        string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase)));
                }
                orderBy = parsed;
            }
            catch (JsonException ex)
            {
                error = $"The 'order_by' query parameter must be valid JSON. {ex.Message}";
                return false;
            }
        }

        options = new VectorQueryOptions(take, skip, orderBy);
        return true;
    }

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static VectorPageInfo WithRequestState(HttpContext httpContext, VectorPageInfo pageInfo)
        => pageInfo with
        {
            HasFilter = !string.IsNullOrWhiteSpace(httpContext.Request.Query["where"].ToString()),
            HasOrderBy = !string.IsNullOrWhiteSpace(httpContext.Request.Query["order_by"].ToString())
        };

    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static void WritePagingHeaders(HttpContext httpContext, VectorPageInfo pageInfo)
    {
        httpContext.Response.Headers["X-Vector-Take"] = pageInfo.Take.ToString();
        httpContext.Response.Headers["X-Vector-Skip"] = pageInfo.Skip.ToString();
        httpContext.Response.Headers["X-Vector-Has-More"] = pageInfo.HasMore ? "true" : "false";
    }

    private static Expression<Func<TModel, bool>> And<TModel>(
        Expression<Func<TModel, bool>> left,
        Expression<Func<TModel, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(TModel), "entity");
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<TModel, bool>>(
            Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to)
        : ExpressionVisitor
    {
        /// <summary>
        /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
        /// </summary>
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
