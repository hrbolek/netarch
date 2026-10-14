namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record RtfFieldBindingRequest(string Placeholder, Guid FormNodeId);
public sealed record NewRtfRevisionRequest(string RtfBase64, RtfFieldBindingRequest[]? Bindings);
public sealed record RtfTemplateInfo(Guid Id, Guid FormVersionId, string Name, DateTimeOffset CreatedAt);
public sealed record RtfTemplateRevisionInfo(Guid id, int revision, string content_type, DateTimeOffset created_at);
