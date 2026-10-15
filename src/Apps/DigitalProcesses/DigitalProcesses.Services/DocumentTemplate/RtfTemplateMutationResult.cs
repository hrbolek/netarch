using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate;

namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
public sealed record RtfTemplateMutationResult(bool Found, RtfTemplateInfo? Template, int? ErrorStatus = null, string? Error = null);
