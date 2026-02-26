using Application.Features.Users.Commands.ActivateUser;
using Application.Features.Users.Queries.GetAllUsers;
using Application.Features.Users.Queries.GetUserById;
using Application.Shared.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.Requests.User;

namespace Web.Controllers;

[ApiController] 
[Route("api/[controller]")] 
public class UsersController(IMediator mediator) : ControllerBase
{
    // GET /api/users
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers(
        CancellationToken cancellationToken)
    {
        var query = new GetAllUsersQuery();
        var users = await mediator.Send(query, cancellationToken);
        
        return Ok(users);
    }

    // GET /api/users/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserById(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var user = await mediator.Send(query, cancellationToken);
        
        return Ok(user);
    }

    // POST /api/users
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await mediator.Send(request.ToCommand(), cancellationToken);
        
        // ✅ Возвращаем 201 Created с Location header
        return CreatedAtAction(nameof(GetUserById), new { id = userId }, userId);
    }

    // PATCH /api/users/{id}/activate
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ActivateUserCommand(id);
        await mediator.Send(command, cancellationToken);
        
        // ✅ 204 No Content — операция выполнена, тело ответа не нужно
        return NoContent();
    }
}
