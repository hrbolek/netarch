using Microsoft.AspNetCore.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.FormNode.Renderers;

public interface IFormNodeRendererRegistry
{
    void Register<TComponent>(string rendererKey) where TComponent : IComponent;
    Type? Resolve(string rendererKey);
}
