using MediatR;

namespace Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    DateTime DateOfBirth
    ) : IRequest<Guid>;
