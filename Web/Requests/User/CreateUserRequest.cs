using Application.Features.Users.Commands.CreateUser;

namespace Web.Requests.User;

public record CreateUserRequest(
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime DateOfBirth)
{
    public CreateUserCommand ToCommand() => new CreateUserCommand(
        Email ?? string.Empty,
        FirstName ?? string.Empty,
        LastName ?? string.Empty,
        DateOfBirth.Kind == DateTimeKind.Utc
            ? DateOfBirth
            : DateTime.SpecifyKind(DateOfBirth, DateTimeKind.Utc) // ✅ Гарантируем Utc;
    );
}
