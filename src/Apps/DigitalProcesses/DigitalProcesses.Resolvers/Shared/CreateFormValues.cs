using Microsoft.AspNetCore.Http;

namespace WebAppArchitecture.DigitalProcesses.Resolvers.Shared;

internal static class CreateFormValues
{
    public static string Required(IFormCollection form,string name){var value=form[name].ToString().Trim(); if(string.IsNullOrWhiteSpace(value)) throw new BadHttpRequestException($"Field '{name}' is required."); return value;}
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static string? Optional(IFormCollection form,string name){var value=form[name].ToString().Trim(); return string.IsNullOrWhiteSpace(value)?null:value;}
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static Guid GuidValue(IFormCollection form,string name)=>Guid.TryParse(Required(form,name),out var value)?value:throw new BadHttpRequestException($"Field '{name}' must be a GUID.");
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static Guid? OptionalGuid(IFormCollection form,string name){var raw=Optional(form,name); if(raw is null)return null; return Guid.TryParse(raw,out var value)?value:throw new BadHttpRequestException($"Field '{name}' must be a GUID.");}
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static int IntValue(IFormCollection form,string name)=>int.TryParse(Required(form,name),out var value)?value:throw new BadHttpRequestException($"Field '{name}' must be an integer.");
    /// <summary>
    /// Realizuje uvedenou operaci vrstvy; její vstupy a vedlejší účinky vyplývají z volaných služeb a navazujícího kontraktu.
    /// </summary>
    public static bool BoolValue(IFormCollection form,string name)=>string.Equals(form[name].ToString(),"true",StringComparison.OrdinalIgnoreCase)||string.Equals(form[name].ToString(),"on",StringComparison.OrdinalIgnoreCase);
}
