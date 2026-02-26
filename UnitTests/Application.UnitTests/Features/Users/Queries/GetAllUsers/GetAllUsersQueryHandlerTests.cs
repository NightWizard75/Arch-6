using AutoMapper;

namespace UnitTests.Application.UnitTests.Features.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandlerTests
{
    private readonly IUserRepository _userRepositoryMock;
    private readonly IMapper _mapperMock;
    private readonly GetAllUsersQueryHandler _handler;

    public GetAllUsersQueryHandlerTests()
    {
        _userRepositoryMock = Substitute.For<IUserRepository>();
        _mapperMock = Substitute.For<IMapper>();
        _handler = new GetAllUsersQueryHandler(_userRepositoryMock, _mapperMock);
    }

    [Fact]
    public async Task Handle_ReturnsAllUsersAsDto()
    {
        // Arrange
        var users = new List<User>
        {
            new User("user1@example.com", "John", "Doe", new DateTime(1990, 1, 1)),
            new User("user2@example.com", "Jane", "Smith", new DateTime(1992, 5, 15))
        };
        
        var userDtos = users.Select(u => new UserDto(
            u.Id, u.Email, u.FirstName, u.LastName, u.DateOfBirth, u.RegistrationDate, u.IsActive)).ToList();
        
        _userRepositoryMock
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(users);
        
        _mapperMock.Map<IEnumerable<UserDto>>(users).Returns(userDtos);

        // Act
        var result = await _handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainEquivalentOf(userDtos[0]);
        result.Should().ContainEquivalentOf(userDtos[1]);
    }

    [Fact]
    public async Task Handle_WhenNoUsers_ReturnsEmptyCollection()
    {
        // Arrange
        var users = new List<User>();
        
        _userRepositoryMock
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(users);
        
        _mapperMock.Map<IEnumerable<UserDto>>(users).Returns(new List<UserDto>());

        // Act
        var result = await _handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
