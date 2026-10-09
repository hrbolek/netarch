namespace WebAppArchitecture.Base.UI.Pages;

/// <summary>Reusable JSON model for an HTML-first entity creation form.</summary>
public sealed record CreatePageModel(string Entity, string? ParentId = null, string? ReturnUrl = null);
