namespace WebAppArchitecture.DigitalProcesses.Models.Shared;
/// <summary>Transport-independent outcome of an auxiliary form operation.</summary>
public sealed record AuxiliaryOperationResult(
    int StatusCode, object? Value = null, bool IsProblem = false, string? Title = null, string? Detail = null)
{
    public static AuxiliaryOperationResult Ok(object value) => new(200, value);
    public static AuxiliaryOperationResult Json(object value) => new(200, value);
    public static AuxiliaryOperationResult NotFound() => new(404);
    public static AuxiliaryOperationResult BadRequest(object value) => new(400, value);
    public static AuxiliaryOperationResult Conflict(object value) => new(409, value);
    public static AuxiliaryOperationResult NoContent() => new(204);
    public static AuxiliaryOperationResult Problem(int statusCode, string title, string? detail = null)
        => new(statusCode, IsProblem: true, Title: title, Detail: detail);
}
