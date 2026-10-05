using Microsoft.Extensions.DependencyInjection;
using WebAppArchitecture.DigitalProcesses.UI.Renderers;

namespace WebAppArchitecture.DigitalProcesses.UI.Routing;

public static class DigitalProcessRendererRegistration
{
    public static IServiceCollection AddDigitalProcessRenderers(this IServiceCollection services)
    {
        var registry = new FormNodeRendererRegistry();
        registry.Register<SectionRenderer>("section");
        registry.Register<TextInputRenderer>("input.text");
        registry.Register<NumberInputRenderer>("input.number");
        registry.Register<DateInputRenderer>("input.date");
        registry.Register<CheckboxRenderer>("input.checkbox");
        registry.Register<ActionButtonRenderer>("action.button");
        services.AddSingleton<IFormNodeRendererRegistry>(registry);
        return services;
    }
}
