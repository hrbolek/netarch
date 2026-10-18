using WebAppArchitecture.DigitalProcesses.Models.Shared;
using System.Text.Json.Nodes;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNodeType;

public interface IFormComponentCatalogService
{
    AuxiliaryOperationResult GetCatalog();
}
