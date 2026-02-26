using Application.Shared.DTOs;
using AutoMapper;
using Domain.Contracts;
using MediatR;

namespace Application.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler(
    IUserRepository userRepository, 
    IMapper mapper
    ) : IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        
        return mapper.Map<IEnumerable<UserDto>>(users);
    }
}
