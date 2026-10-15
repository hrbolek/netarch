namespace WebAppArchitecture.DigitalProcesses.Models.DocumentTemplateRevision;
public sealed record RtfTemplateRevisionInfo(Guid id, int revision, string content_type, DateTimeOffset created_at);
