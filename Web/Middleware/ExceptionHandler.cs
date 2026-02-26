using System.Net.Sockets;
using Application.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Serilog;

namespace Web.Middleware;

public class ExceptionHandler(ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, 
            "Unhandled exception: {ExceptionType} - {Message} (Path: {RequestPath}, TraceId: {TraceId})",
            exception.GetType().Name,
            exception.Message,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server error",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        switch (exception)
        {
            // 🔹 404: Ресурс не найден (бизнес-логика)
            case EntityNotFoundException notFoundException:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Not found";
                problemDetails.Detail = notFoundException.Message;
                break;
                
            // 🔹 400: Ошибка бизнес-логики (не путать с ошибками БД!)
            case InvalidOperationException invalidOpException
                // Проверяем сообщение, чтобы не перехватить лишнего (только конкретные сообщения!)
                when invalidOpException.Message.Contains("already exists") || 
                     invalidOpException.Message.Contains("invalid"):
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Bad request";
                problemDetails.Detail = invalidOpException.Message;
                break;
            
            // 🔹 409: Конфликт данных (например, дубликат)
            case DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("23505") == true: // 23505 = unique_violation in Postgres
                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Conflict";
                problemDetails.Detail = "Запись с такими данными уже существует.";
                break;

            // 🔹 503: База данных недоступна (ПРОВЕРЯЕМ И ВНУТРЕННИЕ ИСКЛЮЧЕНИЯ!)
            case InvalidOperationException transientEx 
                when transientEx.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase) &&
                     (transientEx.InnerException is NpgsqlException || 
                      transientEx.InnerException is SocketException):
    
            // Вариант Б: Прямая NpgsqlException (если не через EF Core)
            case NpgsqlException npgsqlEx 
                when IsConnectionError(npgsqlEx):
    
            // Вариант В: DbUpdateException с NpgsqlException внутри
            case DbUpdateException dbEx2 
                when dbEx2.InnerException is NpgsqlException npgsql && IsConnectionError(npgsql):
    
            // Вариант Г: Прямая SocketException (сеть)
            case SocketException:
    
            // Вариант Д: IOException с признаками проблем сети
            case IOException ioEx 
                when ioEx.Message.Contains("unable to read", StringComparison.OrdinalIgnoreCase):
        
                httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Title = "Service Unavailable";
                problemDetails.Detail = "Сервис временно недоступен. Пожалуйста, попробуйте позже.";
                httpContext.Response.Headers.RetryAfter = "30";
                break;
                
            // 🔹 500: Всё остальное — непредвиденная ошибка
            default:
                // problemDetails уже настроен на 500 по умолчанию
                // ⚠️ Важно: не возвращать exception.Message в продакшене (утечка информации)
                problemDetails.Detail = "Произошла внутренняя ошибка сервера."; 
                break;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
    
    // 🔹 Вспомогательный метод для проверки ошибок подключения
    static bool IsConnectionError(NpgsqlException ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("failed to connect") ||
               msg.Contains("connection refused") ||
               msg.Contains("no connection could be made") ||
               msg.Contains("timeout") ||
               msg.Contains("host unreachable") ||
               ex.InnerException is SocketException;
    }
}
