using Application;
using FluentValidation;
using Infrastructure;
using Infrastructure.Database.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Web.Middleware;
using Web.Validation.Filters;
using Web.Validation.Validators;

var builder = WebApplication.CreateBuilder(args);

// ✅ Регистрация слоёв
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// ✅ Контроллеры + валидация
builder.Services.AddControllers(options =>
    {
        // ✅ Отключаем автоматическое добавление [Required] для non-nullable типов
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        
        // ✅ Глобальный асинхронный фильтр валидации
        options.Filters.Add<ValidationResponseFilter>();
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver =
            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        options.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore;

        // Продолжаем десериализацию после ошибок
        options.SerializerSettings.Error = (sender, args) =>
        {
            args.ErrorContext.Handled = true;
        };
    });

// ✅ Настройка валидации, (опционально, если не запущен FluentValidator)
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        // Этот обработчик теперь сработает только для sync-валидации (DataAnnotations),
        // если её оставить включённой
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value!.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToList()
                );
        
            return ValidationResponseFormat.FromModelStateErrors(errors);
        };
    });

// ✅ Регистрация валидаторов
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// ✅ Глобальная обработка исключений
builder.Services.AddProblemDetails(); 
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<ExceptionHandler>();

// ✅ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration);
});

var app = builder.Build();

// ✅ Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// ✅ Обработчики исключений (порядок важен! Обработчик исключений до контроллера)
app.UseExceptionHandler();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate(); // ✅ Применяет миграции
}

app.Run();
