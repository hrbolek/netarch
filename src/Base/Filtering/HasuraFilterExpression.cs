using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace WebAppArchitecture.Base.Filtering;

public sealed class HasuraFilterException : Exception
{
    public HasuraFilterException(string message)
        : base(message)
    {
    }

    public HasuraFilterException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}


public static class HasuraFilterExpression
{
    // ============================================================
    // Public API
    // ============================================================

    /// <summary>
    /// Converts a Hasura-like dictionary filter into
    /// Expression&lt;Func&lt;TModel, bool&gt;&gt;.
    ///
    /// maxDepth counts model/navigation depth:
    ///
    ///     root entity       = depth 1
    ///     related entity    = depth 2
    ///     next relation     = depth 3
    ///
    /// Logical operators (_and, _or, _not) do not increase depth.
    ///
    /// Default maxDepth = 2 therefore allows filtering by direct
    /// related entities, but prevents deeper relation traversal.
    /// </summary>
    public static Expression<Func<TModel, bool>> Build<TModel>(
        IDictionary<string, object?> filter,
        int maxDepth = 2)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (maxDepth < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDepth),
                maxDepth,
                "Maximum filter depth must be at least 1."
            );
        }

        var parameter =
            Expression.Parameter(
                typeof(TModel),
                "item"
            );

        var body =
            BuildObject(
                typeof(TModel),
                parameter,
                filter,
                currentDepth: 1,
                maxDepth: maxDepth
            );

        return Expression.Lambda<Func<TModel, bool>>(
            body,
            parameter
        );
    }


    /// <summary>
    /// Convenience overload for Dictionary.
    /// </summary>
    public static Expression<Func<TModel, bool>> Build<TModel>(
        Dictionary<string, object?> filter,
        int maxDepth = 2)
    {
        return Build<TModel>(
            (IDictionary<string, object?>)filter,
            maxDepth
        );
    }


    // ============================================================
    // Object level
    // ============================================================

    private static Expression BuildObject(
        Type modelType,
        Expression instance,
        IDictionary<string, object?> filter,
        int currentDepth,
        int maxDepth)
    {
        Expression? result = null;

        foreach (var item in filter)
        {
            Expression current;

            switch (item.Key.ToLowerInvariant())
            {
                case "_and":
                    current =
                        BuildLogicalArray(
                            modelType,
                            instance,
                            item.Value,
                            useAnd: true,
                            currentDepth,
                            maxDepth
                        );

                    break;


                case "_or":
                    current =
                        BuildLogicalArray(
                            modelType,
                            instance,
                            item.Value,
                            useAnd: false,
                            currentDepth,
                            maxDepth
                        );

                    break;


                case "_not":
                {
                    var nested =
                        AsDictionary(item.Value)
                        ?? throw new HasuraFilterException(
                            "_not expects an object."
                        );

                    // Logical nesting does NOT increase
                    // model/navigation depth.
                    current =
                        Expression.Not(
                            BuildObject(
                                modelType,
                                instance,
                                nested,
                                currentDepth,
                                maxDepth
                            )
                        );

                    break;
                }


                default:
                    current =
                        BuildMemberFilter(
                            modelType,
                            instance,
                            item.Key,
                            item.Value,
                            currentDepth,
                            maxDepth
                        );

                    break;
            }

            result =
                result is null
                    ? current
                    : Expression.AndAlso(
                        result,
                        current
                    );
        }

        // Empty filter = match everything.
        return result
            ?? Expression.Constant(true);
    }


    // ============================================================
    // Property / navigation
    // ============================================================

    private static Expression BuildMemberFilter(
        Type modelType,
        Expression instance,
        string memberName,
        object? rawValue,
        int currentDepth,
        int maxDepth)
    {
        var property =
            FindProperty(
                modelType,
                memberName
            );

        var member =
            Expression.Property(
                instance,
                property
            );

        var propertyType =
            property.PropertyType;

        var dictionary =
            AsDictionary(rawValue);

        // --------------------------------------------------------
        // Implicit equality:
        //
        // {
        //     "Status": 1
        // }
        // --------------------------------------------------------

        if (dictionary is null)
        {
            return BuildOperator(
                member,
                "_eq",
                rawValue
            );
        }


        var isOperatorDictionary =
            IsOperatorDictionary(dictionary);


        // --------------------------------------------------------
        // Operator dictionary.
        //
        // Scalar:
        //
        // {
        //     "Name": {
        //         "_ilike": "%demo%"
        //     }
        // }
        //
        // Collection:
        //
        // {
        //     "Roles": {
        //         "_some": {
        //             "Name": {
        //                 "_eq": "Admin"
        //             }
        //         }
        //     }
        // }
        // --------------------------------------------------------

        if (isOperatorDictionary)
        {
            if (
                TryGetEnumerableElementType(
                    propertyType,
                    out var elementType
                )
            )
            {
                return BuildCollectionFilter(
                    member,
                    property,
                    elementType,
                    dictionary,
                    currentDepth,
                    maxDepth
                );
            }

            return BuildScalarOperators(
                member,
                dictionary
            );
        }


        // --------------------------------------------------------
        // A collection must explicitly use _some/_none/_all.
        // --------------------------------------------------------

        if (
            TryGetEnumerableElementType(
                propertyType,
                out _
            )
        )
        {
            throw new HasuraFilterException(
                $"Collection property '{modelType.Name}.{property.Name}' " +
                "requires _some, _none or _all."
            );
        }


        // --------------------------------------------------------
        // Nested object / navigation property.
        //
        // {
        //     "Department": {
        //         "Name": {
        //             "_eq": "IT"
        //         }
        //     }
        // }
        // --------------------------------------------------------

        EnsureCanNavigateDeeper(
            modelType,
            property,
            currentDepth,
            maxDepth
        );


        var nestedType =
            Nullable.GetUnderlyingType(propertyType)
            ?? propertyType;


        var nestedInstance =
            GetNestedInstanceExpression(
                member,
                propertyType
            );


        var nested =
            BuildObject(
                nestedType,
                nestedInstance,
                dictionary,
                currentDepth + 1,
                maxDepth
            );


        // Null-safe navigation:
        //
        // x.Department != null
        // &&
        // x.Department.Name == ...
        //
        if (CanBeNull(propertyType))
        {
            var notNull =
                Expression.NotEqual(
                    member,
                    Expression.Constant(
                        null,
                        propertyType
                    )
                );

            return Expression.AndAlso(
                notNull,
                nested
            );
        }


        return nested;
    }


    // ============================================================
    // Logical operators
    // ============================================================

    private static Expression BuildLogicalArray(
        Type modelType,
        Expression instance,
        object? rawValue,
        bool useAnd,
        int currentDepth,
        int maxDepth)
    {
        var items =
            AsList(rawValue)
            ?? throw new HasuraFilterException(
                useAnd
                    ? "_and expects an array."
                    : "_or expects an array."
            );


        if (items.Count == 0)
        {
            // AND [] = true
            // OR  [] = false
            return Expression.Constant(useAnd);
        }


        Expression? result = null;

        foreach (var rawItem in items)
        {
            var dictionary =
                AsDictionary(rawItem)
                ?? throw new HasuraFilterException(
                    $"{(useAnd ? "_and" : "_or")} " +
                    "expects an array of objects."
                );


            var current =
                BuildObject(
                    modelType,
                    instance,
                    dictionary,

                    // Logical nesting does not increase
                    // relational depth.
                    currentDepth,
                    maxDepth
                );


            if (result is null)
            {
                result = current;
                continue;
            }


            result =
                useAnd
                    ? Expression.AndAlso(
                        result,
                        current
                    )
                    : Expression.OrElse(
                        result,
                        current
                    );
        }


        return result
            ?? Expression.Constant(useAnd);
    }


    // ============================================================
    // Scalar operators
    // ============================================================

    private static Expression BuildScalarOperators(
        Expression member,
        IDictionary<string, object?> operators)
    {
        Expression? result = null;

        foreach (var operation in operators)
        {
            var current =
                BuildOperator(
                    member,
                    operation.Key,
                    operation.Value
                );

            result =
                result is null
                    ? current
                    : Expression.AndAlso(
                        result,
                        current
                    );
        }

        return result
            ?? Expression.Constant(true);
    }


    private static Expression BuildOperator(
        Expression member,
        string operation,
        object? rawValue)
    {
        operation =
            operation.ToLowerInvariant();


        return operation switch
        {
            "_eq" =>
                BuildEqual(
                    member,
                    rawValue,
                    negate: false
                ),

            "_neq" =>
                BuildEqual(
                    member,
                    rawValue,
                    negate: true
                ),

            "_gt" =>
                BuildRelational(
                    member,
                    rawValue,
                    ExpressionType.GreaterThan
                ),

            "_gte" =>
                BuildRelational(
                    member,
                    rawValue,
                    ExpressionType.GreaterThanOrEqual
                ),

            "_lt" =>
                BuildRelational(
                    member,
                    rawValue,
                    ExpressionType.LessThan
                ),

            "_lte" =>
                BuildRelational(
                    member,
                    rawValue,
                    ExpressionType.LessThanOrEqual
                ),

            "_in" =>
                BuildIn(
                    member,
                    rawValue,
                    negate: false
                ),

            "_nin" =>
                BuildIn(
                    member,
                    rawValue,
                    negate: true
                ),

            "_like" =>
                BuildLike(
                    member,
                    rawValue,
                    ignoreCase: false
                ),

            "_ilike" =>
                BuildLike(
                    member,
                    rawValue,
                    ignoreCase: true
                ),

            "_is_null" =>
                BuildIsNull(
                    member,
                    rawValue
                ),

            _ =>
                throw new HasuraFilterException(
                    $"Unsupported filter operator '{operation}'."
                )
        };
    }


    // ============================================================
    // Equality
    // ============================================================

    private static Expression BuildEqual(
        Expression member,
        object? rawValue,
        bool negate)
    {
        if (IsNullValue(rawValue))
        {
            if (!CanBeNull(member.Type))
            {
                return Expression.Constant(
                    negate
                );
            }

            var comparison =
                Expression.Equal(
                    member,
                    Expression.Constant(
                        null,
                        member.Type
                    )
                );

            return negate
                ? Expression.Not(comparison)
                : comparison;
        }


        var constant =
            CreateTypedConstant(
                rawValue,
                member.Type
            );


        var expression =
            Expression.Equal(
                member,
                constant
            );


        return negate
            ? Expression.Not(expression)
            : expression;
    }


    // ============================================================
    // > >= < <=
    // ============================================================

    private static Expression BuildRelational(
        Expression member,
        object? rawValue,
        ExpressionType expressionType)
    {
        if (IsNullValue(rawValue))
        {
            throw new HasuraFilterException(
                $"Operator '{expressionType}' cannot compare with null."
            );
        }


        var constant =
            CreateTypedConstant(
                rawValue,
                member.Type
            );


        try
        {
            return Expression.MakeBinary(
                expressionType,
                member,
                constant
            );
        }
        catch (InvalidOperationException ex)
        {
            throw new HasuraFilterException(
                $"Operator '{expressionType}' cannot be applied " +
                $"to property type '{member.Type.Name}'.",
                ex
            );
        }
    }


    // ============================================================
    // IN / NOT IN
    // ============================================================

    private static Expression BuildIn(
        Expression member,
        object? rawValue,
        bool negate)
    {
        var values =
            AsList(rawValue)
            ?? throw new HasuraFilterException(
                $"{(negate ? "_nin" : "_in")} expects an array."
            );


        var targetType =
            member.Type;


        var array =
            Array.CreateInstance(
                targetType,
                values.Count
            );


        for (var i = 0; i < values.Count; i++)
        {
            var converted =
                ConvertValue(
                    values[i],
                    targetType
                );

            array.SetValue(
                converted,
                i
            );
        }


        var arrayExpression =
            Expression.Constant(
                array,
                targetType.MakeArrayType()
            );


        var contains =
            Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Contains),
                new[] { targetType },
                arrayExpression,
                member
            );


        return negate
            ? Expression.Not(contains)
            : contains;
    }


    // ============================================================
    // LIKE / ILIKE
    // ============================================================

    private static Expression BuildLike(
        Expression member,
        object? rawValue,
        bool ignoreCase)
    {
        if (member.Type != typeof(string))
        {
            throw new HasuraFilterException(
                $"{(ignoreCase ? "_ilike" : "_like")} " +
                $"can only be applied to string properties."
            );
        }


        var pattern =
            ConvertValue(
                rawValue,
                typeof(string)
            ) as string
            ?? throw new HasuraFilterException(
                $"{(ignoreCase ? "_ilike" : "_like")} " +
                "expects a string."
            );


        // Keep the expression SQL/EF friendly.
        //
        // Supported:
        //
        //   abc       -> Equals
        //   abc%      -> StartsWith
        //   %abc      -> EndsWith
        //   %abc%     -> Contains
        //
        // Arbitrary internal '%' or '_' wildcards are intentionally
        // rejected here. A DB-specific implementation can later
        // replace this with EF.Functions.Like / ILike.

        var startsWithPercent =
            pattern.StartsWith('%');

        var endsWithPercent =
            pattern.EndsWith('%');


        var innerPattern =
            pattern.Trim('%');


        if (
            innerPattern.Contains('%')
            ||
            innerPattern.Contains('_')
        )
        {
            throw new HasuraFilterException(
                $"{(ignoreCase ? "_ilike" : "_like")} currently supports " +
                "only prefix/suffix '%' wildcards."
            );
        }


        Expression stringMember =
            member;

        Expression stringPattern =
            Expression.Constant(
                innerPattern
            );


        if (ignoreCase)
        {
            var toLower =
                typeof(string).GetMethod(
                    nameof(string.ToLower),
                    Type.EmptyTypes
                )!;


            stringMember =
                Expression.Call(
                    member,
                    toLower
                );


            stringPattern =
                Expression.Constant(
                    innerPattern.ToLowerInvariant()
                );
        }


        Expression comparison;


        if (
            startsWithPercent
            &&
            endsWithPercent
        )
        {
            comparison =
                Expression.Call(
                    stringMember,
                    nameof(string.Contains),
                    Type.EmptyTypes,
                    stringPattern
                );
        }
        else if (startsWithPercent)
        {
            comparison =
                Expression.Call(
                    stringMember,
                    nameof(string.EndsWith),
                    Type.EmptyTypes,
                    stringPattern
                );
        }
        else if (endsWithPercent)
        {
            comparison =
                Expression.Call(
                    stringMember,
                    nameof(string.StartsWith),
                    Type.EmptyTypes,
                    stringPattern
                );
        }
        else
        {
            comparison =
                Expression.Equal(
                    stringMember,
                    stringPattern
                );
        }


        // null-safe string operation
        var notNull =
            Expression.NotEqual(
                member,
                Expression.Constant(
                    null,
                    typeof(string)
                )
            );


        return Expression.AndAlso(
            notNull,
            comparison
        );
    }


    // ============================================================
    // IS NULL
    // ============================================================

    private static Expression BuildIsNull(
        Expression member,
        object? rawValue)
    {
        var converted =
            ConvertValue(
                rawValue,
                typeof(bool)
            );


        if (converted is not bool isNull)
        {
            throw new HasuraFilterException(
                "_is_null expects a boolean value."
            );
        }


        if (!CanBeNull(member.Type))
        {
            return Expression.Constant(
                !isNull
            );
        }


        var comparison =
            Expression.Equal(
                member,
                Expression.Constant(
                    null,
                    member.Type
                )
            );


        return isNull
            ? comparison
            : Expression.Not(comparison);
    }


    // ============================================================
    // Collections
    // ============================================================

    private static Expression BuildCollectionFilter(
        Expression collection,
        PropertyInfo property,
        Type elementType,
        IDictionary<string, object?> operators,
        int currentDepth,
        int maxDepth)
    {
        Expression? result = null;


        foreach (var operation in operators)
        {
            Expression current;


            switch (operation.Key.ToLowerInvariant())
            {
                case "_some":
                    EnsureCanNavigateDeeper(
                        property.DeclaringType
                            ?? collection.Type,
                        property,
                        currentDepth,
                        maxDepth
                    );

                    current =
                        BuildCollectionAny(
                            collection,
                            elementType,
                            operation.Value,
                            negate: false,
                            currentDepth,
                            maxDepth
                        );

                    break;


                case "_none":
                    EnsureCanNavigateDeeper(
                        property.DeclaringType
                            ?? collection.Type,
                        property,
                        currentDepth,
                        maxDepth
                    );

                    current =
                        BuildCollectionAny(
                            collection,
                            elementType,
                            operation.Value,
                            negate: true,
                            currentDepth,
                            maxDepth
                        );

                    break;


                case "_all":
                    EnsureCanNavigateDeeper(
                        property.DeclaringType
                            ?? collection.Type,
                        property,
                        currentDepth,
                        maxDepth
                    );

                    current =
                        BuildCollectionAll(
                            collection,
                            elementType,
                            operation.Value,
                            currentDepth,
                            maxDepth
                        );

                    break;


                case "_is_null":
                    current =
                        BuildIsNull(
                            collection,
                            operation.Value
                        );

                    break;


                default:
                    throw new HasuraFilterException(
                        $"Unsupported collection operator " +
                        $"'{operation.Key}' on property '{property.Name}'."
                    );
            }


            result =
                result is null
                    ? current
                    : Expression.AndAlso(
                        result,
                        current
                    );
        }


        return result
            ?? Expression.Constant(true);
    }


    private static Expression BuildCollectionAny(
        Expression collection,
        Type elementType,
        object? rawFilter,
        bool negate,
        int currentDepth,
        int maxDepth)
    {
        var dictionary =
            AsDictionary(rawFilter)
            ?? throw new HasuraFilterException(
                $"{(negate ? "_none" : "_some")} expects an object."
            );


        var parameter =
            Expression.Parameter(
                elementType,
                "child"
            );


        var body =
            BuildObject(
                elementType,
                parameter,
                dictionary,
                currentDepth + 1,
                maxDepth
            );


        var lambda =
            Expression.Lambda(
                body,
                parameter
            );


        var any =
            Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Any),
                new[] { elementType },
                collection,
                lambda
            );


        // Collection navigation can technically be null.
        //
        // _some:
        //     collection != null && collection.Any(...)
        //
        // _none:
        //     collection == null || !collection.Any(...)

        if (CanBeNull(collection.Type))
        {
            var nullExpression =
                Expression.Constant(
                    null,
                    collection.Type
                );


            if (negate)
            {
                return Expression.OrElse(
                    Expression.Equal(
                        collection,
                        nullExpression
                    ),
                    Expression.Not(any)
                );
            }


            return Expression.AndAlso(
                Expression.NotEqual(
                    collection,
                    nullExpression
                ),
                any
            );
        }


        return negate
            ? Expression.Not(any)
            : any;
    }


    private static Expression BuildCollectionAll(
        Expression collection,
        Type elementType,
        object? rawFilter,
        int currentDepth,
        int maxDepth)
    {
        var dictionary =
            AsDictionary(rawFilter)
            ?? throw new HasuraFilterException(
                "_all expects an object."
            );


        var parameter =
            Expression.Parameter(
                elementType,
                "child"
            );


        var body =
            BuildObject(
                elementType,
                parameter,
                dictionary,
                currentDepth + 1,
                maxDepth
            );


        var lambda =
            Expression.Lambda(
                body,
                parameter
            );


        var all =
            Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.All),
                new[] { elementType },
                collection,
                lambda
            );


        // Null collection is interpreted as an empty collection.
        // Therefore _all is true for null/empty collection.
        if (CanBeNull(collection.Type))
        {
            return Expression.OrElse(
                Expression.Equal(
                    collection,
                    Expression.Constant(
                        null,
                        collection.Type
                    )
                ),
                all
            );
        }


        return all;
    }


    // ============================================================
    // Depth
    // ============================================================

    private static void EnsureCanNavigateDeeper(
        Type sourceType,
        PropertyInfo property,
        int currentDepth,
        int maxDepth)
    {
        if (currentDepth < maxDepth)
            return;


        throw new HasuraFilterException(
            $"Maximum filter depth {maxDepth} exceeded. " +
            $"Current entity '{sourceType.Name}' is at depth " +
            $"{currentDepth}. Navigation through " +
            $"'{sourceType.Name}.{property.Name}' would require " +
            $"depth {currentDepth + 1}."
        );
    }


    // ============================================================
    // Reflection helpers
    // ============================================================

    private static PropertyInfo FindProperty(
        Type modelType,
        string propertyName)
    {
        return modelType
            .GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance
            )
            .FirstOrDefault(
                property =>
                    string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase
                    )
            )
            ?? throw new HasuraFilterException(
                $"Property '{propertyName}' was not found " +
                $"on type '{modelType.Name}'."
            );
    }


    private static bool IsOperatorDictionary(
        IDictionary<string, object?> dictionary)
    {
        var operatorCount =
            dictionary.Keys.Count(
                key =>
                    key.StartsWith(
                        "_",
                        StringComparison.Ordinal
                    )
            );


        if (
            operatorCount > 0
            &&
            operatorCount != dictionary.Count
        )
        {
            throw new HasuraFilterException(
                "A filter object cannot mix operators and nested " +
                "property names at the same level."
            );
        }


        return operatorCount > 0;
    }


    private static bool TryGetEnumerableElementType(
        Type type,
        out Type elementType)
    {
        elementType = null!;


        // string implements IEnumerable<char>, but is scalar here.
        if (type == typeof(string))
            return false;


        if (type.IsArray)
        {
            elementType =
                type.GetElementType()!;

            return true;
        }


        var enumerableType =
            type
                .GetInterfaces()
                .Concat(new[] { type })
                .FirstOrDefault(
                    candidate =>
                        candidate.IsGenericType
                        &&
                        candidate.GetGenericTypeDefinition()
                            == typeof(IEnumerable<>)
                );


        if (enumerableType is null)
            return false;


        elementType =
            enumerableType
                .GetGenericArguments()[0];


        return true;
    }


    private static bool CanBeNull(
        Type type)
    {
        return !type.IsValueType
            ||
            Nullable.GetUnderlyingType(type) is not null;
    }


    private static Expression GetNestedInstanceExpression(
        Expression member,
        Type propertyType)
    {
        var nullableType =
            Nullable.GetUnderlyingType(propertyType);


        if (nullableType is null)
            return member;


        return Expression.Property(
            member,
            nameof(Nullable<int>.Value)
        );
    }


    // ============================================================
    // Dictionary / array conversion
    // ============================================================

    private static IDictionary<string, object?>? AsDictionary(
        object? value)
    {
        if (value is null)
            return null;


        if (
            value
            is IDictionary<string, object?> dictionary
        )
        {
            return dictionary;
        }


        if (
            value
            is IReadOnlyDictionary<string, object?> readOnly
        )
        {
            return readOnly.ToDictionary(
                item => item.Key,
                item => item.Value,
                StringComparer.OrdinalIgnoreCase
            );
        }


        if (
            value is JsonElement json
            &&
            json.ValueKind == JsonValueKind.Object
        )
        {
            return json
                .EnumerateObject()
                .ToDictionary(
                    property => property.Name,
                    property =>
                        ConvertJsonElement(
                            property.Value
                        ),
                    StringComparer.OrdinalIgnoreCase
                );
        }


        return null;
    }


    private static IReadOnlyList<object?>? AsList(
        object? value)
    {
        if (value is null)
            return null;


        if (
            value is JsonElement json
            &&
            json.ValueKind == JsonValueKind.Array
        )
        {
            return json
                .EnumerateArray()
                .Select(ConvertJsonElement)
                .ToArray();
        }


        if (
            value is IEnumerable enumerable
            &&
            value is not string
        )
        {
            return enumerable
                .Cast<object?>()
                .ToArray();
        }


        return null;
    }


    private static object? ConvertJsonElement(
        JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null =>
                null,

            JsonValueKind.Undefined =>
                null,

            JsonValueKind.True =>
                true,

            JsonValueKind.False =>
                false,

            JsonValueKind.String =>
                element.GetString(),

            JsonValueKind.Number =>
                element.TryGetInt64(out var integer)
                    ? integer
                    : element.TryGetDecimal(out var decimalValue)
                        ? decimalValue
                        : element.GetDouble(),

            JsonValueKind.Object =>
                element
                    .EnumerateObject()
                    .ToDictionary(
                        property => property.Name,
                        property =>
                            ConvertJsonElement(
                                property.Value
                            ),
                        StringComparer.OrdinalIgnoreCase
                    ),

            JsonValueKind.Array =>
                element
                    .EnumerateArray()
                    .Select(ConvertJsonElement)
                    .ToArray(),

            _ =>
                element.ToString()
        };
    }


    // ============================================================
    // Value conversion
    // ============================================================

    private static Expression CreateTypedConstant(
        object? rawValue,
        Type targetType)
    {
        var value =
            ConvertValue(
                rawValue,
                targetType
            );


        return Expression.Constant(
            value,
            targetType
        );
    }


    private static object? ConvertValue(
        object? rawValue,
        Type targetType)
    {
        if (rawValue is JsonElement json)
        {
            rawValue =
                ConvertJsonElement(json);
        }


        if (rawValue is null)
        {
            if (CanBeNull(targetType))
                return null;


            throw new HasuraFilterException(
                $"Null cannot be converted to " +
                $"non-nullable type '{targetType.Name}'."
            );
        }


        var underlyingType =
            Nullable.GetUnderlyingType(targetType);


        var conversionType =
            underlyingType
            ?? targetType;


        try
        {
            object converted;


            if (conversionType.IsInstanceOfType(rawValue))
            {
                converted = rawValue;
            }
            else if (conversionType == typeof(Guid))
            {
                converted =
                    rawValue is Guid guid
                        ? guid
                        : Guid.Parse(
                            Convert.ToString(
                                rawValue,
                                CultureInfo.InvariantCulture
                            )!
                        );
            }
            else if (conversionType == typeof(DateTime))
            {
                converted =
                    rawValue is DateTime dateTime
                        ? dateTime
                        : DateTime.Parse(
                            Convert.ToString(
                                rawValue,
                                CultureInfo.InvariantCulture
                            )!,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind
                        );
            }
            else if (conversionType == typeof(DateTimeOffset))
            {
                converted =
                    rawValue is DateTimeOffset dateTimeOffset
                        ? dateTimeOffset
                        : DateTimeOffset.Parse(
                            Convert.ToString(
                                rawValue,
                                CultureInfo.InvariantCulture
                            )!,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind
                        );
            }
            else if (conversionType == typeof(DateOnly))
            {
                converted =
                    rawValue is DateOnly dateOnly
                        ? dateOnly
                        : DateOnly.Parse(
                            Convert.ToString(
                                rawValue,
                                CultureInfo.InvariantCulture
                            )!,
                            CultureInfo.InvariantCulture
                        );
            }
            else if (conversionType == typeof(TimeOnly))
            {
                converted =
                    rawValue is TimeOnly timeOnly
                        ? timeOnly
                        : TimeOnly.Parse(
                            Convert.ToString(
                                rawValue,
                                CultureInfo.InvariantCulture
                            )!,
                            CultureInfo.InvariantCulture
                        );
            }
            else if (conversionType.IsEnum)
            {
                if (rawValue is string enumName)
                {
                    converted =
                        Enum.Parse(
                            conversionType,
                            enumName,
                            ignoreCase: true
                        );
                }
                else
                {
                    var enumUnderlyingType =
                        Enum.GetUnderlyingType(
                            conversionType
                        );


                    var enumValue =
                        Convert.ChangeType(
                            rawValue,
                            enumUnderlyingType,
                            CultureInfo.InvariantCulture
                        );


                    converted =
                        Enum.ToObject(
                            conversionType,
                            enumValue!
                        );
                }
            }
            else if (conversionType == typeof(string))
            {
                converted =
                    Convert.ToString(
                        rawValue,
                        CultureInfo.InvariantCulture
                    )!;
            }
            else
            {
                converted =
                    Convert.ChangeType(
                        rawValue,
                        conversionType,
                        CultureInfo.InvariantCulture
                    )!;
            }


            // Expression.Constant requires an instance of
            // Nullable<T> when targetType itself is Nullable<T>.
            if (underlyingType is not null)
            {
                return Activator.CreateInstance(
                    targetType,
                    converted
                );
            }


            return converted;
        }
        catch (Exception ex)
            when (
                ex is FormatException
                or InvalidCastException
                or OverflowException
                or ArgumentException
            )
        {
            throw new HasuraFilterException(
                $"Value '{rawValue}' cannot be converted " +
                $"to '{targetType.Name}'.",
                ex
            );
        }
    }


    private static bool IsNullValue(
        object? value)
    {
        if (value is null)
            return true;


        return value is JsonElement json
            &&
            (
                json.ValueKind == JsonValueKind.Null
                ||
                json.ValueKind == JsonValueKind.Undefined
            );
    }
}
