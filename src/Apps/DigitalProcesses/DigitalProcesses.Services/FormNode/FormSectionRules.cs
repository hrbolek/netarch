using WebAppArchitecture.DigitalProcesses.Models.FormNode;
using WebAppArchitecture.DigitalProcesses.Models.FormNodeType;

namespace WebAppArchitecture.DigitalProcesses.Services.FormNode;
internal static class FormSectionRules
{
    internal static void Validate(
        Guid? parentId, FormNodeTypeModel type, int maxOccurs, string? configuration,
        bool hasSectionChildren)
    {
        if (parentId is null && type.renderer_key != "section")
            throw new ArgumentException("Only sections can be added directly to the form root.");
        if (type.renderer_key != "section") return;
        var layout = FormSectionLayout.Read(configuration);
        if (layout == "table" && (parentId is null || maxOccurs <= 1 || hasSectionChildren))
            throw new ArgumentException("Table visualization requires a nested, repeated section without child sections.");
        if (layout == "tabs" && parentId is not null)
            throw new ArgumentException("Tabs are reserved for sections directly below the form root.");
    }

    internal static void ValidateParent(FormNodeModel? parent, FormNodeTypeModel childType)
    {
        if (parent is not null && childType.renderer_key == "section" &&
            FormSectionLayout.Read(parent.configuration_json) == "table")
            throw new ArgumentException("A table section cannot contain child sections. Change its visualization first.");
    }
}
