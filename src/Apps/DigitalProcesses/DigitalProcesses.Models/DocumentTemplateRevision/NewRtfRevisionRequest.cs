using WebAppArchitecture.DigitalProcesses.Models.TemplateFieldBinding;
namespace WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;

public sealed record NewRtfRevisionRequest(string RtfBase64, RtfFieldBindingRequest[]? Bindings);
