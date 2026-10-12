using System.Reflection;
using WebAppArchitecture.Base.Data;

namespace WebAppArchitecture.Base.UI.Components;

/// <summary>Navigation display text only; stable history identity is always route app/entity/id.</summary>
public static class RecentEntityLabel
{
    public static string For(IEntityDTOModel model)
    {
        var type = model.GetType();
        foreach (var propertyName in new[] { "email", "Email", "name", "Name", "visual_label", "VisualLabel" })
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property?.PropertyType != typeof(string)) continue;
            var value = (property.GetValue(model) as string)?.Trim();
            if (string.IsNullOrWhiteSpace(value) || Guid.TryParse(value, out _)) continue;
            return value;
        }
        return model.id.ToString("N")[..8];
    }
}
