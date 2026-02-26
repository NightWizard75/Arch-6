using Domain.Contracts;
using FluentValidation;
using Web.Requests.User;

namespace Web.Validation.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен для заполнения")
            .EmailAddress().WithMessage("Ошибочный формат email")
            .MaximumLength(255).WithMessage("Email может содержать не более 255 символов")
            .MustAsync(async (email, ct) => {
                var exists = await userRepository.ExistsByEmailAsync(email, ct);
                return !exists;
            }).WithMessage("Такое значение поля Email уже существует.");
        
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("FirstName обязателен для заполнения")
            .MaximumLength(100).WithMessage("FirstName может содержать не более 100 символов");
        
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("LastName обязателен для заполнения")
            .MaximumLength(100).WithMessage("LastName может содержать не более 100 символов");
        
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("DateOfBirth обязателен для заполнения")
            .LessThan(DateTime.UtcNow).WithMessage("Дата рождения не должна быть позднее, чем сегодня")
            .Must(date => date.AddYears(14) <= DateTime.UtcNow).WithMessage("Пользователь должен быть старше 14 лет");
    }
}
