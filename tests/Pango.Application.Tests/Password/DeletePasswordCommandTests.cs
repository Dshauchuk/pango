using Pango.Application.UseCases.Password.Commands.DeletePassword;

namespace Pango.Application.Tests.Password;

public class DeletePasswordCommandTests
{
    [Fact]
    public void DeletePasswordCommand_Stores_PasswordId_Correctly()
    {
        // Arrange
        Guid passwordId = Guid.NewGuid();

        // Act
        var command = new DeletePasswordCommand(passwordId);

        // Assert
        Assert.Equal(passwordId, command.PasswordId);
    }
}
