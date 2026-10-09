using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

public static class FakeEntraSeedUsers
{
    public static readonly FakeEntraSeedUser JohnNewbie = new(
        Guid.Parse("97c80a87-8206-4845-98df-2af25ed33916"),
        "John Newbie",
        "john.newbie@world.com",
        "Rector");

    public static readonly FakeEntraSeedUser PlannerAdmin = new(
        Guid.Parse("b06d811c-6945-4c74-bbc7-0222537d2e13"),
        "Planner Admin",
        "planner.admin@world.com",
        "Administrator");

    public static readonly FakeEntraSeedUser Auditor = new(
        Guid.Parse("7c03699a-f3c2-48b5-854f-8302c47145c9"),
        "Auditor",
        "auditor@world.com",
        "Dean");

    public static IReadOnlyList<FakeEntraSeedUser> All { get; } =
    [
        JohnNewbie,
        PlannerAdmin,
        Auditor
    ];
}
