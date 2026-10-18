using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.Base.Security;

public interface ICurrentActorProvider
{
    Guid? TryGetActorUserId(HttpContext httpContext);
}
