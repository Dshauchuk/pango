using Moq;
using Pango.Application.Common.Interfaces.Persistence;
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

public class DeletePasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_Deletes_Password_If_Found()
    {
        // Arrange
        var mockRepository = new Mock<IPasswordRepository>();
        var handler = new DeletePasswordCommandHandler(mockRepository.Object);
        var passwordId = Guid.NewGuid();
        var request = new DeletePasswordCommand(passwordId);
        var password = new Domain.Entities.PangoPassword { Id = passwordId };

        mockRepository.Setup(repo => repo.FindAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>()))
            .ReturnsAsync(password);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        mockRepository.Verify(repo => repo.DeleteAsync(password), Times.Once);
        Assert.False(result.IsError);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task Handle_Does_Not_Delete_Password_If_Not_Found()
    {
        // Arrange
        var mockRepository = new Mock<IPasswordRepository>();
        var handler = new DeletePasswordCommandHandler(mockRepository.Object);
        var passwordId = Guid.NewGuid();
        var request = new DeletePasswordCommand(passwordId);

        mockRepository.Setup(repo => repo.FindAsync(It.IsAny<Func<Domain.Entities.PangoPassword, bool>>()))
            .ReturnsAsync((Domain.Entities.PangoPassword)null);

        // Act
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert
        mockRepository.Verify(repo => repo.DeleteAsync(It.IsAny<Domain.Entities.PangoPassword>()), Times.Never);
        Assert.False(result.IsError);
        Assert.False(result.Value);
    }
}
