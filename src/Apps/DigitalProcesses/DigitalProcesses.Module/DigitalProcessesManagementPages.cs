using WebAppArchitecture.DigitalProcesses.Resolvers.FormVersion;
using WebAppArchitecture.DigitalProcesses.Resolvers.ProcessDefinitionVersion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WebAppArchitecture.DigitalProcesses;

/// <summary>HTML presentation is implemented exclusively by Razor components.</summary>
public static class DigitalProcessesManagementPages
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/process-versions/{id:guid}/permissions", ProcessDefinitionVersionPermissionsResolvers.ApplicantMatrix)
            .WithTags("DigitalProcesses.Designer")
            .WithDescription("Razor-based Applicant access matrix. Draft process versions only allow editing when the current user has Update.");
        endpoints.MapGet("/FormVersionResponse/templates/{id:guid}", FormVersionTemplatesResolvers.TemplatePage)
            .WithTags("DigitalProcesses.Templates")
            .WithDescription("Razor-based RTF template management for a FormVersion.");
        endpoints.MapGet("/form-versions/{id:guid}/templates", FormVersionTemplatesResolvers.LegacyTemplatePage)
            .ExcludeFromDescription();
    }
}
