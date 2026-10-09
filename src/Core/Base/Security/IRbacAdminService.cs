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

public interface IRbacAdminService
{
    Task<RbacGroupRecord> CreateGroupAsync(
        CreateGroupRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<RbacMembershipRecord> AddMembershipAsync(
        AddMembershipRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<RbacRoleRecord> AssignRoleAsync(
        AssignRoleRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<RbacSecurityObjectRecord> CreateSecurityObjectAsync(
        CreateSecurityObjectRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task UpsertRoleTypeAsync(
        UpsertRoleTypeRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task UpsertRoleTypeOperationAsync(
        UpsertRoleTypeOperationRequest request,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
