using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.Base.Resolvers;
using Xunit;

namespace Core.Host.Integration.Tests.SecurityBoundary;

public sealed class EndpointSecurityInventoryTests
{
    private sealed record Coverage(string Method, string Route, Type TestClass, string TestMethod);

    private sealed record EndpointSecurity(
        string Method,
        string Route,
        string? Name,
        string Handler,
        string Authorization,
        string[] ServiceDependencies,
        string ExpectedRbacOperation,
        string[] ExistingSecurityTestEvidence,
        string[] DeclaredContractTests,
        string CoverageStatus);

    [Fact]
    public void EveryRegisteredEndpointAppearsInSecurityCoverageReport()
    {
        using var host = new ResolverBoundaryHost();
        var endpoints = host.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .Distinct().ToArray();
        Assert.NotEmpty(endpoints);
        var coverage = DeclaredCoverage().Concat(endpoints.Where(endpoint =>
                GenericListBoundaryTests.ListContract(endpoint.Metadata.GetMetadata<MethodInfo>()) is not null)
            .Select(endpoint => new Coverage("GET", endpoint.RoutePattern.RawText!,
                typeof(GenericListBoundaryTests), nameof(GenericListBoundaryTests.EveryRegisteredGenericListUsesReadAndRejectsCallerChosenOperation))))
            .ToArray();
        foreach (var entry in coverage)
        {
            Assert.NotNull(entry.TestClass.GetMethod(entry.TestMethod));
            Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText == entry.Route &&
                endpoint.Metadata.GetRequiredMetadata<IHttpMethodMetadata>().HttpMethods.Contains(entry.Method));
        }
        var report = endpoints.SelectMany(endpoint =>
        {
            var handler = endpoint.Metadata.GetMetadata<MethodInfo>();
            var route = endpoint.RoutePattern.RawText ?? throw new InvalidOperationException("Unnamed route pattern");
            var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            var authorization = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
                ? "Explicit AllowAnonymous metadata; domain service policy still requires review"
                : policies.Count > 0
                    ? string.Join("; ", policies.Select(policy => $"ASP.NET policy={policy.Policy}; roles={policy.Roles}; schemes={policy.AuthenticationSchemes}"))
                    : "No ASP.NET authorization policy metadata; handler/service owns authorization (not proven by metadata)";
            var services = handler?.GetParameters().Select(parameter => parameter.ParameterType)
                .Where(type => ResolverBoundaryHost.IsServiceContract(type) ||
                    type.Assembly.GetName().Name?.EndsWith(".Services", StringComparison.Ordinal) == true)
                .Select(type => type.ToString()).Order().ToArray() ?? [];
            var operation = Operation(handler);
            return (endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? ["ANY"])
                .Select(method =>
                {
                    var tests = coverage.Where(entry => entry.Method == method && entry.Route == route)
                        .Select(entry => entry.TestClass.Name + "." + entry.TestMethod).Distinct().Order().ToArray();
                    return new EndpointSecurity(method, route,
                        endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName,
                        handler is null ? "UNKNOWN: missing MethodInfo" : handler.DeclaringType?.FullName + "." + handler.Name,
                        authorization, services, operation, ExistingEvidence(method, route), tests,
                        tests.Length == 0 ? "UNCOVERED by strict HTTP contract suite" : "Declared contract cases; consult TRX for pass/fail and scenario scope");
                });
        }).OrderBy(entry => entry.Route, StringComparer.Ordinal).ThenBy(entry => entry.Method, StringComparer.Ordinal).ToArray();
        Assert.Equal(endpoints.Sum(endpoint => endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Count ?? 1), report.Length);
        Assert.Contains(report, entry => entry.Route.StartsWith("/architecture/UserResponse/", StringComparison.Ordinal));
        Assert.Contains(report, entry => entry.Route.StartsWith("/digital-processes/", StringComparison.Ordinal));
        Assert.Contains(report, entry => entry.Route.StartsWith("/events/", StringComparison.Ordinal));

        var directory = Path.Combine(ResolverDependencyTests.RepositoryRoot(), "artifacts", "security-boundaries");
        Directory.CreateDirectory(directory);
        var configuration = typeof(Program).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        File.WriteAllText(Path.Combine(directory, "endpoint-security.json"),
            JsonSerializer.Serialize(new
            {
                configuration,
                scope = "Actual Program routes for this build; middleware-only paths and static files are not RouteEndpoints.",
                limitation = "Declared tests are scenario coverage, not proof of complete authorization. UNKNOWN policies require manual review. See TRX for executed checks.",
                totals = new
                {
                    endpoints = report.Length,
                    withDeclaredContractTests = report.Count(entry => entry.DeclaredContractTests.Length > 0),
                    withoutDeclaredContractTests = report.Count(entry => entry.DeclaredContractTests.Length == 0),
                    unknownRbacOperation = report.Count(entry => entry.ExpectedRbacOperation.StartsWith("UNKNOWN", StringComparison.Ordinal))
                },
                endpoints = report
            }, new JsonSerializerOptions { WriteIndented = true }));
        var markdown = new StringBuilder("# Endpoint security coverage\n\n");
        markdown.AppendLine($"Build: {configuration}. Registered method/route pairs: {report.Length}.");
        markdown.AppendLine("This is an inventory, not a full authorization guarantee. Declared tests may cover only one scenario; consult TRX. UNKNOWN operations and UNCOVERED paths require review.");
        markdown.AppendLine("\n| Method | Route | Handler | Authorization | Service dependencies | Expected RBAC operation | Existing security evidence | Declared strict contract tests |");
        markdown.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var entry in report)
            markdown.AppendLine($"| {entry.Method} | {entry.Route} | {Cell(entry.Handler)} | {Cell(entry.Authorization)} | {Cell(string.Join(", ", entry.ServiceDependencies))} | {Cell(entry.ExpectedRbacOperation)} | {Cell(string.Join(", ", entry.ExistingSecurityTestEvidence))} | {Cell(entry.DeclaredContractTests.Length == 0 ? entry.CoverageStatus : string.Join(", ", entry.DeclaredContractTests))} |");
        File.WriteAllText(Path.Combine(directory, "endpoint-security.md"), markdown.ToString());
        host.Verify();
    }

    private static string Cell(string value) => value.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");

    private static string[] ExistingEvidence(string method, string route)
    {
        List<string> evidence = [];
        if (method == "GET" && route is "/architecture/GroupResponse/list" or "/architecture/GroupResponse/create-targets")
            evidence.Add("tests/Core/Host.Integration.Tests/GroupListOperationTests.cs#ListUsesReadPermissionAndCreatePickerUsesCreatePermission");
        if ((method, route) is ("GET", "/architecture/UserResponse/view/{id:guid}") or
            ("PUT", "/architecture/UserResponse/update") or ("POST", "/architecture/UserResponse/update/{id:guid}"))
            evidence.Add("tests/Core/Host.Integration.Tests/SecuredUpdateTests.cs#JsonAndFormUpdatesRequireOriginalTokenAndKeepAuditServerOwned");
        if (method == "GET" && route is "/digital-processes/RequestResponse/list" or
            "/digital-processes/SubmissionResponse/list" or "/digital-processes/SubmissionNodeResponse/list")
            evidence.Add("tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcessesAuthorizationHttpTests.cs#T11_EmptyDatabase_DoesNotRevealAnyRuntimeEntity (empty-data case only)");
        if ((method, route) is ("POST", "/digital-processes/RequestResponse/start") or
            ("POST", "/digital-processes/RequestResponse/transition/{id:guid}"))
            evidence.Add("tests/Apps/DigitalProcesses.Integration.Tests/DigitalProcessesCommandAtomicityTests.cs (unknown-id negative cases)");
        foreach (var item in evidence)
            Assert.True(File.Exists(Path.Combine(ResolverDependencyTests.RepositoryRoot(), item.Split(['#', ' '], StringSplitOptions.RemoveEmptyEntries)[0])),
                $"Stale existing test evidence: {item}");
        return evidence.ToArray();
    }

    private static string Operation(MethodInfo? handler)
    {
        if (GenericListBoundaryTests.ListContract(handler) is not null)
            return "Read (fixed generic list, including compiled forwarding wrappers)";
        if (handler?.DeclaringType == typeof(BaseResolvers))
            return handler.Name switch
            {
                "Read" or "Link" => "Read through service projection",
                "List" => "Read (fixed)",
                "ListForCreate" => "Create (fixed target picker)",
                "Update" => "Read preflight; Update DTO permission; service Update",
                "Delete" => "Read preflight; Delete DTO permission; service Delete",
                _ => "UNKNOWN: inspect generic handler"
            };
        if (handler?.DeclaringType == typeof(WebAppArchitecture.User.Resolvers.UserResolvers) && handler.Name == "Memberships")
            return "Read parent and Read memberships (fixed relation predicate)";
        return "UNKNOWN: service-owned/custom handler; no operation inferred from route or method name";
    }

    private static IEnumerable<Coverage> DeclaredCoverage()
    {
        var core = typeof(CoreResolverBoundaryTests);
        foreach (var entity in new[] { "User", "Group", "Role", "Membership" })
        {
            var prefix = "/architecture/" + entity + "Response";
            yield return new("GET", prefix + "/list", core, nameof(CoreResolverBoundaryTests.ListsUseDesignatedServiceAndFixedReadOperation));
            yield return new("GET", prefix + "/list", core, nameof(CoreResolverBoundaryTests.CallerCannotChooseListOperationAndRejectedInputCallsNoService));
            yield return new("GET", prefix + "/view/{id:guid}", core, nameof(CoreResolverBoundaryTests.ReadAndLinkUseOnlyTheirDesignatedService));
            yield return new("GET", prefix + "/edit/{id:guid}", core, nameof(CoreResolverBoundaryTests.ReadAndLinkUseOnlyTheirDesignatedService));
            yield return new("GET", prefix + "/link/{id:guid}", core, nameof(CoreResolverBoundaryTests.ReadAndLinkUseOnlyTheirDesignatedService));
            yield return new("GET", prefix + "/view/{id:guid}", core, nameof(CoreResolverBoundaryTests.ServiceExceptionsNeverBecomeSuccessfulResponses));
            yield return new("DELETE", prefix + "/delete", core, nameof(CoreResolverBoundaryTests.MissingOrUnreadableObjectStopsDeleteBeforeAuthorizationOrMutation));
            yield return new("DELETE", prefix + "/delete", core, nameof(CoreResolverBoundaryTests.DeleteUsesFixedOperationAndStopsMutationWhenDenied));
            yield return new("PUT", prefix + "/update", core, nameof(CoreResolverBoundaryTests.UpdatePreservesTokenAndTrustedActorDespiteHostileInput));
            yield return new("POST", prefix + "/create-contained", core, nameof(CoreResolverBoundaryTests.CreateContainedDelegatesTypedInputAndNeverTurnsDenialIntoSuccess));
        }
        yield return new("GET", "/architecture/GroupResponse/create-targets", core, nameof(CoreResolverBoundaryTests.CreateTargetsRemainFixedToCreate));
        yield return new("PUT", "/architecture/UserResponse/update", core, nameof(CoreResolverBoundaryTests.UpdatePreservesTokenAndTrustedActorDespiteHostileInput));
        yield return new("PUT", "/architecture/UserResponse/update", core, nameof(CoreResolverBoundaryTests.MissingActorOrTokenStopsUpdateBeforeService));
        yield return new("DELETE", "/architecture/UserResponse/delete", core, nameof(CoreResolverBoundaryTests.DeleteUsesFixedOperationAndStopsMutationWhenDenied));
        yield return new("GET", "/architecture/UserResponse/memberships/{id:guid}", core, nameof(CoreResolverBoundaryTests.CustomMembershipVectorBindsParentPredicateAndReadOperation));
        yield return new("POST", "/architecture/UserResponse/vector", core, nameof(CoreResolverBoundaryTests.StructuredVectorUsesFixedVectorOperationAndRequiresActor));
        var digital = typeof(DigitalProcessResolverBoundaryTests);
        yield return new("POST", "/digital-processes/FormDefinitionResponse/create-typed", digital, nameof(DigitalProcessResolverBoundaryTests.CreateUsesTypedServiceAndIgnoresActorAndScopeOverrides));
        yield return new("POST", "/digital-processes/FormDefinitionResponse/create-typed", digital, nameof(DigitalProcessResolverBoundaryTests.ApiCreateNeverTurnsServiceFailureIntoSuccess));
        yield return new("POST", "/digital-processes/FormDefinitionResponse/create-form", digital, nameof(DigitalProcessResolverBoundaryTests.FormCreateDenialIs403AndDoesNotRedirect));
        yield return new("GET", "/digital-processes/FormDefinitionResponse/create-permission", digital, nameof(DigitalProcessResolverBoundaryTests.CreatePermissionDelegatesOnlyToPermissionServiceMethod));
        yield return new("POST", "/digital-processes/FormDefinitionResponse/new-version/{id:guid}", digital, nameof(DigitalProcessResolverBoundaryTests.NewVersionUsesVersionServiceAndTrustedParentId));
        foreach (var route in new[] { "/start", "/transition/{id:guid}" })
        {
            yield return new("POST", "/digital-processes/RequestResponse" + route, digital, nameof(DigitalProcessResolverBoundaryTests.RuntimeCommandsUseRequestServiceAndCannotOverrideInitiator));
            yield return new("POST", "/digital-processes/RequestResponse" + route, digital, nameof(DigitalProcessResolverBoundaryTests.DeniedRuntimeCommandNeverReturnsSuccessOrInvokesFollowUpServices));
        }
        yield return new("POST", "/digital-processes/ai/form-design", digital, nameof(DigitalProcessResolverBoundaryTests.AuxiliaryCustomResolverPreservesServiceOutcomeStatus));
        foreach (var route in new[] { "/events/EventResponse/list", "/events/EventTypeResponse/list",
            "/events/EventResponse/view/{id:guid}", "/events/EventResponse/children/{id:guid}" })
            yield return new("GET", route, typeof(EventsResolverBoundaryTests), nameof(EventsResolverBoundaryTests.AnonymousRequestsStopBeforeConcreteServiceTouchesItsDependencies));
    }
}
