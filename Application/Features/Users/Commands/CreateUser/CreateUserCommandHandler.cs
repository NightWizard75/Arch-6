using AutoMapper;
using Domain.Contracts;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler(IUserRepository userRepository, IMapper mapper)
    : IRequestHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Проверка на уникальный Email
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            throw new InvalidOperationException($"User with email {request.Email} already exists.");
        }

        // 2. Маппинг Command → Entity (создание объекта)
        var user = mapper.Map<User>(request);
        
        // 3. 🔹 ПРОВЕРКА БИЗНЕС-ИНВАРИАНТА (Domain check) 🔹
        user.EnsureValidForRegistration();

        // 4. Сохранение
        await userRepository.AddAsync(user, cancellationToken);

        return user.Id;
    }
}
