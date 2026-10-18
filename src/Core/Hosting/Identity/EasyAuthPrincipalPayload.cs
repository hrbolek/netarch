namespace WebAppArchitecture.Base.Security;

internal sealed record EasyAuthPrincipalPayload(List<EasyAuthPrincipalClaim>? claims);
