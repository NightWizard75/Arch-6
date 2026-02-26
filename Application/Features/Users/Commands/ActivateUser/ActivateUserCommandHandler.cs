using Application.Shared.Exceptions;
using Domain.Contracts;
using MediatR;

namespace Application.Features.Users.Commands.ActivateUser;

public class ActivateUserCommandHandler(IUserRepository userRepository)
    : IRequestHandler<ActivateUserCommand, Unit>
{
    public async Task<Unit> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new EntityNotFoundException("User", request.UserId);
        
        user.Activate();
        
        await userRepository.UpdateAsync(user, cancellationToken);

        return Unit.Value;
    }
}
