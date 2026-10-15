using WebAppArchitecture.DigitalProcesses.Resolvers.FormDefinition;
using WebAppArchitecture.DigitalProcesses.DBLayer;
using WebAppArchitecture.Base.Security;
using Xunit;

namespace DigitalProcesses.Integration.Tests;

public sealed class FormDefinitionCreationGuidanceTests
{
    [Fact]
    public void DeniedHtml_ExplainsRequiredOperationAndScope()
    {
        Assert.Contains("Permission required", FormCreateAuthorizationMessage.Html);
        Assert.Contains("Create Form Definitions", FormCreateAuthorizationMessage.Html);
        Assert.Contains(RbacWellKnown.FormDefinitionCreationScopeId.ToString("D"), FormCreateAuthorizationMessage.Html);
        Assert.Contains("Create", FormCreateAuthorizationMessage.Guidance);
        Assert.Contains("EffectiveAccess", FormCreateAuthorizationMessage.Guidance);
    }
}
