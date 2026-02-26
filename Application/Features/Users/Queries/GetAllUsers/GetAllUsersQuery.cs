using Application.Shared.DTOs;
using MediatR;

namespace Application.Features.Users.Queries.GetAllUsers;

public record GetAllUsersQuery : IRequest<IEnumerable<UserDto>>;
