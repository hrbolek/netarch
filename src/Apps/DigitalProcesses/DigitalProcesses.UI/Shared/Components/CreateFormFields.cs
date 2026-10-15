namespace WebAppArchitecture.DigitalProcesses.UI.Shared.Components;

public static class CreateFormFields
{
    public static DigitalProcessCreatePageModel Page(string title,string action,string listUrl,string description,params DigitalProcessCreateField[] fields)
        => new(title, action, listUrl, fields, description);
    public static DigitalProcessCreateField Text(string name,string label,bool required=true,string? value=null,string? help=null)=>new(name,label,"text",required,help,value);
    public static DigitalProcessCreateField GuidField(string name,string label,bool required=true,string? help=null)=>new(name,label,"text",required,help??"GUID identifier.");
    public static DigitalProcessCreateField EntityField(string name, string label, IEnumerable<DigitalProcessSelectOption> options, bool required = true)
        => new(name, label, "entity-select", required, "Choose from entities you can read; use the filter to narrow the list.", Options: options.ToArray());
    public static DigitalProcessCreateField EntityLookup(string name, string label, string listUrl, string[] searchFields, bool required = true, string? parentField = null, string? parentProperty = null)
        => new(name, label, "entity-select", required, "Results are limited to 25; filtering starts at two characters.", LookupUrl: listUrl, SearchFields: searchFields, ParentField: parentField, ParentProperty: parentProperty);
    public static DigitalProcessCreateField TextArea(string name,string label,bool required=true)=>new(name,label,"textarea",required);
    public static DigitalProcessCreateField Number(string name,string label,string value,string? min=null,string? max=null)=>new(name,label,"number",true,null,value,false,min,max);
    public static DigitalProcessCreateField Check(string name,string label)=>new(name,label,"checkbox",false);
}
