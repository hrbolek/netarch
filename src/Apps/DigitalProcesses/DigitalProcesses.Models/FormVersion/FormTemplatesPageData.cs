using System.Text.Json;

namespace WebAppArchitecture.DigitalProcesses.Models.FormVersion;
public sealed record FormTemplatesPageData(FormVersionResponse Model, bool MayEdit);
