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

public static class RbacEndpointResolvers
{
    public static IResult Health() => Results.Ok(new
    {
        status = "ok",
        root_group_id = RbacWellKnown.RootGroupId,
        root_security_object_id = RbacWellKnown.RootSecurityObjectId,
        bootstrap_admin_user_id = RbacWellKnown.BootstrapAdminUserId,
        actor_header = "X-Actor-UserId"
    });

    public static async Task<IResult> CreateGroup(CreateGroupRequest request,HttpContext httpContext,IRbacAdminService adminService,ICurrentActorProvider actorProvider,CancellationToken cancellationToken)
    {
        var actor=actorProvider.TryGetActorUserId(httpContext); if(actor is null)return Results.Unauthorized();
        try { return Results.Ok(await adminService.CreateGroupAsync(request,actor.Value,cancellationToken)); }
        catch(InvalidOperationException){ return Results.Forbid(); }
    }
    public static async Task<IResult> AddMembership(AddMembershipRequest request,HttpContext httpContext,IRbacAdminService adminService,ICurrentActorProvider actorProvider,CancellationToken cancellationToken)
    {
        var actor=actorProvider.TryGetActorUserId(httpContext); if(actor is null)return Results.Unauthorized();
        try { return Results.Ok(await adminService.AddMembershipAsync(request,actor.Value,cancellationToken)); }
        catch(InvalidOperationException){ return Results.Forbid(); }
    }
    public static async Task<IResult> AssignRole(AssignRoleRequest request,HttpContext httpContext,IRbacAdminService adminService,ICurrentActorProvider actorProvider,CancellationToken cancellationToken)
    {
        var actor=actorProvider.TryGetActorUserId(httpContext); if(actor is null)return Results.Unauthorized();
        try { return Results.Ok(await adminService.AssignRoleAsync(request,actor.Value,cancellationToken)); }
        catch(InvalidOperationException ex){ return Results.BadRequest(new { error=ex.Message }); }
    }
    public static async Task<IResult> CreateSecurityObject(CreateSecurityObjectRequest request,HttpContext httpContext,IRbacAdminService adminService,ICurrentActorProvider actorProvider,CancellationToken cancellationToken)
    {
        var actor=actorProvider.TryGetActorUserId(httpContext); if(actor is null)return Results.Unauthorized();
        try { return Results.Ok(await adminService.CreateSecurityObjectAsync(request,actor.Value,cancellationToken)); }
        catch(InvalidOperationException ex){ return Results.BadRequest(new { error=ex.Message }); }
    }
    public static async Task<IResult> UpsertRoleType(UpsertRoleTypeRequest request,HttpContext httpContext,IRbacAdminService adminService,ICurrentActorProvider actorProvider,CancellationToken cancellationToken)
    {
        var actor=actorProvider.TryGetActorUserId(httpContext); if(actor is null)return Results.Unauthorized();
        try { await adminService.UpsertRoleTypeAsync(request,actor.Value,cancellationToken); return Results.Ok(); }
        catch(InvalidOperationException ex){ return Results.BadRequest(new { error=ex.Message }); }
    }
    public static async Task<IResult> UpsertRoleTypeOperation(UpsertRoleTypeOperationRequest request,HttpContext httpContext,IRbacAdminService adminService,ICurrentActorProvider actorProvider,CancellationToken cancellationToken)
    {
        var actor=actorProvider.TryGetActorUserId(httpContext); if(actor is null)return Results.Unauthorized();
        try { await adminService.UpsertRoleTypeOperationAsync(request,actor.Value,cancellationToken); return Results.Ok(); }
        catch(InvalidOperationException ex){ return Results.BadRequest(new { error=ex.Message }); }
    }
    public static async Task<IResult> CanAccess(Guid userId,Guid rbacId,string operation,IRbacAuthorizationService authService,CancellationToken cancellationToken)
    {
        var allowed=await authService.IsAllowedAsync(userId,rbacId,operation,cancellationToken);
        return Results.Ok(new { user_id=userId, rbacobject_id=rbacId, operation, allowed });
    }
    public static async Task<IResult> Permissions(Guid rbacId,HttpContext httpContext,IRbacAuthorizationService authService,IRbacPermissionQueryService permissionQueryService,ICurrentActorProvider actorProvider,CancellationToken cancellationToken)
    {
        var actor=actorProvider.TryGetActorUserId(httpContext);
        if(actor is null)return Results.Content("<div class='alert alert-warning mb-0'>Unauthorized: actor identity missing.</div>","text/html; charset=utf-8");
        if(!await authService.IsAllowedAsync(actor.Value,rbacId,RbacOperations.Read,cancellationToken))return Results.Content("<div class='alert alert-warning mb-0'>You are not allowed to inspect permissions for this object.</div>","text/html; charset=utf-8");
        var permissions=await permissionQueryService.GetEffectivePermissionsAsync(rbacId,cancellationToken);
        if(permissions.Count==0)return Results.Content("<div class='text-muted'>No active permissions found.</div>","text/html; charset=utf-8");
        var builder=new StringBuilder(); builder.Append("<ul class='list-group list-group-flush'>");
        foreach(var permission in permissions)
        {
            var userLabel=!string.IsNullOrWhiteSpace(permission.user_name)?permission.user_name:permission.user_id.ToString();
            var groupLabel=!string.IsNullOrWhiteSpace(permission.group_name)?permission.group_name:permission.group_id.ToString();
            var validity=permission.valid_to is null?$"from {permission.valid_from:yyyy-MM-dd HH:mm}":$"from {permission.valid_from:yyyy-MM-dd HH:mm} to {permission.valid_to:yyyy-MM-dd HH:mm}";
            var operations=permission.operations.Count==0?"none":string.Join(", ",permission.operations);
            var userHref=$"/architecture/UserResponse/view/{permission.user_id}"; var groupHref=$"/architecture/GroupResponse/view/{permission.group_id}"; var roleHref=$"/architecture/RoleResponse/view/{permission.role_id}";
            builder.Append("<li class='list-group-item'>");
            builder.Append($"<div><a href='{WebUtility.HtmlEncode(userHref)}'><strong>{WebUtility.HtmlEncode(userLabel)}</strong></a> in role <a href='{WebUtility.HtmlEncode(roleHref)}'><code>{WebUtility.HtmlEncode(permission.role_type)}</code></a>{(permission.is_deputy?" <span class='badge text-bg-secondary'>deputy</span>":string.Empty)}</div>");
            builder.Append($"<div class='small text-muted'>Group: <a href='{WebUtility.HtmlEncode(groupHref)}'>{WebUtility.HtmlEncode(groupLabel)}</a> | Validity: {WebUtility.HtmlEncode(validity)}</div>");
            builder.Append($"<div class='small'>Operations: {WebUtility.HtmlEncode(operations)}</div>"); builder.Append("</li>");
        }
        builder.Append("</ul>"); return Results.Content(builder.ToString(),"text/html; charset=utf-8");
    }
}
