using AutoMapper;

namespace UnitTests.Application.UnitTests.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandlerTests
{
    private readonly IUserRepository _userRepositoryMock;
    private readonly IMapper _mapperMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepositoryMock = Substitute.For<IUserRepository>();
        _mapperMock = Substitute.For<IMapper>();
        _handler = new CreateUserCommandHandler(_userRepositoryMock, _mapperMock);
    }

    [Fact]
    public async Task Handle_WhenEmailExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new CreateUserCommand(
            "test@example.com",
            "John",
            "Doe",
            new DateTime(1990, 1, 1));
        
        _userRepositoryMock
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"User with email {command.Email} already exists.");
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_AddsUserAndReturnsId()
    {
        // Arrange
        var command = new CreateUserCommand(
            "test@example.com",
            "John",
            "Doe",
            new DateTime(1990, 1, 1));
        
        var user = new User(command.Email, command.FirstName, command.LastName, command.DateOfBirth);
        
        _userRepositoryMock
            .ExistsByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(false);
        
        _mapperMock.Map<User>(command).Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(user.Id);
        await _userRepositoryMock.Received(1).AddAsync(user, Arg.Any<CancellationToken>());
    }
}
