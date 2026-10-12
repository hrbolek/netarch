using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.UI.Pages;

public sealed record DigitalProcessCreatePageModel(
    string Title,
    string Action,
    string? ListUrl,
    IReadOnlyCollection<DigitalProcessCreateField> Fields,
    string? Description = null,
    string SubmitLabel = "Create");
