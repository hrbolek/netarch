using WebAppArchitecture.DigitalProcesses.Models.DocumentTemplate;

namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
public sealed record RtfTemplateDownload(RtfTemplateInfo Template, byte[] Content);
