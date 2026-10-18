using System.Text;
using System.Text.Json;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WebAppArchitecture.Base.Security;

public sealed record FakeEntraSeedUser(
    Guid id,
    string Name,
    string Email,
    string RootRoleType
);
