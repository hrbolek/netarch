
namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplateRevision;
public sealed record RtfRevisionDownload(Guid TemplateId, int Revision, string ContentType, byte[] Content);
