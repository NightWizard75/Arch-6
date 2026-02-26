# 🏗️ Полное руководство по Clean Architecture

## 📋 Содержание
1. [Задача 1: Настройка зависимостей между проектами](#задача-1-настройка-зависимостей-между-проектами)
2. [Задача 2: Доменная модель и репозиторий](#задача-2-доменная-модель-и-репозиторий)
3. [Задача 3: Команды и запросы с MediatR](#задача-3-команды-и-запросы-с-mediatr)
4. [Задача 4: Web-слой](#задача-4-web-слой)
5. [Задача 5: Модульные тесты](#задача-5-модульные-тесты)
6. [Задача 6: Настройка DI и конфигурации](#задача-6-настройка-di-и-конфигурации)

---

## 🎯 **Задача 1: Настройка зависимостей между проектами в Clean Architecture**

### **1. Application → Domain**
```bash
dotnet add Application/Application.csproj reference Domain/Domain.csproj
```

### **2. Infrastructure → Domain и Application**
```bash
dotnet add Infrastructure/Infrastructure.csproj reference Domain/Domain.csproj
dotnet add Infrastructure/Infrastructure.csproj reference Application/Application.csproj
```

### **3. Web → все остальные проекты**
```bash
dotnet add Web/Web.csproj reference Domain/Domain.csproj
dotnet add Web/Web.csproj reference Application/Application.csproj
dotnet add Web/Web.csproj reference Infrastructure/Infrastructure.csproj
```

---

## 🏛️ **Задача 2: Доменная модель и репозиторий**

Создание сущности `User` и интерфейса `IUserRepository` в проекте Domain согласно принципам DDD и Clean Architecture.

### 📦 **Сущность User**
**📄 Domain/Entities/User.cs**

Используется ✅ **Rich Model (DDD Pattern)**
Поведение инкапсулировано внутри сущности. Сущность сама защищает свою целостность.

### 🔄 **Как именно происходит взаимодействие?**

```
┌─────────────────────────────────────────┐
│ 1. Application Layer (Use Case)         │
│ • Получает команду от Web               │
│ • Загружает агрегат (User)              │
│ • Вызывает domain-метод                 │
│ • Сохраняет изменения                   │
└────────────────┬────────────────────────┘
                 │ Вызов: user.UpdateEmail(...)
                 ▼
┌─────────────────────────────────────────┐
│ 2. Domain Layer (Entity)                │
│ • Проверяет инварианты                  │
│ • Изменяет приватное состояние          │
│ • Генерирует Domain Event (опц.)        │
│ • Возвращает управление                 │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│ 3. Infrastructure Layer (Persistence)   │
│ • Видит изменения в агрегате            │
│ • Сохраняет в БД                        │
│ • Публикует Domain Events (если есть)   │
└─────────────────────────────────────────┘
```

### 📋 **Шпаргалка: Что где должно быть?**

| Операция | Где должна быть | Почему |
|-----------------------------------------------------------|-------------------------------------------|----------------------------------------------------|
| Проверка email != null | Domain (конструктор) | Техническая защита, всегда актуальна |
| Проверка «email уникален» | Application (перед вызовом домена) | Требует внешнего запроса к БД, не знание сущности |
| Изменение email с валидацией | Domain (UpdateEmail()) | Инкапсуляция правила: «как правильно менять email» |
| Решение «можно ли менять email этому пользователю сейчас» | Application | Контекстное бизнес-правило, зависит от сценария |
| Отправка письма при смене email | Infrastructure (подписка на Domain Event) | Техническая деталь, не должна быть в ядре |
| Логирование изменения | Infrastructure (через Domain Event) | Cross-cutting concern |

### **UserProfile** - Это конфигурация маппинга между типами для AutoMapper (сущности ↔ DTO ↔ Commands).

### 📊 **Как это работает (магия AutoMapper)**

#### **1️⃣ Регистрация в DependencyInjection.cs**
**📄 Application/DependencyInjection.cs**

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly());
        });
        // ✅ Эта строка автоматически находит все Profile-классы
        services.AddAutoMapper(config => { }, Assembly.GetExecutingAssembly());
        // ^^^^^^^^ ^^^^^^^^^^^^^^^^^^^^^^^^^^
        // конфигурация сборка для сканирования
        return services;
    }
}
```

#### **2️⃣ Что делает AddAutoMapper с Assembly?**

| AutoMapper сканирует сборку Application |
|-----------------------------------------|
|                                         |
| Ищет все классы, которые наследуют:    |
| Profile (из AutoMapper)                 |
|                                         |
| Находит:                                |
| ✅ UserProfile : Profile                |
| ✅ OrderProfile : Profile               |
| ✅ ProductProfile : Profile             |
|                                         |
| Автоматически регистрирует при:        |
| - Старте приложения (DI контейнер)     |
| - Первом вызове IMapper.Map<,>()       |
| - Валидации конфигурации (опционально) |

#### **3️⃣ Когда используется?**

| Момент | Что происходит |
|--------------------------|-----------------------------------------------------------|
| Старт приложения | AutoMapper сканирует сборку → регистрирует Profile-классы |
| Первый вызов IMapper.Map | Создаётся оптимизированный маппер для типа |
| Runtime (каждый запрос) | Используется кэшированный маппер для преобразования |

### **📄 Что конфигурирует UserProfile.cs?**

**📄 Application/Shared/Mappings/UserProfile.cs**

```csharp
using AutoMapper;
using Domain.Entities;
using Application.Shared.DTOs;
using Application.Features.Users.Commands.CreateUser;

namespace Application.Shared.Mappings;

// ✅ Наследуется от Profile
public class UserProfile : Profile
{
    public UserProfile()
    {
        // ═══════════════════════════════════════════════════
        // 1. Entity → DTO (для ответов API / Query-результатов)
        // ═══════════════════════════════════════════════════
        CreateMap<User, UserDto>();

        // ═══════════════════════════════════════════════════
        // 2. Command → Entity (для создания сущности)
        // ═══════════════════════════════════════════════════
        CreateMap<CreateUserCommand, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}
```

### 🎯 **Детальный разбор методов CreateMap**

#### **🔹 CreateMap<User, UserDto>()**

| Направление | Когда используется | Пример |
|:--------------:|:-----------------------------:|:-----------------------:|
| User → UserDto | Query Handlers → API Response | GetUserByIdQueryHandler |

**📄 Features/Users/Queries/GetUserById/GetUserByIdQueryHandler.cs**

```csharp
public class GetUserByIdQueryHandler(
    IUserRepository userRepository,
    IMapper mapper) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("User", request.Id);

        // ✅ AutoMapper использует конфигурацию из UserProfile
        return mapper.Map<UserDto>(user);
        // ^^^^^^^^ ^^^^^^ ^^^^
        // результат источник
    }
}
```

**Что происходит "под капотом":**

```csharp
// ❌ Без AutoMapper (бойлерплейт)
return new UserDto(
    user.Id,
    user.Email,
    user.FirstName,
    user.LastName,
    user.DateOfBirth,
    user.RegistrationDate,
    user.IsActive);

// ✅ С AutoMapper (одна строка)
return mapper.Map<UserDto>(user);
```

#### **🔹 CreateMap<CreateUserCommand, User>()**

| Направление | Когда используется | Пример |
|:---------------:|:-------------------------------------:|:-------------------------:|
| Command → User | Command Handlers → Создание сущности | CreateUserCommandHandler |

**📄 Features/Users/Commands/CreateUser/CreateUserCommandHandler.cs**

```csharp
public class CreateUserCommandHandler(
    IUserRepository userRepository,
    IMapper mapper) : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new InvalidOperationException($"User with email {request.Email} already exists.");
        }

        // ✅ AutoMapper использует конфигурацию из UserProfile
        var user = mapper.Map<User>(request);
        // ^^^^^^ ^^^^^
        // результат источник (Command)

        await userRepository.AddAsync(user, cancellationToken);
        return user.Id;
    }
}
```

**Что происходит "под капотом":**

```csharp
// ❌ Без AutoMapper (бойлерплейт)
var user = new User(
    request.Email,
    request.FirstName,
    request.LastName,
    request.DateOfBirth);

// ✅ С AutoMapper (одна строка)
var user = _mapper.Map<User>(request);
```

### 🔧 **Методы конфигурации в UserProfile**

| Метод | Назначение | Пример |
|:--------------------------------------------------------------------------|:---------------------------|:-------------------------------------------------------------------------------------------------|
| CreateMap<Source, Dest>() | Базовый маппинг | CreateMap<User, UserDto>() |
| .ForMember(dest => dest.Prop, opt => opt.Ignore()) | Игнорировать свойство | .ForMember(dest => dest.Id, opt => opt.Ignore()) |
| .ForMember(dest => dest.Prop, opt => opt.MapFrom(src => src.Other)) | Маппинг с другого свойства | .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName)) |
| .ForMember(dest => dest.Prop, opt => opt.MapFrom(src => SomeMethod(src))) | Маппинг через метод | .ForMember(dest => dest.Age, opt => opt.MapFrom(src => CalculateAge(src.DateOfBirth))) |
| .BeforeMap((src, dest) => ...) | Логика перед маппингом | .BeforeMap((src, dest) => dest.CreatedAt = DateTime.UtcNow) |
| .AfterMap((src, dest) => ...) | Логика после маппинга | .AfterMap((src, dest) => _logger.LogInfo("Mapped {Id}", dest.Id)) |

### 📋 **Версия UserProfile с примерами**

**📄 Application/Shared/Mappings/UserProfile.cs**

```csharp
using AutoMapper;
using Domain.Entities;
using Application.Shared.DTOs;
using Application.Features.Users.Commands.CreateUser;

namespace Application.Shared.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // ═══════════════════════════════════════════════════
        // 1. Entity → DTO (для Query-результатов)
        // ═══════════════════════════════════════════════════
        CreateMap<User, UserDto>();

        // ═══════════════════════════════════════════════════
        // 2. Command → Entity (для создания)
        // ═══════════════════════════════════════════════════
        CreateMap<CreateUserCommand, User>()
            // Игнорируем свойства, которые генерируются в сущности
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());

        // ═══════════════════════════════════════════════════
        // 3. Пример с кастомным маппингом (если понадобится)
        // ═══════════════════════════════════════════════════
        // CreateMap<UpdateUserCommand, User>()
        // .ForMember(dest => dest.FullName, opt => opt.MapFrom(
        // src => $"{src.FirstName} {src.LastName}"));
    }
}
```

### ⚠️ **Частые ошибки и решения**

| Ошибка | Причина | Решение |
|----------------------------|------------------------------------------|-------------------------------------------------------------|
| Error mapping types | Свойства не совпадают по имени/типу | Добавить .ForMember(..., opt => opt.MapFrom(...)) |
| NullReferenceException | Источник null, а тип не nullable | Проверять null перед маппингом или использовать UserDto? |
| Ignored property has value | Свойство игнорируется, но имеет значение | Убедиться, что .Ignore() для всех генерируемых свойств |
| Configuration not loaded | Сборка не указана в AddAutoMapper | Добавить правильную сборку: Assembly.GetExecutingAssembly() |

### 🏆 **Best Practices для UserProfile**

| Практика | Пример |
|------------------------------------|---------------------------------------------------|
| Один Profile на сущность/фичу | UserProfile, OrderProfile, ProductProfile |
| Группировка по направлению | Комментарии: // Entity → DTO, // Command → Entity |
| Игнорирование генерируемых свойств | .ForMember(dest => dest.Id, opt => opt.Ignore()) |
| Валидация конфигурации в тестах | AssertConfigurationIsValid() |
| Избегать сложной логики в маппинге | Сложная логика → в handler, не в Profile |

### ✅ **Итого**

| Вопрос | Ответ |
|:------------------------------|-----------------------------------------------------------|
| Кто ссылается на UserProfile? | AutoMapper через AddAutoMapper + reflection |
| Когда используется? | При старте (регистрация) + runtime (каждый IMapper.Map) |
| Почему нет прямых ссылок? | Конвенция AutoMapper + reflection |
| Что будет без него? | IMapper.Map не сможет преобразовать типы → ошибка runtime |
| Можно ли удалить? | ❌ Нет — потеряете маппинг между типами |

---

## 🚀 **Задача 3: Команды и запросы с MediatR**

MediatR — библиотека для реализации паттерна Mediator / CQRS.

```csharp
var userId = await mediator.Send(request.ToCommand(), cancellationToken);
```

### 🔍 **Пошаговый разбор:**

| Шаг | Что происходит |
|-----|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 1 | request.ToCommand() — конвертирует DTO из контроллера (CreateUserRequest) в команду приложения (CreateUserCommand) |
| 2 | mediator.Send(command, ct) — MediatR ищет в DI контейнере обработчик для типа CreateUserCommand (т.е. класс, реализующий IRequestHandler<CreateUserCommand, Guid>) |
| 3 | Находит CreateUserCommandHandler и вызывает его метод Handle(command, ct) |
| 4 | Хендлер выполняет бизнес-логику: создаёт сущность User, сохраняет через репозиторий, возвращает Guid нового пользователя |
| 5 | Этот Guid возвращается в контроллер и далее клиенту как результат 201 Created |

### 🗺️ **Визуальная схема:**

```
Контроллер
    │
    ▼
CreateUserRequest (DTO для HTTP)
    │ .ToCommand()
    ▼
CreateUserCommand (команда для Application)
    │ mediator.Send()
    ▼
CreateUserCommandHandler (Application Layer)
    │ • Валидация бизнес-правил
    │ • Создание User-сущности
    │ • userRepository.AddAsync(user)
    │ • SaveChangesAsync()
    ▼
Guid (ID нового пользователя)
    │
    ▼
Контроллер → CreatedAtAction(..., userId) → 201 Created
```

---

## 🌐 **Задача 4: Web-слой**

### **Шаг 1: Структура проекта Web**

**📋 Web/**
```
├── Controllers/
│   └── UsersController.cs
├── Requests/
│   └── CreateUserRequest.cs
├── Validators/
│   └── CreateUserRequestValidator.cs
├── Middleware/
│   ├── ExceptionHandler.cs
│   └── ValidationExceptionHandler.cs
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── Web.csproj
```

### **📋 Шаг 2: Настройка пакетов Web.csproj**

#### **📦 CLI-команды:**

```bash
cd src/Web
dotnet add package MediatR --version 14.0.0
dotnet add package FluentValidation --version 12.1.1
dotnet add package FluentValidation.DependencyInjectionExtensions --version 12.1.1
```

### **📋 Шаг 3: Создание Request DTO**

#### **📄 Web/Requests/CreateUserRequest.cs**

### **📋 Шаг 4: Создание FluentValidation Validator**

#### **📄 Web/Validators/CreateUserRequestValidator.cs**

Стандартное использование FluentValidation не позволяет использовать асинхронные вызовы к базе данных. По-другому проверить уникальность email не получится. Делать синхронные запросы крайне не рекомендуется.

Поэтому необходимо создать фильтр `ValidationResponseFilter`, который будет обрабатывать и синхронные, и асинхронные запросы.

Но есть важная особенность. Если в реквесте:

```csharp
public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    DateTime DateOfBirth)
```

определить параметры как обязательные, то при разборе запроса с пустым `FirstName` или другими полями, которые определены в базе как обязательные, до `CreateUserRequestValidator` код не доходит.

Вот что происходит: в ответе появляется ошибка на английском языке:

`"The LastName field is required."`

Это стандартное сообщение ASP.NET Core на английском языке.

Если бы это был FluentValidation, сообщение было бы:

`"LastName обязателен для заполнения" (как указано в правиле).`

**Вывод:** Ошибка добавлена в ModelState до того, как сработал ValidationResponseFilter.

В Clean Architecture поток запроса выглядит так:

```
1. HTTP Request (JSON)
       │
       ▼
2. [Model Binding] ← 🔥 ВОТ ЗДЕСЬ!
   • ASP.NET Core парсит JSON
   • Видит, что в JSON нет поля "LastName" (есть только "LastName1")
   • Видит, что в C# record свойство LastName имеет тип string (не nullable)
   • Автоматически добавляет ошибку: "The LastName field is required."
   • Добавляет её в ModelState
       │
       ▼
3. [Filters Pipeline]
   • Запускается ValidationResponseFilter
       │
       ▼
4. [Фильтр проверяет ModelState]
   • Видит: ModelState.IsValid == false (из-за ошибки биндинга!)
   • Формирует ответ с тем, что есть в ModelState
```

Когда модель-биндер видит свойство типа `string` (без `?`), он считает его обязательным. Если поле отсутствует в JSON — он автоматически добавляет ошибку в ModelState.

Это поведение встроено в фреймворк и работает независимо от FluentValidation.

```csharp
public record CreateUserRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime DateOfBirth) // Можно оставить обязательным, потому что
                          // Model Binding добавляет автоматическую ошибку required только для non-nullable reference types (как string),
                          // но НЕ для value types (как DateTime, int, Guid).
```

### 🔍 **Почему так происходит?**

**Для string LastName (Reference Type):**

| Шаг | Что происходит |
|-----|-------------------------------------------------------------------------------------------|
| 1 | В JSON запросе нет поля LastName |
| 2 | Model Binding видит: свойство string (reference type) + включены Nullable Reference Types |
| 3 | Логика фреймворка: "Если string не nullable и значения нет → это ошибка" |
| 4 | Добавляет в ModelState: "The LastName field is required." |

**Для DateTime DateOfBirth (Value Type):**

| Шаг | Что происходит |
|-----|---------------------------------------------------------------------------------------|
| 1 | В JSON запросе нет поля dateOfBirth |
| 2 | Model Binding видит: свойство DateTime (struct, value type) |
| 3 | Логика фреймворка: "Value type не может быть null, просто присвою дефолтное значение" |
| 4 | Устанавливает DateOfBirth = default(DateTime) = 01.01.0001 00:00:00 |
| 5 | Ошибки не добавляет! (значение технически есть, просто "пустое") |

Видимо, поэтому использование FluentValidation более корректно, т.к. FluentValidation поймал бы это, потому что для DateTime метод `.NotEmpty()` проверяет:

```
// Внутренняя логика FluentValidation для DateTime:
public bool IsNotEmpty(DateTime value) => value != default(DateTime);
```

### **📋 Шаг 5: Middleware для обработки исключений**

#### **📄 Web/Middleware/ValidationExceptionHandler.cs**
#### **📄 Web/Middleware/ExceptionHandler.cs**

Добавил обработку различных вариантов отсутствия подключения к БД (ошибка 503 Service Unavailable).

### **📋 Шаг 6: Контроллер UsersController**

#### **📄 Web/Controllers/UsersController.cs**

### **📋 Шаг 7: Infrastructure — DbContext и Repository**

#### **📄 Infrastructure/Database/Context/ApplicationDbContext.cs**
#### **📄 Infrastructure/Database/Configurations/UserConfiguration.cs**

Это конфигурация маппинга сущности User на таблицу БД для EF Core.

### 📊 **Как это работает (магия EF Core)**

#### **1️⃣ Регистрация в ApplicationDbContext.cs**

**📄 Infrastructure/Database/Context/ApplicationDbContext.cs**

**📄 Infrastructure/Repositories/UserRepository.cs**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    // ✅ Эта строка автоматически находит все IEntityTypeConfiguration<>
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
}
```

#### **2️⃣ Что делает ApplyConfigurationsFromAssembly?**

Собирает правила (конфигурации) в память EF Core, чтобы он понял, как должны выглядеть таблицы.

| EF Core сканирует сборку Infrastructure |
|---------------------------------------------------------------|
| Ищет все классы, которые реализуют:                           |
| IEntityTypeConfiguration<T>                                   |
|                                                               |
| Находит:                                                      |
| ✅ UserConfiguration : IEntityTypeConfiguration<User>         |
| ✅ OrderConfiguration : IEntityTypeConfiguration<Order>       |
| ✅ ProductConfiguration : IEntityTypeConfiguration<Product>   |
|                                                               |
| Автоматически применяет конфигурацию при:                     |
| - Создании модели БД                                          |
| - Генерации миграций                                          |
| - Применении миграций                                         |

### **📋 Шаг 8: appsettings.json**

#### **📄 Web/appsettings.json**

💡 **Для использования User Secrets:**

```bash
cd src/Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=..."
```

### **📋 Шаг 9: Program.cs**

#### **📄 Web/Program.cs**

---

## 🧪 **Задача 5: Модульные тесты для MediatR handlers**

### **📁 Шаг 1: Структура тестового проекта**

```
tests/
└── Application.UnitTests/
    ├── Features/
    │   └── Users/
    │       ├── Commands/
    │       │   ├── CreateUser/
    │       │   │   └── CreateUserCommandHandlerTests.cs
    │       │   └── ActivateUser/
    │       │       └── ActivateUserCommandHandlerTests.cs
    │       └── Queries/
    │           ├── GetUserById/
    │           │   └── GetUserByIdQueryHandlerTests.cs
    │           └── GetAllUsers/
    │               └── GetAllUsersQueryHandlerTests.cs
    ├── Shared/
    │   └── Mappings/
    │       └── UserProfileTests.cs
    ├── Application.UnitTests.csproj
    └── GlobalUsings.cs
```

### **📋 Шаг 2: Настройка пакетов UnitTests.csproj**

#### **📦 CLI-команды:**

```bash
cd UnitTests
# Добавить пакеты
dotnet add package NSubstitute --version 5.3.0
dotnet add package FluentAssertions --version 7.0.0
dotnet add package Microsoft.NET.Test.Sdk --version 17.12.0
dotnet add package Microsoft.Extensions.Logging.Abstractions

# Добавить ссылки на проекты
dotnet add reference ../../src/Application/Application.csproj
dotnet add reference ../../src/Domain/Domain.csproj
```

### **📄 Шаг 3: Настроить GlobalUsings.cs**

#### **📄 UnitTests/GlobalUsings.cs**

### **📄 Шаг 4: Тесты**

#### **1. для CreateUserCommandHandler**
#### **2. для ActivateUserCommandHandler**
#### **3. для GetUserByIdQueryHandler**
#### **4. для GetAllUsersQueryHandler**
#### **5. для AutoMapper Profile**

---

## 🔧 **Задача 6: Настройка DI и конфигурации**

### **📁 Шаг 1: Extension methods для каждого слоя**

#### **📄 Infrastructure/DependencyInjection.cs**
#### **📄 Application/DependencyInjection.cs**

Для слоя Domain:

```
Domain/
├── Entities/
├── Interfaces/
└── ❌ DependencyInjection.cs ← НЕ ДОЛЖЕН существовать!
```

| Проблема | Объяснение |
|----------------------|---------------------------------------------|
| Вводит в заблуждение | Создаёт впечатление, что Domain требует DI |
| Нарушает чистоту | Domain должен быть независим от фреймворков |
| Бесполезный код | Метод ничего не делает |

Если очень хочется, то будет выглядеть так:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Domain;

public static class DependencyInjection
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        // ✅ Domain слой не регистрирует сервисы (только интерфейсы)
        // А что здесь регистрировать? Пустой метод?
        // Но метод нужен для консистентности и будущего расширения
        return services;
    }
}
```

### **🔐 Шаг 2: Добавить конфигурирование DI слоев в сборку**

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
```

### **🔐 Шаг 3: User Secrets для разработки**

#### **📦 Настройка User Secrets**

```bash
# 1. Перейти в Web-проект
cd src/Web

# 2. Инициализировать User Secrets (создаёт secrets.json)
dotnet user-secrets init

# 3. Добавить строку подключения
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=userdb;Username=postgres;Password=MySecretPassword123"

# 4. Просмотреть все секреты
dotnet user-secrets list

# 5. Удалить секрет
dotnet user-secrets remove "ConnectionStrings:DefaultConnection"

# 6. Очистить все секреты
dotnet user-secrets clear
```

### **📋 Шаг 3: Настройка EF Core миграций**

#### **📦 Установка EF Core Tools**

```bash
# Глобально (один раз)
dotnet tool install --global dotnet-ef

# Или локально в решение
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

#### **📄 Infrastructure.csproj для миграций**

```xml
<ItemGroup>
    <!-- ✅ EF Core пакеты -->
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
  </ItemGroup>
```

```bash
# (запускать из Web, где есть appsettings.json)
cd src/Web

# Создать миграцию
dotnet ef migrations add InitialCreate --project ../Infrastructure --output-dir Database/Migrations

# Удалить последнюю миграцию
dotnet ef migrations remove --project ../Infrastructure

# Применить миграции к БД
dotnet ef database update --project ../Infrastructure
```

```bash
cd src/Web
dotnet ef migrations add InitialCreate --project ../Infrastructure --output-dir Database/Migrations
```

### **📋 Шаг 4: Проверка и запуск**

```bash
# 1. Очистка и сборка всего решения
dotnet clean
dotnet restore
dotnet build

# 2. Запуск Web-проекта
cd src/Web
dotnet run

# 3. Открыть Swagger
# http://localhost:5043/swagger
# https://localhost:7224/swagger (HTTPS)
```
