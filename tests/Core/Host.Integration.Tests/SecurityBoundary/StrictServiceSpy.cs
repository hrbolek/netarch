using System.Reflection;
using Microsoft.AspNetCore.Http;
using WebAppArchitecture.Base.Security;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed record ServiceCall(
    MethodInfo Method,
    object?[] Arguments,
    Guid? Actor,
    string? Path);

public class StrictServiceSpy : DispatchProxy
{
    private readonly Queue<(MethodInfo Method, Func<ServiceCall, object?> Respond)> expectations = new();
    private readonly List<ServiceCall> calls = new();
    private readonly List<string> unexpected = new();
    private readonly object gate = new();
    private Func<HttpContext?> context = () => null;
    private readonly ICurrentActorProvider actors = new WebAppArchitecture.Base.Security.HeaderOrClaimActorProvider();

    public IReadOnlyList<ServiceCall> Calls
    {
        get { lock (gate) return calls.ToArray(); }
    }

    public static (T Service, StrictServiceSpy Spy) Create<T>() where T : class
    {
        var service = Create<T, StrictServiceSpy>();
        return (service, (StrictServiceSpy)(object)service);
    }

    public void Bind(Func<HttpContext?> currentContext) => context = currentContext;

    public void Expect(string method, Func<ServiceCall, object?> respond, params Type[] genericArguments)
        => Configure(method, null, respond, genericArguments);

    public void ExpectOverload(string method, int parameterCount, Func<ServiceCall, object?> respond, params Type[] genericArguments)
        => Configure(method, parameterCount, respond, genericArguments);

    private void Configure(string name, int? parameterCount, Func<ServiceCall, object?> respond, Type[] genericArguments)
    {
        var candidates = GetType().GetInterfaces().SelectMany(contract => contract.GetMethods())
            .Where(method => method.Name == name && method.GetGenericArguments().Length == genericArguments.Length &&
                (parameterCount is null || method.GetParameters().Length == parameterCount))
            .Distinct().ToArray();
        if (candidates.Length != 1)
            throw new InvalidOperationException($"Expected one interface method for {name}, found {candidates.Length}. Select an unambiguous overload.");
        var method = genericArguments.Length == 0 ? candidates[0] : candidates[0].MakeGenericMethod(genericArguments);
        lock (gate)
            expectations.Enqueue((method, respond));
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);
        var http = context();
        var call = new ServiceCall(targetMethod, args ?? [],
            http is null ? null : actors.TryGetActorUserId(http), http?.Request.Path.Value);
        Func<ServiceCall, object?> respond;
        lock (gate)
        {
            calls.Add(call);
            if (!expectations.TryDequeue(out var expected) ||
                !expected.Method.Equals(targetMethod))
            {
                var message = $"Unexpected service call: {targetMethod.DeclaringType?.Name}.{targetMethod.Name}";
                unexpected.Add(message);
                throw new InvalidOperationException(message);
            }
            respond = expected.Respond;
        }
        try
        {
            return respond(call);
        }
        catch (Xunit.Sdk.XunitException exception)
        {
            // A resolver may catch exceptions; it must not hide a failed
            // argument assertion inside an otherwise expected HTTP error.
            lock (gate)
                unexpected.Add(exception.Message);
            throw;
        }
    }

    public void Verify()
    {
        lock (gate)
        {
            Assert.Empty(unexpected);
            Assert.Empty(expectations);
        }
    }
}
