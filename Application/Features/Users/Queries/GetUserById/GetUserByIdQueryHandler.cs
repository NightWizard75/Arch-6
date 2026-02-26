using Application.Shared.DTOs;
using Application.Shared.Exceptions;
using AutoMapper;
using Domain.Contracts;
using MediatR;

namespace Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler(IUserRepository userRepository, IMapper mapper)
    : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken)
                   ?? throw new EntityNotFoundException("User", request.Id);
        
        return mapper.Map<UserDto>(user);
    }
}
