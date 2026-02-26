namespace Application.Shared.Responses;

public record ValidationErrorResponse
{
    public string Message { get; set; } = "Указанные данные были неверными.";
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
