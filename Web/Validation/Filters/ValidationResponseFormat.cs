using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Web.Validation.Filters;

public static class ValidationResponseFormat
{
    private const string DefaultMessage = "Указанные данные были неверными.";
    private const int DefaultStatusCode = StatusCodes.Status422UnprocessableEntity;

    /// <summary>
    /// Создает ответ с ошибками валидации из ошибок ModelState
    /// </summary>
    public static ObjectResult FromModelStateErrors(
        IDictionary<string, List<string>> errors,
        string? message = null,
        int? statusCode = null)
    {
        // Нормализуем имена полей: убираем "$.", "[FromBody] book.", "book." и т.д.
        var normalizedErrors = errors
            .Where(kvp => kvp.Value.Count > 0)
            .ToDictionary(
                kvp => NormalizePropertyName(kvp.Key),
                kvp => kvp.Value
            );

        var response = new
        {
            message = message ?? DefaultMessage,
            errors = normalizedErrors
        };

        return new ObjectResult(response)
        {
            StatusCode = statusCode ?? DefaultStatusCode
        };
    }

    /// <summary>
    /// Создает ответ с ошибками валидации из результатов FluentValidation
    /// </summary>
    public static ObjectResult FromValidationFailures(
        IEnumerable<ValidationFailure> failures,
        string? message = null,
        int? statusCode = null)
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToList()
            );

        return FromModelStateErrors(errors, message, statusCode);
    }

    /// <summary>
    /// Нормализует имя свойства для вывода в ошибке.
    /// Убирает префиксы вроде "$.", "book.", "[FromBody] book." и т.д.
    /// </summary>
    private static string NormalizePropertyName(string propertyName)
    {
        // Убираем путь в формате JSON: "$.publicationYear" → "publicationYear"
        if (propertyName.StartsWith("$."))
        {
            propertyName = propertyName.Substring(2);
        }

        // Убираем имя параметра действия: "book.Title" → "Title"
        // или "[FromBody] book.Title" → "Title"
        var lastDotIndex = propertyName.LastIndexOf('.');
        if (lastDotIndex > 0)
        {
            propertyName = propertyName.Substring(lastDotIndex + 1);
        }

        // Убираем квадратные скобки (если остались)
        propertyName = propertyName
            .Replace("[", "")
            .Replace("]", "")
            .Replace("FromBody", "")
            .Replace(" ", "")
            .Trim();

        return propertyName;
    }
}
