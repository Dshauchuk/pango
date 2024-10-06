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