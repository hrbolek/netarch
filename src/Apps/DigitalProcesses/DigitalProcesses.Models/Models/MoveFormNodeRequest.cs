namespace WebAppArchitecture.DigitalProcesses.Models;

public sealed record MoveFormNodeRequest(Guid? parent_id, int position = 0);
