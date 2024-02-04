using Moq;
using Pango.Application.Common.Interfaces.Persistence;
using Pango.Application.UseCases.Password.Commands.NewPassword;

namespace Pango.Application.Tests.Password;

public class NewPasswordCommandTests
{
    [Fact]
    public void NewPasswordCommand_Creation_Succeeds_With_Default_Properties()
    {
        // Arrange
        string name = "Password";
        string login = "user";
        string value = "encrypted_value";

        // Act
        var command = new NewPasswordCommand(name, login, value);

        // Assert
        Assert.Equal(name, command.Name);
        Assert.Equal(login, command.Login);
        Assert.Equal(value, command.Value);
        Assert.NotNull(command.Properties);
        Assert.Empty(command.Properties);
    }
}

public class NewPasswordCommandHandlerTests
{
    [Fact]
    public async Task NewPasswordCommandHandler_Creates_Password_Successfully()
    {
        // Arrange
        var mockRepository = new Mock<IPasswordRepository>();
        var handler = new NewPasswordCommandHandler(mockRepository.Object);
        var request = new NewPasswordCommand("Password", "user", "encrypted_value");

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<Domain.Entities.Password>()), Times.Once);
        Assert.False(result.IsError);
    }
}
