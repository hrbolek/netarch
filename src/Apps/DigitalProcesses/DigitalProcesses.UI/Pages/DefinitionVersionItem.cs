using WebAppArchitecture.DigitalProcesses.DBLayer;
namespace WebAppArchitecture.DigitalProcesses.UI.Pages;
public sealed record DefinitionVersionItem(Guid Id, int Number, bool Published, DateTimeOffset Updated);
