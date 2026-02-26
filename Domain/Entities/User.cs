// Domain/Entities/User.cs

using Domain.Exceptions;

namespace Domain.Entities;

public class User : BaseEntity
{
    // 🔹 Protected parameterless constructor для EF Core
    protected User() { }

    // 🔹 Публичный конструктор для создания новых пользователей
    public User(string email, string firstName, string lastName, DateTime dateOfBirth)
    {
        Id = Guid.NewGuid();
        
        // ✅ Технические guard'ы: защита от ошибок программирования
        Email = email ?? throw new ArgumentNullException(nameof(email));
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        
        DateOfBirth = dateOfBirth;
        RegistrationDate = DateTime.UtcNow;
        IsActive = true;
    }

    // 🔹 Свойства с private set — изменение только через методы
    public Guid Id { get; protected set; }
    public string Email { get; protected set; } = string.Empty;
    public string FirstName { get; protected set; } = string.Empty;
    public string LastName { get; protected set; } = string.Empty;
    public DateTime DateOfBirth { get; protected set; }
    public DateTime RegistrationDate { get; protected set; }
    public bool IsActive { get; protected set; }

    // 🔹 Бизнес-методы: единственные точки входа для изменения состояния
    
    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
    }

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new UserRegistrationException("Email cannot be empty.");
        
        if (email == Email) return; // Нет изменений
        
        Email = email;
        // Domain Event: AddDomainEvent(new EmailChangedEvent(Id, email));
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    // 🔹 Явные методы для проверки инвариантов (вызываются по необходимости)
    
    /// <summary>
    /// Проверяет, что пользователь соответствует требованиям для регистрации.
    /// Вызывается в Application-слое перед сохранением нового пользователя.
    /// </summary>
    public void EnsureValidForRegistration()
    {
        if (DateOfBirth.AddYears(14) > DateTime.UtcNow)
            throw new UserRegistrationException("User must be at least 14 years old.");
    }

    /// <summary>
    /// Проверяет, что пользователь активен.
    /// Вызывается перед операциями, требующими активного аккаунта.
    /// </summary>
    public void EnsureActive()
    {
        if (!IsActive)
            throw new UserRegistrationException("User account is not active.");
    }
}
