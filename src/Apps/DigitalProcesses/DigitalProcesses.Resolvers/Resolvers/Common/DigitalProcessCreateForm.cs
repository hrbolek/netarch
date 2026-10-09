using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WebAppArchitecture.DigitalProcesses.UI.Pages;

namespace WebAppArchitecture.DigitalProcesses.Resolvers;

internal static class DigitalProcessCreateForm
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
    public static string Required(IFormCollection form,string name){var value=form[name].ToString().Trim(); if(string.IsNullOrWhiteSpace(value)) throw new BadHttpRequestException($"Field '{name}' is required."); return value;}
    public static string? Optional(IFormCollection form,string name){var value=form[name].ToString().Trim(); return string.IsNullOrWhiteSpace(value)?null:value;}
    public static Guid GuidValue(IFormCollection form,string name)=>Guid.TryParse(Required(form,name),out var value)?value:throw new BadHttpRequestException($"Field '{name}' must be a GUID.");
    public static Guid? OptionalGuid(IFormCollection form,string name){var raw=Optional(form,name); if(raw is null)return null; return Guid.TryParse(raw,out var value)?value:throw new BadHttpRequestException($"Field '{name}' must be a GUID.");}
    public static int IntValue(IFormCollection form,string name)=>int.TryParse(Required(form,name),out var value)?value:throw new BadHttpRequestException($"Field '{name}' must be an integer.");
    public static bool BoolValue(IFormCollection form,string name)=>string.Equals(form[name].ToString(),"true",StringComparison.OrdinalIgnoreCase)||string.Equals(form[name].ToString(),"on",StringComparison.OrdinalIgnoreCase);
}
