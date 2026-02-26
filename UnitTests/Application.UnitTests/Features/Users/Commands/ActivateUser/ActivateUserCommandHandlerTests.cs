namespace UnitTests.Application.UnitTests.Features.Users.Commands.ActivateUser;

public class ActivateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepositoryMock;
    private readonly ActivateUserCommandHandler _handler;

    public ActivateUserCommandHandlerTests()
    {
        _userRepositoryMock = Substitute.For<IUserRepository>();
        _handler = new ActivateUserCommandHandler(_userRepositoryMock);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var command = new ActivateUserCommand(Guid.NewGuid());
        
        _userRepositoryMock
            .GetByIdAsync(command.UserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage($"Entity \"User\" with key ({command.UserId}) was not found.");
    }

    [Fact]
    public async Task Handle_WhenUserFound_ActivatesAndUpdatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateUserCommand(userId);
        
        var user = new User("test@example.com", "John", "Doe", new DateTime(1990, 1, 1));
        
        _userRepositoryMock
            .GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.IsActive.Should().BeTrue();
        await _userRepositoryMock.Received(1).UpdateAsync(user, Arg.Any<CancellationToken>());
    }
}
