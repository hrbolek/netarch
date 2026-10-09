namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record UpdateFormNodeRequest(
    string key,
    string? label = null,
    string? description = null,
    string? configuration_json = null,
    int min_occurs = 1,
    int max_occurs = 1);
