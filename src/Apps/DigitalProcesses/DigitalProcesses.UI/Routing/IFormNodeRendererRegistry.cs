using WebAppArchitecture.DigitalProcesses.DBLayer;
using Microsoft.AspNetCore.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.Routing;

public interface IFormNodeRendererRegistry
{
    void Register<TComponent>(string rendererKey) where TComponent : IComponent;
    Type? Resolve(string rendererKey);
}
