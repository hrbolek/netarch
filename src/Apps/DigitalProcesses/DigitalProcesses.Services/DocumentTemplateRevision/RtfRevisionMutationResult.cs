using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;

namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplateRevision;
public sealed record RtfRevisionMutationResult(bool Found, RtfTemplateRevisionInfo? Revision, string? Error = null);
