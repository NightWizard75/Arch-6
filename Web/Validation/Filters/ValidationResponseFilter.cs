using FluentValidation;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Web.Validation.Filters;

public class ValidationResponseFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        
        if (controllerActionDescriptor != null)
        {
            foreach (var parameter in controllerActionDescriptor.Parameters)
            {
                var parameterName = parameter.Name;
                var parameterType = parameter.ParameterType;
                
                // Пропускаем примитивы и CancellationToken
                if (IsPrimitiveType(parameterType) || parameterType == typeof(CancellationToken))
                    continue;
                
                var parameterValue = context.ActionArguments.TryGetValue(parameterName, out var value) 
                    ? value 
                    : null;
                
                // Резолвим валидатор через DI
                var validatorType = typeof(IValidator<>).MakeGenericType(parameterType);
                var validator = serviceProvider.GetService(validatorType) as IValidator;
                
                // ✅ ЗАПУСКАЕМ ВАЛИДАТОР ВСЕГДА, если он есть и значение не null
                if (validator != null && parameterValue != null)
                {
                    // Асинхронный вызов валидации
                    var validationResult = await validator.ValidateAsync(
                        new ValidationContext<object>(parameterValue), 
                        context.HttpContext.RequestAborted);
                    
                    // Добавляем ошибки FluentValidation в ModelState
                    foreach (var failure in validationResult.Errors)
                    {
                        var fieldName = ToCamelCase(failure.PropertyName);
                        
                        // Добавляем только если такой ошибки ещё нет (избегаем дубликатов)
                        if (!context.ModelState.ContainsKey(fieldName) || 
                            !context.ModelState[fieldName]!.Errors.Any(e => e.ErrorMessage == failure.ErrorMessage))
                        {
                            context.ModelState.AddModelError(fieldName, failure.ErrorMessage);
                        }
                    }
                }
            }
        }
        
        // ✅ Единая проверка: если есть ошибки (биндинг + валидация) — возвращаем ответ
        if (!context.ModelState.IsValid)
        {
            var errors = new Dictionary<string, List<string>>();
            
            foreach (var kvp in context.ModelState.Where(m => m.Value?.Errors.Count > 0))
            {
                var fieldName = NormalizeFieldName(kvp.Key, context);
                
                if (string.IsNullOrEmpty(fieldName))
                    continue;
                
                var errorMessages = kvp.Value!.Errors
                    .Select(e => FormatErrorMessage(fieldName, e))
                    .Distinct()
                    .ToList();
                
                if (errorMessages.Count > 0)
                    errors[fieldName] = errorMessages;
            }

            var response = ValidationResponseFormat.FromModelStateErrors(errors);
            context.Result = response;
            
            // var response = new ValidationErrorResponse
            // {
            //     Message = "Указанные данные были неверными.",
            //     Errors = errors.ToDictionary(
            //         kvp => kvp.Key,
            //         kvp => kvp.Value.ToArray()
            //         ) 
            // };

            // context.Result = new ObjectResult(response)
            // {
            //     StatusCode = StatusCodes.Status422UnprocessableEntity
            // };
            return; // Не продолжаем выполнение контроллера
        }
        
        // Валидация пройдена — выполняем контроллер
        await next();
    }

    private static string ToCamelCase(string? input)
    {
        if (string.IsNullOrEmpty(input) || !char.IsUpper(input[0]))
            return input ?? string.Empty;
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }

    private static string NormalizeFieldName(string? key, ActionExecutingContext context)
    {
        if (string.IsNullOrEmpty(key) || key == "$")
            return string.Empty;

        var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
        var parameterNames = controllerActionDescriptor?.Parameters
            .Select(p => p.Name)
            .ToList() ?? new List<string>();

        if (parameterNames.Contains(key, StringComparer.OrdinalIgnoreCase))
            return string.Empty;

        if (key.StartsWith("$.", StringComparison.OrdinalIgnoreCase))
            return key.Substring(2);

        foreach (var paramName in parameterNames)
        {
            var simplePrefix = $"{paramName}.";
            if (key.StartsWith(simplePrefix, StringComparison.OrdinalIgnoreCase))
                return key.Substring(simplePrefix.Length);
        }

        return key;
    }

    private bool IsPrimitiveType(Type type) => type.IsPrimitive || 
                                               type == typeof(string) || 
                                               type == typeof(decimal) || 
                                               type == typeof(DateTime) ||
                                               type == typeof(Guid) ||
                                               type.IsEnum;

    private static string FormatErrorMessage(string fieldName, ModelError error)
    {
        if (!string.IsNullOrEmpty(error.ErrorMessage))
        {
            var msg = error.ErrorMessage;
            
            if (msg.Contains("could not be converted", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("The input was not valid", StringComparison.OrdinalIgnoreCase))
            {
                return $"Неверный формат поля '{fieldName}'.";
            }
            
            if (msg.Contains("Path:") && msg.Contains("LineNumber:"))
            {
                return msg.Split("Path:")[0].Trim();
            }
            
            return msg;
        }
        
        return $"Неверный формат поля '{fieldName}'.";
    }
}
