using Application.Shared.Mappings;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Application.UnitTests.Shared.Mappings;

public class UserProfileTests
{
    private readonly IMapper _mapper;

    public UserProfileTests()
    {
        var configurationExpression = new MapperConfigurationExpression();
        configurationExpression.AddProfile<UserProfile>();
        
        var configuration = new MapperConfiguration(
            configurationExpression, 
            NullLoggerFactory.Instance);
        
        configuration.AssertConfigurationIsValid();
        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void UserProfile_Configuration_ShouldBeValid()
    {
        _mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public void User_To_UserDto_Mapping_ShouldWork()
    {
        var user = new User("test@example.com", "John", "Doe", new DateTime(1990, 1, 1));
        var dto = _mapper.Map<UserDto>(user);
        
        dto.Should().NotBeNull();
        dto.Email.Should().Be(user.Email);
        dto.FirstName.Should().Be(user.FirstName);
        dto.LastName.Should().Be(user.LastName);
    }

    [Fact]
    public void CreateUserCommand_To_User_Mapping_ShouldWork()
    {
        var command = new CreateUserCommand(
            "test@example.com",
            "John",
            "Doe",
            new DateTime(1990, 1, 1));
        
        var user = _mapper.Map<User>(command);
        
        user.Should().NotBeNull();
        user.Email.Should().Be(command.Email);
        user.FirstName.Should().Be(command.FirstName);
        user.LastName.Should().Be(command.LastName);
    }
}
