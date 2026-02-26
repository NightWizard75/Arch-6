using AutoMapper;

namespace UnitTests.Application.UnitTests.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandlerTests
{
    private readonly IUserRepository _userRepositoryMock;
    private readonly IMapper _mapperMock;
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _userRepositoryMock = Substitute.For<IUserRepository>();
        _mapperMock = Substitute.For<IMapper>();
        _handler = new GetUserByIdQueryHandler(_userRepositoryMock, _mapperMock);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);
        
        _userRepositoryMock
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Entity \"User\" with key ({userId}) was not found.");
    }

    [Fact]
    public async Task Handle_WhenUserFound_ReturnsUserDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var query = new GetUserByIdQuery(userId);
        
        var user = new User("test@example.com", "John", "Doe", new DateTime(1990, 1, 1));
        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.DateOfBirth,
            user.RegistrationDate,
            user.IsActive);
        
        _userRepositoryMock
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);
        
        _mapperMock.Map<UserDto>(user).Returns(userDto);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
    }
}
