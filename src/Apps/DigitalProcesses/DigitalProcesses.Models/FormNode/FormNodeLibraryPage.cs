namespace WebAppArchitecture.DigitalProcesses.Models.FormNode;
public sealed record FormNodeLibraryPage(IReadOnlyList<FormNodeLibraryEntry> items, int total, int offset, int limit);
