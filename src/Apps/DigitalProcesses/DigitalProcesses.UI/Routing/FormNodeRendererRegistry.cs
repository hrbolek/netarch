using Microsoft.AspNetCore.Components;

namespace WebAppArchitecture.DigitalProcesses.UI.Routing;

public sealed class FormNodeRendererRegistry : IFormNodeRendererRegistry
{
    private readonly Dictionary<string,Type> renderers=new(StringComparer.OrdinalIgnoreCase);
    public void Register<TComponent>(string rendererKey) where TComponent:IComponent => renderers[rendererKey]=typeof(TComponent);
    public Type? Resolve(string rendererKey)=>renderers.TryGetValue(rendererKey,out var type)?type:null;
}
