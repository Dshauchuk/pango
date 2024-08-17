using Pango.Application.UseCases.User.Commands.ChangePassword;
using Pango.Persistence.File;

namespace Pango.Application.Tests.Password;

public class ChangeUserPasswordCommandTests
{
    [Fact]
    public void DeletePasswordCommand_Stores_PasswordId_Correctly()
    {
        // Arrange
        Guid passwordId = Guid.NewGuid();

        //// Act
        //var command = new ChangePasswordCommandHandler(new UserFileStorageManager()

        //// Assert
        //Assert.Equal(passwordId, command.PasswordId);
    }
}
