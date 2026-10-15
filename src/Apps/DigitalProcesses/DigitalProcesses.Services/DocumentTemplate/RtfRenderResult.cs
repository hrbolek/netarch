
namespace WebAppArchitecture.DigitalProcesses.Services.DocumentTemplate;
public sealed record RtfRenderResult(bool Found, byte[]? Content = null, string? Error = null);
