using MediatR;

namespace Application.Features.Users.Commands.ActivateUser;

public record ActivateUserCommand(
    Guid UserId
    ) : IRequest<Unit>;
